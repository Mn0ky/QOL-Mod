using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using Steamworks;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace QOL;

public static class ChatCommands
{
    private static readonly List<Command> Cmds = new()
    {
        new Command("adv", AdvCmd, 0, false).SetAlwaysPublic(),
        new Command("alias", AliasCmd, 1, true, CmdNames),
        new Command("config", ConfigCmd, 1, true, ConfigHandler.GetConfigKeys().ToList()),
        new Command("deathmsg", DeathMsgCmd, 0, false).MarkAsToggle(),
        new Command("dm", DmCmd, 1, false, PlayerUtils.PlayerColorsParams),
        new Command("fov", FovCmd, 1, true),
        new Command("fps", FpsCmd, 1, true),
        new Command("friend", FriendCmd, 1, true, PlayerUtils.PlayerColorsParams),
        new Command("gg", GgCmd, 0, true).MarkAsToggle(),
        new Command("help", HelpCmd, 0, true),
        new Command("hp", HpCmd, 0, false, PlayerUtils.PlayerColorsParams).SetAlwaysPublic(),
        new Command("id", IdCmd, 1, true, PlayerUtils.PlayerColorsParams),
        new Command("invite", InviteCmd, 0, true),
        new Command("lobhp", LobHpCmd, 0, false).SetAlwaysPublic(),
        new Command("lobregen", LobRegenCmd, 0, false).SetAlwaysPublic(),
        new Command("logprivate", LogPrivateCmd, 1, true, CmdNames),
        new Command("logpublic", LogPublicCmd, 1, true, CmdNames),
        new Command("lowercase", LowercaseCmd, 0, true).MarkAsToggle(),
        new Command("nuky", NukyCmd, 0, true).MarkAsToggle(),
        new Command("mute", MuteCmd, 1, true, PlayerUtils.PlayerColorsParams),
        new Command("music", MusicCmd, 1, true, new List<string>(3) {"loop", "play", "skip"}),
        new Command("ouch", OuchCmd, 0, true).MarkAsToggle(),
        new Command("ping", PingCmd, 1, true, PlayerUtils.PlayerColorsParams),
        new Command("private", PrivateCmd, 0, true),
        new Command("profile", ProfileCmd, 1, true, PlayerUtils.PlayerColorsParams),
        new Command("public", PublicCmd, 0, true),
        new Command("rainbow", RainbowCmd, 0, true).MarkAsToggle(),
        new Command("resolution", ResolutionCmd, 2, true),
        new Command("rich", RichCmd, 0, true).MarkAsToggle(),
        new Command("shrug", ShrugCmd, 0, false).SetAlwaysPublic(),
        new Command("stat", StatCmd, 1, true),
        new Command("suicide", SuicideCmd, 0, true),
        new Command("translate", TranslateCmd, 0, true).MarkAsToggle(),
        new Command("uncensor", UncensorCmd, 0, true).MarkAsToggle(),
        new Command("uwu", UwuCmd, 0, true).MarkAsToggle(),
        new Command("ver", VerCmd, 0, true),
        new Command("winnerhp", WinnerHpCmd, 0, false).MarkAsToggle(),
        new Command("winstreak", WinstreakCmd, 0, true).MarkAsToggle()
    };

    public static readonly Dictionary<string, Command> CmdDict = Cmds.ToDictionary(cmd => cmd.Name.Substring(1),
        cmd => cmd,
        StringComparer.InvariantCultureIgnoreCase);
        
    public static readonly List<string> CmdNames = Cmds.Select(cmd => cmd.Name).ToList();

