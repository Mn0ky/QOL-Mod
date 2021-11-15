using UnityEngine;
using Steamworks;
using HarmonyLib;
using TMPro;

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
            switch (targetSpawnColor)
            {
                case "yellow":
                    return 0;
                case "blue":
                    return 1;
                case "red":
                    return 2;
                case "green":
                    return 3;
                default:
                    return ushort.MaxValue;
            }
        }
        public static string GetColorFromID(ushort x) // Returns the corresponding color from the specified spawnID
        {
            switch (x)
            {
                case 1:
                    return "Blue";
                case 2:
                    return "Red";
                case 3:
                    return "Green";
                default:
                    return "Yellow";
            }
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
            return (Helper.GetNetworkPlayer(Helper.GetIDFromColor(colorWanted)).GetComponentInChildren<HealthHandler>().health.ToString() + "%");
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
                return;
            }
            Debug.Log("That wasn't the local player!");
        }
        public static CSteamID lobbyID; // The ID of the current lobby
        
        public static readonly CSteamID localPlayerSteamID = SteamUser.GetSteamID(); // The steamID of the local user (ours)

        public static NetworkPlayer localNetworkPlayer; // The networkPlayer of the local user (ours)

        public static bool isTranslating = Plugin.configTranslation.Value; // True if auto-translations are enabled, false by default
        
        public static bool autoGG = Plugin.configAutoGG.Value; // True if auto gg on death is enabled, false by default

        public static bool uwuifyText; // True if uwufiy text is enabled, false by default

        public static bool chatCensorshipBypass = Plugin.configchatCensorshipBypass.Value; // True if chat censoring is bypassed, false by default

        public static Color customPlayerColor = Plugin.configCustomColor.Value;

        public static TextMeshPro tmpText;

        public static int winStreak = 0;
    }
}
