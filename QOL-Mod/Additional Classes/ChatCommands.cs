using System;
using System.Text;
using HarmonyLib;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QOL
{

    public class ChatCommands
    {
        public static void SingleArgument(string cmd, string msg)
        {
            switch (cmd)
            {
                case "hp": // Outputs HP of targeted color to chat
                    string localHealth = Helper.GetHPOfPlayer(Helper.localNetworkPlayer.NetworkSpawnID);
                    Helper.localNetworkPlayer.OnTalked("My HP: " + localHealth);
                    break;
                case "gg": // Enables or disables automatic "gg" upon death
                    Helper.autoGG = !Helper.autoGG;
                    break;
                case "adv": // Outputs player-specified msg from config to chat, blank by default
                    Helper.localNetworkPlayer.OnTalked(Plugin.configAdvCmd.Value);
                    break;
                case "uncensor": // Enables/disables chat censorship
                    Helper.chatCensorshipBypass = !Helper.chatCensorshipBypass;
                    break;
                case "winstreak": // Enables/disables winstreak system
                    Helper.ToggleWinstreak();
                    break;
                case "rich": // Enables rich text for chat messages
                    TextMeshPro theText = Traverse.Create(Helper.localChat).Field("text").GetValue() as TextMeshPro;
                    theText.richText = !theText.richText;
                    break;
                case "shrug": // Appends shrug emoticon to end of chat message
                    msg = msg.Replace("/shrug", "") + " \u00af\\_" + Plugin.configEmoji.Value + "_/\u00af";
                    Helper.localNetworkPlayer.OnTalked(msg);
                    break;
                case "uwu": // Enables uwuifier for chat messages
                    Helper.uwuifyText = !Helper.uwuifyText;
                    break;
                case "lobregen": // Outputs whether regen is enabled (true) or disabled (false) for the lobby to chat
                    Helper.localNetworkPlayer.OnTalked("Lobby Regen: " + Convert.ToBoolean(OptionsHolder.regen));
                    break;
                case "private": // Privates the lobby (no player can publicly join unless invited)
                    Helper.ToggleLobbyVisibility(false);
                    break;
                case "public": // Publicizes the lobby (any player can join through quick match)
                    Helper.ToggleLobbyVisibility(true);
                    break;
                case "invite": // Builds a "join game" link (same one you'd find on a steam profile) for lobby and copies it to clipboard
                    GUIUtility.systemCopyBuffer = Helper.GetJoinGameLink();
                    Helper.localNetworkPlayer.OnTalked("Join link copied to clipboard!");
                    break;
                case "translate": // Enables/disables the auto-translate system for chat
                    Helper.isTranslating = !Helper.isTranslating;
                    break;
                case "lobhp": // Outputs the HP setting for the lobby to chat
                    Helper.localNetworkPlayer.OnTalked("Lobby HP: " + OptionsHolder.HP);
                    break;
                case "ping": // Outputs the ping for the specified player. In this case, it would send nothing since the local user's ping is not recorded
                    Helper.localNetworkPlayer.OnTalked("Can't ping yourself!");
                    break;
                case "rainbow": // Enables/disables the rainbow system, TODO: Work on improving and fixing this!
                    Helper.ToggleRainbow();
                    break;
                case "id": // Outputs the specified user's SteamID
                    GUIUtility.systemCopyBuffer = SteamUser.GetSteamID().ToString();
                    Helper.localNetworkPlayer.OnTalked("SteamID copied to clipboard");
                    break;
                case "winnerhp": // Enables/Disables system for outputting the HP of the winner after each round to chat
                    Helper.HPWinner = !Helper.HPWinner;
                    break;
                case "nuky": // Enables/disables Nuky chat mode
                    Helper.nukChat = !Helper.nukChat;
                    if (Helper.routineUsed != null) Helper.localChat.StopCoroutine(Helper.routineUsed);
                    break;
                case "lowercase": // Enables/Disables chat messages always being sent in lowercase
                    Helper.onlyLower = !Helper.onlyLower;
                    break;
                case "suicide":
                    Helper.localNetworkPlayer.UnitWasDamaged(5, true, DamageType.LocalDamage, true);
                    break;
                case "help": // Opens up the steam overlay to the GitHub readme, specifically the Chat Commands section
                    SteamFriends.ActivateGameOverlayToWebPage("https://github.com/Mn0ky/QOL-Mod#chat-commands");
                    break;
                case "ver": // Outputs mod version number to chat
                    Helper.localNetworkPlayer.OnTalked(Plugin.VersionNumber);
                    break;
                default: // Command is invalid or improperly specified
                    Helper.SendCommandError("Command not found.");
                    break;
            }
        }

        public static void DoubleArgument(string[] cmds, string msg)
        {
            ushort targetID;

            switch (cmds[0])
            {
                case "hp": // Outputs HP of targeted color to chat
                    string targetHealth = Helper.GetHPOfPlayer(Helper.GetIDFromColor(cmds[1]));
                    Helper.localNetworkPlayer.OnTalked(Helper.GetCapitalColor(cmds[1]) + " HP: " + targetHealth);
                    break;
                case "shrug": // Appends shrug emoticon to end of chat message
                    msg = msg.Replace("/shrug", "") + " \u00af\\_" + Plugin.configEmoji.Value + "_/\u00af";
                    Helper.localNetworkPlayer.OnTalked(msg);
                    break;
                case "stat": // Outputs a stat of the local user (WeaponsThrown, Falls, BulletShot, and etc.)
                    CharacterStats myStats = Helper.localNetworkPlayer.GetComponentInParent<CharacterStats>();
                    Helper.localNetworkPlayer.OnTalked("My " + cmds[1] + ": " + Helper.GetTargetStatValue(myStats, cmds[1]));
                    break;
                case "id": // Outputs the specified player's SteamID
                    targetID = Helper.GetIDFromColor(cmds[1]);
                    GUIUtility.systemCopyBuffer = Helper.GetSteamID(targetID).ToString();
                    Helper.localNetworkPlayer.OnTalked(Helper.GetCapitalColor(cmds[1]) + "'s steamID copied to clipboard!");
                    break;
                case "ping": // Outputs the specified player's ping
                    targetID = Helper.GetIDFromColor(cmds[1]);
                    Helper.localNetworkPlayer.OnTalked(Helper.GetCapitalColor(cmds[1]) + " Ping: " + Helper.clientData[targetID].Ping);
                    break;
                case "mute": // Mutes the specified player (Only for the current lobby and only client-side)
                    targetID = Helper.GetIDFromColor(cmds[1]);
                    if (!Helper.mutedPlayers.Contains(targetID)) Helper.mutedPlayers.Add(targetID);
                    else Helper.mutedPlayers.Remove(targetID);
                    break;
                default: // Command is invalid or improperly specified
                    Helper.SendCommandError("Command not found.");
                    break;
            }
        }

        public static void TripleArgument(string[] cmds, string msg)
        {
            switch (cmds[0])
            {
                case "shrug": // Appends shrug emoticon to end of chat message
                    msg = msg.Replace("/shrug", "") + " \u00af\\_" + Plugin.configEmoji.Value + "_/\u00af";
                    Helper.localNetworkPlayer.OnTalked(msg);
                    break;
                case "stat": // Outputs a stat of the specified player (WeaponsThrown, Falls, BulletShot, and etc.)
                    CharacterStats targetPlayerStats = Helper.GetNetworkPlayer(Helper.GetIDFromColor(cmds[1])).GetComponentInParent<CharacterStats>();
                    Helper.localNetworkPlayer.OnTalked(cmds[1] + ", " + cmds[2] + ": " + Helper.GetTargetStatValue(targetPlayerStats, cmds[2]));
                    break;
                default:
                    Helper.SendCommandError("Command not found.");
                    break;
            }
        }
    }
}