    public static void InitializeCmds()
    {
        if (!Directory.Exists(Plugin.InternalsPath))
            Directory.CreateDirectory(Plugin.InternalsPath);
            
        if (File.Exists(Plugin.CmdVisibilityStatesPath))
            LoadCmdVisibilityStates();
            
        if (File.Exists(Plugin.CmdAliasesPath))
            LoadCmdAliases();
        
        // Reflection hackery so that auto-params for the alias and log cmds work
        const string autoParamsBackingField = $"<{nameof(Command.AutoParams)}>k__BackingField";
        Traverse.Create(CmdDict["alias"]).Field(autoParamsBackingField).SetValue(CmdNames); 
        Traverse.Create(CmdDict["logprivate"]).Field(autoParamsBackingField).SetValue(CmdNames);
        Traverse.Create(CmdDict["logpublic"]).Field(autoParamsBackingField).SetValue(CmdNames);
        
        // On-startup options need to map their values to respective cmds
        CmdDict["gg"].IsEnabled = ConfigHandler.GetEntry<bool>("ggstartup");
        CmdDict["uncensor"].IsEnabled = ConfigHandler.GetEntry<bool>("uncensorstartup");
        CmdDict["rich"].IsEnabled = ConfigHandler.GetEntry<bool>("richtextstartup");
        CmdDict["translate"].IsEnabled = ConfigHandler.GetEntry<bool>("translatestartup");
        CmdDict["winnerhp"].IsEnabled = ConfigHandler.GetEntry<bool>("winnerhpstartup");
        CmdDict["winstreak"].IsEnabled = ConfigHandler.GetEntry<bool>("winstreakstartup");
        CmdDict["rainbow"].IsEnabled = ConfigHandler.GetEntry<bool>("rainbowstartup");
    }

    private static void LoadCmdAliases()
    {
        try
        {
            Debug.Log("Setting saved aliases of cmds");
                
            foreach (var pair in JSONNode.Parse(File.ReadAllText(Plugin.CmdAliasesPath)))
            {
                foreach (var alias in pair.Value.AsArray)
                {
                    var cmdName = pair.Key;
                    if (!CmdDict.ContainsKey(cmdName))
                    {
                        Debug.LogWarning("Command: " + cmdName + " DNE!! Assuming it no longer exists and skipping...");
                        continue;
                    }
                    
                    var cmd = CmdDict[cmdName];
                    var aliasStr = ((string)alias.Value).Substring(1); // substring so no prefix
                    cmd.Aliases.Add(Command.CmdPrefix + aliasStr);
                }
            }

            Debug.Log("Adding aliases of cmds to cmd dict and list");
                
            foreach (var cmd in Cmds)
            {
                CmdNames.AddRange(cmd.Aliases);
                foreach (var alias in cmd.Aliases) 
                    CmdDict[alias.Substring(1)] = cmd;
            }
            
            CmdNames.Sort();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to change cmd alias, something went wrong: " + e);
            Debug.Log("Resetting cmd alias json to prevent corruption...");
            SaveCmdAliases();
        }
    }
        
    private static void SaveCmdAliases()
    {
        var cmdAliasesJson = new JSONObject();
            
        foreach (var cmd in Cmds)
        {
            var aliasHolder = new JSONArray();

            foreach (var alias in cmd.Aliases) 
                aliasHolder.Add(alias);

            cmdAliasesJson.Add(cmd.Name.Substring(1), aliasHolder);
        }

        File.WriteAllText(Plugin.CmdAliasesPath, cmdAliasesJson.ToString());
    }

