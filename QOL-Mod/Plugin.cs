using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
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

                configCustomColor = Config.Bind("Player Color Options",
                    "CustomColor",
                    new Color(1, 1, 1),
                    "Specify a custom player color? (Use a HEX value)");

                configQOLMenuKeybind = Config.Bind("Menu Options", // The section under which the option is shown
                    "QOLMenuKeybind",
                    new KeyboardShortcut(KeyCode.LeftShift, KeyCode.F1), // The key of the configuration option in the configuration file
                    "Change the keybind for opening the QOL Menu? (Only specify a single key or two keys)"); // Description of the option to show in the config file

                configStatMenuKeybind = Config.Bind("Menu Options",
                    "StatWindowKeybind",
                    new KeyboardShortcut(KeyCode.LeftShift, KeyCode.F2),
                    "Change the keybind for opening the Stat Window? (Only specify a single key or two keys)");

                configQOLMenuPlacement = Config.Bind("Menu Options",
                    "QOLMenuLocation",
                    "0X 100Y",
                    "Change the default opening position of the QOL menu?");

                configStatMenuPlacement = Config.Bind("Menu Options",
                    "StatMenuLocation",
                    "800X 100Y",
                    "Change the default opening position of the Stat menu?");

                GUIManager.QOLMenuPos = MenuPosParser(configQOLMenuPlacement.Value);
                GUIManager.StatMenuPos = MenuPosParser(configStatMenuPlacement.Value);

                configWinStreakLog = Config.Bind("Winstreak Options",
                    "AlwaysTrackWinstreak",
                    false,
                    "Always keep track of your winstreak instead of only when enabled?");

                configWinStreakFontsize = Config.Bind("Winstreak Options",
                    "WinstreakFontsize",
                    200,
                    "Change the fontsize of your winstreak message?");

                configWinStreakColors = Config.Bind("Winstreak Options",
                    "WinstreakColors",
                    "FF0000 FFEB04 00FF00",
                    "Change the default winstreak colors? HEX values only, space separated. Each color will show in the order of the ranges below. The number of colors should be equal to the number of ranges! Max 50.");

                foreach (var colorStr in configWinStreakColors.Value.Split(' '))
                {
                    ColorUtility.TryParseHtmlString('#' + colorStr, out var color);
                    Logger.LogInfo("color :" + color);
                    GameManagerPatch.winstreakColors1.Add(color);
                }
                GameManagerPatch.winstreakColors2 = new List<Color>(GameManagerPatch.winstreakColors1);

                configWinStreakRanges = Config.Bind("Winstreak Options",
                    "WinstreakRanges",
                    "0-1 1-2 2-3",
                    "Change the default ranges? Add more ranges, space separated, to support more colors. Once the last range is reached the corresponding color will be used for all subsequent wins until the streak is lost. Max 50.");

                foreach (var streakRange in configWinStreakRanges.Value.Split(' '))
                {
                    Logger.LogInfo("range: " + streakRange);
                    int[] streakRangeNums = Array.ConvertAll(streakRange.Split('-'), int.Parse);
                    Logger.LogInfo("nums: " + streakRangeNums[0] + " " + streakRangeNums[1]);
                    
                    GameManagerPatch.winstreakRanges1.Add(streakRangeNums[1] - streakRangeNums[0]);
                }
                GameManagerPatch.winstreakRanges2 = new List<int>(GameManagerPatch.winstreakRanges1);

                configAutoGG = Config.Bind("On-Startup Options", // The section under which the option is shown
                    "AutoGG",
                    false, // The key of the configuration option in the configuration file
                    "Enable AutoGG on startup?"); // Description of the option to show in the config file

                configchatCensorshipBypass = Config.Bind("On-Startup Options",
                    "ChatCensorshipBypass",
                    false,
                    "Disable chat censorship on startup?");

                configRichText = Config.Bind("On-Startup Options",
                    "RichText",
                    false,
                    "Enable rich text for chat on startup?");

                configAdvCmd = Config.Bind("Startup Options",
                    "AdvertiseMsg",
                    "",
                    "Modify the output of /adv? By default the message is blank but can be changed to anything.");

                configTranslation = Config.Bind("On-Startup Options",
                    "AutoTranslations",
                    false,
                    "Enable auto-translation for chat messages to English on startup?");

                configNoResize = Config.Bind("Misc. Options",
                    "NoResize",
                    true,
                    "Do not shrink username font if name is over 12 characters? (This is providing large name support)");

                // configCustomName = Config.Bind("On-Startup Options",
                //     "CustomUsername",
                //     string.Empty,
                //     "Specify a custom username? (client-side only)");

                configHPWinner = Config.Bind("Misc. Options",
                    "AlwaysShowHPOfWinner",
                    false,
                    "Always show the HP of the winner of the round?");

                configEmoji = Config.Bind("Misc. Options",
                    "ShrugEmoji",
                    "☹",
                    "Specify the emoji used in the shrug command? Only the 15 TMP defaults are available: 😋, 😍, 😎, 😀, 😁, 😂, 😃, 😄, 😅, 😆, 😉, 😘, 🤣, ☺, ☹");

                configAuthKeyForTranslation = Config.Bind("Misc. Options",
                    "AutoAuthTranslationsAPIKey",
                    string.Empty,
                    "Put your API key for Google Translate V2 here (Optional)");
            }
            catch (Exception ex)
            {   
                Logger.LogError("Exception on loading configuration: " + ex.InnerException);
            }
        }

        private float[] MenuPosParser(string menuPos) => Array.ConvertAll(menuPos.Replace("X", "").Replace("Y", "").Split(' '), float.Parse);

        public static ConfigEntry<bool> configchatCensorshipBypass;
        public static ConfigEntry<bool> configAutoGG;
        public static ConfigEntry<bool> configRichText;
        public static ConfigEntry<bool> configTranslation;
        public static ConfigEntry<bool> configWinStreakLog;
        public static ConfigEntry<bool> configNoResize;
        public static ConfigEntry<bool> configHPWinner;
        public static ConfigEntry<Color> configCustomColor;
        public static ConfigEntry<string> configAuthKeyForTranslation;
        //public static ConfigEntry<string> configCustomName;
        public static ConfigEntry<KeyboardShortcut> configQOLMenuKeybind;
        public static ConfigEntry<KeyboardShortcut> configStatMenuKeybind;
        public static ConfigEntry<string> configWinStreakColors;
        public static ConfigEntry<string> configWinStreakRanges;
        public static ConfigEntry<int> configWinStreakFontsize;
        public static ConfigEntry<string> configAdvCmd;
        public static ConfigEntry<string> configEmoji;
        public static ConfigEntry<string> configQOLMenuPlacement;
        public static ConfigEntry<string> configStatMenuPlacement;


        public const string VersionNumber = "1.0.13"; // Version number
    }
}
