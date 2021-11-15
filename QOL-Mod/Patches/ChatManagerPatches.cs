using System;
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
            Helper.AssignLocalNetworkPlayerAndRichText(localNetworkPlayer, __instance); // Assigns value of m_NetworkPlayer to Helper.localNetworkPlayer if the networkPlayer is ours (also if text should be rich or not)
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
                    System.Reflection.Emit.Label jumpToCheckForArrowKeysLabel = ilGen.DefineLabel();

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

            if (Helper.uwuifyText && !string.IsNullOrEmpty(message))
            {
                NetworkPlayer networkPlayer = Traverse.Create(__instance).Field("m_NetworkPlayer").GetValue() as NetworkPlayer;
                if (networkPlayer.HasLocalControl)
                {
                    networkPlayer.OnTalked(ChatManagerPatches.UwUify(message));
                    return false;
                }
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
            text = text.TrimStart('/');

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
                string localHealth = Helper.localNetworkPlayer.GetComponentInChildren<HealthHandler>().health + "%";
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
            else if (text == "uwu") // Enables uwuifier
            {
                Helper.uwuifyText = !Helper.uwuifyText;

            }
            else if (text == "private") // Privates the lobby (no player can publicly join unless invited)
            {
                if (matchmaking.IsHost)
                {
                    SteamMatchmaking.SetLobbyJoinable(Helper.lobbyID, false);
                    Helper.localNetworkPlayer.OnTalked("Lobby made private!");
                }
                else
                {
                    Helper.localNetworkPlayer.OnTalked("Need to be host!");
                }
            }
            else if (text == "public") // Publicizes the lobby (any player can join through quick match)
            {
                if (matchmaking.IsHost)
                {
                    SteamMatchmaking.SetLobbyJoinable(Helper.lobbyID, true);
                    Helper.localNetworkPlayer.OnTalked("Lobby made public!");
                }
                else
                {
                    Helper.localNetworkPlayer.OnTalked("Need to be host!");
                }
            }
            else if (text == "invite") // Builds a "join game" link (same one you'd find on a steam profile) for lobby and copies it to clipboard
            {
                Debug.Log("LobbyID: " + Helper.lobbyID);
                Debug.Log("Verification test, should return 25: " + SteamMatchmaking.GetLobbyData(Helper.lobbyID, StickFightConstants.VERSION_KEY));
                GUIUtility.systemCopyBuffer = Helper.GetJoinGameLink();
                Helper.localNetworkPlayer.OnTalked("Join link copied to clipboard!");
            }
            else if (text == "translate") // Whether or not to enable automatic translations
            {
                Helper.isTranslating = !Helper.isTranslating;
            }
            else if (text == "lobhealth")
            {
                Helper.localNetworkPlayer.OnTalked("Lobby HP: " + OptionsHolder.HP);
            }
            else if (text == "ver")
            {
                Helper.localNetworkPlayer.OnTalked(Plugin.VersionNumber);
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

        public static string UwUify(string targetText) // TODO: Improve logic here one day
        {
            StringBuilder newMessage = new StringBuilder(targetText);
            for (int i = 0; i < targetText.Length; i++)
            {
                Debug.Log("length: " + targetText.Length);
                Debug.Log("newmessage length: " + newMessage.Length);
                Debug.Log("i : " + i);
                char curChar = char.ToLower(newMessage[i]);
                Debug.Log(i + ": curchar : " + curChar);

                if (i >= newMessage.Length - 1)
                {
                    Debug.Log("breaking!");
                    break;
                }

                if (curChar == 'l' || curChar == 'r')
                {
                    Debug.Log("found r or l");
                    newMessage[i] = 'w';
                }

                else if (curChar == 't')    
                {
                    if (i + 2 < newMessage.Length)
                    {
                        Debug.Log("Found t, past message length test");
                        if (char.ToLower(newMessage[i + 1]) == 'h')
                        {
                            Debug.Log("replacing 'th' with 'd'");
                            newMessage[i] = 'd';
                            newMessage.Remove(i + 1, 1); // Perhaps use replace() method here?
                        }
                    }
                }

                if (curChar is 'a' or 'e' or 'i' or 'o' or 'u') // Maybe use || instead of is/or
                {
                    Debug.Log("Found vowel");
                    if (i + 2 < newMessage.Length)
                    {
                        if (char.ToLower(newMessage[i + 1]) == 't')
                        {
                            newMessage.Insert(i + 1, 'w');
                        }
                    }
                }
            }
            Debug.Log("newMessage : " + newMessage);

            return newMessage.ToString();
        }

        public static int upArrowCounter; // Holds how many times the uparrow key is pressed

        public static List<string> backupTextList = new() // Ends up containing previous messages sent by us (up to 20)
        {
            string.Empty // Initialized with an empty string so that the list isn't null when attempting to perform on it
        };

        private static MatchmakingHandler matchmaking = UnityEngine.Object.FindObjectOfType<MatchmakingHandler>();
    }
}