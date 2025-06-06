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
    public static void StartMethodPostfix(ChatManager __instance, ref TMP_InputField ___chatField)
    {
        var playerID = Traverse.Create(__instance)
            .Field("m_NetworkPlayer")
            .GetValue<NetworkPlayer>()
            .NetworkSpawnID;
            
        // Assigns m_NetworkPlayer value to Helper.localNetworkPlayer if networkPlayer is ours
        Helper.InitValues(__instance, playerID);
        ___chatField.restoreOriginalTextOnEscape = false; // Manually clear text on escape using Update()
    }
        
    // Transpiler patch for Update() of ChatManager; Adds CIL instructions to call CheckForArrowKeys()
    public static IEnumerable<CodeInstruction> UpdateMethodTranspiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator ilGen) 
    {
        var stopTypingMethod = AccessTools.Method(typeof(ChatManager), "StopTyping");
        var chatFieldInfo = AccessTools.Field(typeof(ChatManager), "chatField");
        var getKeyDownMethod = AccessTools.Method(typeof(Input), nameof(Input.GetKeyDown), new[] {typeof(KeyCode)});
        var checkForArrowKeysMethod = AccessTools.Method(typeof(ChatManagerPatches), nameof(CheckForArrowKeysAndAutoComplete));
        var clearChatFieldMethod = AccessTools.Method(typeof(ChatManagerPatches), nameof(ClearChatField));
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
            
            instructionList.InsertRange(i - 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                // Gets value of chatField field
                new CodeInstruction(OpCodes.Ldfld, chatFieldInfo),
                // Calls ClearChatField() with value of chatField
                new CodeInstruction(OpCodes.Call, clearChatFieldMethod)
            });
                
            break;
        }
            
        return instructionList.AsEnumerable(); // Returns the now modified list of IL instructions
    }

    public static void ClearChatField(TMP_InputField chatField) => chatField.text = "";

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

    public static bool ReplaceUnacceptableWordsMethodPrefix(ref string message, ref string __result)
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
        var args = message.TrimStart(Command.CmdPrefix).Trim().Split(' '); // Sanitising input
        
        var targetCommandTyped = args[0];

        if (!ChatCommands.CmdDict.ContainsKey(targetCommandTyped)) // If command is not found
        {
            Helper.SendModOutput("Specified command or it's alias not found. See /help for full list of commands.", 
                Command.LogType.Warning, false);
            return;
        }
        
        ChatCommands.CmdDict[targetCommandTyped].Execute(args.Skip(1).ToArray()); // Skip first element (original cmd)
    }

    // Checks if the up-arrow or down-arrow key is pressed, if so then
    // set the chatField.text to whichever message the user stops on
    public static void CheckForArrowKeysAndAutoComplete(TMP_InputField chatField)
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) && _upArrowCounter < _backupTextList.Count)
        {
            chatField.text = _backupTextList[_upArrowCounter];
            _upArrowCounter++;
            
            chatField.DeactivateInputField(); // Necessary to properly update carat pos
            chatField.stringPosition = chatField.text.Length;
            chatField.ActivateInputField();
            
            return;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) && _upArrowCounter > 0)
        {
            _upArrowCounter--;
            chatField.text = _backupTextList[_upArrowCounter];
            
            chatField.DeactivateInputField(); // Necessary to properly update carat pos
            chatField.stringPosition = chatField.text.Length;
            chatField.ActivateInputField();
            
            return;
        }

        const string rTxtFmt = "<#000000BB><u>";
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
                    var richTxtStartPos = txt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                    if (richTxtStartPos != -1 && txt.Substring(0, richTxtStartPos) == cmdMatch)
                    {
                        chatField.text = cmdMatch;
                        return;
                    }
    
                    if (Input.GetKeyDown(KeyCode.Tab))
                    {
                        chatField.DeactivateInputField(); // Necessary to properly update carat pos
                        chatField.text = cmdMatch;
                        chatField.stringPosition = chatField.text.Length;
                        chatField.ActivateInputField();
                    }
                    
                    return;
                }
                
                chatField.richText = true;
                chatField.text += txtLen <= cmdMatchLen ? rTxtFmt + cmdMatch.Substring(txtLen) : Command.CmdPrefix;
            }
            else if (chatField.richText)
            { // Already a cmd typed
                var cmdAndParam = parsedTxt.Split(' ');
                var cmdDetectedIndex = ChatCommands.CmdNames.IndexOf(cmdAndParam[0]);

                if (cmdDetectedIndex == -1)
                {
                    var effectStartPos = txt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                    if (effectStartPos == -1)
                        // This will only occur if a cmd is fully typed and then more chars are added after
                        return;

                    chatField.text = txt.Remove(effectStartPos);
                    return;
                }

                var cmdMatch = ChatCommands.CmdNames[cmdDetectedIndex];
                var targetCmd = ChatCommands.CmdDict[cmdMatch.Substring(1)];
                var targetCmdParams = targetCmd.AutoParams;

                if (targetCmdParams == null) return; // Cmd may not take any params
                if (cmdAndParam.Length <= 1 || cmdAndParam[0].Length != cmdMatch.Length) return;
                
                // Focusing on auto-completing the parameter now
                var paramTxt = cmdAndParam![1].Replace(" ", "");
                var paramTxtLen = paramTxt.Length;
                    
                //Debug.Log("paramTxt: \"" + paramTxt + "\"");
                var paramsMatched = targetCmdParams.FindAll(
                        word => word.StartsWith(paramTxt, StringComparison.InvariantCultureIgnoreCase));
                
                // Len check is band-aid so spaces don't break it, this will affect dev on nest parameters if it happens
                if (paramsMatched.Count > 0 && cmdAndParam.Length < 3)
                {
                    var paramMatch = paramsMatched[0];
                    var paramMatchLen = paramMatch.Length;
                            
                    if (paramTxtLen == paramMatchLen)
                    {
                        var paramRichTxtStartPos = paramTxt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                        if (paramRichTxtStartPos != -1 && paramTxt.Substring(0, paramRichTxtStartPos) == paramMatch)
                        {
                            chatField.text = chatField.text.Remove(txtLen - paramMatchLen - rTxtFmt.Length + 1, 14);
                            return;
                        }
                                
                        if (Input.GetKeyDown(KeyCode.Tab))
                        {   // Auto-completes the suggested parameter. Input field is made immutable so str pos is set correctly
                            chatField.DeactivateInputField();

                            if (ReferenceEquals(targetCmdParams, PlayerUtils.PlayerColorsParams))
                            {   // Change player color to 1 letter variant to encourage shorthand alternative
                                // Multiply by 2 to get correct shorthand index for color
                                var colorIndex = Helper.GetIDFromColor(paramMatch) * 2;
                                paramMatch = PlayerUtils.PlayerColorsParams[colorIndex];
                            }
                            
                            // string.Remove() so we don't rely on the update loop to remove the rich txt leftovers
                            chatField.text = txt.Remove(txtLen - paramMatchLen - rTxtFmt.Length) + paramMatch;
                            chatField.stringPosition = chatField.text.Length;
                            chatField.ActivateInputField();
                        }
                                
                        return;
                    }
                    
                    chatField.text += rTxtFmt + paramMatch.Substring(paramTxtLen);
                    chatField.richText = true;
                }
                else if (chatField.richText) // TODO: Implement support for rich text as argument input
                {
                    var effectStartPos = txt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
                    if (effectStartPos == -1) return;

                    chatField.text = txt.Remove(effectStartPos);
                }
            }
        }
        else if (chatField.richText)
        {
            var effectStartPos = txt.IndexOf(rTxtFmt, StringComparison.InvariantCultureIgnoreCase);
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
    public static string UwUify(string targetText)
    {
        var i = 0;
        var newMessage = new StringBuilder(targetText);
        while (i < newMessage.Length)
        {
            if (!char.IsLetter(newMessage[i]))
            {
                i++;
                continue;
            }
            
            var c = char.ToLower(newMessage[i]);
            var nextC = i < newMessage.Length - 1 ? char.ToLower(newMessage[i + 1]) : '\0';
            
            switch (c)
            {
                case 'r' or 'l':
                    newMessage[i] = char.IsUpper(newMessage[i]) ? 'W' : 'w';
                    break;
                case 't' when nextC == 'h':
                    newMessage[i] = char.IsUpper(newMessage[i]) ? 'D' : 'd';
                    newMessage.Remove(i + 1, 1);
                    break;
                case 'n' when nextC != ' ' && nextC != 'g' && nextC != 't' && nextC != 'd':
                    newMessage.Insert(i + 1, char.IsUpper(newMessage[i]) ? 'Y' : 'y');
                    break;
                default:
                    if (Helper.IsVowel(c) && nextC == 't') 
                        newMessage.Insert(i + 1, char.IsUpper(newMessage[i]) ? 'W' : 'w');
                    break;
            }
            i++;
        }

        return newMessage.ToString();
    }

    private static int _upArrowCounter; // Holds how many times the up-arrow key is pressed while typing
    //private static bool _startedTypingParam;

    // List to contain previous messages sent by us (up to 20)
    private static List<string> _backupTextList = new(21) 
    {
        "" // has an empty string so that the list isn't null when attempting to perform on it
    };
}