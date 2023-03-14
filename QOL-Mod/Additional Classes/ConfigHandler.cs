using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QOL;

public static class ConfigHandler
{
    private static readonly Dictionary<string, ConfigEntryBase> EntriesDict = new(StringComparer.InvariantCultureIgnoreCase);
    
    // Config sections
    private const string MenuSec = "Menu Options";
    private const string MiscSec = "Misc. Options";
    private const string OnstartupSec = "On-Startup Options";
    private const string PlayerColorSec = "Player Color Options";
    private const string WinstreakSec = "Winstreak Options";

    public static bool AllOutputPublic { get; set; }
    
    // Properties that need to be updated whenever their respective config entry is modified
    public static bool IsCustomPlayerColor { get; private set; }
    public static bool IsCustomName { get; private set; }
    public static string CustomName { get; private set; }
    public static Color[] DefaultColors { get; } = new Color[4];
    public static string[] OuchPhrases { get; private set; }
    public static string[] DeathPhrases { get; private set; }
    

    public static void InitializeConfig(ConfigFile config)
    {
        var customColorEntry = config.Bind(PlayerColorSec,
            "CustomColor",
            new Color(1, 1, 1),
            "Specify a custom player color? (Use a HEX value)");

        var customColorEntryKey = customColorEntry.Definition.Key;
        EntriesDict[customColorEntryKey] = customColorEntry;
        
        customColorEntry.SettingChanged += (_, _) =>
        {
            var newColor = customColorEntry.Value;
            var defaultValue = (Color) customColorEntry.DefaultValue;
                
            IsCustomPlayerColor = newColor != defaultValue;
            var localUser = Helper.localNetworkPlayer;
            
            if (IsCustomPlayerColor)
            {
                MultiplayerManagerPatches.ChangeAllCharacterColors(newColor, localUser.gameObject);
                return;
            }
            
            MultiplayerManagerPatches.ChangeAllCharacterColors(DefaultColors[localUser.NetworkSpawnID], localUser.gameObject);
        };

        EntriesDict["CustomColorOnParticle"] = config.Bind(PlayerColorSec,
        "CustomColorOnParticle",
        false,
        "Apply your custom color for even your walking, jumping, and punching particles?");

        EntriesDict["RainbowSpeed"] = config.Bind(PlayerColorSec,
            "RainbowSpeed",
            0.05f,
            "Change the speed of the color shifting in rainbow mode (/rainbow)?");

        EntriesDict["RainbowStartup"] = config.Bind(PlayerColorSec,
            "RainbowStartup",
            false,
            "Start with rainbow mode enabled?");

        var defaultPlayerColorsEntry = config.Bind(PlayerColorSec,
            "DefaultPlayerColors",
            "D88C47 5573AD D6554D 578B49",
            "Change the default player colors? (Order is: Yellow, Blue, Red, and then Green)");

        var defaultPlayerColorsEntryKey = defaultPlayerColorsEntry.Definition.Key;
        EntriesDict[defaultPlayerColorsEntryKey] = defaultPlayerColorsEntry;

        defaultPlayerColorsEntry.SettingChanged += (_, _) =>
        {
            UpdateDefaultPlayerColors(defaultPlayerColorsEntryKey);
            var userID = Helper.localNetworkPlayer.NetworkSpawnID;
            
            for (var index = 0; index < DefaultColors.Length; index++)
            {
                var player = GameManager.Instance.mMultiplayerManager.PlayerControllers[index];
                
                if (player != null && (player.playerID != userID || !IsCustomPlayerColor)) 
                    MultiplayerManagerPatches.ChangeAllCharacterColors(DefaultColors[index], player.gameObject);
            }
        };
        UpdateDefaultPlayerColors(defaultPlayerColorsEntryKey);

        var customCrownColorEntry = config.Bind(PlayerColorSec,
            "CustomCrownColor",
            (Color) new Color32(255, 196, 68, 255), // #FFC444FF
            "Change the default crown (for when a player wins a match) color? (Use a HEX value)");

        var customCrownColorEntryKey = customCrownColorEntry.Definition.Key;
        EntriesDict[customCrownColorEntryKey] = customCrownColorEntry;

        customCrownColorEntry.SettingChanged += (_, _) =>
        {
            var customCrownColor = customCrownColorEntry.Value;
            var defaultValue = (Color) customCrownColorEntry.DefaultValue;

            if (customCrownColor == defaultValue) return;
            
            var crown = UnityEngine.Object.FindObjectOfType<Crown>().gameObject;
            foreach (var sprite in crown.GetComponentsInChildren<SpriteRenderer>(true)) 
                sprite.color = customCrownColor;
            
            var winCounters = UnityEngine.Object.FindObjectOfType<WinCounterUI>();
            foreach (var crownCount in winCounters.GetComponentsInChildren<TextMeshProUGUI>(true))
                crownCount.GetComponentInChildren<Image>().color = customCrownColor;
        };

        var qolMenuKeybindEntry = config.Bind(MenuSec, 
            "QOLMenuKeybind",
            new KeyboardShortcut(KeyCode.LeftShift, KeyCode.F1),
            "Change the keybind for opening the QOL Menu? Only specify a single key or two keys. " +
            "All keycodes can be found at the bottom of the page here: " +
            "https://docs.unity3d.com/ScriptReference/KeyCode.html");

        var qolMenuKeybindEntryKey = qolMenuKeybindEntry.Definition.Key;
        EntriesDict[qolMenuKeybindEntryKey] = qolMenuKeybindEntry;

        qolMenuKeybindEntry.SettingChanged += (_, _) =>
        {
            var shortcut = qolMenuKeybindEntry.Value;
            var guiManager = GUIManager.Instance;
            
            guiManager.qolMenuKey1 = shortcut.MainKey;
            guiManager.qolMenuKey2 = shortcut.Modifiers.LastOrDefault();
            
            guiManager.singleMenuKey = guiManager.qolMenuKey2 == KeyCode.None;
        };

        var statWindowKeybindEntry = config.Bind(MenuSec,
            "StatWindowKeybind",
            new KeyboardShortcut(KeyCode.LeftShift, KeyCode.F2),
            "Change the keybind for opening the Stat Window? Only specify a single key or two keys. " +
            "All keycodes can be found at the bottom of the page here: " +
            "https://docs.unity3d.com/ScriptReference/KeyCode.html");

        var statWindowKeybindEntryKey = statWindowKeybindEntry.Definition.Key;
        EntriesDict[statWindowKeybindEntryKey] = statWindowKeybindEntry;
        
        statWindowKeybindEntry.SettingChanged += (_, _) =>
        {
            var shortcut = statWindowKeybindEntry.Value;
            var guiManager = GUIManager.Instance;
            
            guiManager.statWindowKey1 = shortcut.MainKey;
            guiManager.statWindowKey2 = shortcut.Modifiers.LastOrDefault();

            guiManager.singleStatKey = guiManager.statWindowKey2 == KeyCode.None;
        };

        var qolMenuCoordsEntry = config.Bind(MenuSec,
            "QOLMenuCoords",
            "0X 100Y",
            "Change the default opening position of the QOL menu?");

        var qolMenuCoordsEntryKey = qolMenuCoordsEntry.Definition.Key;
        EntriesDict[qolMenuCoordsEntryKey] = qolMenuCoordsEntry;

        qolMenuCoordsEntry.SettingChanged += (_, _) =>
        {
            var newCoords = MenuPosParser(qolMenuCoordsEntry.Value);
            GUIManager.QolMenuPos = newCoords;
            var guiManager = GUIManager.Instance;
            
            guiManager.menuRect.x = newCoords[0];
            guiManager.menuRect.y = newCoords[1];
        }; 

        var statMenuCoordsEntry = config.Bind(MenuSec,
            "StatMenuCoords",
            "800X 100Y",
            "Change the default opening position of the Stat menu?");

        var statMenuCoordsEntryKey = statMenuCoordsEntry.Definition.Key;
        EntriesDict[statMenuCoordsEntryKey] = statMenuCoordsEntry;
        
        statMenuCoordsEntry.SettingChanged += (_, _) =>
        {
            var newCoords = MenuPosParser(statMenuCoordsEntry.Value);
            GUIManager.StatMenuPos = newCoords;
            var guiManager = GUIManager.Instance;

            guiManager.statMenuRect.x = newCoords[0];
            guiManager.statMenuRect.y = newCoords[1];
            guiManager.globalStatMenuRect.x = newCoords[0];
            guiManager.globalStatMenuRect.y = newCoords[1];
        };

        GUIManager.QolMenuPos = MenuPosParser(qolMenuCoordsEntry.Value);
        GUIManager.StatMenuPos = MenuPosParser(statMenuCoordsEntry.Value);

        EntriesDict["WinstreakStartup"] = config.Bind(WinstreakSec,
            "WinstreakStartup",
            false,
            "Always keep track of your winstreak instead of only when enabled?");

        EntriesDict["WinstreakFontsize"] = config.Bind(WinstreakSec,
            "WinstreakFontsize",
            200,
            "Change the fontsize of your winstreak message?");

        var winstreakColorsEntry = config.Bind(WinstreakSec,
            "WinstreakColors",
            "FF0000 FFEB04 00FF00",
            "Change the default winstreak colors? HEX values only, space separated. " 
            + "Each color will show in the order of the ranges below. " + "The number of colors should be equal " 
            + "to the number of ranges! Max 50.");

        var winstreakColorsEntryKey = winstreakColorsEntry.Definition.Key;
        EntriesDict[winstreakColorsEntryKey] = winstreakColorsEntry;

        winstreakColorsEntry.SettingChanged += (_, _) => UpdateWinstreakColors(winstreakColorsEntryKey);
        UpdateWinstreakColors(winstreakColorsEntryKey);

        var winstreakRangesEntry = config.Bind(WinstreakSec,
            "WinstreakRanges",
            "0-1 1-2 2-3",
            "Change the default ranges? Add more ranges, space separated, to support more colors. " 
            + "Once the last range is reached the corresponding color will be used for all subsequent wins " 
            + "until the streak is lost. Max 50.");

        var winstreakRangesEntryKey = winstreakRangesEntry.Definition.Key;
        EntriesDict[winstreakRangesEntryKey] = winstreakRangesEntry;
        
        winstreakRangesEntry.SettingChanged += (_, _) => UpdateWinstreakRanges(winstreakRangesEntryKey);
        UpdateWinstreakRanges(winstreakRangesEntryKey);

        EntriesDict["GGStartup"] = config.Bind(OnstartupSec, // The section under which the option is shown
            "GGStartup",
            false, // The key of the configuration option in the configuration file
            "Enable AutoGG on startup?"); // Description of the option to show in the config file
            
        EntriesDict["AlwaysPublicOutput"] = config.Bind(OnstartupSec,
            "AlwaysPublicOutput",
            false,
            "Enable AlwaysPublicOutput on start-up, where all mod logs in chat are no longer client-side?");

        EntriesDict["UncensorStartup"] = config.Bind(OnstartupSec,
            "UncensorStartup",
            false,
            "Disable chat censorship on startup?");

        EntriesDict["RichtextStartup"] = config.Bind(OnstartupSec,
            "RichtextStartup",
            true,
            "Enable rich text for chat on startup?");
            
        EntriesDict["TranslateStartup"] = config.Bind(OnstartupSec,
            "TranslateStartup",
            false,
            "Enable auto-translation for chat messages to English on startup?");
            
        EntriesDict["WinnerHPStartup"] = config.Bind(OnstartupSec,
            "WinnerHPStartup",
            false,
            "Enable always show the HP for the winner of the round on-startup?");
            
        var cmdPrefixEntry = config.Bind(MiscSec,
            "CommandPrefix",
            "/", 
            "Change the default command prefix character? (Recommended: /, !, $, ., &, ?, ~)");

        var cmdPrefixEntryKey = cmdPrefixEntry.Definition.Key;
        EntriesDict[cmdPrefixEntryKey] = cmdPrefixEntry;

        cmdPrefixEntry.SettingChanged += (_, _) =>
        {
            Command.CmdPrefix = cmdPrefixEntry.Value.Length == 1
                ? cmdPrefixEntry.Value[0]
                : '/';

            var cmdNames = ChatCommands.CmdNames;
            
            for (var i = 0; i < cmdNames.Count; i++) 
                cmdNames[i] = Command.CmdPrefix + cmdNames[i].Substring(1);
        };

        EntriesDict["AdvertiseMsg"] = config.Bind(MiscSec,
            "AdvertiseMsg",
            "",
            "Modify the output of /adv? By default the message is blank but can be changed to anything.");

        var deathPhrasesEntry = config.Bind(MiscSec, 
            "DeathMsgs",
            "Me dead >:",
            "Add a custom messages to send out upon your death? Add multiple values by separating them with commas.");
        
        var deathPhrasesEntryKey = deathPhrasesEntry.Definition.Key;
        EntriesDict[deathPhrasesEntryKey] = deathPhrasesEntry;
        
        deathPhrasesEntry.SettingChanged += (_, _) => DeathPhrases = ParseCommaSepPhrases(deathPhrasesEntryKey);

        EntriesDict["MsgDuration"] = config.Bind(MiscSec,
            "MsgDuration",
            0.0f,
            "Extend the amount of seconds per chat message by a specified amount? (Decimals allowed)");

        EntriesDict["ResizeName"] = config.Bind(MiscSec,
            "ResizeName",
            true,
            "Auto-resize a player's name if it's over 12 characters? (This provides large name support)");

        var customUsernameEntry = config.Bind(MiscSec,
            "CustomUsername",
            "",
            "Specify a custom username? (client-side only)");

        var customUsernameEntryKey = customUsernameEntry.Definition.Key;
        EntriesDict[customUsernameEntryKey] = customUsernameEntry;
        
        customUsernameEntry.SettingChanged += (_, _) =>
        {
            CustomName = customUsernameEntry.Value;
            IsCustomName = !string.IsNullOrEmpty(CustomName);
        };

        EntriesDict["FixCrownTxt"] = config.Bind(MiscSec,
            "FixCrownTxt",
            true,
            "Auto-resize win counter font so wins into the hundreds/thousands display properly?");

        var ouchPhrasesEntry = config.Bind(MiscSec,
            "OuchPhrases",
            "ow, owie, ouch, ouchie, much pain",
            "Words to be used by OuchMode? Comma seperated. (/ouch)");

        var ouchPhrasesEntryKey = ouchPhrasesEntry.Definition.Key;
        EntriesDict[ouchPhrasesEntryKey] = ouchPhrasesEntry;
        
        ouchPhrasesEntry.SettingChanged += (_, _) => OuchPhrases = ParseCommaSepPhrases(ouchPhrasesEntryKey);

        EntriesDict["ShrugEmoji"] = config.Bind(MiscSec,
            "ShrugEmoji",
            "☹",
            "Specify the emoji used in the shrug command? Only the following 15 TMP defaults are available: " 
            + "😋, 😍, 😎, 😀, 😁, 😂, 😃, 😄, 😅, 😆, 😉, 😘, 🤣, ☺, ☹");

        EntriesDict["AutoAuthTranslationsAPIKey"] = config.Bind(MiscSec,
            "AutoAuthTranslationsAPIKey",
            "",
            "Put your API key for Google Translate V2 here (Optional)");

        AllOutputPublic = GetEntry<bool>("AlwaysPublicOutput"); // Does not trigger onChanged method when modified
        
        // Values that need to be parsed and initialized now
        DeathPhrases = ParseCommaSepPhrases(deathPhrasesEntryKey);
        OuchPhrases = ParseCommaSepPhrases(ouchPhrasesEntryKey);
        IsCustomPlayerColor = customColorEntry.Value != (Color)customColorEntry.DefaultValue;
        IsCustomName = !string.IsNullOrEmpty(customUsernameEntry.Value);
        CustomName = customUsernameEntry.Value;
    }

