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

            // var InitDataFromServerRecievedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnInitFromServer");
            // var InitDataFromServerRecievedMethodPostfix = new HarmonyMethod(typeof(MultiplayerManagerPatches).GetMethod(nameof(MultiplayerManagerPatches.InitDataFromServerRecievedMethodPostfix))); // Patches InitDataFromServerRecieved() with transpiler method
            // harmonyInstance.Patch(InitDataFromServerRecievedMethod, postfix: InitDataFromServerRecievedMethodPostfix);

            var OnPlayerSpawnedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnPlayerSpawned");
            var OnPlayerSpawnedMethodPostfix = new HarmonyMethod(typeof(MultiplayerManagerPatches).GetMethod(nameof(MultiplayerManagerPatches.OnPlayerSpawnedMethodPostfix)));
            harmonyInstance.Patch(OnPlayerSpawnedMethod, postfix: OnPlayerSpawnedMethodPostfix);
        }

        public static void OnServerJoinedMethodPostfix()
        {
            InitGUI();
        }

        public static void OnServerCreatedMethodPostfix()
        {
            InitGUI();
        }

        public static void OnPlayerSpawnedMethodPostfix(MultiplayerManager __instance)
        {
            //Debug.Log(__instance.LocalPlayerIndex);
            if (Helper.customPlayerColor == defaultColor && !__instance.ConnectedClients[__instance.LocalPlayerIndex].ControlledLocally) return;
            //count += 1;

            Debug.Log("My index: " + __instance.LocalPlayerIndex);
            Debug.Log("Checking players");
            foreach (var player in UnityEngine.Object.FindObjectsOfType<NetworkPlayer>())
            {
                if (player.NetworkSpawnID != __instance.LocalPlayerIndex)
                {
                    Debug.Log("id not equal");
                    if (colorsToReset.Contains(player.NetworkSpawnID))
                    {
                        Debug.Log("resetting now, id of: " + player.NetworkSpawnID);
                        Debug.Log("resetting with old color: " + ColorUtility.ToHtmlStringRGBA(Helper.defaultColors[player.NetworkSpawnID]));

                        var oldCharacter = player.transform.root.gameObject;
                        Debug.Log("Assigned old character");

                        foreach (var t in oldCharacter.GetComponentsInChildren<LineRenderer>())
                        {
                            t.sharedMaterial.color = Helper.defaultColors[player.NetworkSpawnID];
                            Debug.Log("Assigned old color");
                        }

                        foreach (SpriteRenderer spriteRenderer in oldCharacter.GetComponentsInChildren<SpriteRenderer>())
                        {
                            spriteRenderer.color = Helper.defaultColors[player.NetworkSpawnID];
                            spriteRenderer.GetComponentInParent<SetColorWhenDamaged>().startColor = Helper.defaultColors[player.NetworkSpawnID];
                        }

                        foreach (var partSys in oldCharacter.GetComponentsInChildren<ParticleSystem>())
                        {
                            partSys.startColor = Helper.defaultColors[player.NetworkSpawnID];
                        }
                        Traverse.Create(oldCharacter.GetComponentInChildren<BlockAnimation>()).Field("startColor").SetValue(Helper.defaultColors[player.NetworkSpawnID]);

                        colorsToReset.Remove(player.NetworkSpawnID);
                    }
                    continue;
                }

                Debug.Log("doing color stuff");

                Debug.Log("Found ourselves!");
                if (!colorsToReset.Contains(player.NetworkSpawnID))
                {
                    colorsToReset.Add(player.NetworkSpawnID);
                    Debug.Log("reset count: " + colorsToReset.Count);
                }
                var character = player.transform.root.gameObject;
                Debug.Log("Assigned character");

                foreach (var t in character.GetComponentsInChildren<LineRenderer>())
                {
                    t.sharedMaterial.color = Helper.customPlayerColor;
                    Debug.Log("Assigned color");
                }

                foreach (SpriteRenderer spriteRenderer in character.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (spriteRenderer.transform.tag != "DontChangeColor")
                    {
                        spriteRenderer.color = Helper.customPlayerColor;
                    }
                }
            }
        }

        /*public static void InitDataFromServerRecievedMethodPostfix(ref byte[] data, MultiplayerManager __instance)
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

            myColors[playerByte].SetColor("_Color", Helper.customPlayerColor);
            Debug.Log("color I wanted: " + Helper.customPlayerColor);
            Debug.Log("color I set it to: "+ myColors[playerByte].color);
        }*/



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

        //private static byte playerByte;
        //private static Color oldColor = new(1, 1, 1);
        private static Color defaultColor = new(1, 1, 1);
        private static List<int> colorsToReset = new(3);
        //private static int count = 0;
        //private static List<LineRenderer> oldLineRenderers = new(6);
        //private static List<SpriteRenderer> oldSpriteRenderers = new(6);
        //private static ushort oldID;
    }
}

