using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using TMPro;


namespace QOL
{
    class MultiplayerManagerPatches
    {
        public static void Patches(Harmony harmonyInstance) // Multiplayer methods to patch with the harmony __instance
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
            TextMeshProUGUI[] playerNames = Traverse.Create(UnityEngine.Object.FindObjectOfType<OnlinePlayerUI>()).Field("mPlayerTexts").GetValue() as TextMeshProUGUI[];
            Debug.Log("My index: " + __instance.LocalPlayerIndex);
            Debug.Log("Checking players");
            foreach (var player in UnityEngine.Object.FindObjectsOfType<NetworkPlayer>())
            {
                if (player.NetworkSpawnID != __instance.LocalPlayerIndex)
                {

                    Debug.Log("id not equal");
                    Debug.Log("resetting now, id of: " + player.NetworkSpawnID);
                    //Debug.Log("resetting with old color: " + ColorUtility.ToHtmlStringRGBA(Helper.defaultColors[player.NetworkSpawnID]));

                    var oldCharacter = player.transform.root.gameObject;
                    var oldColor = Plugin.defaultColors[player.NetworkSpawnID];
                    Debug.Log("Assigned old character");
                    //Debug.Log("old color to assign: " + oldColor.ToString());

                    ChangeLineRendColor(oldColor, oldCharacter);
                    ChangeSpriteRendColor(oldColor, oldCharacter);

                    foreach (var partSys in oldCharacter.GetComponentsInChildren<ParticleSystem>()) partSys.startColor = oldColor;

                    Traverse.Create(oldCharacter.GetComponentInChildren<BlockAnimation>()).Field("startColor").SetValue(oldColor);
                    ChangeWinTextColor(oldColor, player.NetworkSpawnID);
                    playerNames[player.NetworkSpawnID].color = oldColor;

                    //colorsToReset.Remove(player.NetworkSpawnID);
                }
                else
                {
                    Debug.Log("Found ourselves!");
                    var character = player.transform.root.gameObject;

                    if (Helper.customPlayerColor == defaultColor) ChangeAllCharacterColors(Plugin.defaultColors[player.NetworkSpawnID], character, playerNames);
                    else ChangeAllCharacterColors(Helper.customPlayerColor, character, playerNames);
                }
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
            foreach (var t in character.GetComponentsInChildren<LineRenderer>()) t.sharedMaterial.color = colorWanted;
        }

        public static void ChangeWinTextColor(Color colorWanted, int playerID) // TODO: Simplify this!
        {
            var winTexts = Traverse.Create(UnityEngine.Object.FindObjectOfType<WinCounterUI>()).Field("mPlayerWinTexts").GetValue<TextMeshProUGUI[]>();
            winTexts[playerID].color = colorWanted;
        }

        public static void ChangeAllCharacterColors(Color colorWanted, GameObject character, TextMeshProUGUI[] playerNames)
        {
            int playerID = character.GetComponent<NetworkPlayer>().NetworkSpawnID;

            ChangeLineRendColor(colorWanted, character);
            ChangeSpriteRendColor(colorWanted, character);
            ChangeWinTextColor(colorWanted, playerID);
            foreach (var partSys in character.GetComponentsInChildren<ParticleSystem>()) partSys.startColor = colorWanted;
            Traverse.Create(character.GetComponentInChildren<BlockAnimation>()).Field("startColor").SetValue(colorWanted);
            playerNames[playerID].color = colorWanted;
        }


        public static void InitGUI()
        {
            try
            {
                new GameObject("GUIHandler").AddComponent<GUIManager>();
                Debug.Log("Added GUIManager!");
            }
            catch (Exception ex)
            {
                Debug.Log("Exception on starting GUIManager: " + ex.Message);
            }
        }

        private static readonly Color defaultColor = new(1, 1, 1);
        //private static List<int> colorsToReset = new(3);
    }
}

