using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using SimpleJson;
using SimpleJSON;
using Steamworks;
using TMPro;
using UnityEngine;

namespace QOL
{
    public class ChatCommands
    {
        public static void SingleArgument(string cmd, string msg)
        {
            switch (cmd)    
            {
                case "hp": // Outputs HP of ourselves to chat
                    var localColor = Helper.GetColorFromID(Helper.localNetworkPlayer.NetworkSpawnID);
                    Helper.SendChatMsg("My HP: " + new PlayerHP(localColor).HP);
                    return;
                case "gg": // Enables or disables automatic "gg" upon death
                    Helper.AutoGG = !Helper.AutoGG;
                    Helper.SendChatMsg("Toggled AutoGG.", LogLevel.Success, Helper.AutoGG, false);
                    return;
                case "adv": // Outputs player-specified msg from config to chat, blank by default
                    Helper.SendChatMsg(Plugin.ConfigAdvCmd.Value);
                    return;
                case "uncensor": // Enables/disables chat censorship
                    Helper.ChatCensorshipBypass = !Helper.ChatCensorshipBypass;
                    Helper.SendChatMsg("Toggled ChatCensorship.", LogLevel.Success, Helper.ChatCensorshipBypass, false);
                    return;
                case "ouch":
                    Helper.IsOwMode = !Helper.IsOwMode;
                    Helper.SendChatMsg("Toggled OuchMode.", LogLevel.Success, Helper.IsOwMode, false);
                    return;
                case "winstreak": // Enables/disables winstreak system
                    Helper.ToggleWinstreak();
                    Helper.SendChatMsg("Toggled Winstreak system.", LogLevel.Success, Helper.WinStreakEnabled, false);
                    return;
                case "rich": // Enables rich text for chat messages
                    var theText = Traverse.Create(Helper.LocalChat).Field("text").GetValue<TextMeshPro>();
                    theText.richText = !theText.richText;
                    Helper.SendChatMsg("Toggled Richtext.", LogLevel.Success, theText.richText, false);
                    return;
                case "shrug": // Appends shrug emoticon to end of chat message
                    msg = msg.Replace("/shrug", "") + " \u00af\\_" + Plugin.ConfigEmoji.Value + "_/\u00af";
                    Helper.SendChatMsg(msg);
                    return;
                case "uwu": // Enables uwuifier for chat messages
                    Helper.UwuifyText = !Helper.UwuifyText;
                    Helper.SendChatMsg("Toggled UwUifier.", LogLevel.Success, Helper.UwuifyText, false);
                    return;
                case "fov":
                    Debug.Log("camera fov: " + Camera.main!.fieldOfView);
                    Camera.main.fieldOfView = 200;
                    Helper.SendChatMsg("Set FOV to: " + 200, LogLevel.Success, true, false);
                    return;
                case "lobregen": // Outputs whether regen is enabled (true) or disabled (false) for the lobby to chat
                    Helper.SendChatMsg("Lobby Regen: " + Convert.ToBoolean(OptionsHolder.regen));
                    return;
                case "private": // Privates the lobby (no player can publicly join unless invited)
                    Helper.ToggleLobbyVisibility(false);
                    return;
                case "public": // Publicizes the lobby (any player can join through quick match)
                    Helper.ToggleLobbyVisibility(true);
                    return;
                case "invite": // Builds a "join game" link (same one you'd find on a steam profile) for lobby and copies it to clipboard
                    GUIUtility.systemCopyBuffer = Helper.GetJoinGameLink();
                    Helper.SendChatMsg("Join link copied to clipboard!", LogLevel.Success, true, CmdOutputVisibility[cmd]);
                    return;
                case "translate": // Enables/disables the auto-translate system for chat
                    Helper.IsTranslating = !Helper.IsTranslating;
                    Helper.SendChatMsg("Toggled Auto-Translate.", LogLevel.Success, Helper.IsTranslating, false);
                    return;
                case "lobhp": // Outputs the HP setting for the lobby to chat
                    Helper.SendChatMsg("Lobby HP: " + OptionsHolder.HP);
                    return;
                case "ping": // Outputs the ping for the specified player. In this case, it would send nothing since the local user's ping is not recorded
                    Helper.SendChatMsg("Can't ping yourself!", LogLevel.Warning, true, CmdOutputVisibility[cmd]);
                    return;
                case "rainbow": // Enables/disables the rainbow system, TODO: Work on improving and fixing this!
                    Helper.ToggleRainbow();
                    Helper.SendChatMsg("Toggled PlayerRainbow.", LogLevel.Success, Helper.RainbowEnabled, false);
                    return;
                case "id": // Outputs the specified user's SteamID
                    GUIUtility.systemCopyBuffer = SteamUser.GetSteamID().ToString();
                    Helper.SendChatMsg("My SteamID copied to clipboard", LogLevel.Success, true, CmdOutputVisibility[cmd]);
                    return;
                case "winnerhp": // Enables/Disables system for outputting the HP of the winner after each round to chat
                    Helper.HPWinner = !Helper.HPWinner;
                    Helper.SendChatMsg("Toggled WinnerHP Announcer.", LogLevel.Success, Helper.HPWinner, false);
                    return;
                case "nuky": // Enables/disables Nuky chat mode
                    Helper.NukChat = !Helper.NukChat;
                    if (Helper.RoutineUsed != null) Helper.LocalChat.StopCoroutine(Helper.RoutineUsed);
                    Helper.SendChatMsg("Toggled NukyChat.", LogLevel.Success, Helper.NukChat, false);
                    return;
                case "lowercase": // Enables/Disables chat messages always being sent in lowercase
                    Helper.OnlyLower = !Helper.OnlyLower;
                    Helper.SendChatMsg("Toggled LowercaseOnly.", LogLevel.Success, Helper.OnlyLower, false);
                    return;
                case "suicide": // Kills user
                    Helper.localNetworkPlayer.UnitWasDamaged(5, true, DamageType.LocalDamage, true);
                    Helper.SendChatMsg("I've perished.", LogLevel.Success, true, CmdOutputVisibility[cmd]);
                    return;
                case "help": // Opens up the steam overlay to the GitHub readme, specifically the Chat Commands section
                    SteamFriends.ActivateGameOverlayToWebPage("https://github.com/Mn0ky/QOL-Mod#chat-commands");
                    return;
                case "ver": // Outputs mod version number to chat
                    Helper.SendChatMsg(Plugin.VersionNumber, LogLevel.Success, true, CmdOutputVisibility[cmd]);
                    return;
                default: // Command is invalid or improperly specified
                    Helper.SendChatMsg("Command not found.", LogLevel.Warning, true, false);
                    return;
            }
        }

