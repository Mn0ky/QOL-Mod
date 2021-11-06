using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace QOL
{
    [BepInPlugin("monky.plugins.QOL", "QOL Mod", "1.0.8")]
    [BepInProcess("StickFight.exe")]    
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo("Plugin 'monky.plugins.QOL' is loaded! [v1.0.8]");
            Logger.LogInfo("Hello from monk :D");
            try
            {
                Harmony harmony = new Harmony("monky.QOL"); // Creates harmony instance with identifier
                Logger.LogInfo("Applying ChatManager patches...");
                ChatManagerPatches.Patches(harmony);
                Logger.LogInfo("Applying MatchmakingHandler patch...");
                MatchmakingHandlerPatch.Patch(harmony);
                Logger.LogInfo("Applying MultiplayerManager patches...");
                MultiplayerManagerPatches.Patches(harmony);
                Logger.LogInfo("Applying NetworkPlayer patch...");
                NetworkPlayerPatch.Patch(harmony);
                Logger.LogInfo("Applying Controller patch...");
                ControllerPatch.Patch(harmony);
            }
            catch (Exception ex)
            {
                Logger.LogError("Exception on applying patches: " + ex.InnerException);
            }

            try
            {
                Logger.LogInfo("Loading configuration options from config file...");

                configAutoGG = Config.Bind("Startup Options", // The section under which the option is shown
                    "AutoGG",
                    false, // The key of the configuration option in the configuration file
                    "Enable AutoGG on startup?"); // Description of the option to show in the config file

                configchatCensorshipBypass = Config.Bind("Startup Options",
                    "ChatCensorshipBypass",
                    false,
                    "Disable chat censorship on startup?");

                configRichText = Config.Bind("Startup Options",
                    "RichTextInChat",
                    false,
                    "Enable rich text for chat on startup?");

                configTranslation = Config.Bind("Startup Options",
                    "AutoTranslations",
                    false,
                    "Enable auto-translations for messages to English?");

                configAuthKeyForTranslation = Config.Bind("Startup Options",
                    "AutoAuthTranslationsAPIKey",
                    string.Empty,
                    "Put your API key for Google Translate V2 here (Optional)");
            }
            catch (Exception ex)
            {
                Logger.LogError("Exception on loading configuration: " + ex.InnerException);
            }
        }
        public static ConfigEntry<bool> configchatCensorshipBypass;
        public static ConfigEntry<bool> configAutoGG;
        public static ConfigEntry<bool> configRichText;
        public static ConfigEntry<bool> configTranslation;
        public static ConfigEntry<string> configAuthKeyForTranslation;
    }
}
