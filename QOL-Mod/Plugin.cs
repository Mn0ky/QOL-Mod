using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using SimpleJson;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QOL;

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
            Logger.LogInfo("Applying MapSelectionHandlerPatch...");
            MapSelectionHandlerPatches.Patch(harmony);
        }
        catch (Exception ex)
        {
            Logger.LogError("Exception on applying patches: " + ex.InnerException);
        }
        
        if (!Directory.Exists(InternalsPath))
            Directory.CreateDirectory(InternalsPath);
        
        if (!File.Exists(MapPresetsPath))
        {
            var emptyPresetList = new JSONObject();
            emptyPresetList.Add("savedPresets", new JSONArray());
            File.WriteAllText(MapPresetsPath, emptyPresetList.ToString());
        }

        if (!File.Exists(GunPresetsPath))
        {
            var emptyPresetList = new JSONObject();
            emptyPresetList.Add("savedPresets", new JSONArray());
            File.WriteAllText(GunPresetsPath, emptyPresetList.ToString());
        }
        
        MapPresetHandler.InitializeMapPresets();
        GunPresetHandler.InitializeGunPresets();

        try
        {
            Logger.LogInfo("Loading configuration options from config file...");
            ConfigHandler.InitializeConfig(Config);
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
            File.WriteAllText(MusicPath + "README.txt", "Only WAV and OGG audio files are supported.\n" 
                                                        + "For MP3 and other types, please convert them first!");
        }

        // Loading highscore from txt
        if (StatsFileExists)
        {
            GameManagerPatches.WinstreakHighScore = JSONNode.Parse(File.ReadAllText(StatsPath))["winstreakHighscore"];
            Debug.Log("Loading winstreak highscore of: " + GameManagerPatches.WinstreakHighScore);
        }
        else
            GameManagerPatches.WinstreakHighScore = 0;
        
        
        ChatCommands.InitializeCmds();
    }

    public static void InitModText()
    {
        var modText = new GameObject("ModText");
        var canvas = modText.AddComponent<Canvas>();
        var canvasScaler = modText.AddComponent<CanvasScaler>();
        var modTextTMP = modText.AddComponent<TextMeshProUGUI>();
        
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        modTextTMP.text = "<color=red>Monky's QOL Mod</color> " + "<color=white>v" + VersionNumber + " </color><color=#00bbff>Testing";
        
        modTextTMP.fontSizeMax = 25;
        modTextTMP.fontSize = 25;
        modTextTMP.enableAutoSizing = true;
        modTextTMP.color = Color.red;
        modTextTMP.fontStyle = FontStyles.Bold;
        modTextTMP.alignment = TextAlignmentOptions.TopRight;
        modTextTMP.richText = true;
    }

    public const string VersionNumber = "1.17.4"; // Version number
    public const string Guid = "monky.plugins.QOL";
    public static string NewUpdateVerCode = "";
        
    public static readonly string MusicPath = Paths.PluginPath + "\\QOL-Mod\\Music\\";
    public static readonly string InternalsPath = Paths.PluginPath + "\\QOL-Mod\\Internal\\";
        
    public static readonly string StatsPath = Paths.PluginPath + "\\QOL-Mod\\StatsData.json";
    public static readonly string CmdAliasesPath = InternalsPath + "CmdAliases.json";
    public static readonly string CmdVisibilityStatesPath = InternalsPath + "CmdVisibilityStates.json";
    public static readonly string MapPresetsPath = InternalsPath + "SavedMapPresets.json";
    public static readonly string GunPresetsPath = InternalsPath + "SavedGunPresets.json";

    public static bool StatsFileExists = File.Exists(StatsPath);
}