        public static void DoubleArgument(string[] cmds, string msg)
        {
            ushort targetID;
                
            switch (cmds[0])
            {
                case "logpublic":
                    var targetCmdForPublic = cmds[1].Replace("\"", "").Replace("/", "");
                    if (targetCmdForPublic == "all")
                    {
                        Helper.SendChatMsg("Toggled public logging for all commands.", LogLevel.Success, 
                            true, false);
                        Helper.AllOutputPublic = true;
                        SaveCmdVisibilityStates();
                        return;
                    }
                    if (CmdList.Contains("/" + targetCmdForPublic))
                    {
                        CmdOutputVisibility[targetCmdForPublic] = true;
                        Helper.SendChatMsg("Toggled public logging for " + targetCmdForPublic + ".", LogLevel.Success, 
                            true, false);
                        SaveCmdVisibilityStates();
                        return;
                    }    
                    Helper.SendChatMsg("Specified command not found.", LogLevel.Warning, true, false);
                    return;
                case "logprivate":
                    var targetCmdForPrivate = cmds[1].Replace("\"", "").Replace("/", "");
                    if (targetCmdForPrivate == "all")
                    {
                        Helper.SendChatMsg("Toggled private logging for all applicable commands.", LogLevel.Success, 
                            true, false);
                        Helper.AllOutputPublic = false;
                        SaveCmdVisibilityStates();
                        return;
                    }
                    if (CmdList.Contains("/" + targetCmdForPrivate))
                    {
                        CmdOutputVisibility[targetCmdForPrivate] = false;
                        Helper.SendChatMsg("If applicable, toggled private logging for " + targetCmdForPrivate + ".", LogLevel.Success, 
                            true, false);
                        SaveCmdVisibilityStates();
                        return;
                    }    
                    Helper.SendChatMsg("Specified command not found.", LogLevel.Warning, true, false);
                    return;
                case "friend":
                    var steamID = Helper.GetSteamID(Helper.GetIDFromColor(cmds[1]));
                    SteamFriends.ActivateGameOverlayToUser("friendadd", steamID);
                    return;
                case "profile":
                    SteamFriends.ActivateGameOverlayToUser("steamid", Helper.GetSteamID(Helper.GetIDFromColor(cmds[1])));
                    return;
                case "hp": // Outputs HP of targeted color to chat
                    var targetHP = new PlayerHP(cmds[1]);
                    Helper.SendChatMsg(targetHP.FullColor + " HP: " + targetHP.HP);
                    return;
                case "fps":
                    var targetFPS = int.Parse(cmds[1]);
                    if (targetFPS < 60)
                    {
                        Helper.SendChatMsg("Target FPS cannot be below 60.", LogLevel.Warning, true, false);
                        return;
                    } 
                    Application.targetFrameRate = targetFPS;
                    Helper.SendChatMsg("Target framerate is now: " + targetFPS, LogLevel.Success, true, 
                        CmdOutputVisibility["fps"]);
                    return;
                case "shrug": // Appends shrug emoticon to end of chat message
                    msg = msg.Replace("/shrug", "") + " \u00af\\_" + Plugin.ConfigEmoji.Value + "_/\u00af";
                    Helper.SendChatMsg(msg);
                    return;
                case "stat": // Outputs a stat of the local user (WeaponsThrown, Falls, BulletShot, and etc.)
                    var myStats = Helper.localNetworkPlayer.GetComponentInParent<CharacterStats>();
                    Helper.SendChatMsg("My " + cmds[1] + ": " + Helper.GetTargetStatValue(myStats, cmds[1]), LogLevel.Success, 
                        true, CmdOutputVisibility[cmds[0]]);
                    return;
                case "music": // Music commands
                    switch (cmds[1])
                    {
                        case "skip": // Skips to the next song or if all have been played, a random one
                            Helper.IsSongLoop = false;
                            var nextSongMethod = AccessTools.Method(typeof(MusicHandler), "PlayNext");
                            nextSongMethod.Invoke(MusicHandler.Instance, null);
                            Helper.SendChatMsg("Current song skipped.", LogLevel.Success, true, CmdOutputVisibility[cmds[0]]);
                            return;
                        case "loop": // Loops the current song
                            Helper.IsSongLoop = !Helper.IsSongLoop;
                            Helper.SendChatMsg("Song looping toggled.", LogLevel.Success, Helper.IsSongLoop, CmdOutputVisibility[cmds[0]]);
                            return;
                        default:
                            Helper.SendChatMsg("Please specify a parameter. Use /help to see them all.",
                                LogLevel.Success, true, CmdOutputVisibility[cmds[0]]);
                            return;
                    }
                case "id": // Outputs the specified player's SteamID
                    PlayerSteamID targetSteamID = new (cmds[1]);
                    GUIUtility.systemCopyBuffer = targetSteamID.SteamID;
                    Helper.SendChatMsg(targetSteamID.FullColor + "'s steamID copied to clipboard!", LogLevel.Success, 
                        true, CmdOutputVisibility[cmds[0]]);
                    return;
                case "ping": // Outputs the specified player's ping
                    PlayerPing targetPing = new (cmds[1]);
                    Helper.SendChatMsg(targetPing.FullColor + targetPing.Ping, LogLevel.Success, true, CmdOutputVisibility[cmds[0]]);
                    return;
                case "mute": // Mutes the specified player (Only for the current lobby and only client-side)
                    targetID = Helper.GetIDFromColor(cmds[1]);

                    if (!Helper.MutedPlayers.Contains(targetID))
                    {
                        Helper.MutedPlayers.Add(targetID);
                        Helper.SendChatMsg("Muted: " + Helper.GetColorFromID(targetID), LogLevel.Success, true, CmdOutputVisibility[cmds[0]]);
                        return;
                    }

                    Helper.MutedPlayers.Remove(targetID);
                    Helper.SendChatMsg("Unmuted: " + Helper.GetColorFromID(targetID), LogLevel.Success, true, CmdOutputVisibility[cmds[0]]);
                    return;
                default: // Command is invalid or improperly specified
                    Helper.SendChatMsg("Command not found.", LogLevel.Warning, true, false);
                    return;
            }
        }

