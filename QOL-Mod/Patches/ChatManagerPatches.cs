using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;


namespace QOL
{
    public class ChatManagerPatches
    {
        public static void Patches(Harmony harmonyInstance) // ChatManager methods to patch with the harmony __instance
        {
            var startMethod = AccessTools.Method(typeof(ChatManager), "Start");
            var startMethodPostfix = new HarmonyMethod(typeof(ChatManagerPatches)
                .GetMethod(nameof(StartMethodPostfix))); // Patches Start() with prefix method
            harmonyInstance.Patch(startMethod, postfix: startMethodPostfix);

            var updateMethod = AccessTools.Method(typeof(ChatManager), "Update");
            var updateMethodTranspiler = new HarmonyMethod(typeof(ChatManagerPatches)
                .GetMethod(nameof(UpdateMethodTranspiler))); // Patches Update() with transpiler method
            harmonyInstance.Patch(updateMethod, transpiler: updateMethodTranspiler);

            var stopTypingMethod = AccessTools.Method(typeof(ChatManager), "StopTyping");
            var stopTypingMethodPostfix = new HarmonyMethod(typeof(ChatManagerPatches)
                .GetMethod(nameof(StopTypingMethodPostfix))); // Patches StopTyping() with postfix method
            harmonyInstance.Patch(stopTypingMethod, postfix: stopTypingMethodPostfix);

            var sendChatMessageMethod = AccessTools.Method(typeof(ChatManager), "SendChatMessage");
            var sendChatMessageMethodPrefix = new HarmonyMethod(typeof(ChatManagerPatches)
                .GetMethod(nameof(SendChatMessageMethodPrefix)));
            harmonyInstance.Patch(sendChatMessageMethod, prefix: sendChatMessageMethodPrefix);

            var replaceUnacceptableWordsMethod = AccessTools.Method(typeof(ChatManager), "ReplaceUnacceptableWords");
            var replaceUnacceptableWordsMethodPrefix = new HarmonyMethod(typeof(ChatManagerPatches)
                .GetMethod(nameof(ReplaceUnacceptableWordsMethodPrefix)));
            harmonyInstance.Patch(replaceUnacceptableWordsMethod, prefix: replaceUnacceptableWordsMethodPrefix);

            var talkMethod = AccessTools.Method(typeof(ChatManager), "Talk");
            var talkMethodPostfix = new HarmonyMethod(typeof(ChatManagerPatches).GetMethod(nameof(TalkMethodPostfix)));
            harmonyInstance.Patch(talkMethod, postfix: talkMethodPostfix);
        }
        
        // TODO: Remove unneeded parameters and perhaps this entire method
        public static void StartMethodPostfix(ChatManager __instance)
        {
            var playerID = Traverse.Create(__instance)
                .Field("m_NetworkPlayer")
                .GetValue<NetworkPlayer>()
                .NetworkSpawnID;
            
            // Assigns m_NetworkPlayer value to Helper.localNetworkPlayer if networkPlayer is ours
            Helper.InitValues(__instance, playerID);
        }
        
        // TODO: Refactor to use InsertRange() and no index-specific instructions
        // Transpiler patch for Update() of ChatManager; Adds CIL instructions to call CheckForArrowKeys()
        public static IEnumerable<CodeInstruction> UpdateMethodTranspiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator ilGen) 
        {
            var stopTypingMethod = AccessTools.Method(typeof(ChatManager), "StopTyping");
            var checkForArrowKeysMethod = AccessTools.Method(typeof(ChatManagerPatches), "CheckForArrowKeys"); 
            var instructionList = instructions.ToList(); // Creates list of IL instructions for Update() from enumerable

            for (var i = 0; i < instructionList.Count; i++)
            {
                if (!instructionList[i].Calls(stopTypingMethod)) continue;
                
                var jumpToCheckForArrowKeysLabel = ilGen.DefineLabel();

                var instruction0 = instructionList[17];
                instruction0.opcode = OpCodes.Brfalse_S;
                instruction0.operand = jumpToCheckForArrowKeysLabel;
                instruction0.labels.Clear();

                var instruction1 = new CodeInstruction(OpCodes.Ldarg_0);
                instruction1.labels.Add(jumpToCheckForArrowKeysLabel);
                instructionList.Insert(20, instruction1);
                
                // Gets value of chatField field
                var instruction2 = new CodeInstruction(OpCodes.Ldfld, instructionList[9].operand); 
                instructionList.Insert(21, instruction2);
                
                // Calls CheckForArrowKeys() with value of chatField
                var instruction3 = new CodeInstruction(OpCodes.Call, checkForArrowKeysMethod);
                instructionList.Insert(22, instruction3);
                break;
            }
            
            return instructionList.AsEnumerable(); // Returns the now modified list of IL instructions
        }

        public static void StopTypingMethodPostfix()
        {
            Debug.Log("ChatManagerPatches.upArrowCounter : " + _upArrowCounter);
            _upArrowCounter = 0; // When player is finished typing, reset the counter for # of up-arrow presses
        }
        