    public static T GetEntry<T>(string entryKey, bool defaultValue = false) 
        => defaultValue ? (T)EntriesDict[entryKey].DefaultValue : (T)EntriesDict[entryKey].BoxedValue;

    public static void ModifyEntry(string entryKey, string value) 
        => EntriesDict[entryKey].SetSerializedValue(value);

    public static void ResetEntry(string entryKey)
    {
        var configEntry = EntriesDict[entryKey];
        configEntry.BoxedValue = configEntry.DefaultValue;
    }

    public static bool EntryExists(string entryKey)
        => EntriesDict.ContainsKey(entryKey);

    public static string[] GetConfigKeys() => EntriesDict.Keys.ToArray();
    
    // ****************************************************************************************************
    //                        All OnChanged methods below for certain config entries                                     
    // ****************************************************************************************************

    private static float[] MenuPosParser(string menuPos) 
        => Array.ConvertAll(menuPos.ToLower()
                .Replace("x", "")
                .Replace("y", "")
                .Split(' '), float.Parse);

    private static string[] ParseCommaSepPhrases(string entryKey)
    {
        var phrases = GetEntry<string>(entryKey).Split(new []{", "}, StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < phrases.Length; index++)
        {
            var phrase = phrases[index];
            phrases[index] = phrase.Replace(",,", ",");
        }

        return phrases;
    }