        public static void TripleArgument(string[] cmds, string msg)
        {
            switch (cmds[0])
            {
                case "shrug": // Appends shrug emoticon to end of chat message
                    msg = msg.Replace("/shrug", "") + " \u00af\\_" + Plugin.ConfigEmoji.Value + "_/\u00af";
                    Helper.SendChatMsg(msg);
                    return;
                case "stat": // Outputs a stat of the specified player (WeaponsThrown, Falls, BulletShot, and etc.)
                    if (cmds[1] == "all")
                    {
                        var statMsg = GameManager.Instance.mMultiplayerManager.PlayerControllers
                            .Where(player => player != null)
                            .Aggregate("",
                                (current,
                                        player) => current +
                                                   Helper.GetColorFromID((ushort)player.playerID) +
                                                   ", " +
                                                   cmds[2] +
                                                   ": " +
                                                   Helper.GetTargetStatValue(player.GetComponent<CharacterStats>(),
                                                       cmds[2]) +
                                                   "\n");

                        Helper.SendChatMsg(statMsg, LogLevel.Success, true, CmdOutputVisibility[cmds[0]]);
                        return;
                    }    
                    
                    var targetStats = new PlayerStat(cmds[1]);
                    Helper.SendChatMsg(
                        targetStats.FullColor + ", " + cmds[2] + ": " +
                        Helper.GetTargetStatValue(targetStats.Stats, cmds[2]), LogLevel.Success, true,
                        CmdOutputVisibility[cmds[0]]);
                    return;
                case "resolution":
                    Screen.SetResolution(int.Parse(cmds[1]), int.Parse(cmds[2]),
                        Convert.ToBoolean(OptionsHolder.fullscreen));
                    Helper.SendChatMsg("Set new resolution of: " + cmds[1] + "x" + cmds[2], LogLevel.Success, CmdOutputVisibility[cmds[0]]);
                    return;
                case "music":
                    switch (cmds[1])
                    {
                        case "play": // Plays song that corresponds to the specified index (0 to # of songs - 1)
                            var songIndex = int.Parse(cmds[2]);
                            var musicHandler = MusicHandler.Instance;

                            if (songIndex > musicHandler.myMusic.Length - 1 || songIndex < 0)
                            {
                                Helper.SendChatMsg(
                                    $"Invalid index: input must be between 0 and {musicHandler.myMusic.Length - 1}.",
                                    LogLevel.Warning, true, CmdOutputVisibility[cmds[0]]);
                                return;
                            }

                            Traverse.Create(musicHandler).Field("currntSong").SetValue(songIndex);

                            var audioSource = musicHandler.GetComponent<AudioSource>();
                            audioSource.clip = musicHandler.myMusic[songIndex].clip;
                            audioSource.volume = musicHandler.myMusic[songIndex].volume;
                            audioSource.Play();

                            Helper.SendChatMsg(
                                $"Now playing song #{songIndex} out of {musicHandler.myMusic.Length - 1}.",
                                LogLevel.Success, true, CmdOutputVisibility[cmds[0]]);
                            return;
                        default:
                            Helper.SendChatMsg("Please specify a parameter. Use /help to see them all.",
                                LogLevel.Success, true, CmdOutputVisibility[cmds[0]]);
                            return;
                    }
                default:
                    Helper.SendChatMsg("Command not found.", LogLevel.Warning, true, false);
                    return;
            }
        }

