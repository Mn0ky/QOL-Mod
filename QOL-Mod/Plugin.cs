using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SimpleJSON;
using TMPro;
using UnityEngine;

namespace QOL
{
    [BepInPlugin(Guid, "QOL Mod", VersionNumber)]
    [BepInProcess("StickFight.exe")]    
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo("Plugin " + Guid + " is loaded! [v" + VersionNumber + "]");
            try
            {
                Harmony harmony = new ("monky.QOL"); // Creates harmony instance with identifier
                Logger.LogInfo("Applying ChatManager patches...");
                ChatManagerPatches.Patches(harmony);
                Logger.LogInfo("Applying MatchmakingHandler patch..."); 
                MatchmakingHandlerPatches.Patch(harmony);
                Logger.LogInfo("Applying MultiplayerManager patches...");
                MultiplayerManagerPatches.Patches(harmony);
                Logger.LogInfo("Applying NetworkPlayer patch...");
                NetworkPlayerPatch.Patch(harmony);
                Logger.LogInfo("Applying Controller patches..."); 
                ControllerPatches.Patch(harmony);
                Logger.LogInfo("Applying GameManager patch...");
                GameManagerPatches.Patch(harmony);
                Logger.LogInfo("Applying OnlinePlayerUI patch...");
                OnlinePlayerUIPatch.Patch(harmony);
                Logger.LogInfo("Applying P2PPackageHandler patch...");
                P2PPackageHandlerPatch.Patch(harmony);
                Logger.LogInfo("Applying CharacterInformation patch...");
                CharacterInformationPatch.Patch(harmony);
                Logger.LogInfo("Applying BossTimer patch...");
                BossTimerPatch.Patch(harmony);
                Logger.LogInfo("Applying MusicHandlerPatch...");
                MusicHandlerPatch.Patch(harmony);
                Logger.LogInfo("Applying MovementPatch...");
                MovementPatch.Patch(harmony);
            }
            catch (Exception ex)
            {
                Logger.LogError("Exception on applying patches: " + ex.InnerException);
            }

