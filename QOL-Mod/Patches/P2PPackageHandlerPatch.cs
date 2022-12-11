using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Steamworks;
using UnityEngine;

namespace QOL;

public class P2PPackageHandlerPatch
{
    public static void Patch(Harmony harmonyInstance)
    {
        var checkMessageTypeMethod = AccessTools.Method(typeof(P2PPackageHandler), "CheckMessageType");
        var checkMessageTypeMethodTranspiler = new HarmonyMethod(typeof(P2PPackageHandlerPatch)
            .GetMethod(nameof(CheckMessageTypeMethodTranspiler)));
        harmonyInstance.Patch(checkMessageTypeMethod, transpiler: checkMessageTypeMethodTranspiler);
    }

    public static IEnumerable<CodeInstruction> CheckMessageTypeMethodTranspiler(
        IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
    {
        var onKickedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnKicked");
        var instructionList = instructions.ToList();

        for (var i = 0; i < instructionList.Count; i++)
        {
            if (!instructionList[i].Calls(onKickedMethod)) continue;
                
            instructionList.InsertRange(i + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_3),
                CodeInstruction.Call(typeof(P2PPackageHandlerPatch), nameof(FindPlayerWhoSentKickPcktAndAlertUser))
            });

            Debug.Log("Found and patched CheckMessageType method!!");
            break;
        }

        return instructionList.AsEnumerable();
    }
        
    // SteamID's are Monky and Rexi
    private static void FindPlayerWhoSentKickPcktAndAlertUser(CSteamID kickPacketSender)
    {
        var senderPlayerColor = Helper.GetColorFromID(Helper.ClientData
            .First(data => data.ClientID == kickPacketSender)
            .PlayerObject.GetComponent<NetworkPlayer>()
            .NetworkSpawnID);

        if (kickPacketSender.m_SteamID is not (76561198202108442 or 76561198870040513))
        {
            Helper.TrustedKicker = false;
            Helper.SendModOutput("Blocked kick sent by: " + senderPlayerColor, Command.LogType.Warning, 
                false);
            return;
        }   

        Helper.TrustedKicker = true;
    }
}