        private static void SaveCmdVisibilityStates()
            => File.WriteAllText(Plugin.CmdVisibilityStatesPath, MarshalCmdVisibilityStates().ToString());

        private static JSONObject MarshalCmdVisibilityStates()
        {
            var cmdStatesJson = new JSONObject();
            
            foreach (var pair in CmdOutputVisibility)
                cmdStatesJson.Add(pair.Key, pair.Value);

            return cmdStatesJson;
        }

        public enum LogLevel
        {
            PublicMsg,
            Success,
            Warning
        }

        public static readonly List<string> CmdList = new() 
        {
            "/adv",
            "/fov",
            "/fps",
            "/friend",
            "/gg",
            "/help",
            "/hp",
            "/id",
            "/invite",
            "/lobhp",
            "/lobregen",
            "/logprivate",
            "/logpublic",
            "/lowercase",
            "/nuky",
            "/mute",
            "/music",
            "/music play",
            "/music loop",
            "/music skip",
            "/ouch",
            "/ping",
            "/private",
            "/profile",
            "/public",
            "/rainbow",
            "/resolution",
            "/rich",
            "/shrug",
            "/stat",
            "/suicide",
            "/translate",
            "/uncensor",
            "/uwu", 
            "/ver",
            "/winnerhp",
            "/winstreak"
        };
        
        // All cmd visibilities are private by default unless winnerhp
        public static readonly Dictionary<string, bool> CmdOutputVisibility =
            CmdList.ToDictionary(cmd => cmd.Remove(0, 1), cmd => cmd.Contains("winnerhp"));
    }
}
