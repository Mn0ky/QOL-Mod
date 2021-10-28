using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
namespace QOL
{
    class NetworkPlayerPatch
    {
        public static void Patch(Harmony harmonyInstance) // Multiplayer methods to patch with the harmony instance
        {

            var SyncClientChatMethod = AccessTools.Method(typeof(NetworkPlayer), "SyncClientChat");
            var SyncClientChatMethodPrefix = new HarmonyMethod(typeof(NetworkPlayerPatch).GetMethod(nameof(NetworkPlayerPatch.SyncClientChatMethodPrefix))); // Patches SyncClientChat with prefix method
            harmonyInstance.Patch(SyncClientChatMethod, prefix: SyncClientChatMethodPrefix);
        }

        public static bool SyncClientChatMethodPrefix(ref byte[] data, NetworkPlayer __instance)
        {
            if (!Helper.isTranslating)
            {
                return true;
            }
            translateMessage(data, __instance);
            return false;
        }

        public static void translateMessage(byte[] data, NetworkPlayer __instance)
        {
            string textToTranslate = Encoding.UTF8.GetString(data);
            Debug.Log("Got message: " + textToTranslate);
            string mHasLocalControl = Traverse.Create(__instance).Field("mHasLocalControl").GetValue() as string; // TODO: fix this!!
            Debug.Log("mHasLocalControl : " + mHasLocalControl);
            if (bool.Parse(mHasLocalControl))
            {
                __instance.StartCoroutine(Translate.Process("en", textToTranslate, delegate (string s) { Helper.localNetworkPlayer.OnTalked(s); }));
                return;
            }
            ChatManager mChatManager = Traverse.Create(__instance).Field("mChatManager").GetValue() as ChatManager;
            __instance.StartCoroutine(Translate.Process("en", textToTranslate, delegate (string s) { mChatManager.Talk(s); }));
        }
    }
}
