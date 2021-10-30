using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Steamworks;
using HarmonyLib;

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
                case "blue":
                    return 1;
                case "red":
                    return 2;
                case "green":
                    return 3;
                default:
                    return 0;
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
            return (Helper.GetNetworkPlayer(Helper.GetIDFromColor(colorWanted)).GetComponentInChildren<HealthHandler>().health.ToString());
        }
        public static void GetJoinGameLink() // Actually sticks the "join game" link together
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
            GUIUtility.systemCopyBuffer = joinLink;
        }
        public static CSteamID lobbyID; // The ID of the current lobby
        public static CSteamID localPlayerSteamID; // The steamID of the local player
        public static NetworkPlayer localNetworkPlayer;
        public static bool isTranslating;
    }
}