using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;

namespace QOL
{
    // UNUSED, left for reference purposes
    class OnlinePlayerUIPatch
    {
        public static void Patch(Harmony harmonyInstance) // GameManager methods to patch with the harmony __instance
        {
            var UpdateMethod = AccessTools.Method(typeof(OnlinePlayerUI), "Update");
            var UpdateMethodTranspiler= new HarmonyMethod(typeof(OnlinePlayerUIPatch).GetMethod(nameof(OnlinePlayerUIPatch.UpdateMethodTranspiler))); // Patches NetworkAllPlayersDiedButOne() with postfix method
            harmonyInstance.Patch(UpdateMethod, transpiler: UpdateMethodTranspiler);
        }

        public static IEnumerable<CodeInstruction> UpdateMethodTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
        {
            Debug.Log("Displaying instructions for Update() in OnlinePlayerUI\n**********************************************");

            //MethodInfo checkForCustomNameMethod = typeof(OnlinePlayerUIPatch).GetMethod(nameof(CheckForCustomName), BindingFlags.Static | BindingFlags.Public); // Get MethodInfo for checkForCustomName() //TODO: uncom for testing
            MethodInfo HelloWorldMethod = typeof(OnlinePlayerUIPatch).GetMethod(nameof(HelloWorld), BindingFlags.Static | BindingFlags.Public);
            //Debug.Log("checkForCustomNameMethod: " + checkForCustomNameMethod);
            List<CodeInstruction> instructionsList = instructions.ToList();

            var len = instructionsList.Count;
            
            // Label jumpToLdargLabel = ilGen.DefineLabel();
            //
            // CodeInstruction instruction0 = instructionsList[42];
            // instruction0.labels.Add(jumpToLdargLabel);

            /*CodeInstruction instruction0_5 = instructionsList[35];
            instruction0_5.opcode = OpCodes.Call;
            instruction0_5.operand = HelloWorldMethod;

            CodeInstruction instruction0_6 = new CodeInstruction(OpCodes.Ldarg_0);
            instructionsList.Insert(36, instruction0_6);*/

            CodeInstruction instruction1 = new CodeInstruction(OpCodes.Ldloc_1);
            instruction1.labels.Add(instructionsList[35].labels[0]);
            instructionsList.Insert(35, instruction1);
            Debug.Log("instruction1 labels: " + instruction1.labels[0].ToString());

            instructionsList[36].labels.Clear();

            //CodeInstruction instruction2 = new CodeInstruction(OpCodes.Call, checkForCustomNameMethod); //TODO: uncom for testing
            //instructionsList.Insert(39, instruction2); //TODO: uncom for testing

            instructionsList.RemoveRange(40, 4);

            instructionsList.RemoveAt(58);
            instructionsList[57].opcode = OpCodes.Brtrue;
            instructionsList[57].operand = instructionsList[82].labels[0];
            instructionsList[58].labels.Clear();

            // CodeInstruction instruction1_5 = new CodeInstruction(OpCodes.Ldarg_0);
            // instructionsList.Insert(36, instruction1_5);
            //
            // CodeInstruction instruction2 = new CodeInstruction(OpCodes.Call, checkForCustomNameMethod);
            // instructionsList.Insert(39, instruction2);

            for (var i = 0; i < instructionsList.Count; i++)
            {
                 Debug.Log(i + "\t" + instructionsList[i]);
            }

            return instructionsList.AsEnumerable();
        }

        // public static void CheckForCustomName(int i, TextMeshProUGUI[] playerNames, ConnectedClientData clientData) //TODO: uncom for testing
        // {
        //     Debug.Log("Made it to check for custom name!");
        //     if (i == Helper.localNetworkPlayer.NetworkSpawnID && Helper.isCustomName) // Add conditional if working on this to check Helper.isCustomName
        //     {
        //         Debug.Log("custom name wanted: " + Plugin.configCustomName.Value);
        //         playerNames[i].text = "this is a test";
        //     }
        //     playerNames[i].text = clientData.PlayerName;
        //     Debug.Log("more test message");
        // }

        public static void HelloWorld()
        {
            Debug.Log("Hello World test");
        }
    }
}
