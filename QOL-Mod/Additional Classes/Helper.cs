using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Steamworks;
using HarmonyLib;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace QOL
{
    public class Helper
    {
        // Returns the steamID of the specified spawnID
        public static CSteamID GetSteamID(ushort targetID) => ClientData[targetID].ClientID;

        // Returns the corresponding spawnID from the specified color
        public static ushort GetIDFromColor(string targetSpawnColor) => targetSpawnColor switch
        { 
            "yellow" or "y" => 0, 
            "blue" or "b" => 1,
            "red" or "r" => 2,
            "green" or "g" => 3,
                _ => ushort.MaxValue
        };

        // Returns the corresponding color from the specified spawnID
        public static string GetColorFromID(ushort x) => x switch { 1 => "Blue", 2 => "Red", 3 => "Green", _ => "Yellow" };

        // Returns the targeted player based on the specified spawnID
        public static NetworkPlayer GetNetworkPlayer(ushort targetID) => ClientData[targetID].PlayerObject.GetComponent<NetworkPlayer>();

        // Gets the steam profile name of the specified steamID
        public static string GetPlayerName(CSteamID passedClientID) => SteamFriends.GetFriendPersonaName(passedClientID);

        // Actually sticks the "join game" link together (url prefix + appID + LobbyID + SteamID)
        public static string GetJoinGameLink() => $"steam://joinlobby/674940/{lobbyID}/{localPlayerSteamID}";

        public static void ToggleLobbyVisibility(bool open)
        {
            if (MatchmakingHandler.Instance.IsHost)
            {
                var changeLobbyTypeMethod = typeof(MatchmakingHandler).GetMethod("ChangeLobbyType", 
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (open)
                {
                    changeLobbyTypeMethod!.Invoke(MatchmakingHandler.Instance,
                        new object[]
                        {
                            ELobbyType.k_ELobbyTypePublic
                        });
                    
                    SendChatMsg("Lobby made public!", ChatCommands.LogLevel.Success, true, ChatCommands.CmdOutputVisibility["public"]);
                }
                else
                {
                    changeLobbyTypeMethod!.Invoke(MatchmakingHandler.Instance,
                        new object[]
                        {
                            ELobbyType.k_ELobbyTypePrivate
                        });
                    
                    SendChatMsg("Lobby made private!", ChatCommands.LogLevel.Success, true, ChatCommands.CmdOutputVisibility["private"]);
                }

                return;
            }

            SendChatMsg("Need to be host!", ChatCommands.LogLevel.Warning, true, false);
        }

        // Assigns some commonly accessed values as well as runs anything that needs to be everytime a lobby is joined
        public static void InitValues(ChatManager __instance, ushort playerID)
        {
            if (playerID != GameManager.Instance.mMultiplayerManager.LocalPlayerIndex) return;
            
            foreach (var filePath in Directory.GetFiles(Plugin.MusicPath))
            {
                var acceptableFileExtension = filePath.Substring(filePath.Length - 4); // .OGG or .WAV, both are 4 char

                if (acceptableFileExtension is ".ogg" or ".wav")
                    __instance.StartCoroutine(ImportWav(filePath, CreateSongAndAddToMusic));
            }

            ClientData = GameManager.Instance.mMultiplayerManager.ConnectedClients;
            MutedPlayers.Clear();

            var localID = GameManager.Instance.mMultiplayerManager.LocalPlayerIndex;
            localNetworkPlayer = ClientData[localID].PlayerObject.GetComponent<NetworkPlayer>();
            LocalChat = ClientData[localID].PlayerObject.GetComponentInChildren<ChatManager>();

            Debug.Log("Assigned the localNetworkPlayer!: " + localNetworkPlayer.NetworkSpawnID);

            TMPText = Traverse.Create(__instance).Field("text").GetValue<TextMeshPro>();
            TMPText.richText = Plugin.ConfigRichText.Value;
            // Increase caret width so caret won't disappear at certain times
            Traverse.Create(__instance).Field("chatField").GetValue<TMP_InputField>().caretWidth = 3;

            if (Plugin.ConfigFixCrownWinCount.Value)
            {
                var counter = UnityEngine.Object.FindObjectOfType<WinCounterUI>();
                foreach (var crownCount in counter.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    crownCount.enableAutoSizing = true;
                    crownCount.GetComponentInChildren<Image>().color = Plugin.ConfigCustomCrownColor.Value;
                }
            }

            if (NameResize)
            {
                var playerNames = Traverse.Create(UnityEngine.Object.FindObjectOfType<OnlinePlayerUI>())
                    .Field("mPlayerTexts")
                    .GetValue<TextMeshProUGUI[]>();

                foreach (var name in playerNames)
                {
                    name.fontSizeMin = 45;
                    name.fontSizeMax = 45;
                    name.overflowMode = TextOverflowModes.Overflow;
                    name.enableWordWrapping = false;
                }
            }

            if (Plugin.ConfigCustomCrownColor.Value != (Color) Plugin.ConfigCustomCrownColor.DefaultValue)
            {
                var crown = UnityEngine.Object.FindObjectOfType<Crown>().gameObject;
                
                foreach (var sprite in crown.GetComponentsInChildren<SpriteRenderer>(true)) 
                    sprite.color = Plugin.ConfigCustomCrownColor.Value;
            }

            GameObject rbHand = new ("RainbowHandler");
            rbHand.AddComponent<RainbowManager>().enabled = false;
            RainbowEnabled = false;

            if (Plugin.ConfigAlwaysRainbow.Value) ToggleRainbow();
        }

        public static void ToggleWinstreak()
        {
            if (!WinStreakEnabled)
            {
                WinStreakEnabled = true;
                GameManager.Instance.winText.fontSize = Plugin.ConfigWinStreakFontsize.Value;
                return;
            }

            WinStreakEnabled = false;
        }

        public static void ToggleRainbow()
        {
            if (!RainbowEnabled)
            {
                Debug.Log("trying to start RainBowHandler");
                UnityEngine.Object.FindObjectOfType<RainbowManager>().enabled = true;
                RainbowEnabled = true;
                return;
            }

            UnityEngine.Object.FindObjectOfType<RainbowManager>().enabled = false;
            RainbowEnabled = false;
        }

        public static string GetTargetStatValue(CharacterStats stats, string targetStat)
        {
            foreach (var stat in typeof(CharacterStats).GetFields())
                if (stat.Name.ToLower() == targetStat)
                    return stat.GetValue(stats).ToString();

            return "No value";
        }

        public static void SendChatMsg(string msg, ChatCommands.LogLevel logLevel = default, bool toggleState = true, bool outputPublic = true)
        {
            if (logLevel != default && !AllOutputPublic && !outputPublic)
            {
                GameManager.Instance.StartCoroutine(SendClientSideMsg(logLevel, msg, toggleState));
                return;
            }

            localNetworkPlayer.OnTalked(msg);
        }

        private static IEnumerator SendClientSideMsg(ChatCommands.LogLevel logLevel, string msg, bool toggleState)
        {
            var msgColor = logLevel switch
            {
                ChatCommands.LogLevel.Warning => "<#FF7F50>",
                // Enabled => green, disabled => gray
                ChatCommands.LogLevel.Success => toggleState ? "<#006400>" : "<#56595c>",
                _ => ""
            };

            var origRichTextValue = TMPText.richText;

            var chatMsgDuration = 1.5f + msg.Length * 0.075f; // Time that chat msg will last till closing animation
            var extraTime = Plugin.ConfigMsgDuration.Value;
            if (extraTime > 0) chatMsgDuration += extraTime; // Taking into account any possible extra msg time specified in config

            TMPText.richText = true;
            LocalChat.Talk(msgColor + msg);

            // Add 3 grace seconds to original msg duration so rich text doesn't stop during closing animation
            yield return new WaitForSeconds(chatMsgDuration + 3f); 
            TMPText.richText = origRichTextValue;
        }

        // Adapted from: https://github.com/deadlyfingers/UnityWav#notes
        public static IEnumerator ImportWav(string url, Action<AudioClip> callback)
        {
            url = "file:///" + url.Replace(" ", "%20");
            Debug.Log("Loading song: " + url);

            using var www = UnityWebRequest.GetAudioClip(url, AudioType.UNKNOWN);
            yield return www.Send();

            if (www.isError)
            {
                Debug.LogWarning("Audio error:" + www.error);
                yield break;
            }

            var audioClip = DownloadHandlerAudioClip.GetContent(www);
            callback(audioClip);
        }

        public static void CreateSongAndAddToMusic(AudioClip audioClip)
            => MusicHandler.Instance.myMusic = MusicHandler.Instance.myMusic.AddToArray(new MusicClip { clip = audioClip });

        // Fancy bit-manipulation of a char's ASCII values to check whether it's a vowel or not
        public static bool IsVowel(char c) => (0x208222 >> (c & 0x1f) & 1) != 0;

        public static CSteamID lobbyID; // The ID of the current lobby
        
        public static readonly CSteamID localPlayerSteamID = SteamUser.GetSteamID(); // The steamID of the local user (ours)
        public static NetworkPlayer localNetworkPlayer; // The networkPlayer of the local user (ours)
        public static List<ushort> MutedPlayers = new(4);

        public static bool IsTranslating = Plugin.ConfigTranslation.Value; // True if auto-translations are enabled, false by default
        public static bool AutoGG = Plugin.ConfigAutoGG.Value; // True if auto gg on death is enabled, false by default
        public static bool UwuifyText; // True if uwufiy text is enabled, false by default
        public static bool WinStreakEnabled = Plugin.ConfigWinStreakLog.Value;
        public static bool ChatCensorshipBypass = Plugin.ConfigchatCensorshipBypass.Value; // True if chat censoring is bypassed, false by default
        public static readonly Color CustomPlayerColor = Plugin.ConfigCustomColor.Value;
        public static bool AllOutputPublic = Plugin.ConfigAllOutputPublic.Value;
        public static readonly bool IsCustomPlayerColor = Plugin.ConfigCustomColor.Value != new Color(1, 1, 1);
        public static readonly bool IsCustomName = !string.IsNullOrEmpty(Plugin.ConfigCustomName.Value);
        public static bool IsOwMode;
        public static bool IsSongLoop;
        public static readonly string[] OuchPhrases = Plugin.ConfigOuchPhrases.Value.Split(' ');
        private static readonly bool NameResize = Plugin.ConfigNoResize.Value;
        public static bool NukChat;
        public static bool IsTrustedKicker;
        public static bool OnlyLower;
        public static bool HPWinner = Plugin.ConfigHPWinner.Value;
        public static bool RainbowEnabled;
        public static IEnumerator RoutineUsed;

        public static ConnectedClientData[] ClientData;
        public static ChatManager LocalChat;

        public static TextMeshPro TMPText;
        public static int WinStreak = 0;

        public static HoardHandler[] Hoards = new HoardHandler[2];
    }
}
