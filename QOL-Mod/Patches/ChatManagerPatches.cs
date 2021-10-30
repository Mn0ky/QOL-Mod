using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
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
            var AwakeMethod = AccessTools.Method(typeof(ChatManager), "Awake");
            var AwakeMethodPostfix = new HarmonyMethod(typeof(ChatManagerPatches).GetMethod(nameof(ChatManagerPatches.AwakeMethodPostfix))); // Patches Awake with prefix method
            harmonyInstance.Patch(AwakeMethod, postfix: AwakeMethodPostfix);

            var StartMethod = AccessTools.Method(typeof(ChatManager), "Start");
            var StartMethodPostfix = new HarmonyMethod(typeof(ChatManagerPatches).GetMethod(nameof(ChatManagerPatches.StartMethodPostfix))); // Patches Awake with prefix method
            harmonyInstance.Patch(StartMethod, postfix: StartMethodPostfix);

            var SendChatMessageMethod = AccessTools.Method(typeof(ChatManager), "SendChatMessage");
            var SendChatMessageMethodPrefix = new HarmonyMethod(typeof(ChatManagerPatches).GetMethod(nameof(ChatManagerPatches.SendChatMessageMethodPrefix))); // Patches SendChatMessage with prefix method
            harmonyInstance.Patch(SendChatMessageMethod, prefix: SendChatMessageMethodPrefix);
    }
        public static void AwakeMethodPostfix(ChatManager __instance)
        {
            // __instance.gameObject.transform.root.gameObject.AddComponent<GUIManager>();
            // Debug.Log("Awake method skipped!");
        }
        public static void StartMethodPostfix(ChatManager __instance)
        {
            NetworkPlayer localNetworkPlayer = Traverse.Create(__instance).Field("m_NetworkPlayer").GetValue() as NetworkPlayer;
            Helper.AssignLocalNetworkPlayer(localNetworkPlayer);

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
            string text = message.ToLower();
            text = text.TrimStart(new char[] {'/'});

            if (text.Contains("hp") && Helper.localNetworkPlayer.HasLocalControl) // Sends HP of targeted color to chat
            {
                if (text.Length > 2)
                {
                    string colorWanted = text.Substring(3);
                    //string targetHealth = Helper.GetNetworkPlayer(Helper.GetIDFromColor(colorWanted)).GetComponentInChildren<HealthHandler>().health.ToString();
                    Debug.Log("Helper.localNetworkPlayer : " + Helper.localNetworkPlayer);
                    Debug.Log("Helper.localNetworkPlayer.NetworkSpawnID : " + Helper.localNetworkPlayer.NetworkSpawnID);
                    Helper.localNetworkPlayer.OnTalked(colorWanted + " HP: " + Helper.GetHPOfPlayer(colorWanted));
                    return;
                }

                Debug.Log("Looking for my health!");
                Debug.Log("Helper.localNetworkPlayer : " + Helper.localNetworkPlayer);
                Debug.Log("Helper.localNetworkPlayer.NetworkSpawnID : " + Helper.localNetworkPlayer.NetworkSpawnID);
                string localHealth = Helper.localNetworkPlayer.GetComponentInChildren<HealthHandler>().health.ToString() + "%";
                Debug.Log("Current Health: " + localHealth);
                Helper.localNetworkPlayer.OnTalked("My HP: " + localHealth);
                
            }

            else if (text.Contains("shrug")) // Adds shrug emoticon to end of chat message
            {
                message = message.Replace("/shrug", "");
                message += " \u00af\\_(ツ)_/\u00af";
                Helper.localNetworkPlayer.OnTalked(message);
                
            }

            else if (text == "rich") // Enables rich text for chat messages
            {
                TextMeshPro theText = Traverse.Create(__instance).Field("text").GetValue() as TextMeshPro;
                theText.richText = !theText.richText;
                
            }
            else if (text == "private") // Privates the lobby (no player can publicly join unless invited)
            {
                SteamMatchmaking.SetLobbyJoinable(Helper.lobbyID, false);
                Helper.localNetworkPlayer.OnTalked("Lobby is now private!");
            }
            else if (text == "public") // Publicizes the lobby (any player can join through quick match)
            {
                SteamMatchmaking.SetLobbyJoinable(Helper.lobbyID, true);
                Helper.localNetworkPlayer.OnTalked("Lobby is now public!");
            }
            else if (
                text == "invite") // Builds a "join game" link (same one you'd find on a steam profile) for lobby and copies it to clipboard
            {
                Debug.Log("LobbyID: " + Helper.lobbyID);
                Debug.Log("Verification test, should return 25: " +
                          SteamMatchmaking.GetLobbyData(Helper.lobbyID, StickFightConstants.VERSION_KEY));
                Helper.GetJoinGameLink();
                Helper.localNetworkPlayer.OnTalked("Join link copied to clipboard!");
            }
            else if (text == "translate") // Whether or not to enable automatic translations
            {
                Helper.isTranslating = !Helper.isTranslating;
                //__instance.StartCoroutine(Translate.Process("en", "Bonjour.", delegate (string s) { Helper.localNetworkPlayer.OnTalked(s); }));
                
            }
        }
    }
}
