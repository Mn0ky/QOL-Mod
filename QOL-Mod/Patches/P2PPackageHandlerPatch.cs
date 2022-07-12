using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace QOL
{
    public class P2PPackageHandlerPatch
    {
        public static void Patch(Harmony harmonyInstance)
        {
            var checkMessageTypeMethod = AccessTools.Method(typeof(P2PPackageHandler), "CheckMessageType");
            var checkMessageTypeMethodTranspiler = new HarmonyMethod(typeof(P2PPackageHandlerPatch).GetMethod(nameof(P2PPackageHandlerPatch.CheckMessageTypeMethodTranspiler))); // Patches Start() with prefix method
            harmonyInstance.Patch(checkMessageTypeMethod, transpiler: checkMessageTypeMethodTranspiler);
        }

        public static IEnumerable<CodeInstruction> CheckMessageTypeMethodTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            var onKickedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnKicked");

            List<CodeInstruction> instructionList = instructions.ToList(); // Generates a list of CIL instructions for Update() 
            var len = instructionList.Count;
              
            for (var i = 0; i < len; i++)
            {
                if (instructionList[i].Calls(onKickedMethod))
                {
                    instructionList.InsertRange(i + 1, new CodeInstruction[]
                    {
                        new CodeInstruction(OpCodes.Ldarg_3),
                        CodeInstruction.Call(typeof(P2PPackageHandlerPatch), "FindPlayerWhoSentKickPcktAndAlertUser")
                    });

                    Debug.Log("Found and patched CheckMessageType method!!");
                    break;
                }
            }

            return instructionList.AsEnumerable();
        }

        private static void FindPlayerWhoSentKickPcktAndAlertUser(CSteamID kickPacketSender)
        {
            var senderPlayerColor = Helper.GetColorFromID(Helper.clientData.First(data => data.ClientID == kickPacketSender).PlayerObject.GetComponent<NetworkPlayer>().NetworkSpawnID);
            Helper.SendLocalMsg("Attempted kick by: " + senderPlayerColor, ChatCommands.LogLevel.Warning);
        }
    }


}
