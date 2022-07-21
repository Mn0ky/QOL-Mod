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

            var onServerJoinedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnServerJoined");
            var onServerJoinedMethodPostfix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
                .GetMethod(nameof(OnServerJoinedMethodPostfix)));
            harmonyInstance.Patch(onServerJoinedMethod, postfix: onServerJoinedMethodPostfix);

            var onServerCreatedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnServerCreated");
            var onServerCreatedMethodPostfix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
                .GetMethod(nameof(OnServerCreatedMethodPostfix)));
            harmonyInstance.Patch(onServerCreatedMethod, postfix: onServerCreatedMethodPostfix);

            var onPlayerSpawnedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnPlayerSpawned");
            var onPlayerSpawnedMethodPostfix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
                .GetMethod(nameof(OnPlayerSpawnedMethodPostfix)));
            harmonyInstance.Patch(onPlayerSpawnedMethod, postfix: onPlayerSpawnedMethodPostfix);

            var onKickedMethod = AccessTools.Method(typeof(MultiplayerManager), "OnKicked");
            var onKickedMethodPrefix = new HarmonyMethod(typeof(MultiplayerManagerPatches)
                .GetMethod(nameof(OnKickedMethodPrefix)));
            harmonyInstance.Patch(onKickedMethod, prefix: onKickedMethodPrefix);
        }

        public static void OnServerJoinedMethodPostfix() => InitGUI();

        public static void OnServerCreatedMethodPostfix() => InitGUI();
        
        // Guards against kick attempts made towards the user by skipping the method, if not Monky or Rexi
        public static bool OnKickedMethodPrefix() => Helper.IsTrustedKicker;
        
        public static void OnPlayerSpawnedMethodPostfix(MultiplayerManager __instance)
        {
            foreach (var player in UnityEngine.Object.FindObjectsOfType<NetworkPlayer>())
            {
                if (player.NetworkSpawnID != __instance.LocalPlayerIndex)
                {
                    var otherCharacter = player.transform.root.gameObject;
                    var otherColor = Plugin.DefaultColors[player.NetworkSpawnID];

                    ChangeAllCharacterColors(otherColor, otherCharacter);
                }
                else
                {
                    var character = player.transform.root.gameObject;

                    ChangeAllCharacterColors(!Helper.IsCustomPlayerColor
                            ? Plugin.DefaultColors[player.NetworkSpawnID]
                            : Helper.CustomPlayerColor,
                        character);
                }
            }
        }

        public static void ChangeSpriteRendColor(Color colorWanted, GameObject character)
        {
            foreach (var spriteRenderer in character.GetComponentsInChildren<SpriteRenderer>())
            {
                spriteRenderer.color = colorWanted;
                spriteRenderer.GetComponentInParent<SetColorWhenDamaged>().startColor = colorWanted;
            }
        }

        public static void ChangeLineRendColor(Color colorWanted, GameObject character)
        {
            foreach (var t in character.GetComponentsInChildren<LineRenderer>()) 
                t.sharedMaterial.color = colorWanted;
        }

        public static void ChangeParticleColor(Color colorWanted, GameObject character)
        {
            var unchangedEffects = new[]
            {
                "punchPartilce",
                "JumpParticle",
                "landParticle (1)",
                "footParticle",
                "footParticle (1)"
            };

            foreach (var partSys in character.GetComponentsInChildren<ParticleSystem>())
            {
                if (unchangedEffects.Contains(partSys.name) && !Plugin.ConfigCustomColorOnParticle.Value) 
                    continue;
                
                var main = partSys.main;
                main.startColor = colorWanted;
            }
        }

        public static void ChangeWinTextColor(Color colorWanted, int playerID)
        {
            var winTexts = Traverse.Create(UnityEngine.Object.FindObjectOfType<WinCounterUI>())
                .Field("mPlayerWinTexts").GetValue<TextMeshProUGUI[]>();
            winTexts[playerID].color = colorWanted;
        }

        public static void ChangeAllCharacterColors(Color colorWanted, GameObject character)
        {
            var playerID = 0;
            if (MatchmakingHandler.Instance.IsInsideLobby) 
                playerID = character.GetComponent<NetworkPlayer>().NetworkSpawnID;

            ChangeLineRendColor(colorWanted, character);
            ChangeSpriteRendColor(colorWanted, character);
            ChangeParticleColor(colorWanted, character);
            ChangeWinTextColor(colorWanted, playerID);

            Traverse.Create(character.GetComponentInChildren<BlockAnimation>()).Field("startColor").SetValue(colorWanted);
            var playerNames = Traverse.Create(UnityEngine.Object.FindObjectOfType<OnlinePlayerUI>())
                .Field("mPlayerTexts").GetValue<TextMeshProUGUI[]>();

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
    }
}

