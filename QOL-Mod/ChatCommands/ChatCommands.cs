using System;
using System.Collections.Generic;

using System.Text;
using HarmonyLib;
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
                    Helper.localNetworkPlayer.OnTalked("My HP: " + new PlayerHP(localColor).HP);
                    return;
                case "gg": // Enables or disables automatic "gg" upon death
                    Helper.autoGG = !Helper.autoGG;
                    Helper.SendLocalMsg("Toggled AutoGG.", LogLevel.Success);
                    return;
                case "adv": // Outputs player-specified msg from config to chat, blank by default
                    Helper.localNetworkPlayer.OnTalked(Plugin.configAdvCmd.Value);
                    return;
                case "uncensor": // Enables/disables chat censorship
                    Helper.chatCensorshipBypass = !Helper.chatCensorshipBypass;
                    Helper.SendLocalMsg("Toggled ChatCensorship.", LogLevel.Success);
                    return;
                case "ouch":
                    Helper.IsOwMode = !Helper.IsOwMode;
                    Helper.SendLocalMsg("Toggled OuchMode.", LogLevel.Success);
                    return;
                case "winstreak": // Enables/disables winstreak system
                    Helper.ToggleWinstreak();
                    Helper.SendLocalMsg("Toggled Winstreak system.", LogLevel.Success);
                    return;
                case "rich": // Enables rich text for chat messages
                    TextMeshPro theText = Traverse.Create(Helper.localChat).Field("text").GetValue<TextMeshPro>();
                    theText.richText = !theText.richText;
                    Helper.SendLocalMsg("Toggled Richtext.", LogLevel.Success);
                    return;
                case "shrug": // Appends shrug emoticon to end of chat message
                    msg = msg.Replace("/shrug", "") + " \u00af\\_" + Plugin.configEmoji.Value + "_/\u00af";
                    Helper.localNetworkPlayer.OnTalked(msg);
                    return;
                case "uwu": // Enables uwuifier for chat messages
                    Helper.uwuifyText = !Helper.uwuifyText;
                    Helper.SendLocalMsg("Toggled UwUifier.", LogLevel.Success);
                    return;
                case "fov":
                    Debug.Log("camera fov: " + Camera.main.fieldOfView);
                    Camera.main.fieldOfView = 200;
                    return;
                case "lobregen": // Outputs whether regen is enabled (true) or disabled (false) for the lobby to chat
                    Helper.localNetworkPlayer.OnTalked("Lobby Regen: " + Convert.ToBoolean(OptionsHolder.regen));
                    return;
                case "private": // Privates the lobby (no player can publicly join unless invited)
                    Helper.ToggleLobbyVisibility(false);
                    Helper.SendLocalMsg("Lobby made private!", LogLevel.Success);
                    return;
                case "public": // Publicizes the lobby (any player can join through quick match)
                    Helper.ToggleLobbyVisibility(true);
                    Helper.SendLocalMsg("Lobby made public!", LogLevel.Success);
                    return;
                case "invite": // Builds a "join game" link (same one you'd find on a steam profile) for lobby and copies it to clipboard
                    GUIUtility.systemCopyBuffer = Helper.GetJoinGameLink();
                    Helper.SendLocalMsg("Join link copied to clipboard!", LogLevel.Success);
                    return;
                case "translate": // Enables/disables the auto-translate system for chat
                    Helper.isTranslating = !Helper.isTranslating;
                    Helper.SendLocalMsg("Toggled Auto-Translate.", LogLevel.Success);
                    return;
                case "lobhp": // Outputs the HP setting for the lobby to chat
                    Helper.localNetworkPlayer.OnTalked("Lobby HP: " + OptionsHolder.HP);
                    return;
                case "ping": // Outputs the ping for the specified player. In this case, it would send nothing since the local user's ping is not recorded
                    Helper.SendLocalMsg("Can't ping yourself!", LogLevel.Warning);
                    return;
                case "rainbow": // Enables/disables the rainbow system, TODO: Work on improving and fixing this!
                    Helper.ToggleRainbow();
                    Helper.SendLocalMsg("Toggled PlayerRainbow.", LogLevel.Success);
                    return;
                case "id": // Outputs the specified user's SteamID
                    GUIUtility.systemCopyBuffer = SteamUser.GetSteamID().ToString();
                    Helper.SendLocalMsg("My SteamID copied to clipboard", LogLevel.Success);
                    return;
                case "winnerhp": // Enables/Disables system for outputting the HP of the winner after each round to chat
                    Helper.HPWinner = !Helper.HPWinner;
                    Helper.SendLocalMsg("Toggled WinnerHP Announcer.", LogLevel.Success);
                    return;
                case "nuky": // Enables/disables Nuky chat mode
                    Helper.nukChat = !Helper.nukChat;
                    if (Helper.routineUsed != null) Helper.localChat.StopCoroutine(Helper.routineUsed);
                    Helper.SendLocalMsg("Toggled NukyChat.", LogLevel.Success);
                    return;
                case "lowercase": // Enables/Disables chat messages always being sent in lowercase
                    Helper.onlyLower = !Helper.onlyLower;
                    Helper.SendLocalMsg("Toggled LowercaseOnly.", LogLevel.Success);
                    return;
                case "suicide": // Kills user
                    Helper.localNetworkPlayer.UnitWasDamaged(5, true, DamageType.LocalDamage, true);
                    Helper.SendLocalMsg("You are now dead.", LogLevel.Success);
                    return;
                case "help": // Opens up the steam overlay to the GitHub readme, specifically the Chat Commands section
                    SteamFriends.ActivateGameOverlayToWebPage("https://github.com/Mn0ky/QOL-Mod#chat-commands");
                    return;
                case "ver": // Outputs mod version number to chat
                    Helper.SendLocalMsg(Plugin.VersionNumber, LogLevel.Success);
                    return;
                default: // Command is invalid or improperly specified
                    Helper.SendLocalMsg("Command not found.", LogLevel.Warning);
                    return;
            }
        }

        public static void DoubleArgument(string[] cmds, string msg)
        {
            ushort targetID;
                
            switch (cmds[0])
            {
                case "hp": // Outputs HP of targeted color to chat
                    var targetHP = new PlayerHP(cmds[1]);
                    Helper.localNetworkPlayer.OnTalked(targetHP.FullColor + " HP: " + targetHP.HP);
                    return;
                case "fps":
                    var targetFPS = int.Parse(cmds[1]);
                    Application.targetFrameRate = targetFPS;
                    Helper.SendLocalMsg("Target framerate is now: " + targetFPS, LogLevel.Success);
                    return;
                case "shrug": // Appends shrug emoticon to end of chat message
                    msg = msg.Replace("/shrug", "") + " \u00af\\_" + Plugin.configEmoji.Value + "_/\u00af";
                    Helper.localNetworkPlayer.OnTalked(msg);
                    return;
                case "stat": // Outputs a stat of the local user (WeaponsThrown, Falls, BulletShot, and etc.)
                    CharacterStats myStats = Helper.localNetworkPlayer.GetComponentInParent<CharacterStats>();
                    Helper.SendLocalMsg("My " + cmds[1] + ": " + Helper.GetTargetStatValue(myStats, cmds[1]), LogLevel.Success);
                    return;
                case "music": // Music commands
                    switch (cmds[1])
                    {
                        case "skip": // Skips to the next song or if all have been played, a random one
                            Helper.IsSongLoop = false;
                            var nextSongMethod = AccessTools.Method(typeof(MusicHandler), "PlayNext");
                            nextSongMethod.Invoke(MusicHandler.Instance, null);
                            Helper.SendLocalMsg("Current song skipped.", LogLevel.Success);
                            return;
                        case "loop": // Loops the current song
                            Helper.IsSongLoop = !Helper.IsSongLoop;
                            Helper.SendLocalMsg("Song looping toggled.", LogLevel.Success);
                            return;
                        default:
                            Helper.SendLocalMsg("Please specify a parameter. Use <b>/help</b> to see them all.", LogLevel.Success);
                            return;
                    }
                case "id": // Outputs the specified player's SteamID
                    PlayerSteamID targetSteamID = new (cmds[1]);
                    GUIUtility.systemCopyBuffer = targetSteamID.SteamID;
                    Helper.SendLocalMsg(targetSteamID.FullColor + "'s steamID copied to clipboard!", LogLevel.Success);
                    return;
                case "ping": // Outputs the specified player's ping
                    PlayerPing targetPing = new (cmds[1]);
                    Helper.SendLocalMsg(targetPing.FullColor + targetPing.Ping, LogLevel.Success);
                    return;
                case "mute": // Mutes the specified player (Only for the current lobby and only client-side)
                    targetID = Helper.GetIDFromColor(cmds[1]);

                    if (!Helper.mutedPlayers.Contains(targetID))
                    {
                        Helper.mutedPlayers.Add(targetID);
                        Helper.SendLocalMsg("Muted: " + Helper.GetColorFromID(targetID), LogLevel.Success);
                        return;
                    }

                    Helper.mutedPlayers.Remove(targetID);
                    Helper.SendLocalMsg("Unmuted: " + Helper.GetColorFromID(targetID), LogLevel.Success);
                    return;
                default: // Command is invalid or improperly specified
                    Helper.SendLocalMsg("Command not found.", LogLevel.Warning);
                    return;
            }
        }

        public static void TripleArgument(string[] cmds, string msg)
        {
            switch (cmds[0])
            {
                case "shrug": // Appends shrug emoticon to end of chat message
                    msg = msg.Replace("/shrug", "") + " \u00af\\_" + Plugin.configEmoji.Value + "_/\u00af";
                    Helper.localNetworkPlayer.OnTalked(msg);
                    return;
                case "stat": // Outputs a stat of the specified player (WeaponsThrown, Falls, BulletShot, and etc.)
                    var targetStats = new PlayerStat(cmds[1]);
                    Helper.SendLocalMsg(targetStats.FullColor + ", " + cmds[2] + ": " + Helper.GetTargetStatValue(targetStats.Stats, cmds[2]), LogLevel.Success);
                    return;
                case "resolution":
                    Screen.SetResolution(int.Parse(cmds[1]), int.Parse(cmds[2]), Convert.ToBoolean(OptionsHolder.fullscreen));
                    Helper.SendLocalMsg("Set new resolution of: " + cmds[1] + "x" + cmds[2], LogLevel.Success);
                    return;
                case "music":
                    switch (cmds[1])
                    {
                        case "play": // Plays song that corresponds to the specified index (0 to # of songs - 1)
                            var songIndex = int.Parse(cmds[2]);
                            var musicHandler = MusicHandler.Instance;

                            if (songIndex > musicHandler.myMusic.Length - 1 || songIndex < 0)
                            {
                                Helper.SendLocalMsg($"Invalid index: input must be between 0 and {musicHandler.myMusic.Length - 1}.", LogLevel.Warning);
                                return;
                            }

                            Traverse.Create(musicHandler).Field("currntSong").SetValue(songIndex);

                            var audioSource = musicHandler.GetComponent<AudioSource>();
                            audioSource.clip = musicHandler.myMusic[songIndex].clip;
                            audioSource.volume = musicHandler.myMusic[songIndex].volume;
                            audioSource.Play();

                            Helper.SendLocalMsg($"Now playing song #{songIndex} out of {musicHandler.myMusic.Length - 1}.", LogLevel.Success);
                            return;
                        default:
                            Helper.SendLocalMsg("Please specify a parameter. Use <b>/help</b> to see them all.", LogLevel.Success);
                            return;
                    }
                default:
                    Helper.SendLocalMsg("Command not found.", LogLevel.Warning);
                    return;
            }
        }

        public enum LogLevel
        {
            Success,
            Warning
        }
    }

    
}
