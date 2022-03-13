using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using System.Reflection.Emit;
using TMPro;


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
            if (Helper.customPlayerColor == defaultColor) return;

            Debug.Log("My index: " + __instance.LocalPlayerIndex);
            Debug.Log("Checking players");
            foreach (var player in UnityEngine.Object.FindObjectsOfType<NetworkPlayer>())
            {
                if (player.NetworkSpawnID != __instance.LocalPlayerIndex)
                {
                    if (colorsToReset.Contains(player.NetworkSpawnID))
                    {
                        Debug.Log("id not equal");
                        Debug.Log("resetting now, id of: " + player.NetworkSpawnID);
                        //Debug.Log("resetting with old color: " + ColorUtility.ToHtmlStringRGBA(Helper.defaultColors[player.NetworkSpawnID]));

                        var oldCharacter = player.transform.root.gameObject;
                        var oldColor = Helper.defaultColors[player.NetworkSpawnID];
                        Debug.Log("Assigned old character");

                        ChangeLineRendColor(oldColor, oldCharacter);
                        ChangeSpriteRendColor(oldColor, oldCharacter);

                        foreach (var partSys in oldCharacter.GetComponentsInChildren<ParticleSystem>())
                        {
                            partSys.startColor = oldColor;
                        }

                        Traverse.Create(oldCharacter.GetComponentInChildren<BlockAnimation>()).Field("startColor").SetValue(oldColor);
                        ChangeWinTextColor(oldColor, player.NetworkSpawnID);

                        colorsToReset.Remove(player.NetworkSpawnID);
                    }
                    continue;
                }
                Debug.Log("Found ourselves!");
                if (!colorsToReset.Contains(player.NetworkSpawnID))
                {
                    colorsToReset.Add(player.NetworkSpawnID);
                    Debug.Log("reset count: " + colorsToReset.Count);
                }

                var character = player.transform.root.gameObject;
                Debug.Log("Assigned character");

                ChangeLineRendColor(Helper.customPlayerColor, character);
                ChangeSpriteRendColor(Helper.customPlayerColor, character);
                ChangeWinTextColor(Helper.customPlayerColor, player.NetworkSpawnID);
            }
        }

        public static void ChangeSpriteRendColor(Color colorWanted, GameObject character)
        {
            foreach (SpriteRenderer spriteRenderer in character.GetComponentsInChildren<SpriteRenderer>())
            {
                spriteRenderer.color = colorWanted;
                spriteRenderer.GetComponentInParent<SetColorWhenDamaged>().startColor = colorWanted;
            }
        }

        public static void ChangeLineRendColor(Color colorWanted, GameObject character)
        {
            foreach (var t in character.GetComponentsInChildren<LineRenderer>())
            {
                t.sharedMaterial.color = colorWanted;
                Debug.Log("Assigned color");
            }
        }

        public static void ChangeWinTextColor(Color colorWanted, int playerID) // TODO: Simplify this!
        {
            var winTexts = Traverse.Create(UnityEngine.Object.FindObjectOfType<WinCounterUI>()).Field("mPlayerWinTexts").GetValue<TextMeshProUGUI[]>();
            winTexts[playerID].color = colorWanted;
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

