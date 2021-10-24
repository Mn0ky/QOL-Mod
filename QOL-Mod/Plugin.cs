using System;
using BepInEx;
using HarmonyLib;   

namespace QOL
{
    [BepInPlugin("monky.plugins.QOL", "QOL Mod", "1.0.5")]
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
                ChatManagerPatches.Patches(harmony);
            }
            catch (Exception ex)
            {
                Logger.LogInfo("Exception on applying patches: " + ex.Message);
            }
        }
    }
}
