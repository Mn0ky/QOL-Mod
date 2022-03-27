using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using HarmonyLib;
using TMPro;

namespace QOL
{
    public class Helper
    {
        // Returns the steamID of the specified spawnID
        public static CSteamID GetSteamID(ushort targetID) => clientData[targetID].ClientID;

        // Returns the corresponding spawnID from the specified color
        public static ushort GetIDFromColor(string targetSpawnColor) => targetSpawnColor switch {"yellow" => 0, "blue" => 1, "red" => 2, "green" => 3, _ => ushort.MaxValue};

        // Returns the corresponding color from the specified spawnID
        public static string GetColorFromID(ushort x) => x switch {1 => "Blue", 2 => "Red", 3 => "Green", _ => "Yellow"};

        // Returns the corresponding color from the specified spawnID
        public static string GetCapitalColor(string color) => char.ToUpper(color[0]) + color.Substring(1);

        // Returns the targeted player based on the specified spawnID
        public static NetworkPlayer GetNetworkPlayer(ushort targetID) => clientData[targetID].PlayerObject.GetComponent<NetworkPlayer>();

        // Gets the steam profile name of the specified steamID
        public static string GetPlayerName(CSteamID passedClientID) => SteamFriends.GetFriendPersonaName(passedClientID);

        public static string GetHPOfPlayer(string colorWanted) => clientData[GetIDFromColor(colorWanted)].PlayerObject.GetComponent<HealthHandler>().health + "%";

        public static string GetHPOfPlayer(ushort idWanted) => GetNetworkPlayer(idWanted).GetComponentInChildren<HealthHandler>().health + "%";

        // Actually sticks the "join game" link together (url prefix + appID + LobbyID + SteamID)
        public static string GetJoinGameLink() => $"steam://joinlobby/674940/{lobbyID}/{localPlayerSteamID}";

        public static void InitValues(ChatManager __instance, ushort playerID) // Assigns the networkPlayer as the local one if it matches our steamID (also if text should be rich or not)
        {
            if (playerID != GameManager.Instance.mMultiplayerManager.LocalPlayerIndex) return;

            clientData = GameManager.Instance.mMultiplayerManager.ConnectedClients;
            mutedPlayers.Clear();

            byte localID = GameManager.Instance.mMultiplayerManager.LocalPlayerIndex;
            localNetworkPlayer = clientData[localID].PlayerObject.GetComponent<NetworkPlayer>();
            localChat = clientData[localID].PlayerObject.GetComponentInChildren<ChatManager>();

            Debug.Log("Assigned the localNetworkPlayer!: " + localNetworkPlayer.NetworkSpawnID);

            tmpText = Traverse.Create(__instance).Field("text").GetValue() as TextMeshPro;
            tmpText.richText = Plugin.configRichText.Value;

            if (Plugin.configFixCrown.Value)
            {
                WinCounterUI counter = UnityEngine.Object.FindObjectOfType<WinCounterUI>();

                foreach (var crownCount in counter.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    Debug.Log("crown autosize: " + crownCount.autoSizeTextContainer);
                    crownCount.enableAutoSizing = true;
                }
            }

            if (NameResize) // If reading custom names then make sure to add conditional here
            {
                TextMeshProUGUI[] playerNames = Traverse.Create(UnityEngine.Object.FindObjectOfType<OnlinePlayerUI>()).Field("mPlayerTexts").GetValue() as TextMeshProUGUI[]; //TODO: Simplify this, cough cough references

                foreach (var name in playerNames)
                {
                    name.enableAutoSizing = true;
                    name.fontSizeMax = 45;
                }
            }

            // if (isCustomName)
            // {
            //     Debug.Log("custom name:" + Plugin.configCustomName.Value);
            //     playerNames[localNetworkPlayer.NetworkSpawnID].GetComponent<TextMeshProUGUI>().text = Plugin.configCustomName.Value;
            //     Debug.Log(playerNames[localNetworkPlayer.NetworkSpawnID].GetComponent<TextMeshProUGUI>().text);
            // }

            GameObject rbHand = new("RainbowHandler");
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
            {
                if (stat.Name.ToLower() == targetStat)
                {
                    return stat.GetValue(stats).ToString();
                }
            }
            return "No value";
        }

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
        public static Color customPlayerColor = Plugin.configCustomColor.Value;
        //public static bool isCustomName = !string.IsNullOrEmpty(Plugin.configCustomName.Value);
        public static bool NameResize = Plugin.configNoResize.Value;
        public static bool nukChat;
        public static bool onlyLower;
        public static bool HPWinner = Plugin.configHPWinner.Value;
        public static bool rainbowEnabled;
        public static IEnumerator routineUsed;

        public static MatchmakingHandler matchmaking;
        public static ConnectedClientData[] clientData;
        public static ChatManager localChat;
        //public static GameManager gameManager;

        public static TextMeshPro tmpText;

        public static Color[] defaultColors = {
            new(0.846f, 0.549f, 0.280f),
            new(0.333f, 0.449f, 0.676f),
            new(0.838f, 0.335f, 0.302f),
            new(0.339f, 0.544f, 0.288f)
        };

        public static int winStreak = 0;
    }
}