        public static bool SendChatMessageMethodPrefix(ref string message, ChatManager __instance)
        {
            if (_backupTextList[0] != message && message.Length <= 350) SaveForUpArrow(message);

            if (message.StartsWith("/"))
            {
                Commands(message);
                return false;
            }

            if (Helper.UwuifyText && !string.IsNullOrEmpty(message) && Helper.localNetworkPlayer.HasLocalControl)
            {
                if (Helper.NukChat)
                {
                    message = UwUify(message);
                    Helper.RoutineUsed = WaitCoroutine(message);
                    __instance.StartCoroutine(Helper.RoutineUsed);
                    return false;
                }

                Helper.localNetworkPlayer.OnTalked(UwUify(message));
                return false;
            }

            if (Helper.NukChat)
            {
                if (Helper.OnlyLower) message = message.ToLower();
                Helper.RoutineUsed = WaitCoroutine(message);
                __instance.StartCoroutine(Helper.RoutineUsed);
                return false;
            }

            if (Helper.OnlyLower)
            {
                Helper.localNetworkPlayer.OnTalked(message.ToLower());
                return false;
            }

            return true;
        }

        public static bool ReplaceUnacceptableWordsMethodPrefix(ref string message, ref string __result) // Prefix method for patching the original (ReplaceUnacceptableWordsMethod)
        {
            if (Helper.ChatCensorshipBypass)
            {
                Debug.Log("skipping censorship");
                __result = message;
                return false;
            }

            Debug.Log("censoring message");
            return true;
        }
        
        // Method which increases duration of a chat message by set amount in config
        public static void TalkMethodPostfix(ref float ___disableChatIn)
        {
            var extraTime = Plugin.ConfigMsgDuration.Value;
            if (extraTime > 0) ___disableChatIn += extraTime;
        }

        private static void Commands(string message)
        {
            Debug.Log("Made it to beginning of commands!");
            var text = message.ToLower().TrimStart('/').Split(' ');

            switch (text.Length)
            {
                case 1:
                    ChatCommands.SingleArgument(text[0], message);
                    return;
                case 2:
                    ChatCommands.DoubleArgument(text, message);
                    return;
                default:
                    ChatCommands.TripleArgument(text, message);
                    return;
            }
        }
        
        // Checks if the up-arrow or down-arrow key is pressed, if so then
        // set the chatField.text to whichever message the user stops on
        public static void CheckForArrowKeys(TMP_InputField chatField)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) && _upArrowCounter < _backupTextList.Count)
            {
                Debug.Log("UpArrow, current: " + _upArrowCounter);
                chatField.text = _backupTextList[_upArrowCounter];
                _upArrowCounter++;
                Debug.Log("UpArrow, now: " + _upArrowCounter);
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) && _upArrowCounter > 0)
            {
                Debug.Log("DownArrow, current: " + _upArrowCounter);
                _upArrowCounter--;
                chatField.text = _backupTextList[_upArrowCounter];
                Debug.Log("DownArrow, now: " + _upArrowCounter);
            }
        }
        
        // Checks if the message should be inserted then inserts it into the 0th index of backup list
        private static void SaveForUpArrow(string backupThisText)
        {
            if (_backupTextList.Count <= 20)
            {
                _backupTextList.Insert(0, backupThisText);
                return;
            }

            _backupTextList.RemoveAt(19);
            _backupTextList.Insert(0, backupThisText);
        }

        private static IEnumerator WaitCoroutine(string msg)
        {
            var msgParts = msg.Split(' ');

            foreach (var text in msgParts)
            {
                Helper.localNetworkPlayer.OnTalked(text);
                yield return new WaitForSeconds(0.45f);
            }
        }
        
        // UwUifies a message if possible, not perfect
        private static string UwUify(string targetText)
        {
            var i = 0;
            var newMessage = new StringBuilder(targetText.ToLower()).Append(0);
            while (i < newMessage.Length)
            {
                if (!char.IsLetter(newMessage[i]))
                {
                    i++;
                    continue;
                }
                var c = newMessage[i];
                switch (c)
                {
                    case 'r' or 'l':
                        newMessage[i] = 'w';
                        break;
                    case 't' when newMessage[i + 1] == 'h':
                        newMessage[i] = 'd';
                        newMessage.Remove(i + 1, 1);
                        break;
                    default:
                        if (Helper.IsVowel(c) && newMessage[i + 1] == 't') newMessage.Insert(i + 1, 'w');
                        break;
                }
                i++;
            }
            
            return newMessage.Remove(newMessage.Length - 1, 1).ToString();
        }

        private static int _upArrowCounter; // Holds how many times the up-arrow key is pressed while typing
        
        // List to contain previous messages sent by us (up to 20)
        private static List<string> _backupTextList = new(21) 
        {
            "" // has an empty string so that the list isn't null when attempting to perform on it
        };
    }
}