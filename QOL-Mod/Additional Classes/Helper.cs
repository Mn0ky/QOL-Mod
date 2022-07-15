using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Steamworks;
using SimpleJSON;
using HarmonyLib;
using TMPro;
using BepInEx;
using UnityEngine.Networking;

namespace QOL
{
    public class Helper
    {
        // Returns the steamID of the specified spawnID
        public static CSteamID GetSteamID(ushort targetID) => clientData[targetID].ClientID;

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
        public static NetworkPlayer GetNetworkPlayer(ushort targetID) => clientData[targetID].PlayerObject.GetComponent<NetworkPlayer>();

        // Gets the steam profile name of the specified steamID
        public static string GetPlayerName(CSteamID passedClientID) => SteamFriends.GetFriendPersonaName(passedClientID);

        // Actually sticks the "join game" link together (url prefix + appID + LobbyID + SteamID)
        public static string GetJoinGameLink() => $"steam://joinlobby/674940/{lobbyID}/{localPlayerSteamID}";

        public static void ToggleLobbyVisibility(bool open)
        {
            if (MatchmakingHandler.Instance.IsHost)
            {
                MethodInfo ChangeLobbyTypeMethod = typeof(MatchmakingHandler).GetMethod("ChangeLobbyType", BindingFlags.NonPublic | BindingFlags.Instance);

                if (open)
                {
                    ChangeLobbyTypeMethod.Invoke(MatchmakingHandler.Instance, new object[] { ELobbyType.k_ELobbyTypePublic });
                    localNetworkPlayer.OnTalked("Lobby made public!");
                }

                else ChangeLobbyTypeMethod.Invoke(MatchmakingHandler.Instance, new object[] { ELobbyType.k_ELobbyTypePrivate });

                return;
            }
            
            localNetworkPlayer.OnTalked("Need to be host!");
        }

        // Assigns some commonly accessed values as well as runs anything that needs to be everytime a lobby is joined
        public static void InitValues(ChatManager __instance, ushort playerID)
        {
            if (playerID != GameManager.Instance.mMultiplayerManager.LocalPlayerIndex) return;
            
            foreach (var filePath in Directory.GetFiles(Plugin.MusicPath))
            {
                var acceptableFileExtension = filePath.Substring(filePath.Length - 4); // .OGG or .WAV, both are 4 char

                if (acceptableFileExtension == ".ogg" || acceptableFileExtension == ".wav")
                    __instance.StartCoroutine(ImportWav(filePath, audioClip => CreateSongAndAddToMusic(audioClip)));
            }

            clientData = GameManager.Instance.mMultiplayerManager.ConnectedClients;
            mutedPlayers.Clear();

            byte localID = GameManager.Instance.mMultiplayerManager.LocalPlayerIndex;
            localNetworkPlayer = clientData[localID].PlayerObject.GetComponent<NetworkPlayer>();
            localChat = clientData[localID].PlayerObject.GetComponentInChildren<ChatManager>();

            Debug.Log("Assigned the localNetworkPlayer!: " + localNetworkPlayer.NetworkSpawnID);

            tmpText = Traverse.Create(__instance).Field("text").GetValue<TextMeshPro>();
            tmpText.richText = Plugin.configRichText.Value;

            if (Plugin.configFixCrown.Value)
            {
                WinCounterUI counter = UnityEngine.Object.FindObjectOfType<WinCounterUI>();
                foreach (var crownCount in counter.GetComponentsInChildren<TextMeshProUGUI>(true)) crownCount.enableAutoSizing = true;
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
                }
            }

            if (Plugin.configCustomCrownColor.Value != (Color) Plugin.configCustomCrownColor.DefaultValue)
            {
                var crown = UnityEngine.Object.FindObjectOfType<Crown>().gameObject;
                foreach (var sprite in crown.GetComponentsInChildren<SpriteRenderer>(true)) sprite.color = Plugin.configCustomCrownColor.Value;
            }

            GameObject rbHand = new ("RainbowHandler");
            rbHand.AddComponent<RainbowManager>().enabled = false;
            rainbowEnabled = false;