            try
            {
                Logger.LogInfo("Loading configuration options from config file...");

                ConfigCustomColor = Config.Bind("Player Color Options",
                    "CustomColor",
                    new Color(1, 1, 1),
                    "Specify a custom player color? (Use a HEX value)");

                ConfigCustomColorOnParticle = Config.Bind("Player Color Options",
                    "CustomColorOnParticle",
                    false,
                    "Apply your custom color for even your walking, jumping, and punching particles?");

                ConfigRainbowSpeed = Config.Bind("Player Color Options",
                    "RainbowSpeed",
                    0.05f,
                    "Change the speed of the color shifting in rainbow mode (/rainbow)?");

                ConfigAlwaysRainbow = Config.Bind("Player Color Options",
                    "RainbowEnabled",
                    false,
                    "Start with rainbow mode enabled?");

                ConfigDefaultColors = Config.Bind("Player Color Options",
                    "DefaultPlayerColors",
                    "D88C47 5573AD D6554D 578B49",
                    "Change the default player colors? (Order is: Yellow, Blue, Red, and then Green)");

                ConfigCustomCrownColor = Config.Bind("Player Color Options",
                    "CustomCrownColor",
                    (Color) new Color32(255, 196, 68, 255), // #FFC444FF
                    "Change the default crown (for when a player wins a match) color? (Use a HEX value)");

                var spacedColors = ConfigDefaultColors.Value.Split(' ');
                
                for (var index = 0; index < spacedColors.Length; index++)
                {
                    ColorUtility.TryParseHtmlString(spacedColors[index].Insert(0, "#"), out var convColor);
                    DefaultColors[index] = convColor;
                }

                ConfigQolMenuKeybind = Config.Bind("Menu Options", // The section under which the option is shown
                    "QOLMenuKeybind",
                    new KeyboardShortcut(KeyCode.LeftShift, KeyCode.F1), // The key of the configuration option in the configuration file
                    "Change the keybind for opening the QOL Menu? Only specify a single key or two keys. All keycodes can be found at the bottom of the page here: https://docs.unity3d.com/ScriptReference/KeyCode.html"); // Description of the option to show in the config file

                ConfigStatMenuKeybind = Config.Bind("Menu Options",
                    "StatWindowKeybind",
                    new KeyboardShortcut(KeyCode.LeftShift, KeyCode.F2),
                    "Change the keybind for opening the Stat Window? Only specify a single key or two keys. All keycodes can be found at the bottom of the page here: https://docs.unity3d.com/ScriptReference/KeyCode.html");

                ConfigQOLMenuPlacement = Config.Bind("Menu Options",
                    "QOLMenuLocation",
                    "0X 100Y",
                    "Change the default opening position of the QOL menu?");

                ConfigStatMenuPlacement = Config.Bind("Menu Options",
                    "StatMenuLocation",
                    "800X 100Y",
                    "Change the default opening position of the Stat menu?");

                GUIManager.QolMenuPos = MenuPosParser(ConfigQOLMenuPlacement.Value);
                GUIManager.StatMenuPos = MenuPosParser(ConfigStatMenuPlacement.Value);

                ConfigWinStreakLog = Config.Bind("Winstreak Options",
                    "AlwaysTrackWinstreak",
                    false,
                    "Always keep track of your winstreak instead of only when enabled?");

                ConfigWinStreakFontsize = Config.Bind("Winstreak Options",
                    "WinstreakFontsize",
                    200,
                    "Change the fontsize of your winstreak message?");

                ConfigWinStreakColors = Config.Bind("Winstreak Options",
                    "WinstreakColors",
                    "FF0000 FFEB04 00FF00",
                    "Change the default winstreak colors? HEX values only, space separated. Each color will show in the order of the ranges below. The number of colors should be equal to the number of ranges! Max 50.");

                foreach (var colorStr in ConfigWinStreakColors.Value.Split(' '))
                {
                    ColorUtility.TryParseHtmlString('#' + colorStr, out var color);
                    GameManagerPatches.WinstreakColors1.Add(color);
                }
                
                GameManagerPatches.WinstreakColors2 = new List<Color>(GameManagerPatches.WinstreakColors1);

                ConfigWinStreakRanges = Config.Bind("Winstreak Options",
                    "WinstreakRanges",
                    "0-1 1-2 2-3",
                    "Change the default ranges? Add more ranges, space separated, to support more colors. Once the last range is reached the corresponding color will be used for all subsequent wins until the streak is lost. Max 50.");

                foreach (var streakRange in ConfigWinStreakRanges.Value.Split(' '))
                {
                    Logger.LogInfo("range: " + streakRange);
                    var streakRangeNums = Array.ConvertAll(streakRange.Split('-'), int.Parse);
                    Logger.LogInfo("nums: " + streakRangeNums[0] + " " + streakRangeNums[1]);
                    
                    GameManagerPatches.WinstreakRanges1.Add(streakRangeNums[1] - streakRangeNums[0]);
                }
                
                GameManagerPatches.WinstreakRanges2 = new List<int>(GameManagerPatches.WinstreakRanges1);

                ConfigAutoGG = Config.Bind("On-Startup Options", // The section under which the option is shown
                    "AutoGG",
                    false, // The key of the configuration option in the configuration file
                    "Enable AutoGG on startup?"); // Description of the option to show in the config file
                
                ConfigAllOutputPublic = Config.Bind("On-Startup Options",
                    "AlwaysPublicOutput",
                    false,
                    "Enable AlwaysPublicOutput on start-up, where all mod logs in chat are no longer client-side?");

                ConfigchatCensorshipBypass = Config.Bind("On-Startup Options",
                    "ChatCensorshipBypass",
                    false,
                    "Disable chat censorship on startup?");

                ConfigRichText = Config.Bind("On-Startup Options",
                    "RichText",
                    true,
                    "Enable rich text for chat on startup?");

                ConfigAdvCmd = Config.Bind("Misc. Options",
                    "AdvertiseMsg",
                    "",
                    "Modify the output of /adv? By default the message is blank but can be changed to anything.");

                ConfigMsgDuration = Config.Bind("Misc. Options",
                    "MsgDuration",
                    0.0f,
                    "Extend the amount of seconds per chat message by a specified amount? (Decimals allowed)");

                ConfigTranslation = Config.Bind("On-Startup Options",
                    "AutoTranslations",
                    false,
                    "Enable auto-translation for chat messages to English on startup?");

                ConfigNoResize = Config.Bind("Misc. Options",
                    "ResizeName",
                    true,
                    "Auto-resize a player's name if it's over 12 characters? (This provides large name support)");

                ConfigCustomName = Config.Bind("Misc. Options",
                    "CustomUsername",
                    "",
                    "Specify a custom username? (client-side only)");

                ConfigFixCrownWinCount = Config.Bind("Misc. Options",
                    "FixCrownTxt",
                    true,
                    "Auto-resize win counter font so wins into the hundreds/thousands display properly?");

                ConfigOuchPhrases = Config.Bind("Misc. Options",
                    "OuchPhrases",
                    "ow owie ouch ouchie",
                    "Words to be used by OuchMode? Space seperated. (/ouch)");

                ConfigHPWinner = Config.Bind("On-Startup Options",
                    "AlwaysShowHPOfWinner",
                    false,
                    "Enable always show the HP for the winner of the round on-startup?");

                ConfigEmoji = Config.Bind("Misc. Options",
                    "ShrugEmoji",
                    "☹",
                    "Specify the emoji used in the shrug command? Only the following 15 TMP defaults are available: 😋, 😍, 😎, 😀, 😁, 😂, 😃, 😄, 😅, 😆, 😉, 😘, 🤣, ☺, ☹");

                ConfigAuthKeyForTranslation = Config.Bind("Misc. Options",
                    "AutoAuthTranslationsAPIKey",
                    "",
                    "Put your API key for Google Translate V2 here (Optional)");
            }
            catch (Exception ex)
            {   
                Logger.LogError("Exception on loading configuration: " + ex.StackTrace + ex.Message + ex.Source +
                                ex.InnerException);
            }
            
