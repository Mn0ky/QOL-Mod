using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

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
            if (text.Contains("hp") && localNetworkPlayer.HasLocalControl)
            {
                if (text.Length > 2)
                {
                    string colorWanted = text.Substring(3);
                    string targetHealth = ChatManagerPatches.GetNetworkPlayer(ChatManagerPatches.GetIDFromColor(colorWanted)).GetComponentInChildren<HealthHandler>().health.ToString();
                    localNetworkPlayer.OnTalked(colorWanted + " HP: " + targetHealth);
                    return;
                }
                string localHealth = localNetworkPlayer.GetComponentInChildren<HealthHandler>().health.ToString();
                Debug.Log("Current Health: " + localHealth);
                localNetworkPlayer.OnTalked("My HP: " + localHealth);
                return;
            }
        }
        public static ushort GetIDFromColor(string targetSpawnColor)
        {
            if (targetSpawnColor == "yellow")
            {
                return 0;
            }
            if (targetSpawnColor == "blue")
            {
                return 1;
            }
            if (targetSpawnColor == "red")
            {
                return 2;
            }
            if (targetSpawnColor == "green")
            {
                return 3;
            }
            return ushort.MaxValue;
        }
        public static global::NetworkPlayer GetNetworkPlayer(ushort targetID)
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
    }
}
