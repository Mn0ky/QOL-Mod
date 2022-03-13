using System;
using BepInEx.Configuration;
using UnityEngine;
using Steamworks;
using HarmonyLib;
using TMPro;
using UnityEngine.Assertions.Must;

namespace QOL
{
    public class Helper
    {
        public static CSteamID GetSteamID(ushort targetID) // Returns the steamID of the specified spawnID
        {
            ConnectedClientData[] connectedClients = Traverse.Create(UnityEngine.Object.FindObjectOfType<MultiplayerManager>()).Field("mConnectedClients").GetValue() as ConnectedClientData[];

            return connectedClients[(int)targetID].ClientID;
        }

        public static ushort GetIDFromColor(string targetSpawnColor) // Returns the corresponding spawnID from the specified color
        {
            return targetSpawnColor switch
            {
                "yellow" => 0,
                "blue" => 1,
                "red" => 2,
                "green" => 3,
                _ => ushort.MaxValue,
            };
        }

        public static string GetColorFromID(ushort x) // Returns the corresponding color from the specified spawnID
        {
            return x switch
            {
                1 => "Blue",
                2 => "Red",
                3 => "Green",
                _ => "Yellow",
            };
        }

        public static string GetCapitalColor(string color) // Returns the corresponding color from the specified spawnID
        {
            return char.ToUpper(color[0]) + color.Substring(1);
        }

        public static NetworkPlayer GetNetworkPlayer(ushort targetID) // Returns the targeted player based on the specified spawnID
        {
            foreach (NetworkPlayer networkPlayer in UnityEngine.Object.FindObjectsOfType<NetworkPlayer>())
            {
                if (networkPlayer.NetworkSpawnID == targetID)
                {
                    return networkPlayer;
                }
            }
            return null;
        }
        public static string GetPlayerName(CSteamID passedClientID) // Gets the steam profile name of the specified steamID
        {
            return SteamFriends.GetFriendPersonaName(passedClientID);
        }

        public static string GetHPOfPlayer(string colorWanted)
        {
            Debug.Log("colorwanted, hpofplayer: " + colorWanted);
            return (Helper.GetNetworkPlayer(Helper.GetIDFromColor(colorWanted)).GetComponentInChildren<HealthHandler>().health + "%");
        }

        public static string GetHPOfPlayer(ushort idWanted)
        {
            return (Helper.GetNetworkPlayer(idWanted).GetComponentInChildren<HealthHandler>().health + "%");
        }

        public static string GetJoinGameLink() // Actually sticks the "join game" link together
        {
            string urlAndProtocolPrefix = "steam://joinlobby/";
            string appID = "674940/";
            string joinLink = string.Concat(new string[]
            {
            urlAndProtocolPrefix,
            appID,
            Helper.lobbyID.ToString(),
            "/",
            Helper.localPlayerSteamID.ToString()
            });
            Debug.Log("joinLink: " + joinLink);
            return joinLink;
        }

        public static void AssignLocalNetworkPlayerAndRichText(NetworkPlayer localNetworkPlayer, ChatManager __instance) // Assigns the networkPlayer as the local one if it matches our steamID (also if text should be rich or not)
        {
            if (GetSteamID(localNetworkPlayer.NetworkSpawnID) == Helper.localPlayerSteamID)
            {
                Helper.localNetworkPlayer = localNetworkPlayer;
                Debug.Log("Assigned the localNetworkPlayer!");
                Helper.tmpText = Traverse.Create(__instance).Field("text").GetValue() as TextMeshPro;
                Helper.tmpText.richText = Plugin.configRichText.Value;

                if (Helper.NameResize) // If reading custom names then make sure to add conditional here
                {
                    TextMeshProUGUI[] playerNames = Traverse.Create(UnityEngine.Object.FindObjectOfType<OnlinePlayerUI>()).Field("mPlayerTexts").GetValue() as TextMeshProUGUI[]; //TODO: Simplify this, cough cough references

                    foreach (var name in playerNames)
                    {
                        //playerNames[localNetworkPlayer.NetworkSpawnID].GetComponent<TextMeshProUGUI>().fontSize = 19.5f;
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

                if (Plugin.configFixCrown.Value)
                {
                    foreach (var crownCount in UnityEngine.Object.FindObjectOfType<WinCounterUI>().GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        crownCount.enableAutoSizing = true;
                    }
                }
                return;
            }
            Debug.Log("That wasn't the local player!");
        }

        public static void ToggleWinstreak()
        {
            if (!winStreakEnabled)
            {
                winStreakEnabled = true;
                gameManager.winText.fontSize = Plugin.configWinStreakFontsize.Value;
                return;
            }

            winStreakEnabled = false;
            //gameManager.winText.fontSize = 200;
        }

        public static bool IsVowel(char c) // Fancy bit manipulation of a character's ASCII values to check if it's a vowel or not
        {
            return (0x208222 >> (c & 0x1f) & 1) != 0;
        }

        public static CSteamID lobbyID; // The ID of the current lobby
        
        public static readonly CSteamID localPlayerSteamID = SteamUser.GetSteamID(); // The steamID of the local user (ours)
        public static NetworkPlayer localNetworkPlayer; // The networkPlayer of the local user (ours)

        public static bool isTranslating = Plugin.configTranslation.Value; // True if auto-translations are enabled, false by default
        public static bool autoGG = Plugin.configAutoGG.Value; // True if auto gg on death is enabled, false by default
        public static bool uwuifyText; // True if uwufiy text is enabled, false by default
        public static bool winStreakEnabled = Plugin.configWinStreakLog.Value;
        public static bool chatCensorshipBypass = Plugin.configchatCensorshipBypass.Value; // True if chat censoring is bypassed, false by default
        public static Color customPlayerColor = Plugin.configCustomColor.Value;
        //public static bool isCustomName = !string.IsNullOrEmpty(Plugin.configCustomName.Value);
        public static bool NameResize = Plugin.configNoResize.Value;
        public static bool nukChat;
        public static bool HPWinner = Plugin.configHPWinner.Value;

        public static MatchmakingHandler matchmaking;
        public static GameManager gameManager;


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
