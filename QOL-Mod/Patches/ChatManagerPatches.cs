using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Steamworks;


namespace QOL
{
    public class ChatManagerPatches
    {
        public static void Patches(Harmony harmonyInstance) // ChatManager methods to patch with the harmony instance
        {
            var StartMethod = AccessTools.Method(typeof(ChatManager), "Start");
            var StartMethodPostfix = new HarmonyMethod(typeof(ChatManagerPatches).GetMethod(nameof(ChatManagerPatches.StartMethodPostfix))); // Patches Start() with prefix method
            harmonyInstance.Patch(StartMethod, postfix: StartMethodPostfix);

            var UpdateMethod = AccessTools.Method(typeof(ChatManager), "Update");
            var UpdateMethodTranspiler = new HarmonyMethod(typeof(ChatManagerPatches).GetMethod(nameof(ChatManagerPatches.UpdateMethodTranspiler))); // Patches Update() with transpiler method
            harmonyInstance.Patch(UpdateMethod, transpiler: UpdateMethodTranspiler);

            var StopTypingMethod = AccessTools.Method(typeof(ChatManager), "StopTyping");
            var StopTypingMethodPostfix = new HarmonyMethod(typeof(ChatManagerPatches).GetMethod(nameof(ChatManagerPatches.StopTypingMethodPostfix))); // Patches StopTyping() with postfix method
            harmonyInstance.Patch(StopTypingMethod, postfix: StopTypingMethodPostfix);

            var SendChatMessageMethod = AccessTools.Method(typeof(ChatManager), "SendChatMessage");
            var SendChatMessageMethodPrefix = new HarmonyMethod(typeof(ChatManagerPatches).GetMethod(nameof(ChatManagerPatches.SendChatMessageMethodPrefix))); // Patches SendChatMessage() with prefix method
            harmonyInstance.Patch(SendChatMessageMethod, prefix: SendChatMessageMethodPrefix);

            var ReplaceUnacceptableWordsMethod = AccessTools.Method(typeof(ChatManager), "ReplaceUnacceptableWords");
            var ReplaceUnacceptableWordsMethodPrefix = new HarmonyMethod(typeof(ChatManagerPatches).GetMethod(nameof(ChatManagerPatches.ReplaceUnacceptableWordsMethodPrefix))); // Patches ReplaceUnacceptableWords() with prefix method
            harmonyInstance.Patch(ReplaceUnacceptableWordsMethod, prefix: ReplaceUnacceptableWordsMethodPrefix);
        }

        public static void StartMethodPostfix(ChatManager __instance)
        {
            NetworkPlayer localNetworkPlayer = Traverse.Create(__instance).Field("m_NetworkPlayer").GetValue() as NetworkPlayer;
            Helper.AssignLocalNetworkPlayer(localNetworkPlayer); // Assigns value of m_NetworkPlayer to Helper.localNetworkPlayer if the networkPlayer is ours

        }

        public static IEnumerable<CodeInstruction> UpdateMethodTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen) // Transpiler patch for Update() of ChatManager; Adds CIL instructions to call CheckForArrowKeys()
        {

            var StopTypingMethod = typeof(ChatManager).GetMethod("StopTyping", BindingFlags.Instance | BindingFlags.NonPublic);
            var CheckForArrowKeysMethod = typeof(ChatManagerPatches).GetMethod(nameof(CheckForArrowKeys), BindingFlags.Static | BindingFlags.Public);
            var list = instructions.ToList();
            var len = list.Count;
            for (var i = 0; i < len; i++)
            {
                if (list[i].Calls(StopTypingMethod))
                {
                    Label jumpToCheckForArrowKeysLabel = ilGen.DefineLabel();

                    CodeInstruction instruction0 = list[17];
                    instruction0.opcode = OpCodes.Brfalse_S;
                    instruction0.operand = jumpToCheckForArrowKeysLabel;
                    instruction0.labels.Clear();

                    CodeInstruction instruction1 = new CodeInstruction(OpCodes.Ldarg_0);
                    instruction1.labels.Add(jumpToCheckForArrowKeysLabel);
                    list.Insert(20, instruction1);

                    Debug.Log("list[9].operand" + list[9].operand);
                    CodeInstruction instruction2 = new CodeInstruction(OpCodes.Ldfld, list[9].operand);
                    list.Insert(21, instruction2);

                    CodeInstruction instruction3 = new CodeInstruction(OpCodes.Call, CheckForArrowKeysMethod);
                    list.Insert(22, instruction3);
                    break;
                }
            }

            for (var i = 0; i < len; i++)
            {
                Debug.Log(i + "\t" + list[i]);
            }

            return list.AsEnumerable();
        }

        public static void StopTypingMethodPostfix()
        {
            Debug.Log("ChatManagerPatches.upArrowCounter : " + ChatManagerPatches.upArrowCounter);
            ChatManagerPatches.upArrowCounter = 0; // When player is finished typing, reset the counter for # of uparrow presses
        }
        public static bool SendChatMessageMethodPrefix(ref string message, ChatManager __instance) // Prefix method for patching the original (SendChatMessageMethod)
        {
            ChatManagerPatches.SaveForUpArrow(message);
            if (message.StartsWith("/"))
            {
                ChatManagerPatches.Commands(message, __instance);
                return false;
            }

            return true;
        }
        public static bool ReplaceUnacceptableWordsMethodPrefix(ref string message, ref string __result) // Prefix method for patching the original (ReplaceUnacceptableWordsMethod)
        {
            if (Helper.chatCensorshipBypass)
            {
                Debug.Log("skipping censorship");
                __result = message;
                return false;
            }

            Debug.Log("censoring message");
            return true;
        }