    private static void LoadCmdVisibilityStates()
    {
        try
        {
            foreach (var pair in JSONNode.Parse(File.ReadAllText(Plugin.CmdVisibilityStatesPath)))
            {
                var cmdName = pair.Key;
                if (!CmdDict.ContainsKey(cmdName))
                {
                    Debug.LogWarning("Command: " + cmdName + " DNE!! Assuming it no longer exists and skipping...");
                    continue;
                }    
                
                Debug.Log("Setting saved visibility of cmd: " + cmdName);
                CmdDict[cmdName].IsPublic = pair.Value;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to change cmd state, something went wrong: " + e);
            Debug.Log("Resetting cmd visibility states json to prevent corruption...");
            SaveCmdVisibilityStates();
        }
    }

    private static void SaveCmdVisibilityStates()
    {
        var cmdStatesJson = new JSONObject();
            
        foreach (var cmd in Cmds)
            cmdStatesJson.Add(cmd.Name.Remove(0, 1), cmd.IsPublic);
            
        File.WriteAllText(Plugin.CmdVisibilityStatesPath, cmdStatesJson.ToString());
    }

    // ****************************************************************************************************
    //                                    All chat command methods below                                      
    // ****************************************************************************************************
        
    // Outputs player-specified msg from config to chat, blank by default
    private static void AdvCmd(string[] args, Command cmd) 
        => cmd.SetOutputMsg(ConfigHandler.GetEntry<string>("AdvertiseMsg"));

    private static void AliasCmd(string[] args, Command cmd)
    {
        var resetAlias = args.Length == 1; // Should be true even if cmd has a space char after it 
        var targetCmdName = args[0].Replace("\"", "").Replace(Command.CmdPrefix, "").ToLower();
        Command targetCmd = null;

        if (CmdDict.ContainsKey(targetCmdName))
            targetCmd = CmdDict[targetCmdName];

        if (targetCmd == null)
        {
            cmd.SetOutputMsg("Specified command or alias not found.");
            cmd.SetLogType(Command.LogType.Warning);
            return;
        }

        if (resetAlias)
        {
            cmd.SetOutputMsg("Removed aliases for " + targetCmd.Name + ".");
                
            foreach (var alias in targetCmd.Aliases)
            {
                CmdDict.Remove(alias);
                CmdNames.Remove(alias);
            }

            targetCmd.Aliases.Clear();
            CmdNames.Sort();
            SaveCmdAliases();
            return;
        }
            
        var newAlias = Command.CmdPrefix + args[1].Replace("\"", "").Replace(Command.CmdPrefix, "").ToLower();

        if (CmdNames.Contains(newAlias))
        {
            if (cmd.Name == newAlias)
            {
                cmd.SetOutputMsg("Invalid alias: already exists as name of a command.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }
                
            cmd.SetOutputMsg("Invalid alias: already exists.");
            cmd.SetLogType(Command.LogType.Warning);
            return;
        }
        
        CmdDict[newAlias.Substring(1)] = targetCmd;
        targetCmd.Aliases.Add(newAlias);
        CmdNames.Add(newAlias);

        cmd.SetOutputMsg("Added alias " + newAlias + " for " + targetCmd.Name + ".");
        CmdNames.Sort();
        SaveCmdAliases();
    }

    private static void ConfigCmd(string[] args, Command cmd)
    {
        var entryKey = args[0].Replace('"', "").ToLower(); // Sanitize

        var newEntryValue = args.Length switch
        {
            > 2 => string.Join(" ", args, 1, args.Length - 1).Replace('"', ""),
            > 1 => args[1].Replace('"', ""),
            _ => ""
        };

        if (!ConfigHandler.EntryExists(entryKey))
        {
            cmd.SetOutputMsg("Invalid key. Try fixing any spelling mistakes.");
            cmd.SetLogType(Command.LogType.Warning);
            return;
        }

        if (string.IsNullOrEmpty(newEntryValue))
        {
            ConfigHandler.ResetEntry(entryKey);
            cmd.SetOutputMsg("Config option has been reset to default.");
            return;
        }
        
        ConfigHandler.ModifyEntry(entryKey, newEntryValue);
        cmd.SetOutputMsg("Config option has been updated.");
    }

    private static void DeathMsgCmd(string[] args, Command cmd)
    {
        cmd.Toggle();
        cmd.SetOutputMsg("Toggled AutoDeathMsg.");
    }

    private static void DmCmd(string[] args, Command cmd)
    {
        var targetID = Helper.GetIDFromColor(args[0]);
        var targetSteamID = Helper.GetSteamID(targetID);
        var msg = string.Join(" ", args, 1, args.Length - 1);
        var msgBytes = Encoding.UTF8.GetBytes(msg);
        
        var channel = Helper.localNetworkPlayer.NetworkSpawnID switch
        {
            0 => 3, // Yellow
            1 => 5, // Red
            2 => 7, // Green
            3 => 9, // Blue
            _ => throw new ArgumentOutOfRangeException()
        };

        P2PPackageHandler.Instance.SendP2PPacketToUser(targetSteamID,
            msgBytes,
            P2PPackageHandler.MsgType.PlayerTalked,
            EP2PSend.k_EP2PSendReliable,
            channel);
    }

    private static void FovCmd(string[] args, Command cmd) // TODO: Do tryparse instead to provide better error handling
    {
        var success = int.TryParse(args[0], out var newFov);
        if (!success)
        {
            cmd.SetOutputMsg("Error parsing FOV value.");
            cmd.SetLogType(Command.LogType.Warning);
        }

        
        Camera.main!.fieldOfView = newFov;
        cmd.SetOutputMsg("Set FOV to: " + newFov);
    }

    private static void FpsCmd(string[] args, Command cmd)
    {
        var success = int.TryParse(args[0], out var targetFPS);
        if (!success)
        {
            cmd.SetOutputMsg("Error parsing FPS value.");
            cmd.SetLogType(Command.LogType.Warning);
        }    
            
        if (targetFPS < 60)
        {
            cmd.SetOutputMsg("FPS cannot be set below 60.");
            cmd.SetLogType(Command.LogType.Warning);
            return;
        }

        Application.targetFrameRate = targetFPS;
        cmd.SetOutputMsg("Target framerate is now: " + targetFPS);
    }

    private static void FriendCmd(string[] args, Command cmd)
    {
        var steamID = Helper.GetSteamID(Helper.GetIDFromColor(args[0]));
        SteamFriends.ActivateGameOverlayToUser("friendadd", steamID);
    }
        
    // Enables or disables automatic "gg" upon death
    private static void GgCmd(string[] args, Command cmd)
    {
        cmd.Toggle();
        cmd.SetOutputMsg("Toggled AutoGG.");
    }

    // Opens up the steam overlay to the GitHub readme, specifically the Chat Commands section
    private static void HelpCmd(string[] args, Command cmd) 
        => SteamFriends.ActivateGameOverlayToWebPage("https://github.com/Mn0ky/QOL-Mod#chat-commands");
        
    // Outputs HP of targeted color to chat
    private static void HpCmd(string[] args, Command cmd)
    {
        if (args.Length == 0)
        {
            cmd.SetOutputMsg("My HP: " + Helper.GetPlayerHp(Helper.localNetworkPlayer.NetworkSpawnID));
            return;
        }

        // Assuming user wants another player's hp
        var targetID = Helper.GetIDFromColor(args[0]);

        if (GameManager.Instance.mMultiplayerManager.ConnectedClients[targetID] == null)
        {
            cmd.SetOutputMsg(Helper.GetColorFromID(targetID) + " is not in the lobby.");
            cmd.SetLogType(Command.LogType.Warning);
            return;
        }    
        
        cmd.SetOutputMsg(Helper.GetColorFromID(targetID) + " HP: " + Helper.GetPlayerHp(targetID));
    }
        
    // Outputs the specified player's SteamID
    private static void IdCmd(string[] args, Command cmd)
    {
        var targetColor = Helper.GetIDFromColor(args[0]);
        GUIUtility.systemCopyBuffer = Helper.GetSteamID(targetColor).m_SteamID.ToString();

        cmd.SetOutputMsg(Helper.GetColorFromID(targetColor) + "'s steamID copied to clipboard!");
    }
        
    // Builds a "join game" link (same one you'd find on a steam profile) for the lobby and copies it to clipboard
    private static void InviteCmd(string[] args, Command cmd)
    {
        GUIUtility.systemCopyBuffer = Helper.GetJoinGameLink();
        cmd.SetOutputMsg("Join link copied to clipboard!");
    }

    // Outputs the HP setting for the lobby to chat
    private static void LobHpCmd(string[] args, Command cmd) 
        => cmd.SetOutputMsg("Lobby HP: " + OptionsHolder.HP);
        
    // Outputs whether regen is enabled (true) or disabled (false) for the lobby to chat
    private static void LobRegenCmd(string[] args, Command cmd)
        => cmd.SetOutputMsg("Lobby Regen: " + Convert.ToBoolean(OptionsHolder.regen));

    private static void LogPrivateCmd(string[] args, Command cmd)
    {
        var targetCmdName = args[0].Replace("\"", "").Replace("/", "")
            .TrimStart(Command.CmdPrefix)
            .ToLower(); // Even though CmdDict is case-insensitive we need to compare it to "all"
        
        if (targetCmdName == "all")
        {
            cmd.SetOutputMsg("Toggled private logging for all applicable commands.");
            SaveCmdVisibilityStates();
            return;
        }

        if (!CmdDict.ContainsKey(targetCmdName))
        {
            Debug.Log("targetCmdName: " + targetCmdName);
            cmd.SetOutputMsg("Specified command not found.");
            cmd.SetLogType(Command.LogType.Warning);
            return;
        }

        var targetCmd = CmdDict[targetCmdName];
        
        if (targetCmd.AlwaysPublic)
        {
            cmd.SetOutputMsg(targetCmd.Name + " is restricted to public logging only.");
            cmd.SetLogType(Command.LogType.Warning);
            return;
        }
        
        targetCmd.IsPublic = false;
        cmd.SetOutputMsg("Toggled private logging for " + targetCmd.Name + ".");
        SaveCmdVisibilityStates();
    }

    private static void LogPublicCmd(string[] args, Command cmd)
    {
        var targetCmdName = args[0].Replace("\"", "").Replace("/", "")
            .TrimStart(Command.CmdPrefix)
            .ToLower();
        
        if (targetCmdName == "all")
        {
            cmd.SetOutputMsg("Toggled public logging for all applicable commands.");
            ConfigHandler.AllOutputPublic = true;
            SaveCmdVisibilityStates();
            return;
        }

        if (!CmdDict.ContainsKey(targetCmdName))
        {
            cmd.SetOutputMsg("Specified command not found.");
            cmd.SetLogType(Command.LogType.Warning);
            return;
        }

        var targetCmd = CmdDict[targetCmdName];

        if (targetCmd.AlwaysPrivate)
        {
            cmd.SetOutputMsg(targetCmd.Name + " is restricted to private logging only.");
            cmd.SetLogType(Command.LogType.Warning);
            return;
        }
        
        targetCmd.IsPublic = true;
        cmd.SetOutputMsg("Toggled public logging for " + targetCmd.Name + ".");
        SaveCmdVisibilityStates();
    }
        
    // Enables/Disables chat messages always being sent in lowercase
    private static void LowercaseCmd(string[] args, Command cmd)
    {
        cmd.Toggle();
        cmd.SetOutputMsg("Toggled LowercaseOnly.");
    }
        
    // Enables/disables Nuky chat mode
    private static void NukyCmd(string[] args, Command cmd)
    {
        cmd.Toggle();
        if (Helper.RoutineUsed != null) Helper.LocalChat.StopCoroutine(Helper.RoutineUsed);
        cmd.SetOutputMsg("Toggled NukyChat.");
    }
        
    // Mutes the specified player (Only for the current lobby and only client-side)
    private static void MuteCmd(string[] args, Command cmd) 
    {
        var targetID = Helper.GetIDFromColor(args[0]);

        if (!Helper.MutedPlayers.Contains(targetID))
        {
            Helper.MutedPlayers.Add(targetID);
            cmd.IsEnabled = true;
            cmd.SetOutputMsg("Muted: " + Helper.GetColorFromID(targetID));
            return;
        }

        Helper.MutedPlayers.Remove(targetID);
        cmd.IsEnabled = false;
        cmd.SetOutputMsg("Unmuted: " + Helper.GetColorFromID(targetID));
    }
        
    // Music commands
    private static void MusicCmd(string[] args, Command cmd)
    {
        switch (args[0].ToLower())
        {
            case "skip": // Skips to the next song or if all have been played, a random one
                Helper.SongLoop = false;
                var nextSongMethod = AccessTools.Method(typeof(MusicHandler), "PlayNext");
                nextSongMethod.Invoke(MusicHandler.Instance, null);
                cmd.SetOutputMsg("Current song skipped.");
                return;
            case "loop": // Loops the current song
                Helper.SongLoop = !Helper.SongLoop;
                cmd.SetOutputMsg("Song looping toggled.");
                return;
            case "play": // Plays song that corresponds to the specified index (0 to # of songs - 1)
                var songIndex = int.Parse(args[1]);
                var musicHandler = MusicHandler.Instance;

                if (songIndex > musicHandler.myMusic.Length - 1 || songIndex < 0)
                {
                    cmd.SetOutputMsg($"Invalid index: input must be between 0 and {musicHandler.myMusic.Length - 1}.");
                    cmd.SetLogType(Command.LogType.Warning);
                    return;
                }

                Traverse.Create(musicHandler).Field("currntSong").SetValue(songIndex);

                var audioSource = musicHandler.GetComponent<AudioSource>();
                audioSource.clip = musicHandler.myMusic[songIndex].clip;
                audioSource.volume = musicHandler.myMusic[songIndex].volume;
                audioSource.Play();

                cmd.SetOutputMsg($"Now playing song #{songIndex} out of {musicHandler.myMusic.Length - 1}.");
                return;
            default:
                return;
        }
    }

    private static void OuchCmd(string[] args, Command cmd)
    {
        cmd.Toggle();
        cmd.SetOutputMsg("Toggled OuchMode.");
    }
        
    // Outputs the specified player's ping
    private static void PingCmd(string[] args, Command cmd)
    {
        var targetID = Helper.GetIDFromColor(args[0]);
        
        if (targetID == Helper.localNetworkPlayer.NetworkSpawnID)
        {
            cmd.SetOutputMsg("Can't ping yourself.");
            cmd.SetLogType(Command.LogType.Warning);
            return;
        }
        
        var targetPing = Helper.ClientData[targetID].Ping;

        cmd.SetOutputMsg(Helper.GetColorFromID(targetID) + targetPing);
    }
        
    // Privates the lobby (no player can publicly join unless invited)
    private static void PrivateCmd(string[] args, Command cmd)
    {
        if (!MatchmakingHandler.Instance.IsHost)
        {
            cmd.SetOutputMsg("Need to be host!");
            cmd.SetLogType(Command.LogType.Warning);
            return;
        }

        var changeLobbyTypeMethod = AccessTools.Method(typeof(MatchmakingHandler), "ChangeLobbyType");
        changeLobbyTypeMethod!.Invoke(MatchmakingHandler.Instance, new object[]
        {
            ELobbyType.k_ELobbyTypePrivate
        });

        cmd.SetOutputMsg("Lobby made private!");
    }

    private static void ProfileCmd(string[] args, Command cmd) 
        => SteamFriends.ActivateGameOverlayToUser("steamid", Helper.GetSteamID(Helper.GetIDFromColor(args[0])));
        
    // Publicizes the lobby (any player can join through quick match)
    private static void PublicCmd(string[] args, Command cmd)
    {
        if (!MatchmakingHandler.Instance.IsHost)
        {
            cmd.SetOutputMsg("Need to be host!");
            cmd.SetLogType(Command.LogType.Warning);
            return;
        }

        var changeLobbyTypeMethod = AccessTools.Method(typeof(MatchmakingHandler), "ChangeLobbyType");
        changeLobbyTypeMethod!.Invoke(MatchmakingHandler.Instance, new object[]
        {
            ELobbyType.k_ELobbyTypePublic
        });

        cmd.SetOutputMsg("Lobby made public!");
    }

    // Enables/disables the rainbow system
    private static void RainbowCmd(string[] args, Command cmd)
    {
        cmd.Toggle();
        Object.FindObjectOfType<RainbowManager>().enabled = cmd.IsEnabled;
            
        cmd.SetOutputMsg("Toggled PlayerRainbow.");
    }

    private static void ResolutionCmd(string[] args, Command cmd)
    {
        var width = int.Parse(args[0]);
        var height = int.Parse(args[1]);

        Screen.SetResolution(width, height, Convert.ToBoolean(OptionsHolder.fullscreen));
        cmd.SetOutputMsg("Set new resolution of: " + width + "x" + height);
    }
        
    // Enables/disables rich text for chat messages
    private static void RichCmd(string[] args, Command cmd)
    {
        cmd.Toggle();
        var chatPopup = Traverse.Create(Helper.LocalChat).Field("text").GetValue<TextMeshPro>();
        chatPopup.richText = cmd.IsEnabled;
            
        cmd.SetOutputMsg("Toggled Richtext.");
    }

    // Appends shrug emoticon to the end of the msg just sent
    private static void ShrugCmd(string[] args, Command cmd)
    {
        var msg = string.Join(" ", args, 0, args.Length) + " \u00af\\_" +
                  ConfigHandler.GetEntry<string>("ShrugEmoji") + "_/\u00af";
        
        cmd.SetOutputMsg(msg);
    }

    // Outputs a stat of the target user (WeaponsThrown, Falls, BulletShot, and etc.)
    private static void StatCmd(string[] args, Command cmd)
    {
        if (args.Length == 1)
        {
            var targetStat = args[0].ToLower();
            var myStats = Helper.localNetworkPlayer.GetComponentInParent<CharacterStats>();

            cmd.SetOutputMsg("My " + targetStat + ": " + Helper.GetTargetStatValue(myStats, targetStat));
            return;
        }

        if (args[0] == "all")
        {
            var targetStat = args[1].ToLower();

            var statMsg = "";
            foreach (var player in GameManager.Instance.mMultiplayerManager.PlayerControllers)
            {
                if (player != null) 
                    statMsg = statMsg +
                              Helper.GetColorFromID((ushort) player.playerID) + ", " +
                              targetStat + ": " +
                              Helper.GetTargetStatValue(player.GetComponent<CharacterStats>(), targetStat) +
                              "\n";
            }
                
            cmd.SetOutputMsg(statMsg);
            return;
        }

        var targetID = Helper.GetIDFromColor(args[0]);
        var targetStats = Helper.GetNetworkPlayer(targetID).GetComponentInParent<CharacterStats>();
        var targetPlayerStat = args[1].ToLower();
            
        cmd.SetOutputMsg(Helper.GetColorFromID(targetID) + ", " + targetPlayerStat + ": " +
                         Helper.GetTargetStatValue(targetStats, targetPlayerStat));
    }

    // Kills user
    private static void SuicideCmd(string[] args, Command cmd)
    {
        Helper.localNetworkPlayer.UnitWasDamaged(5, true, DamageType.LocalDamage, true);
        cmd.SetOutputMsg("I've perished.");
    }
        
    // Enables/disables the autargetStatto-translate system for chat
    private static void TranslateCmd(string[] args, Command cmd)
    {
        cmd.Toggle();
        cmd.SetOutputMsg("Toggled Auto-Translate.");
    }
        
    // Enables/disables chat censorship
    private static void UncensorCmd(string[] args, Command cmd)
    {
        cmd.Toggle();
        cmd.SetOutputMsg("Toggled Chat De-censoring.");
    }
        
    // Enables UwUifier for chat messages
    private static void UwuCmd(string[] args, Command cmd)
    {
        cmd.Toggle();
        cmd.SetOutputMsg("Toggled UwUifier.");
    }
        
    // Outputs the mod version number to chat
    private static void VerCmd(string[] args, Command cmd) => cmd.SetOutputMsg("QOL Mod: " + Plugin.VersionNumber);

    // Enables/Disables system for outputting the HP of the winner after each round to chat
    private static void WinnerHpCmd(string[] args, Command cmd)
    {
        cmd.Toggle();
        cmd.SetOutputMsg("Toggled WinnerHP Announcer.");
    }
        
    // Enables/disables winstreak system
    private static void WinstreakCmd(string[] args, Command cmd)
    {
        cmd.Toggle();
        if (cmd.IsEnabled)
        {
            cmd.IsEnabled = true;
            GameManager.Instance.winText.fontSize = ConfigHandler.GetEntry<int>("WinstreakFontsize");
        }
            
        cmd.SetOutputMsg("Toggled Winstreak system.");
    }
}