            if (Plugin.configAlwaysRainbow.Value) ToggleRainbow();
        }

        public static void ToggleWinstreak()
        {
            if (!winStreakEnabled)
            {
                winStreakEnabled = true;
                GameManager.Instance.winText.fontSize = Plugin.configWinStreakFontsize.Value;
                return;
            }

            winStreakEnabled = false;
        }

        public static void ToggleRainbow()
        {
            if (!rainbowEnabled)
            {
                Debug.Log("trying to start RainBowHandler");
                UnityEngine.Object.FindObjectOfType<RainbowManager>().enabled = true;
                rainbowEnabled = true;
                return;
            }

            UnityEngine.Object.FindObjectOfType<RainbowManager>().enabled = false;
            rainbowEnabled = false;
        }

        public static string GetTargetStatValue(CharacterStats stats, string targetStat)
        {
            foreach (var stat in typeof(CharacterStats).GetFields())
                if (stat.Name.ToLower() == targetStat)
                    return stat.GetValue(stats).ToString();

            return "No value";
        }

        public static void SendLocalMsg(string msg, ChatCommands.LogLevel logLevel) => GameManager.Instance.StartCoroutine(SendClientSideMsg(logLevel, msg));

        private static IEnumerator SendClientSideMsg(ChatCommands.LogLevel logLevel, string msg)
        {
            string msgColor = logLevel switch
            {
                ChatCommands.LogLevel.Warning => "<#FF7F50>",
                _ => "<#006400>"
            };

            bool origRichTextValue = tmpText.richText;

            float chatMsgDuration = 1.5f + msg.Length * 0.075f; // Time that chat msg will last till closing animation
            var extraTime = Plugin.configMsgDuration.Value;
            if (extraTime > 0) chatMsgDuration += extraTime; // Taking into account any possible extra msg time specified in config

            tmpText.richText = true;
            localChat.Talk(msgColor + msg);

            // Add 3 grace seconds to original msg duration so rich text doesn't stop during closing animation
            yield return new WaitForSeconds(chatMsgDuration + 3f); 
            tmpText.richText = origRichTextValue;
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
            => MusicHandler.Instance.myMusic = MusicHandler.Instance.myMusic.AddToArray(new() { clip = audioClip });

        // Fancy bit-manipulation of a char's ASCII values to check whether it's a vowel or not
        public static bool IsVowel(char c) => (0x208222 >> (c & 0x1f) & 1) != 0;

        public static CSteamID lobbyID; // The ID of the current lobby
        
        public static readonly CSteamID localPlayerSteamID = SteamUser.GetSteamID(); // The steamID of the local user (ours)
        public static NetworkPlayer localNetworkPlayer; // The networkPlayer of the local user (ours)
        public static List<ushort> mutedPlayers = new(4);

        public static bool isTranslating = Plugin.configTranslation.Value; // True if auto-translations are enabled, false by default
        public static bool autoGG = Plugin.configAutoGG.Value; // True if auto gg on death is enabled, false by default
        public static bool uwuifyText; // True if uwufiy text is enabled, false by default
        public static bool winStreakEnabled = Plugin.configWinStreakLog.Value;
        public static bool chatCensorshipBypass = Plugin.configchatCensorshipBypass.Value; // True if chat censoring is bypassed, false by default
        public static Color CustomPlayerColor = Plugin.configCustomColor.Value;
        public static bool IsCustomPlayerColor = Plugin.configCustomColor.Value != new Color(1, 1, 1);
        public static bool IsCustomName = !string.IsNullOrEmpty(Plugin.configCustomName.Value);
        public static bool IsOwMode;
        public static bool IsSongLoop;
        public static string[] OuchPhrases = Plugin.configOuchPhrases.Value.Split(' ');
        public static bool NameResize = Plugin.configNoResize.Value;
        public static bool nukChat;
        public static bool onlyLower;
        public static bool HPWinner = Plugin.configHPWinner.Value;
        public static bool rainbowEnabled;
        public static IEnumerator routineUsed;

        public static ConnectedClientData[] clientData;
        public static ChatManager localChat;

        public static TextMeshPro tmpText;
        public static int winStreak = 0;
    }
}
