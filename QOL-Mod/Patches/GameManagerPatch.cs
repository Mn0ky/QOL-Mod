using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace QOL
{
    class GameManagerPatch
    {
        public static void Patch(Harmony harmonyInstance) // GameManager methods to patch with the harmony instance
        {
            var networkAllPlayersDiedButOneMethod = AccessTools.Method(typeof(GameManager), "NetworkAllPlayersDiedButOne");
            var networkAllPlayersDiedButOnePostfix = new HarmonyMethod(typeof(GameManagerPatch).GetMethod(nameof(GameManagerPatch.networkAllPlayersDiedButOnePostfix))); // Patches NetworkAllPlayersDiedButOne() with postfix method
            harmonyInstance.Patch(networkAllPlayersDiedButOneMethod, postfix: networkAllPlayersDiedButOnePostfix);
        }

        public static void networkAllPlayersDiedButOnePostfix(ref byte winner, GameManager __instance)
        {
            Debug.Log("winner int: " + winner);
            Debug.Log("spawnID: " + Helper.localNetworkPlayer.NetworkSpawnID);
            if (winner == Helper.localNetworkPlayer.NetworkSpawnID && (Helper.winStreakEnabled || Helper.AlwaysTrackWinstreak))
            {
                Debug.Log("Winner is me :D");
                Helper.winStreak++;
                if (Helper.winStreakEnabled)
                {
                    Helper.localNetworkPlayer.OnTalked("Winstreak of: " + Helper.winStreak);
                }

                __instance.winText.color = Helper.winStreak switch
                {
                    < 3 => Color.red,
                    <= 5 => Color.yellow,
                    _ => Color.green
                };

                __instance.winText.text = "Winstreak of " + Helper.winStreak;
                __instance.winText.gameObject.SetActive(true);
                Debug.Log("wintext font size: " + __instance.winText.fontSize);
                return;
            }
            Helper.winStreak = 0;
        }
    }
}
