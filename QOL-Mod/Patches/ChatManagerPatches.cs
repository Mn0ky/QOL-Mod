using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Steamworks;

namespace QOL
{
    public class ChatManagerPatches
    {
        public static void Patches(Harmony harmonyInstance) // ChatManager methods to patch with the harmony instance
        {
            var SendChatMessageMethod = AccessTools.Method(typeof(ChatManager), "SendChatMessage");
            var SendChatMessageMethodPrefix = new HarmonyMethod(typeof(ChatManagerPatches).GetMethod(nameof(ChatManagerPatches.SendChatMessageMethodPrefix))); // Patches SendChatMessage with prefix method
            harmonyInstance.Patch(SendChatMessageMethod, prefix: SendChatMessageMethodPrefix);
        }

        public static bool SendChatMessageMethodPrefix(ref string message, ChatManager __instance) // Prefix method for patching the original (SendChatMessageMethod)
        {
            if (message.StartsWith("/"))
            {
                ChatManagerPatches.Commands(message, __instance);
                return false;
            }
            return true;
        }

        public static void Commands(string message, ChatManager __instance)
        {
            Debug.Log("Made it to beginning of commands!");
            MatchmakingHandler matchmaking = Traverse.Create(UnityEngine.Object.FindObjectOfType<MultiplayerManager>()).Field("mMatchmakingHandler").GetValue() as MatchmakingHandler; // For accessing private variable mServerID in ChatManager (this is the host's steamID)
            CSteamID serverID =  matchmaking.LobbyOwner;
            Debug.Log("Made it passed serverID assignment! : " + serverID);
            NetworkPlayer localNetworkPlayer = Traverse.Create(__instance).Field("m_NetworkPlayer").GetValue() as NetworkPlayer; // For accessing private variable m_NetworkPlayer in ChatManager
            string text = message.ToLower();
            text = text.TrimStart(new char[] { '/' });

            if (text.Contains("shrug")) // Adds shrug emoticon to end of chat message
            {
                message = message.Replace("/shrug", "");
                message += " \u00af\\_(ツ)_/\u00af";
                localNetworkPlayer.OnTalked(message);
                return;
            }

            if (text == "rich") // Enables rich text for chat messages
            {
                TextMeshPro theText = Traverse.Create(__instance).Field("text").GetValue() as TextMeshPro;
                theText.richText = !theText.richText;
                return;
            }
            if (text.Contains("hp") && localNetworkPlayer.HasLocalControl) // Sends HP of targeted color to chat
            {
                if (text.Length > 2)
                {
                    string colorWanted = text.Substring(3);
                    string targetHealth = ChatManagerPatches.GetNetworkPlayer(ChatManagerPatches.GetIDFromColor(colorWanted)).GetComponentInChildren<HealthHandler>().health.ToString();
                    localNetworkPlayer.OnTalked(colorWanted + " HP: " + targetHealth);
                    return;
                }
                Debug.Log("Looking for my health!");
                string localHealth = localNetworkPlayer.GetComponentInChildren<HealthHandler>().health.ToString();
                Debug.Log("Current Health: " + localHealth);
                localNetworkPlayer.OnTalked("My HP: " + localHealth);
                return;
            }
            if (text == "private") // Privates the lobby (no player can publicly join unless invited)
            {
                SteamMatchmaking.SetLobbyJoinable(MatchmakingHandlerPatch.lobbyID, false);
                localNetworkPlayer.OnTalked("Lobby is now private!");
            }
            if (text == "public") // Publicizes the lobby (any player can join through quick match)
            {
                SteamMatchmaking.SetLobbyJoinable(MatchmakingHandlerPatch.lobbyID, true);
                localNetworkPlayer.OnTalked("Lobby is now public!");
            }
            if (text == "invite") // Builds a "join game" link (same one you'd find on a steam profile) for lobby and copies it to clipboard
            {
                Debug.Log("LobbyID: " + MatchmakingHandlerPatch.lobbyID);
                Debug.Log("Verification test, should return 25: " + SteamMatchmaking.GetLobbyData(MatchmakingHandlerPatch.lobbyID, StickFightConstants.VERSION_KEY));
                ChatManagerPatches.GetJoinGameLink(MatchmakingHandlerPatch.lobbyID, ChatManagerPatches.GetSteamID(localNetworkPlayer.NetworkSpawnID));
                localNetworkPlayer.OnTalked("Join link copied to clipboard!");
            }
        }
        public static ushort GetIDFromColor(string targetSpawnColor) // Returns the corresponding spawnID from the specified color
        {
            switch (targetSpawnColor)
            {
                case "blue":
                    return 1;
                case "Red":
                    return 2;
                case "Green":
                    return 3;
                default:
                    return 0;
            }
        }
        public static global::NetworkPlayer GetNetworkPlayer(ushort targetID) // Returns the targeted player based on the specified spawnID
        {
            foreach (global::NetworkPlayer networkPlayer in UnityEngine.Object.FindObjectsOfType<global::NetworkPlayer>())
            {
                if (networkPlayer.NetworkSpawnID == targetID)
                {
                    return networkPlayer;
                }
            }
            return null;
        }
        public static void GetJoinGameLink(CSteamID lobbyID, CSteamID playerSteamID) // Actually sticks "join game" the link together
        {
            string urlAndProtocolPrefix = "steam://joinlobby/";
            string appID = "674940/";
            string joinLink = string.Concat(new string[]
            {
            urlAndProtocolPrefix,
            appID,
            lobbyID.ToString(),
            "/",
            playerSteamID.ToString()
            });
            Debug.Log("joinLink: " + joinLink);
            GUIUtility.systemCopyBuffer = joinLink;
        }
        public static CSteamID GetSteamID(ushort targetID) // Returns the steamID of the specified spawnID
        {
            ConnectedClientData[] connectedClients = Traverse.Create(UnityEngine.Object.FindObjectOfType<MultiplayerManager>()).Field("mConnectedClients").GetValue() as ConnectedClientData[];
            return connectedClients[(int)targetID].ClientID;
        }

    }
}