        public static void Commands(string message, ChatManager __instance)
        {
            Debug.Log("Made it to beginning of commands!");
            string text = message.ToLower();
            text = text.TrimStart(new char[] {'/'});

            if (text.Contains("hp") && Helper.localNetworkPlayer.HasLocalControl) // Sends HP of targeted color to chat
            {
                if (text.Length > 2)
                {
                    string colorWanted = text.Substring(3);
                    Debug.Log("Helper.localNetworkPlayer : " + Helper.localNetworkPlayer);
                    Debug.Log("Helper.localNetworkPlayer.NetworkSpawnID : " + Helper.localNetworkPlayer.NetworkSpawnID);
                    Helper.localNetworkPlayer.OnTalked(colorWanted + " HP: " + Helper.GetHPOfPlayer(colorWanted));
                    return;
                }

                Debug.Log("Looking for my health!");
                Debug.Log("Helper.localNetworkPlayer : " + Helper.localNetworkPlayer);
                Debug.Log("Helper.localNetworkPlayer.NetworkSpawnID : " + Helper.localNetworkPlayer.NetworkSpawnID);
                string localHealth = Helper.localNetworkPlayer.GetComponentInChildren<HealthHandler>().health.ToString() + "%";
                Debug.Log("Current Health: " + localHealth);
                Helper.localNetworkPlayer.OnTalked("My HP: " + localHealth);

            }
            else if (text == "gg") // Enables or disables automatic "gg" upon death
            {
                Helper.autoGG = !Helper.autoGG;
            }
            else if (text == "uncensor") // Enables or disables skip for chat censorship
            {
                Helper.chatCensorshipBypass = !Helper.chatCensorshipBypass;
                Debug.Log("Helper.chatCensorshipBypass : " + Helper.chatCensorshipBypass);
            }
            else if (text.Contains("shrug")) // Adds shrug emoticon to end of chat message
            {
                message = message.Replace("/shrug", "");
                message += " \u00af\\_(ツ)_/\u00af";
                Helper.localNetworkPlayer.OnTalked(message);

            }
            else if (text == "rich") // Enables rich text for chat messages
            {
                TextMeshPro theText = Traverse.Create(__instance).Field("text").GetValue() as TextMeshPro;
                theText.richText = !theText.richText;

            }
            else if (text == "private") // Privates the lobby (no player can publicly join unless invited)
            {
                SteamMatchmaking.SetLobbyJoinable(Helper.lobbyID, false);
                Helper.localNetworkPlayer.OnTalked("Lobby is now private!");
            }
            else if (text == "public") // Publicizes the lobby (any player can join through quick match)
            {
                SteamMatchmaking.SetLobbyJoinable(Helper.lobbyID, true);
                Helper.localNetworkPlayer.OnTalked("Lobby is now public!");
            }
            else if (
                text == "invite") // Builds a "join game" link (same one you'd find on a steam profile) for lobby and copies it to clipboard
            {
                Debug.Log("LobbyID: " + Helper.lobbyID);
                Debug.Log("Verification test, should return 25: " + SteamMatchmaking.GetLobbyData(Helper.lobbyID, StickFightConstants.VERSION_KEY));
                Helper.GetJoinGameLink();
                Helper.localNetworkPlayer.OnTalked("Join link copied to clipboard!");
            }
            else if (text == "translate") // Whether or not to enable automatic translations
            {
                Helper.isTranslating = !Helper.isTranslating;
            }
        }
            
        public static void CheckForArrowKeys(TMP_InputField chatField) // Checks if uparrow or downarrow keys are pressed, if so then set the chatField.text to whichever message the user stops on
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) && ChatManagerPatches.upArrowCounter < ChatManagerPatches.backupTextList.Count)
            {
                Debug.Log("UpArrow, current: " + ChatManagerPatches.upArrowCounter);
                chatField.text = ChatManagerPatches.backupTextList[ChatManagerPatches.upArrowCounter];
                ChatManagerPatches.upArrowCounter++;
                Debug.Log("UpArrow, now: " + ChatManagerPatches.upArrowCounter);
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) && ChatManagerPatches.upArrowCounter > 0)
            {
                Debug.Log("DownArrow, current: " + ChatManagerPatches.upArrowCounter);
                ChatManagerPatches.upArrowCounter--;
                chatField.text = ChatManagerPatches.backupTextList[ChatManagerPatches.upArrowCounter];
                Debug.Log("DownArrow, now: " + ChatManagerPatches.upArrowCounter);
            }
        }

        public static void SaveForUpArrow(string backupThisText) // Checks if the message should be inserted then inserts it into the 0th index of backup list 
        {
            Debug.Log("backupThisText : " + backupThisText);
            Debug.Log("backupTextList[0] : " + backupTextList[0]);
            if (!ChatManagerPatches.backupTextList.Any<string>() && backupThisText.Length <= 350)
            {
                Debug.Log("Attempting to insert");
                ChatManagerPatches.backupTextList.Add(backupThisText);
                ChatManagerPatches.backupTextList.RemoveAt(0);
            }

            if (ChatManagerPatches.backupTextList.Count == 20)
            {
                ChatManagerPatches.backupTextList.RemoveAt(19);
            }

            if (ChatManagerPatches.backupTextList[0] != backupThisText && backupThisText.Length <= 350)
            {
                ChatManagerPatches.backupTextList.Insert(0, backupThisText);
            }
        }

        public static int upArrowCounter; // Holds how many times the uparrow key is pressed

        public static List<string> backupTextList = new() // Ends up containing previous messages sent by us (up to 20)
        {
            string.Empty
        };
    }
}
