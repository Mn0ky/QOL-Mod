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
using Steamworks;


namespace QOL
{
    public class ChatManagerPatches
    {
        public static void Patches(Harmony harmonyInstance) // ChatManager methods to patch with the harmony __instance
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
            ushort playerID = Traverse.Create(__instance).Field("m_NetworkPlayer").GetValue<NetworkPlayer>().NetworkSpawnID;

            Helper.InitValues(__instance, playerID); // Assigns value of m_NetworkPlayer to Helper.localNetworkPlayer if the networkPlayer is ours (also if text should be rich or not)
        }

        public static IEnumerable<CodeInstruction> UpdateMethodTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen) // Transpiler patch for Update() of ChatManager; Adds CIL instructions to call CheckForArrowKeys()
        {

            MethodInfo StopTypingMethod = typeof(ChatManager).GetMethod("StopTyping", BindingFlags.Instance | BindingFlags.NonPublic); // Gets MethodInfo for StopTyping()
            MethodInfo CheckForArrowKeysMethod = typeof(ChatManagerPatches).GetMethod(nameof(CheckForArrowKeys), BindingFlags.Static | BindingFlags.Public); // Get MethodInfo for CheckForArrowKeys() 
            List<CodeInstruction> instructionList = instructions.ToList(); // Generates a list of CIL instructions for Update() 
            var len = instructionList.Count;
            for (var i = 0; i < len; i++)
            {
                if (instructionList[i].Calls(StopTypingMethod))
                {
                    Label jumpToCheckForArrowKeysLabel = ilGen.DefineLabel();

                    CodeInstruction instruction0 = instructionList[17];
                    instruction0.opcode = OpCodes.Brfalse_S;
                    instruction0.operand = jumpToCheckForArrowKeysLabel;
                    instruction0.labels.Clear();

                    CodeInstruction instruction1 = new CodeInstruction(OpCodes.Ldarg_0);
                    instruction1.labels.Add(jumpToCheckForArrowKeysLabel);
                    instructionList.Insert(20, instruction1);

                    // Debug.Log("list[9].operand" + instructionList[9].operand);
                    CodeInstruction instruction2 = new CodeInstruction(OpCodes.Ldfld, instructionList[9].operand); // Gets value of chatField field
                    instructionList.Insert(21, instruction2);

                    CodeInstruction instruction3 = new CodeInstruction(OpCodes.Call, CheckForArrowKeysMethod); // Calls CheckForArrowKeys() with value of chatField
                    instructionList.Insert(22, instruction3);
                    break;
                }
            }

            // TODO: Make above more flexible!
            // for (var i = 0; i < len; i++)
            // {
            //     Debug.Log(i + "\t" + instructionList[i]);
            // }

            return instructionList.AsEnumerable(); // Returns the now modified list of CIL instructions
        }

        public static void StopTypingMethodPostfix()
        {
            Debug.Log("ChatManagerPatches.upArrowCounter : " + ChatManagerPatches.upArrowCounter);
            ChatManagerPatches.upArrowCounter = 0; // When player is finished typing, reset the counter for # of uparrow presses
        }
        public static bool SendChatMessageMethodPrefix(ref string message, ChatManager __instance) // Prefix method for patching the original (SendChatMessageMethod)
        {
            if (ChatManagerPatches.backupTextList[0] != message && message.Length <= 350)
            {
                ChatManagerPatches.SaveForUpArrow(message);
            }

            if (message.StartsWith("/"))
            {
                ChatManagerPatches.Commands(message, __instance);
                return false;
            }

            else if (Helper.uwuifyText && !string.IsNullOrEmpty(message))
            {

                if (Helper.localNetworkPlayer.HasLocalControl)
                {
                    if (Helper.nukChat)
                    {
                        message = UwUify(message);
                        Helper.routineUsed = WaitCoroutine(message);
                        __instance.StartCoroutine(Helper.routineUsed);
                        return false;
                    }
                    Helper.localNetworkPlayer.OnTalked(UwUify(message));
                    return false;
                }
            }

            else if (Helper.nukChat)
            {
                if (Helper.onlyLower) message = message.ToLower();
                Helper.routineUsed = WaitCoroutine(message);
                __instance.StartCoroutine(Helper.routineUsed);
                return false;
            }

            else if (Helper.onlyLower)
            {
                Helper.localNetworkPlayer.OnTalked(message.ToLower());
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
            string[] text = message.ToLower().TrimStart('/').Split(' ');

            switch (text.Length)
            {
                case 1:
                    ChatCommands.SingleArgument(text[0], message);
                    break;
                case 2:
                    ChatCommands.DoubleArgument(text, message);
                    break;
                default:
                    ChatCommands.TripleArgument(text, message);
                    break;
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

            if (ChatManagerPatches.backupTextList.Count <= 20)
            {
                ChatManagerPatches.backupTextList.Insert(0, backupThisText);
            }
            else
            {
                ChatManagerPatches.backupTextList.RemoveAt(19);
                ChatManagerPatches.backupTextList.Insert(0, backupThisText);
            }
        }

        public static IEnumerator WaitCoroutine(string msg)
        {
            var msgParts = msg.Split(' ');

            foreach (var text in msgParts)
            {
                Helper.localNetworkPlayer.OnTalked(text);
                yield return new WaitForSeconds(0.45f);
            }
        }

        public static string UwUify(string targetText)
        {
            int i = 0;
            var newMessage = new StringBuilder(targetText.ToLower()).Append(0);   
            while (i < newMessage.Length)
            {
                if (!char.IsLetter(newMessage[i]))
                {
                    i++;
                    continue;
                }
                char c = newMessage[i];
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

        /*public static string GetTargetStatValue(CharacterStats stats, string targetStat)
        {
            foreach (var stat in typeof(CharacterStats).GetFields())
            {
                if (stat.Name.ToLower() == targetStat)
                {
                    return stat.GetValue(stats).ToString();
                }
            }

            return "No value";
        }*/

        public static int upArrowCounter; // Holds how many times the uparrow key is pressed

        public static List<string> backupTextList = new(21) // Ends up containing previous messages sent by us (up to 20)
        {
            string.Empty // Initialized with an empty string so that the list isn't null when attempting to perform on it
        };

        //private static IEnumerator coroutineUsed;
    }
}