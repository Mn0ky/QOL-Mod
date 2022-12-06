﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using SimpleJSON;
using Steamworks;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace QOL
{
    public static class ChatCommands
    {
        private static readonly List<Command> Cmds = new()
        {
            new Command("/adv", AdvCmd, 0, false).SetAlwaysPublic(),
            new Command("/alias", AliasCmd, 1, true),
            new Command("/fov", FovCmd, 1, true),
            new Command("/fps", FpsCmd, 1, true),
            new Command("/friend", FriendCmd, 1, true),
            new Command("/gg", GgCmd, 0, true).MarkAsToggle(),
            new Command("/help", HelpCmd, 0, true),
            new Command("/hp", HpCmd, 1, false).SetAlwaysPublic(),
            new Command("/id", IdCmd, 1, true),
            new Command("/invite", InviteCmd, 0, true),
            new Command("/lobhp", LobHpCmd, 0, false).SetAlwaysPublic(),
            new Command("/lobregen", LobRegenCmd, 0, false).SetAlwaysPublic(),
            new Command("/logprivate", LogPrivateCmd, 1, true),
            new Command("/logpublic", LogPublicCmd, 1, true),
            new Command("/lowercase", LowercaseCmd, 0, true).MarkAsToggle(),
            new Command("/nuky", NukyCmd, 0, true).MarkAsToggle(),
            new Command("/mute", MuteCmd, 1, true),
            new Command("/music", MusicCmd, 2, true),
            new Command("/ouch", OuchCmd, 0, true).MarkAsToggle(),
            new Command("/ping", PingCmd, 1, true),
            new Command("/private", PrivateCmd, 0, true),
            new Command("/profile", ProfileCmd, 1, true),
            new Command("/public", PublicCmd, 0, true),
            new Command("/rainbow", RainbowCmd, 0, true).MarkAsToggle(),
            new Command("/resolution", ResolutionCmd, 2, true),
            new Command("/rich", RichCmd, 0, true).MarkAsToggle(),
            new Command("/shrug", ShrugCmd, 0, false).SetAlwaysPublic(),
            new Command("/stat", StatCmd, 1, true),
            new Command("/suicide", SuicideCmd, 0, true),
            new Command("/translate", TranslateCmd, 0, true).MarkAsToggle(),
            new Command("/uncensor", UncensorCmd, 0, true).MarkAsToggle(),
            new Command("/uwu", UwuCmd, 0, true).MarkAsToggle(),
            new Command("/ver", VerCmd, 0, true),
            new Command("/winnerhp", WinnerHpCmd, 0, false).MarkAsToggle(),
            new Command("/winstreak", WinstreakCmd, 0, true).MarkAsToggle()
        };

        public static Dictionary<string, Command> CmdDict {get; private set;}
        public static Dictionary<string, Command> CmdAliasDict {get; private set;}
        public static List<string> CmdAliases {get; private set;}

        public static void InitializeCmds()
        {
            // Substring for removing the '/' char
            CmdDict = Cmds.ToDictionary(cmd => cmd.Name.Substring(1), cmd => cmd); 
            
            if (!Directory.Exists(Plugin.InternalsPath))
                Directory.CreateDirectory(Plugin.InternalsPath);
            
            if (File.Exists(Plugin.CmdVisibilityStatesPath))
                LoadCmdVisibilityStates();
            
            if (File.Exists(Plugin.CmdAliasesPath))
                LoadCmdAliases();
            
            CmdAliases = Cmds.Select(cmd => cmd.Alias).ToList();
            CmdAliasDict = Cmds.ToDictionary(cmd => cmd.Alias.Substring(1), cmd => cmd);
        }

        private static void LoadCmdAliases()
        {
            try
            {
                foreach (var pair in JSONNode.Parse(File.ReadAllText(Plugin.CmdAliasesPath)))
                {
                    Debug.Log("Setting saved alias of cmd: " + pair.Key);
                    CmdDict[pair.Key].Alias = pair.Value;
                }
            }
            catch (Exception)
            {
                Debug.LogError("Failed to change cmd alias, assuming cmd no longer exists!");
                Debug.Log("Resetting cmd alias json to prevent corruption...");
                SaveCmdAliases();
            }
        }
        
        private static void SaveCmdAliases()
        {
            var cmdAliasesJson = new JSONObject();
            
            foreach (var cmd in Cmds)
                cmdAliasesJson.Add(cmd.Name.Substring(1), cmd.Alias);
            
            File.WriteAllText(Plugin.CmdAliasesPath, cmdAliasesJson.ToString());
        }

        private static void LoadCmdVisibilityStates()
        {
            try
            {
                foreach (var pair in JSONNode.Parse(File.ReadAllText(Plugin.CmdVisibilityStatesPath)))
                {
                    Debug.Log("Setting saved visibility of cmd: " + pair.Key);
                    CmdDict[pair.Key].IsPublic = pair.Value;
                }
            }
            catch (Exception)
            {
                Debug.LogError("Failed to change cmd state, assuming cmd no longer exists!");
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
            => cmd.SetOutputMsg(Plugin.ConfigAdvCmd.Value);

        private static void AliasCmd(string[] args, Command cmd)
        {
            var resetAlias = args.Length == 2; // Should be true even if cmd has a space char after it 
            var targetCmdName = args[1].Replace("\"", "").Replace("/", "");
            Command targetCmd = null;

            if (CmdDict.ContainsKey(targetCmdName))
                targetCmd = CmdDict[targetCmdName];

            if (targetCmd == null && CmdAliasDict.ContainsKey(targetCmdName)) 
                targetCmd = CmdAliasDict[targetCmdName];

            if (targetCmd == null)
            {
                cmd.SetOutputMsg("Specified command not found.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }

            if (resetAlias)
            {
                cmd.SetOutputMsg("Removed alias " + targetCmd.Alias + " of " + targetCmd.Name + ".");
                
                CmdAliasDict.Remove(targetCmd.Alias);
                CmdAliasDict[targetCmd.Name] = targetCmd;
                targetCmd.Alias = targetCmd.Name;
                
                CmdAliases = Cmds.Select(command => command.Alias).ToList();
                SaveCmdAliases();
                return;
            }

            CmdAliasDict.Remove(targetCmd.Alias);
            targetCmd.Alias = "/" + args[2].Replace("\"", "").Replace("/", "");
            CmdAliasDict[targetCmd.Alias.Substring(1)] = targetCmd;

            cmd.SetOutputMsg("Set alias of " + targetCmd.Name + " as " + targetCmd.Alias + ".");
            CmdAliases = Cmds.Select(command => command.Alias).ToList();
            SaveCmdAliases();
        }

        private static void FovCmd(string[] args, Command cmd) // TODO: Do tryparse instead
        {
            var newFov = int.Parse(args[1]);
            Camera.main!.fieldOfView = newFov;
            
            cmd.SetOutputMsg("Set FOV to: " + newFov);
        }

        private static void FpsCmd(string[] args, Command cmd)
        {
            var targetFPS = int.Parse(args[1]);
            
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
            var steamID = Helper.GetSteamID(Helper.GetIDFromColor(args[1]));
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
            if (args.Length == 1)
            {
                cmd.SetOutputMsg("My HP: " + Helper.GetPlayerHP(Helper.localNetworkPlayer.NetworkSpawnID));
                return;
            }

            // Assuming user wants another player's hp
            var targetID = Helper.GetIDFromColor(args[1]);
            cmd.SetOutputMsg(Helper.GetColorFromID(targetID) + " HP: " + Helper.GetPlayerHP(targetID));
        }
        
        // Outputs the specified player's SteamID
        private static void IdCmd(string[] args, Command cmd)
        {
            PlayerSteamID targetSteamID = new (args[1]);
            GUIUtility.systemCopyBuffer = targetSteamID.SteamID;

            cmd.SetOutputMsg(targetSteamID.FullColor + "'s steamID copied to clipboard!");
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
            var targetCmdName = args[1].Replace("\"", "").Replace("/", "");
            
            if (targetCmdName == "all")
            {
                cmd.SetOutputMsg("Toggled private logging for all applicable commands.");
                Helper.AllOutputPublic = false;
                SaveCmdVisibilityStates();
                return;
            }

            var targetCmd = CmdDict.ContainsKey(targetCmdName) ? CmdDict[targetCmdName] : null;
            if (targetCmd == null)
            {
                cmd.SetOutputMsg("Specified command not found.");
                cmd.SetLogType(Command.LogType.Warning);
                return;
            }
            
            targetCmd.IsPublic = false;
            cmd.SetOutputMsg("If applicable, toggled private logging for " + targetCmd.Name + ".");
            SaveCmdVisibilityStates();
        }

        private static void LogPublicCmd(string[] args, Command cmd)
        {
            var targetCmdName = args[1].Replace("\"", "").Replace("/", "");
            if (targetCmdName == "all")
            {
                cmd.SetOutputMsg("Toggled public logging for all applicable commands.");
                Helper.AllOutputPublic = true;
                SaveCmdVisibilityStates();
                return;
            }

            var targetCmd = CmdDict[targetCmdName];
            if (targetCmd != null)
            {
                targetCmd.IsPublic = true;
                cmd.SetOutputMsg("If applicable, toggled public logging for " + targetCmd.Name + ".");
                SaveCmdVisibilityStates();
                return;
            }
            
            cmd.SetOutputMsg("Specified command not found.");
            cmd.SetLogType(Command.LogType.Warning);
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
            var targetID = Helper.GetIDFromColor(args[1]);

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
            switch (args[1])
            {
                case "skip": // Skips to the next song or if all have been played, a random one
                    Helper.IsSongLoop = false;
                    var nextSongMethod = AccessTools.Method(typeof(MusicHandler), "PlayNext");
                    nextSongMethod.Invoke(MusicHandler.Instance, null);
                    cmd.SetOutputMsg("Current song skipped.");
                    return;
                case "loop": // Loops the current song
                    Helper.IsSongLoop = !Helper.IsSongLoop;
                    cmd.SetOutputMsg("Song looping toggled.");
                    return;
                case "play": // Plays song that corresponds to the specified index (0 to # of songs - 1)
                    var songIndex = int.Parse(args[2]);
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
            PlayerPing targetPing = new (args[1]);
            cmd.SetOutputMsg(targetPing.FullColor + targetPing.Ping);
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
            => SteamFriends.ActivateGameOverlayToUser("steamid", Helper.GetSteamID(Helper.GetIDFromColor(args[1])));
        
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

            cmd.SetOutputMsg("Lobby made private!");
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
            var width = int.Parse(args[1]);
            var height = int.Parse(args[2]);

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
            var msg = string.Join(" ", args, 1, args.Length - 1) + " \u00af\\_" + Plugin.ConfigEmoji.Value + "_/\u00af";
            cmd.SetOutputMsg(msg);
        }
        
        // Outputs a stat of the target user (WeaponsThrown, Falls, BulletShot, and etc.)
        private static void StatCmd(string[] args, Command cmd)
        {
            if (args.Length == 2)
            {
                var targetStat = args[1];
                var myStats = Helper.localNetworkPlayer.GetComponentInParent<CharacterStats>();

                cmd.SetOutputMsg("My " + targetStat + ": " + Helper.GetTargetStatValue(myStats, targetStat));
                return;
            }

            if (args[1] == "all")
            {
                var targetStat = args[2];

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
                    
            var targetStats = new PlayerStat(args[1]);
            cmd.SetOutputMsg(targetStats.FullColor + ", " + args[2] + ": " + Helper.GetTargetStatValue(targetStats.Stats, args[2]));
        }
        
        // Kills user
        private static void SuicideCmd(string[] args, Command cmd)
        {
            Helper.localNetworkPlayer.UnitWasDamaged(5, true, DamageType.LocalDamage, true);
            cmd.SetOutputMsg("I've perished.");
        }
        
        // Enables/disables the auto-translate system for chat
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
                GameManager.Instance.winText.fontSize = Plugin.ConfigWinStreakFontsize.Value;
            }
            
            cmd.SetOutputMsg("Toggled Winstreak system.");
        }
    }
}