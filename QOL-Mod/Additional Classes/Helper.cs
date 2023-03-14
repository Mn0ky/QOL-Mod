using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace QOL;

public class Helper
{
    // Returns the steamID of the specified spawnID
    public static CSteamID GetSteamID(ushort targetID) => ClientData[targetID].ClientID;

    // Returns the corresponding spawnID from the specified color
    public static ushort GetIDFromColor(string targetSpawnColor) => targetSpawnColor.ToLower() switch
    { 
        "yellow" or "y" => 0, 
        "blue" or "b" => 1,
        "red" or "r" => 2,
        "green" or "g" => 3,
        _ => ushort.MaxValue
    };

    // Returns the corresponding color from the specified spawnID
    public static string GetColorFromID(ushort x) => x switch { 1 => "Blue", 2 => "Red", 3 => "Green", _ => "Yellow" };

    // Returns the targeted player based on the specified spawnID
    // ReSharper disable Unity.PerformanceAnalysis
    public static NetworkPlayer GetNetworkPlayer(ushort targetID) => ClientData[targetID].PlayerObject.GetComponent<NetworkPlayer>();
        
    // ReSharper disable Unity.PerformanceAnalysis
    public static string GetPlayerHp(ushort targetID) =>
        GetNetworkPlayer(targetID)
            .GetComponentInChildren<HealthHandler>()
            .health + "%";

    // Gets the steam profile name of the specified steamID
    public static string GetPlayerName(CSteamID passedClientID) => SteamFriends.GetFriendPersonaName(passedClientID);

    // Actually sticks the "join game" link together (url prefix + appID + LobbyID + SteamID)
    public static string GetJoinGameLink() => $"steam://joinlobby/674940/{lobbyID}/{localPlayerSteamID}";

    // Assigns some commonly accessed values as well as runs anything that needs to be everytime a lobby is joined
    public static void InitValues(ChatManager __instance, ushort playerID)
    {
        if (playerID != GameManager.Instance.mMultiplayerManager.LocalPlayerIndex) return;
            
        foreach (var filePath in Directory.GetFiles(Plugin.MusicPath))
        {
            var acceptableFileExtension = filePath.Substring(filePath.Length - 4); // .OGG or .WAV, both are 4 char

            if (acceptableFileExtension is ".ogg" or ".wav")
                __instance.StartCoroutine(ImportWav(filePath, CreateSongAndAddToMusic));
        }

        ClientData = GameManager.Instance.mMultiplayerManager.ConnectedClients;
        MutedPlayers.Clear();

        var localID = GameManager.Instance.mMultiplayerManager.LocalPlayerIndex;
        localNetworkPlayer = ClientData[localID].PlayerObject.GetComponent<NetworkPlayer>();
        LocalChat = ClientData[localID].PlayerObject.GetComponentInChildren<ChatManager>();

        Debug.Log("Assigned the localNetworkPlayer!: " + localNetworkPlayer.NetworkSpawnID);

        TMPText = Traverse.Create(__instance).Field("text").GetValue<TextMeshPro>();
        TMPText.richText = ChatCommands.CmdDict["rich"].IsEnabled;
        // Increase caret width so caret won't disappear at certain times
        Traverse.Create(__instance).Field("chatField").GetValue<TMP_InputField>().caretWidth = 3;

        var customCrownColor = ConfigHandler.GetEntry<Color>("CustomCrownColor");
        
        if (ConfigHandler.GetEntry<bool>("FixCrownTxt"))
        {
            var counter = UnityEngine.Object.FindObjectOfType<WinCounterUI>();
            foreach (var crownCount in counter.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                crownCount.enableAutoSizing = true;
                crownCount.GetComponentInChildren<Image>().color = customCrownColor;
            }
        }

        if (ConfigHandler.GetEntry<bool>("ResizeName"))
        {
            var playerNames = Traverse.Create(UnityEngine.Object.FindObjectOfType<OnlinePlayerUI>())
                .Field("mPlayerTexts")
                .GetValue<TextMeshProUGUI[]>();

            foreach (var name in playerNames)
            {
                name.fontSizeMin = 45;
                name.fontSizeMax = 45;
                name.overflowMode = TextOverflowModes.Overflow;
                name.enableWordWrapping = false;
            }
        }

        if (customCrownColor != ConfigHandler.GetEntry<Color>("CustomCrownColor", true))
        {
            var crown = UnityEngine.Object.FindObjectOfType<Crown>().gameObject;
                
            foreach (var sprite in crown.GetComponentsInChildren<SpriteRenderer>(true)) 
                sprite.color = customCrownColor;
        }

        GameObject rbHand = new ("RainbowHandler");
        rbHand.AddComponent<RainbowManager>().enabled = false;
        var rbCmd = ChatCommands.CmdDict["rainbow"];
        if (rbCmd.IsEnabled) rbCmd.Execute();

        if (_notifyUpdateCount < 3)
        {
            Debug.Log("Checking for new mod version...");
            __instance.StartCoroutine(CheckForModUpdate());
            _notifyUpdateCount++;
        }
    }