            InitModText();

            // Loading music from folder
            if (!Directory.Exists(MusicPath))
            {
                Directory.CreateDirectory(MusicPath);
                File.WriteAllText(MusicPath + "README.txt", "Only WAV and OGG audio files are supported.\n" +
                                                            "For MP3 and other types, please convert them first!");
            }

            // Loading highscore from txt
            if (StatsFileExists)
            {
                GameManagerPatches.WinstreakHighScore = JSONNode.Parse(File.ReadAllText(StatsPath))["winstreakHighscore"];
                Debug.Log("Loading winstreak highscore of: " + GameManagerPatches.WinstreakHighScore);
            }
            else
                GameManagerPatches.WinstreakHighScore = 0;

            if (File.Exists(CmdVisibilityStatesPath))
                foreach (var pair in JSONNode.Parse(File.ReadAllText(CmdVisibilityStatesPath)))
                    ChatCommands.CmdOutputVisibility[pair.Key] = pair.Value;
        }

        public static void InitModText()
        {
            var modText = new GameObject("ModText");
            modText.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var modTextTMP = modText.AddComponent<TextMeshProUGUI>();

            modTextTMP.text = "<color=red>Monky's QOL Mod</color> " + "<color=white>v" + VersionNumber;
            modTextTMP.fontSize = 25;
            modTextTMP.color = Color.red;
            modTextTMP.fontStyle = FontStyles.Bold;
            modTextTMP.alignment = TextAlignmentOptions.TopRight;
            modTextTMP.richText = true;
        }

        private static float[] MenuPosParser(string menuPos)
            => Array.ConvertAll(menuPos
                .Replace("X", "")
                .Replace("Y", "")
                .Split(' '), float.Parse);

        public static ConfigEntry<bool> ConfigchatCensorshipBypass;
        public static ConfigEntry<bool> ConfigAutoGG;
        public static ConfigEntry<bool> ConfigAllOutputPublic;
        public static ConfigEntry<bool> ConfigRichText;
        public static ConfigEntry<bool> ConfigTranslation;
        public static ConfigEntry<bool> ConfigWinStreakLog;
        public static ConfigEntry<bool> ConfigNoResize;
        public static ConfigEntry<bool> ConfigHPWinner;
        public static ConfigEntry<Color> ConfigCustomColor;
        public static ConfigEntry<Color> ConfigCustomCrownColor;
        public static ConfigEntry<bool> ConfigCustomColorOnParticle;
        public static ConfigEntry<string> ConfigAuthKeyForTranslation;
        public static ConfigEntry<string> ConfigCustomName;
        public static ConfigEntry<KeyboardShortcut> ConfigQolMenuKeybind;
        public static ConfigEntry<KeyboardShortcut> ConfigStatMenuKeybind;
        public static ConfigEntry<string> ConfigDefaultColors;
        public static ConfigEntry<string> ConfigWinStreakColors;
        public static ConfigEntry<string> ConfigWinStreakRanges;
        public static ConfigEntry<int> ConfigWinStreakFontsize;
        public static ConfigEntry<float> ConfigRainbowSpeed;
        public static ConfigEntry<float> ConfigMsgDuration;
        public static ConfigEntry<string> ConfigAdvCmd;
        public static ConfigEntry<string> ConfigEmoji;
        public static ConfigEntry<string> ConfigQOLMenuPlacement;
        public static ConfigEntry<string> ConfigStatMenuPlacement;
        public static ConfigEntry<string> ConfigOuchPhrases;
        public static ConfigEntry<bool> ConfigFixCrownWinCount;
        public static ConfigEntry<bool> ConfigAlwaysRainbow;

        public static Color[] DefaultColors = new Color[4];

        public const string VersionNumber = "1.16.0"; // Version number
        public const string Guid = "monky.plugins.QOL";
        public static string NewUpdateVerCode = "";
        
        public static readonly string MusicPath = Paths.PluginPath + "\\QOL-Mod\\Music\\";
        public static readonly string StatsPath = Paths.PluginPath + "\\QOL-Mod\\StatsData.json";
        public static readonly string CmdVisibilityStatesPath = Paths.PluginPath + "\\QOL-Mod\\CmdVisibilityStates.json";

        public static bool StatsFileExists = File.Exists(StatsPath);
    }
}
