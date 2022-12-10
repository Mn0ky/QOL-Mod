using System.Text;
using HarmonyLib;
using UnityEngine;

namespace QOL;

class NetworkPlayerPatch
{
    public static void Patch(Harmony harmonyInstance) // NetworkPlayer methods to patch with the harmony instance
    {

        var syncClientChatMethod = AccessTools.Method(typeof(NetworkPlayer), "SyncClientChat");
        var syncClientChatMethodPrefix = new HarmonyMethod(typeof(NetworkPlayerPatch)
            .GetMethod(nameof(SyncClientChatMethodPrefix)));
        harmonyInstance.Patch(syncClientChatMethod, prefix: syncClientChatMethodPrefix);
    }

    public static bool SyncClientChatMethodPrefix(ref byte[] data, NetworkPlayer __instance)
    {
        if (Helper.MutedPlayers.Contains(__instance.NetworkSpawnID)) return false;

        if (!ChatCommands.CmdDict["translate"].IsEnabled) return true;

        TranslateMessage(data, __instance);
        return false;
    }
        
    // TODO: Refactor and expand upon this
    // Checks if auto-translation is enabled, if so then translate it
    private static void TranslateMessage(byte[] data, NetworkPlayer __instance)
    {
        var textToTranslate = Encoding.UTF8.GetString(data);
        Debug.Log("Got message: " + textToTranslate);

        var usingKey = !string.IsNullOrEmpty(Plugin.ConfigAuthKeyForTranslation.Value);

        var mHasLocalControl = Traverse.Create(__instance).Field("mHasLocalControl").GetValue<bool>();
        var mLocalChatManager = AccessTools.StaticFieldRefAccess<ChatManager>(typeof(NetworkPlayer), 
            "mLocalChatManager");
        Debug.Log("mLocalChatManager : " + mLocalChatManager);
        Debug.Log("mHasLocalControl : " + mHasLocalControl);

        if (mHasLocalControl)
        {
            if (usingKey)
            {
                __instance.StartCoroutine(AuthTranslate.TranslateText("auto",
                    "en",
                    textToTranslate,
                    s => mLocalChatManager.Talk(s)));

                return;
            }
                
            __instance.StartCoroutine(Translate.Process("en", 
                textToTranslate, 
                s => mLocalChatManager.Talk(s)));
                
            return;
        }

        var mChatManager = Traverse.Create(__instance).Field("mChatManager").GetValue<ChatManager>();
            
        if (!usingKey)
        {
            __instance.StartCoroutine(Translate.Process("en", 
                textToTranslate, 
                s => mChatManager.Talk(s)));
                
            return;
        }
            
        __instance.StartCoroutine(AuthTranslate.TranslateText("auto",
            "en",
            textToTranslate,
            s => mChatManager.Talk(s)));
    }
}