    public static string GetTargetStatValue(CharacterStats stats, string targetStat)
    {
        foreach (var stat in typeof(CharacterStats).GetFields())
            if (string.Equals(stat.Name, targetStat, StringComparison.InvariantCultureIgnoreCase))
                return stat.GetValue(stats).ToString();

        return "No value";
    }

    public static void SendPublicOutput(string msg) => localNetworkPlayer.OnTalked(msg);
        
    public static void SendModOutput(string msg, Command.LogType logType, bool isPublic = true, bool toggleState = true)
    {
        if (isPublic || ConfigHandler.AllOutputPublic)
        {
            SendPublicOutput(msg);
            return;
        }
            
        var msgColor = logType switch
        {
            Command.LogType.Warning => "<#FF7F50>",
            // Enabled => green, disabled => gray
            Command.LogType.Success => toggleState ? "<#006400>" : "<#56595c>",
            _ => ""
        };
            
        TMPText.richText = true;
        LocalChat.Talk(msgColor + msg);
    }

    // Adapted from: https://github.com/deadlyfingers/UnityWav#notes
    public static IEnumerator ImportWav(string url, Action<AudioClip> callback)
    {
        url = "file:///" + url.Replace(" ", "%20");
        Debug.Log("Loading song: " + url);

        using var www = UnityWebRequest.GetAudioClip(url, AudioType.UNKNOWN);
        yield return www.Send();

        if (www.isError)
        {
            Debug.LogWarning("Audio error:" + www.error);
            yield break;
        }

        var audioClip = DownloadHandlerAudioClip.GetContent(www);
        callback(audioClip);
    }

    private static void CreateSongAndAddToMusic(AudioClip audioClip)
        => MusicHandler.Instance.myMusic = MusicHandler.Instance.myMusic.AddToArray(new MusicClip { clip = audioClip });

    private static IEnumerator CheckForModUpdate()
    {
        if (!string.IsNullOrEmpty(Plugin.NewUpdateVerCode))
        {
            SendModOutput("A new mod update has been detected: <#006400>" + Plugin.NewUpdateVerCode, 
                Command.LogType.Warning, false);
            yield break;
        }
            
        const string latestReleaseUri = "https://api.github.com/repos/Mn0ky/QOL-Mod/releases/latest";
        using var webRequest = UnityWebRequest.Get(latestReleaseUri);
            
        yield return webRequest.Send();

        if (webRequest.isError)
        {
            Debug.LogError(webRequest.error);
            Debug.Log("Error occured during fetch for latest qol mod version!");
            yield break;
        }

        string latestVer = JSONNode.Parse(webRequest.downloadHandler.text)["tag_name"];

        if (latestVer.Remove(0, 1) == Plugin.VersionNumber) yield break;
            
        Plugin.NewUpdateVerCode = latestVer;
        SendModOutput("A new mod update has been detected: <#006400>" + Plugin.NewUpdateVerCode, 
            Command.LogType.Warning, false);
    }

    // Fancy bit-manipulation of a char's ASCII values to check whether it's a vowel or not
    public static bool IsVowel(char c) => (0x208222 >> (c & 0x1f) & 1) != 0;

    public static CSteamID lobbyID; // The ID of the current lobby
        
    public static readonly CSteamID localPlayerSteamID = SteamUser.GetSteamID(); // The steamID of the local user (ours)
    public static NetworkPlayer localNetworkPlayer; // The networkPlayer of the local user (ours)
    public static List<ushort> MutedPlayers = new(4);
    //public static readonly bool IsCustomPlayerColor = Plugin.ConfigCustomColor.Value != new Color(1, 1, 1);
    //public static readonly bool IsCustomName = !string.IsNullOrEmpty(Plugin.ConfigCustomName.Value);
    public static bool SongLoop;
    //public static readonly string[] OuchPhrases = Plugin.ConfigOuchPhrases.Value.Split(' ');
    //private static readonly bool NameResize = Plugin.ConfigNoResize.Value;
    public static bool TrustedKicker;
    private static int _notifyUpdateCount;
    public static IEnumerator RoutineUsed;

    public static ConnectedClientData[] ClientData;
    public static ChatManager LocalChat;

    public static TextMeshPro TMPText;
    public static int WinStreak = 0;

    //public static HoardHandler[] Hoards = new HoardHandler[2];
}