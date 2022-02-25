using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine;

namespace QOL
{
    [BepInPlugin("monky.plugins.QOL", "QOL Mod", VersionNumber)]
    [BepInProcess("StickFight.exe")]    
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo("Plugin 'monky.plugins.QOL' is loaded! [v" + VersionNumber + "]");
            Logger.LogInfo("Hello from monk :D");
            try
            {
                Harmony harmony = new Harmony("monky.QOL"); // Creates harmony instance with identifier
                Logger.LogInfo("Applying ChatManager patches...");
                ChatManagerPatches.Patches(harmony);
                Logger.LogInfo("Applying MatchmakingHandler patch..."); 
                MatchmakingHandlerPatches.Patch(harmony);
                Logger.LogInfo("Applying MultiplayerManager patches...");
                MultiplayerManagerPatches.Patches(harmony);
                Logger.LogInfo("Applying NetworkPlayer patch...");
                NetworkPlayerPatch.Patch(harmony);
                Logger.LogInfo("Applying Controller patch...");
                ControllerPatch.Patch(harmony);
                Logger.LogInfo("Applying GameManager patch...");
                GameManagerPatch.Patch(harmony);
                // Logger.LogInfo("Applying CharacterStats patch...");
                // CharacterStatsPatch.Patch(harmony);
                // Logger.LogInfo("Applying OnlinePlayerUI patch...");
                // OnlinePlayerUIPatch.Patch(harmony);
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

                configWinStreakLog = Config.Bind("Startup Options",
                    "AlwaysTrackWinstreak",
                    false,
                    "Always keep track of your winstreak instead of only when enabled?");

                configRichText = Config.Bind("Startup Options",
                    "RichText",
                    false,
                    "Enable rich text for chat on startup?");

                configTranslation = Config.Bind("Startup Options",
                    "AutoTranslations",
                    false,
                    "Enable auto-translation for chat messages to English on startup?");

                configCustomColor = Config.Bind("Startup Options",
                    "CustomColor",
                    new Color(1, 1, 1),
                    "Specify a custom player color? (Use a HEX value)");

                configNoResize = Config.Bind("Startup Options",
                    "NoResize",
                    false,
                    "Do not shrink username font if name is over 12 characters? (This is providing large name support)");

                configCustomName = Config.Bind("Startup Options",
                    "CustomUsername",
                    string.Empty,
                    "Specify a custom username? (client-side only)");

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
        public static ConfigEntry<bool> configWinStreakLog;
        public static ConfigEntry<bool> configNoResize;
        public static ConfigEntry<Color> configCustomColor;
        public static ConfigEntry<string> configAuthKeyForTranslation;
        public static ConfigEntry<string> configCustomName;

        public const string VersionNumber = "1.0.11"; // Version number
    }
}
