using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
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

        public static void networkAllPlayersDiedButOnePostfix(ref byte winner)
        {
            Debug.Log("winner int: " + winner);
            Debug.Log("spawnID: " + Helper.localNetworkPlayer.NetworkSpawnID);
            if (winner == Helper.localNetworkPlayer.NetworkSpawnID)
            {
                Debug.Log("Winner is me :D");
                Helper.winStreak++;
                Helper.localNetworkPlayer.OnTalked("Winstreak of: " + Helper.winStreak);
            }
            else
            {
                Helper.winStreak = 0;
            }
        }
    }
}
