using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using System.Reflection.Emit;


namespace QOL
{
    class MultiplayerManagerPatches
    {
        public static void Patches(Harmony harmonyInstance) // Multiplayer methods to patch with the harmony instance
        {

            var OnServerJoinedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnServerJoined");
            var OnServerJoinedMethodPostfix = new HarmonyMethod(typeof(MultiplayerManagerPatches).GetMethod(nameof(MultiplayerManagerPatches.OnServerJoinedMethodPostfix))); // Patches OnServerJoined with postfix method
            harmonyInstance.Patch(OnServerJoinedMethod, postfix: OnServerJoinedMethodPostfix);

            var OnServerCreatedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnServerCreated");
            var OnServerCreatedMethodPostfix = new HarmonyMethod(typeof(MultiplayerManagerPatches).GetMethod(nameof(MultiplayerManagerPatches.OnServerCreatedMethodPostfix))); // Patches OnServerCreated with postfix method
            harmonyInstance.Patch(OnServerCreatedMethod, postfix: OnServerCreatedMethodPostfix);

            var InitDataFromServerRecievedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnInitFromServer");
            var InitDataFromServerRecievedMethodTranspiler = new HarmonyMethod(typeof(MultiplayerManagerPatches).GetMethod(nameof(MultiplayerManagerPatches.InitDataFromServerRecievedMethodTranspiler))); // Patches InitDataFromServerRecieved() with transpiler method
            harmonyInstance.Patch(InitDataFromServerRecievedMethod, postfix: InitDataFromServerRecievedMethodTranspiler);
        }

        public static void OnServerJoinedMethodPostfix()
        {
            InitGUI();
        }

        public static void OnServerCreatedMethodPostfix()
        {
            InitGUI();
        }

        public static void InitDataFromServerRecievedMethodTranspiler(ref byte[] data, MultiplayerManager __instance)
        {
            Color defaultColor = new(1, 1, 1);

            if (Helper.customPlayerColor == defaultColor) return;
            Debug.Log("Color not equal to nothing: " + Helper.customPlayerColor);

            Material[] myColors = Traverse.Create(__instance).Field("m_Colors").GetValue() as Material[];
            if (oldColor != defaultColor)
            {
                myColors[(int)playerByte].SetColor("_Color", oldColor);
                Debug.Log("Old color was not default!");
            }

            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                {
                    binaryReader.ReadByte();
                    playerByte = binaryReader.ReadByte();
                }
            }
            oldColor = myColors[playerByte].color;
            Debug.Log("Old color is: " + oldColor);

            myColors[(int)playerByte].SetColor("_Color", Helper.customPlayerColor);
            Debug.Log("color I wanted: " + Helper.customPlayerColor);
            Debug.Log("color I set it to: "+ myColors[playerByte].color.ToString());
        }

        public static void InitGUI()
        {
            try
            {
                new GameObject("GUIHandler").AddComponent<GUIManager>();
                Debug.Log("Added GUIManager from MultiplayerManagerPatches!");
            }
            catch (Exception ex)
            {
                Debug.Log("Exception on starting GUIManager: " + ex.Message);
            }
        }

        private static byte playerByte;
        private static Color oldColor = new(1, 1, 1);
    }
}

