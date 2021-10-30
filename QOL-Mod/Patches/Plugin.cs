using System;
using BepInEx;
using UnityEngine;
using HarmonyLib;   

namespace QOL
{
    [BepInPlugin("monky.plugins.QOL", "QOL Mod", "1.0.7")]
    [BepInProcess("StickFight.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo("Plugin 'monky.plugins.QOL' is loaded!");
            Logger.LogInfo("Hello from monk :D");
            try
            {
                Harmony harmony = new Harmony("monky.QOL"); // Creates harmony instance with identifier
                Logger.LogInfo("Applying ChatManager patches");
                ChatManagerPatches.Patches(harmony);
                Logger.LogInfo("Applying MatchmakingHandler patch");
                MatchmakingHandlerPatch.Patch(harmony);
                Logger.LogInfo("Applying MultiplayerManager patches");
                MultiplayerManagerPatches.Patches(harmony);
                Logger.LogInfo("Applying NetworkPlayer patch");
                NetworkPlayerPatch.Patch(harmony);
            }
            catch (Exception ex)
            {
                Logger.LogError("Exception on applying patches: " + ex.Message);
            }
        }
    }
}
