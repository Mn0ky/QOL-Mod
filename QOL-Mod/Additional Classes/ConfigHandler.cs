using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QOL;

public static class ConfigHandler
{
    private static readonly Dictionary<string, ConfigEntryBase> EntriesDict = new(StringComparer.InvariantCultureIgnoreCase);
    
    public static bool AllOutputPublic { get; set; }
    
    // Properties that need to be updated whenever their respective config entry is modified
    public static bool IsCustomPlayerColor { get; private set; }
    public static bool IsCustomName { get; private set; }
    public static string CustomName { get; private set; }
    public static Color[] DefaultColors { get; private set; } = new Color[4];
    public static string[] OuchPhrases { get; private set; }

    private static ConfigFile _config;

    public static void InitializeConfig(ConfigFile config)
    {
        _config = config;
        
        var customColorEntry = _config.Bind("Player Color Options",
            "CustomColor",
            new Color(1, 1, 1),
            "Specify a custom player color? (Use a HEX value)");
        
        EntriesDict[customColorEntry.Definition.Key] = customColorEntry;
        
        customColorEntry.SettingChanged += (_, _) =>
        {
            var entryKey = customColorEntry.Definition.Key;
            var newColor = GetEntry<Color>(entryKey);
            var defaultValue = GetEntry<Color>(entryKey, true);
                
            IsCustomPlayerColor = newColor != defaultValue;
            var localUser = Helper.localNetworkPlayer;
            
            if (IsCustomPlayerColor)
            {
                MultiplayerManagerPatches.ChangeAllCharacterColors(newColor, localUser.gameObject);
                return;
            }
            
            MultiplayerManagerPatches.ChangeAllCharacterColors(DefaultColors[localUser.NetworkSpawnID], localUser.gameObject);
        };

        EntriesDict["CustomColorOnParticle"] = _config.Bind("Player Color Options",
        "CustomColorOnParticle",
        false,
        "Apply your custom color for even your walking, jumping, and punching particles?");

        EntriesDict["RainbowSpeed"] = _config.Bind("Player Color Options",
            "RainbowSpeed",
            0.05f,
            "Change the speed of the color shifting in rainbow mode (/rainbow)?");

        EntriesDict["RainbowEnabled"] = _config.Bind("Player Color Options",
            "RainbowEnabled",
            false,
            "Start with rainbow mode enabled?");

        var defaultPlayerColorsEntry = _config.Bind("Player Color Options",
            "DefaultPlayerColors",
            "D88C47 5573AD D6554D 578B49",
            "Change the default player colors? (Order is: Yellow, Blue, Red, and then Green)");

        EntriesDict[defaultPlayerColorsEntry.Definition.Key] = defaultPlayerColorsEntry;

        defaultPlayerColorsEntry.SettingChanged += (_, _) =>
        {
            UpdateDefaultPlayerColors();
            var userID = Helper.localNetworkPlayer.NetworkSpawnID;
            
            for (var index = 0; index < DefaultColors.Length; index++)
            {
                var player = GameManager.Instance.mMultiplayerManager.PlayerControllers[index];
                
                if (player != null && (player.playerID != userID || !IsCustomPlayerColor)) 
                    MultiplayerManagerPatches.ChangeAllCharacterColors(DefaultColors[index], player.gameObject);
            }
        };
        
        UpdateDefaultPlayerColors();

        var customCrownColorEntry = _config.Bind("Player Color Options",
            "CustomCrownColor",
            (Color) new Color32(255, 196, 68, 255), // #FFC444FF
            "Change the default crown (for when a player wins a match) color? (Use a HEX value)");

        EntriesDict[customCrownColorEntry.Definition.Key] = customCrownColorEntry;

        customCrownColorEntry.SettingChanged += (_, _) =>
        {
            var entryKey = customCrownColorEntry.Definition.Key;
            var customCrownColor = GetEntry<Color>(entryKey);
            var defaultValue = GetEntry<Color>(entryKey, true);

            if (customCrownColor == defaultValue) return;
            
            var crown = UnityEngine.Object.FindObjectOfType<Crown>().gameObject;
            foreach (var sprite in crown.GetComponentsInChildren<SpriteRenderer>(true)) 
                sprite.color = customCrownColor;
            
            var winCounters = UnityEngine.Object.FindObjectOfType<WinCounterUI>();
            foreach (var crownCount in winCounters.GetComponentsInChildren<TextMeshProUGUI>(true))
                crownCount.GetComponentInChildren<Image>().color = customCrownColor;
        };

        EntriesDict["QOLMenuKeybind"] = _config.Bind("Menu Options", // The section under which the option is shown
            "QOLMenuKeybind",
            new KeyboardShortcut(KeyCode.LeftShift, KeyCode.F1), // The key of the configuration option in the configuration file
            "Change the keybind for opening the QOL Menu? Only specify a single key or two keys. " +
            "All keycodes can be found at the bottom of the page here: " +
            "https://docs.unity3d.com/ScriptReference/KeyCode.html"); // Description of the option to show in the config file

        EntriesDict["StatWindowKeybind"] = _config.Bind("Menu Options",
            "StatWindowKeybind",
            new KeyboardShortcut(KeyCode.LeftShift, KeyCode.F2),
            "Change the keybind for opening the Stat Window? Only specify a single key or two keys. " +
            "All keycodes can be found at the bottom of the page here: " +
            "https://docs.unity3d.com/ScriptReference/KeyCode.html");

        EntriesDict["QOLMenuLocation"] = _config.Bind("Menu Options",
            "QOLMenuLocation",
            "0X 100Y",
            "Change the default opening position of the QOL menu?");

        EntriesDict["StatMenuLocation"] = _config.Bind("Menu Options",
            "StatMenuLocation",
            "800X 100Y",
            "Change the default opening position of the Stat menu?");

        GUIManager.QolMenuPos = MenuPosParser(GetEntry<string>("QOLMenuLocation"));
        GUIManager.StatMenuPos = MenuPosParser(GetEntry<string>("StatMenuLocation"));

        EntriesDict["AlwaysTrackWinstreak"] = _config.Bind("Winstreak Options",
            "AlwaysTrackWinstreak",
            false,
            "Always keep track of your winstreak instead of only when enabled?");

        EntriesDict["WinstreakFontsize"] = _config.Bind("Winstreak Options",
            "WinstreakFontsize",
            200,
            "Change the fontsize of your winstreak message?");

        EntriesDict["WinstreakColors"] = _config.Bind("Winstreak Options",
            "WinstreakColors",
            "FF0000 FFEB04 00FF00",
            "Change the default winstreak colors? HEX values only, space separated. " 
            + "Each color will show in the order of the ranges below. " + "The number of colors should be equal " 
            + "to the number of ranges! Max 50.");

        foreach (var colorStr in GetEntry<string>("WinstreakColors").Split(' '))
        {
            ColorUtility.TryParseHtmlString('#' + colorStr, out var color);
            GameManagerPatches.WinstreakColors.Add(color);
        }

        EntriesDict["WinstreakRanges"] = _config.Bind("Winstreak Options",
            "WinstreakRanges",
            "0-1 1-2 2-3",
            "Change the default ranges? Add more ranges, space separated, to support more colors. " 
            + "Once the last range is reached the corresponding color will be used for all subsequent wins " 
            + "until the streak is lost. Max 50.");

        var colorIndex = 0;
        foreach (var streakRange in GetEntry<string>("WinstreakRanges").Split(' '))
        {
            var streakRangeNums = Array.ConvertAll(streakRange.Split('-'), int.Parse);
            var nTimes = streakRangeNums[1] - streakRangeNums[0];
            Debug.Log("This streak color will appear: " + nTimes + " time(s)");
                
            GameManagerPatches.WinstreakRanges[colorIndex] = (byte)nTimes;
            colorIndex++;
        }
        Array.Copy(GameManagerPatches.WinstreakRanges, 0, GameManagerPatches.WinstreakRangesDefault, 0, 50);
            
        EntriesDict["AutoGG"] = _config.Bind("On-Startup Options", // The section under which the option is shown
            "AutoGG",
            false, // The key of the configuration option in the configuration file
            "Enable AutoGG on startup?"); // Description of the option to show in the config file
            
        EntriesDict["AlwaysPublicOutput"] = _config.Bind("On-Startup Options",
            "AlwaysPublicOutput",
            false,
            "Enable AlwaysPublicOutput on start-up, where all mod logs in chat are no longer client-side?");

        EntriesDict["ChatCensorshipBypass"] = _config.Bind("On-Startup Options",
            "ChatCensorshipBypass",
            false,
            "Disable chat censorship on startup?");

        EntriesDict["RichText"] = _config.Bind("On-Startup Options",
            "RichText",
            true,
            "Enable rich text for chat on startup?");
            
        EntriesDict["AutoTranslations"] = _config.Bind("On-Startup Options",
            "AutoTranslations",
            false,
            "Enable auto-translation for chat messages to English on startup?");
            
        EntriesDict["AlwaysShowHPOfWinner"] = _config.Bind("On-Startup Options",
            "AlwaysShowHPOfWinner",
            false,
            "Enable always show the HP for the winner of the round on-startup?");
            
        EntriesDict["CommandPrefix"] = _config.Bind("Misc. Options",
            "CommandPrefix",
            "/", 
            "Change the default command prefix character? (Recommended: /, !, $, ., &, ?)");

        EntriesDict["AdvertiseMsg"] = _config.Bind("Misc. Options",
            "AdvertiseMsg",
            "",
            "Modify the output of /adv? By default the message is blank but can be changed to anything.");

        EntriesDict["MsgDuration"] = _config.Bind("Misc. Options",
            "MsgDuration",
            0.0f,
            "Extend the amount of seconds per chat message by a specified amount? (Decimals allowed)");

        EntriesDict["ResizeName"] = _config.Bind("Misc. Options",
            "ResizeName",
            true,
            "Auto-resize a player's name if it's over 12 characters? (This provides large name support)");

        var customUsernameEntry = _config.Bind("Misc. Options",
            "CustomUsername",
            "",
            "Specify a custom username? (client-side only)");

        EntriesDict[customUsernameEntry.Definition.Key] = customUsernameEntry;
        
        customUsernameEntry.SettingChanged += (_, _) =>
        {
            CustomName = GetEntry<string>(customUsernameEntry.Definition.Key);
            IsCustomName = !string.IsNullOrEmpty(CustomName);
        };

        EntriesDict["FixCrownTxt"] = _config.Bind("Misc. Options",
            "FixCrownTxt",
            true,
            "Auto-resize win counter font so wins into the hundreds/thousands display properly?");

        var ouchPhrasesEntry = _config.Bind("Misc. Options",
            "OuchPhrases",
            "ow owie ouch ouchie",
            "Words to be used by OuchMode? Space seperated. (/ouch)");

        EntriesDict[ouchPhrasesEntry.Definition.Key] = ouchPhrasesEntry;
        
        ouchPhrasesEntry.SettingChanged += (_, _)
            => OuchPhrases = GetEntry<string>(ouchPhrasesEntry.Definition.Key).Split(' ');

        EntriesDict["ShrugEmoji"] = _config.Bind("Misc. Options",
            "ShrugEmoji",
            "☹",
            "Specify the emoji used in the shrug command? Only the following 15 TMP defaults are available: " 
            + "😋, 😍, 😎, 😀, 😁, 😂, 😃, 😄, 😅, 😆, 😉, 😘, 🤣, ☺, ☹");

        EntriesDict["AutoAuthTranslationsAPIKey"] = _config.Bind("Misc. Options",
            "AutoAuthTranslationsAPIKey",
            "",
            "Put your API key for Google Translate V2 here (Optional)");

        AllOutputPublic = GetEntry<bool>("AlwaysPublicOutput");
        OuchPhrases = GetEntry<string>("OuchPhrases").Split(' ');
        IsCustomPlayerColor = GetEntry<Color>("CustomColor") != GetEntry<Color>("CustomColor", true);
        IsCustomName = !string.IsNullOrEmpty(GetEntry<string>("CustomUsername"));
        CustomName = GetEntry<string>("CustomUsername");
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

    private static float[] MenuPosParser(string menuPos)
        => Array.ConvertAll(menuPos
            .Replace("X", "")
            .Replace("Y", "")
            .Split(' '), float.Parse);

    private static void UpdateDefaultPlayerColors()
    {
        var spacedColors = GetEntry<string>("DefaultPlayerColors").Split(' ');
                
        for (var index = 0; index < spacedColors.Length; index++)
        {
            ColorUtility.TryParseHtmlString(spacedColors[index].Insert(0, "#"), out var convColor);
            DefaultColors[index] = convColor;
        }
    }
}