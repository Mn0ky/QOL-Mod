using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Data.Common;
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
                NetworkPlayer networkPlayer = Traverse.Create(__instance).Field("m_NetworkPlayer").GetValue() as NetworkPlayer;
                if (networkPlayer.HasLocalControl)
                {
                    if (Helper.nukChat)
                    {
                        message = UwUify(message);
                        coroutineUsed = WaitCoroutine(message);
                        __instance.StartCoroutine(coroutineUsed);
                        return false;
                    }
                    networkPlayer.OnTalked(UwUify(message));
                    return false;
                }
            }

            else if (Helper.nukChat)
            {
                coroutineUsed = WaitCoroutine(message);
                __instance.StartCoroutine(coroutineUsed);
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
            text = text.TrimStart('/');

            if (text.StartsWith("hp") && Helper.localNetworkPlayer.HasLocalControl) // Sends HP of targeted color to chat
            {
                if (text.Length > 2)
                {
                    string colorWanted = text.Substring(3);
                    Debug.Log("Helper.localNetworkPlayer : " + Helper.localNetworkPlayer);
                    Debug.Log("Helper.localNetworkPlayer.NetworkSpawnID : " + Helper.localNetworkPlayer.NetworkSpawnID);
                    Helper.localNetworkPlayer.OnTalked(Helper.GetCapitalColor(colorWanted) + " HP: " + Helper.GetHPOfPlayer(colorWanted));
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
            //else if (text == "spam")
            //{
            //    Debug.Log("clipboard: " + GUIUtility.systemCopyBuffer);
            //    __instance.StartCoroutine(WaitCoroutine(GUIUtility.systemCopyBuffer));
            //}
            // else if (text == "stopspam") 
            // {
            //     Debug.Log("stopping spam");
            //     stopSpam = true;
            // }
            else if (text == "adv")
            {
                Helper.localNetworkPlayer.OnTalked(Plugin.configAdvCmd.Value);
            }
            else if (text == "uncensor") // Enables or disables skip for chat censorship
            {
                Helper.chatCensorshipBypass = !Helper.chatCensorshipBypass;
                Debug.Log("Helper.chatCensorshipBypass : " + Helper.chatCensorshipBypass);
            }
            else if (text == "winstreak")
            {
                Helper.ToggleWinstreak();
            }
            else if (text.Contains("shrug")) // Adds shrug emoticon to end of chat message
            {
                message = message.Replace("/shrug", "");
                message += $" \u00af\\_{Plugin.configEmoji.Value}_/\u00af";
                Helper.localNetworkPlayer.OnTalked(message);

            }
            else if (text == "rich") // Enables rich text for chat messages
            {
                TextMeshPro theText = Traverse.Create(__instance).Field("text").GetValue() as TextMeshPro;
                theText.richText = !theText.richText;

            }
            /*else if (text == "testcol") // Enables rich text for chat messages
            {
                var oldCharacter = Helper.GetNetworkPlayer(0);

                foreach (SpriteRenderer spriteRenderer in oldCharacter.GetComponentsInChildren<SpriteRenderer>())
                {
                    Debug.Log("renderer name: " + spriteRenderer.gameObject);
                    //spriteRenderer.color = Helper.defaultColors[0];
                    spriteRenderer.GetComponentInParent<SetColorWhenDamaged>().startColor = Helper.defaultColors[0];
                }

                foreach (var partSys in oldCharacter.GetComponentsInChildren<ParticleSystem>())
                {
                    partSys.startColor = Helper.defaultColors[0];
                }

                Traverse.Create(oldCharacter.GetComponentInChildren<BlockAnimation>()).Field("startColor").SetValue(Helper.defaultColors[0]);
            }*/
            else if (text == "uwu") // Enables uwuifier
            {
                Helper.uwuifyText = !Helper.uwuifyText;
            }
            else if (text == "lobregen")
            {
                Helper.localNetworkPlayer.OnTalked("Lobby Regen: " + Convert.ToBoolean(OptionsHolder.regen)); 
            }
            else if (text == "private") // Privates the lobby (no player can publicly join unless invited)
            {
                if (matchmaking.IsHost)
                {
                    MethodInfo ChangeLobbyTypeMethod = typeof(MatchmakingHandler).GetMethod("ChangeLobbyType", BindingFlags.NonPublic | BindingFlags.Instance);
                    ChangeLobbyTypeMethod.Invoke(matchmaking, new object[] { ELobbyType.k_ELobbyTypeFriendsOnly});
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
                    MethodInfo ChangeLobbyTypeMethod = typeof(MatchmakingHandler).GetMethod("ChangeLobbyType", BindingFlags.NonPublic | BindingFlags.Instance);
                    ChangeLobbyTypeMethod.Invoke(matchmaking, new object[] { ELobbyType.k_ELobbyTypePublic });
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
            else if (text.StartsWith("stat"))
            {
                string[] commandArr = text.Split(' ');
                if (commandArr.Length == 3)
                {

                    CharacterStats playerStats = Helper.GetNetworkPlayer(Helper.GetIDFromColor(commandArr[1])).GetComponentInParent<CharacterStats>();

                    Helper.localNetworkPlayer.OnTalked(commandArr[1] + ", " + commandArr[2] + ": " + GetTargetStatValue(playerStats, commandArr[2])); 
                    return;
                }
                CharacterStats myStats = Helper.localNetworkPlayer.GetComponentInParent<CharacterStats>();
                Helper.localNetworkPlayer.OnTalked("My " + commandArr[1] + ": " + GetTargetStatValue(myStats, commandArr[1]));
            }
            else if (text == "translate") // Whether or not to enable automatic translations
            {
                Helper.isTranslating = !Helper.isTranslating;
            }
            else if (text == "lobhealth")
            {
                Helper.localNetworkPlayer.OnTalked("Lobby HP: " + OptionsHolder.HP);
            }
            else if (text.Contains("ping"))
            {
                ConnectedClientData[] clients = Traverse.Create(UnityEngine.Object.FindObjectOfType<OnlinePlayerUI>()).Field("mClients").GetValue() as ConnectedClientData[];
                Debug.Log("clients length: " + clients.Length);
                ushort colorWanted;

                if (text.Length > 4)
                {
                    colorWanted = Helper.GetIDFromColor(text.Substring(5));
                    Helper.localNetworkPlayer.OnTalked(Helper.GetColorFromID(colorWanted) + " Ping" + clients[colorWanted].Ping);
                    return;
                }
                Helper.localNetworkPlayer.OnTalked("Can't ping yourself!");
            }
            // TODO: Work on this later!!
            // else if (text == "rainbow")
            // {
            //     new GameObject("RainbowHandler").AddComponent<RainbowManager>();
            // }
            else if (text.StartsWith("id"))
            {
                string colorWanted = text.Substring(3);
                GUIUtility.systemCopyBuffer = Helper.GetSteamID(Helper.GetIDFromColor(colorWanted)).ToString();

                Helper.localNetworkPlayer.OnTalked(Helper.GetCapitalColor(colorWanted) + "'s steamID copied to clipboard");
            }
            else if (text == "nukychat")
            {
                Helper.nukChat = !Helper.nukChat;
                if (coroutineUsed != null) __instance.StopCoroutine(coroutineUsed);
            }
            else if (text == "winnerhp")
            {
                Helper.HPWinner = !Helper.HPWinner;
            }
            // else if (text == "customname_test")
            // {
            //     TextMeshProUGUI[] playerNames = Traverse.Create(UnityEngine.Object.FindObjectOfType<OnlinePlayerUI>()).Field("mPlayerTexts").GetValue() as TextMeshProUGUI[];
            //     playerNames[Helper.localNetworkPlayer.NetworkSpawnID].GetComponent<TextMeshProUGUI>().text = "test";
            // }
            else if (text == "help")
            {
                SteamFriends.ActivateGameOverlayToWebPage("https://github.com/Mn0ky/QOL-Mod#chat-commands");
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
            StringBuilder newMessage = new StringBuilder(targetText.ToLower()).Append(0);   
            while (i < newMessage.Length)
            {
                if (!char.IsLetter(newMessage[i]))
                {
                    i++;
                    continue;
                }
                char curChar = newMessage[i];
                switch (curChar)
                {
                    case 'r' or 'l':
                        newMessage[i] = 'w';
                        break;
                    case 't' when newMessage[i + 1] == 'h':
                        newMessage[i] = 'd';
                        newMessage.Remove(i + 1, 1);
                        break;
                    default:
                        if (Helper.IsVowel(curChar) && newMessage[i + 1] == 't')
                        {
                            newMessage.Insert(i + 1, 'w');
                        }
                        break;
                }
                i++;
            }
            return newMessage.Remove(newMessage.Length - 1, 1).ToString();
        }

        public static string GetTargetStatValue(CharacterStats stats, string targetStat)
        {
            foreach (var stat in typeof(CharacterStats).GetFields())
            {
                if (stat.Name.ToLower() == targetStat)
                {
                    return stat.GetValue(stats).ToString();
                }
            }

            return "No value";
        }

        public static int upArrowCounter; // Holds how many times the uparrow key is pressed

        public static List<string> backupTextList = new(21) // Ends up containing previous messages sent by us (up to 20)
        {
            string.Empty // Initialized with an empty string so that the list isn't null when attempting to perform on it
        };

        private static IEnumerator coroutineUsed;

        private static MatchmakingHandler matchmaking = UnityEngine.Object.FindObjectOfType<MatchmakingHandler>();
    }
}