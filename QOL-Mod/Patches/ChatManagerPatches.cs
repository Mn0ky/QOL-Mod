using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace QOL;

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
        
    // Transpiler patch for Update() of ChatManager; Adds CIL instructions to call CheckForArrowKeys()
    public static IEnumerable<CodeInstruction> UpdateMethodTranspiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator ilGen) 
    {
        var stopTypingMethod = AccessTools.Method(typeof(ChatManager), "StopTyping");
        var chatFieldInfo = AccessTools.Field(typeof(ChatManager), "chatField");
        var getKeyDownMethod = AccessTools.Method(typeof(Input), nameof(Input.GetKeyDown), new[] {typeof(KeyCode)});
        var checkForArrowKeysMethod = AccessTools.Method(typeof(ChatManagerPatches), nameof(CheckForArrowKeysAndAutoComplete));
        var instructionList = instructions.ToList(); // Creates list of IL instructions for Update() from enumerable

        for (var i = 0; i < instructionList.Count; i++)
        {
            if (!instructionList[i].Calls(stopTypingMethod) || !instructionList[i - 3].Calls(getKeyDownMethod)) 
                continue;
                
            var jumpToCheckForArrowKeysLabel = ilGen.DefineLabel();
                    
            var instruction0 = instructionList[i - 2];
            instruction0.opcode = OpCodes.Brfalse_S;
            instruction0.operand = jumpToCheckForArrowKeysLabel;
            instruction0.labels.Clear();
                
            instructionList.InsertRange(i + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0).WithLabels(jumpToCheckForArrowKeysLabel),
                // Gets value of chatField field
                new CodeInstruction(OpCodes.Ldfld, chatFieldInfo),
                // Calls CheckForArrowKeys() with value of chatField
                new CodeInstruction(OpCodes.Call, checkForArrowKeysMethod)
            });
                
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

        if (message.StartsWith(Command.CmdPrefix))
        {
            FindAndRunCommand(message);
            return false;
        }

        if (ChatCommands.CmdDict["uwu"].IsEnabled && !string.IsNullOrEmpty(message) && Helper.localNetworkPlayer.HasLocalControl)
        {
            if (ChatCommands.CmdDict["nuky"].IsEnabled)
            {
                message = UwUify(message);
                Helper.RoutineUsed = WaitCoroutine(message);
                __instance.StartCoroutine(Helper.RoutineUsed);
                return false;
            }

            Helper.SendPublicOutput(UwUify(message));
            return false;
        }

        if (ChatCommands.CmdDict["nuky"].IsEnabled)
        {
            if (ChatCommands.CmdDict["lowercase"].IsEnabled) 
                message = message.ToLower();
                
            Helper.RoutineUsed = WaitCoroutine(message);
            __instance.StartCoroutine(Helper.RoutineUsed);
            return false;
        }

        if (ChatCommands.CmdDict["lowercase"].IsEnabled)
        {
            Helper.localNetworkPlayer.OnTalked(message.ToLower());
            return false;
        }

        return true;
    }

    public static bool ReplaceUnacceptableWordsMethodPrefix(ref string message, ref string __result) // Prefix method for patching the original (ReplaceUnacceptableWordsMethod)
    {
        if (ChatCommands.CmdDict["uncensor"].IsEnabled)
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
        var extraTime = ConfigHandler.GetEntry<float>("MsgDuration");
        if (extraTime > 0) ___disableChatIn += extraTime;
    }

    private static void FindAndRunCommand(string message)
    {
        Debug.Log("User is trying to run a command...");
        var args = message.ToLower().TrimStart(Command.CmdPrefix).Split(' ');
        var targetCommandTyped = args[0];

        if (!ChatCommands.CmdDict.ContainsKey(targetCommandTyped)) // If command is not found
        {
            Helper.SendModOutput("Specified command or it's alias not found. See /help for full list of commands.", 
                Command.LogType.Warning, false);
            return;
        }
            
        ChatCommands.CmdDict[targetCommandTyped].Execute(args);
    }
        
    // Checks if the up-arrow or down-arrow key is pressed, if so then
    // set the chatField.text to whichever message the user stops on
    public static void CheckForArrowKeysAndAutoComplete(TMP_InputField chatField)
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) && _upArrowCounter < _backupTextList.Count)
        {
            chatField.text = _backupTextList[_upArrowCounter];
            _upArrowCounter++;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) && _upArrowCounter > 0)
        {
            _upArrowCounter--;
            chatField.text = _backupTextList[_upArrowCounter];
        }

        var txt = chatField.text;
        var txtLen = txt.Length;
        var parsedTxt = chatField.textComponent.GetParsedText();
        // Remove last char of non-richtext str since a random space is added from GetParsedText() 
        parsedTxt = parsedTxt.Remove(parsedTxt.Length - 1);

        if (txtLen > 0 && txt[0] == Command.CmdPrefix)
        {
            // Credit for this easy way of getting the closest matching string from a list
            //https://forum.unity.com/threads/auto-complete-text-field.142181/#post-1741569
            var cmdsMatched = ChatCommands.CmdNames.FindAll(
                word => word.StartsWith(parsedTxt, StringComparison.InvariantCultureIgnoreCase));

            if (cmdsMatched.Count > 0)
            {
                var cmdMatch = cmdsMatched[0];
                var cmdMatchLen = cmdMatch.Length;

                if (chatField.richText && parsedTxt.Length == cmdMatchLen)
                {
                    // Check if cmd has been manually fully typed, if so remove its rich text
                    var richTxtStartPos = txt.IndexOf("<#000000BB>", StringComparison.InvariantCultureIgnoreCase);
                    if (richTxtStartPos != -1 && txt.Substring(0, richTxtStartPos) == cmdMatch)
                    {
                        Debug.Log("setting chaText to cmdMatch");
                        chatField.text = cmdMatch;
                        return;
                    }

                    if (Input.GetKeyDown(KeyCode.Tab))
                    {
                        chatField.DeactivateInputField();
                        chatField.text = cmdMatch;
                        chatField.stringPosition = chatField.text.Length;
                        chatField.ActivateInputField();
                    }
                    
                    return;
                }
                
                Debug.Log("Setting chattext to cmd substr");
                chatField.richText = true;
                chatField.text += "<#000000BB><u>" + cmdMatch.Substring(txtLen);
            }
            else if (chatField.richText)
            {
                var cmdsDetected = ChatCommands.CmdNames.FindAll(word =>
                    parsedTxt.StartsWith(word, StringComparison.InvariantCultureIgnoreCase));
                
                if (cmdsDetected.Count == 0)
                {
                    var effectStartPos = txt.IndexOf("<#000000BB>", StringComparison.InvariantCultureIgnoreCase);
                    if (effectStartPos == -1)
                    {
                        // This will only occur if a cmd is fully typed and then more chars are added after
                        chatField.richText = false;
                        return;
                    }

                    chatField.text = txt.Remove(effectStartPos);
                    chatField.richText = false;
                    return;
                }
                
                var cmdMatch = cmdsDetected[0];
                var targetCmd = ChatCommands.CmdDict[cmdMatch.Substring(1)];
                var targetCmdParams = targetCmd.AutoParams;
                var cmdAndParam = parsedTxt.Split(' ');
                
                if (targetCmdParams == null) return;
                if (cmdAndParam.Length <= 1 || cmdAndParam[0].Length != cmdMatch.Length) return;
                
                // Focusing on auto-completing the parameter now
                Debug.Log("Focusing on auto-completing the parameter now");
                var paramTxt = cmdAndParam![1];
                var paramTxtLen = paramTxt.Length;
                var paramsMatched = targetCmdParams.FindAll(
                        word => word.StartsWith(paramTxt, StringComparison.InvariantCultureIgnoreCase));

                if (paramsMatched.Count > 0)
                {
                    Debug.Log("Got a param match!");
                
                    var paramMatch = paramsMatched[0];
                    var paramMatchLen = paramMatch.Length;
                            
                    if (chatField.richText && paramTxtLen == paramMatchLen)
                    {
                        Debug.Log("paramTxt length == paraMatch length!!");
                        var paramRichTxtStartPos = paramTxt.IndexOf("<#000000BB>", StringComparison.InvariantCultureIgnoreCase);
                        if (paramRichTxtStartPos != -1 && paramTxt.Substring(0, paramRichTxtStartPos) == paramMatch)
                        {
                            // "<#000000BB><u>".Length - 1 == 13
                            Debug.Log("Removing rich txt cause fully typed : " + chatField.text);
                            chatField.text = chatField.text.Remove(txtLen - paramMatchLen - 13, 14);
                            return;
                        }
                                
                        if (Input.GetKeyDown(KeyCode.Tab))
                        {
                            chatField.DeactivateInputField();
                            chatField.text += paramMatch;
                            chatField.stringPosition = chatField.text.Length;
                            chatField.ActivateInputField();
                        }
                                
                        return;
                    }
                
                    var tempStr = "<#000000BB><u>" + paramMatch.Substring(paramTxtLen);
                    Debug.Log("Adding param to chatfield!!! : " + tempStr);
                    chatField.text += tempStr;
                    chatField.richText = true;
                }
                else if (chatField.richText)
                {
                    var effectStartPos = txt.IndexOf("<#000000BB>", StringComparison.InvariantCultureIgnoreCase);
                    
                    if (effectStartPos == -1)
                    {
                        // Occurs when a cmd is sent, richtext needs to be reset
                        chatField.richText = false;
                        return; 
                    }
                    
                    Debug.Log("Removing rich txt... : " + txt);
                    chatField.text = txt.Remove(effectStartPos);
                    chatField.richText = false;
                }
            }
        }
        else if (chatField.richText)
        {
            var effectStartPos = txt.IndexOf("<#000000BB>", StringComparison.InvariantCultureIgnoreCase);
            if (effectStartPos == -1)
            {
                // Occurs when a cmd is sent, richtext needs to be reset
                chatField.richText = false;
                return; 
            }
            chatField.text = txt.Remove(effectStartPos);
            chatField.richText = false;
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
            Helper.SendPublicOutput(text);
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
            var nextC = newMessage[i + 1];
            switch (c)
            {
                case 'r' or 'l':
                    newMessage[i] = 'w';
                    break;
                case 't' when nextC == 'h':
                    newMessage[i] = 'd';
                    newMessage.Remove(i + 1, 1);
                    break;
                case 'n' when nextC != ' ' && nextC != 'g' && nextC != 't' && nextC != 'd':
                    newMessage.Insert(i + 1, 'y');
                    break;
                default:
                    if (Helper.IsVowel(c) && nextC == 't') newMessage.Insert(i + 1, 'w');
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