    private static void UpdateWinstreakRanges(string entryKey)
    {
        var colorIndex = 0;
        foreach (var streakRange in GetEntry<string>(entryKey).Split(' '))
        {
            var streakRangeNums = Array.ConvertAll(streakRange.Split('-'), int.Parse);
            var nTimes = streakRangeNums[1] - streakRangeNums[0];

            GameManagerPatches.WinstreakRanges[colorIndex] = (byte)nTimes;
            colorIndex++;
        }
        
        Array.Copy(GameManagerPatches.WinstreakRanges, 0, GameManagerPatches.WinstreakRangesDefault, 0, 50);
    }
    
    private static void UpdateWinstreakColors(string entryKey)
    {
        foreach (var colorStr in GetEntry<string>(entryKey).Split(' '))
        {
            ColorUtility.TryParseHtmlString('#' + colorStr, out var color);
            GameManagerPatches.WinstreakColors.Add(color);
        }
    }

    private static void UpdateDefaultPlayerColors(string entryKey)
    {
        var spacedColors = GetEntry<string>(entryKey).Split(' ');
                
        for (var index = 0; index < spacedColors.Length; index++)
        {
            ColorUtility.TryParseHtmlString("#" + spacedColors[index], out var convColor);
            DefaultColors[index] = convColor;
        }
    }
}