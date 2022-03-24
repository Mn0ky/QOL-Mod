using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace QOL
{
    class GameManagerPatch
    {
        public static void Patch(Harmony harmonyInstance) // GameManager methods to patch with the harmony __instance
        {
            var networkAllPlayersDiedButOneMethod = AccessTools.Method(typeof(GameManager), "NetworkAllPlayersDiedButOne");
            var networkAllPlayersDiedButOnePostfix = new HarmonyMethod(typeof(GameManagerPatch).GetMethod(nameof(GameManagerPatch.networkAllPlayersDiedButOnePostfix))); // Patches NetworkAllPlayersDiedButOne() with postfix method
            harmonyInstance.Patch(networkAllPlayersDiedButOneMethod, postfix: networkAllPlayersDiedButOnePostfix);
        }

        public static void networkAllPlayersDiedButOnePostfix(ref byte winner, GameManager __instance)
        {
            if (Helper.HPWinner)
            {
                string winnerColor = Helper.GetColorFromID(winner);

                Helper.localNetworkPlayer.OnTalked(winnerColor + " HP: " + Helper.GetHPOfPlayer(winner));
            }

            Debug.Log("winner int: " + winner);
            Debug.Log("spawnID: " + Helper.localNetworkPlayer.NetworkSpawnID);

            if (winner == Helper.localNetworkPlayer.NetworkSpawnID)
            {
                Debug.Log("Winner is me :D");
                Helper.winStreak++;
                var isHigher = DetermineNewHighScore(Helper.winStreak);

                if (!Helper.winStreakEnabled) return;

                __instance.winText.color = DetermineStreakColor();
             
                __instance.winText.fontSize = Plugin.configWinStreakFontsize.Value;
                __instance.winText.text = (isHigher) ? $"New Highscore: {Helper.winStreak}" : $"Winstreak Of {Helper.winStreak}";

                __instance.winText.gameObject.SetActive(true);
                return;
            }

            Debug.Log("winstreak lost");
            __instance.winText.fontSize = 200;
            winstreakRanges1 = new List<int>(winstreakRanges2);
            winstreakColors1 = new List<Color>(winstreakColors2);
            Helper.winStreak = 0;
        }

        public static Color DetermineStreakColor()
        {
            for (int i = 0; i < winstreakRanges1.Count; i++)
            {
                i = 0;
                Debug.Log("streak range value: " + winstreakRanges1[i]);
                if (winstreakRanges1[i] > 0)
                {
                    Debug.Log("Deducting!, ranges count: " + winstreakRanges1.Count);
                    winstreakRanges1[i]--;
                    return winstreakColors1[i];
                }

                if (winstreakRanges1[i] == 0 && winstreakRanges1.Count == 2)
                {
                    Debug.Log("Sending last color!");
                    return winstreakColors2[winstreakColors2.Count - 1];
                }

                Debug.Log("Removing range and color!");
                winstreakRanges1.RemoveAt(i);
                winstreakColors1.RemoveAt(i);
            }

            Debug.Log("Something went wrong!");
            return Color.white;
        }

        public static bool DetermineNewHighScore(int score)
        {
            var scorePath = $"{Paths.PluginPath}\\QOL-Mod\\WinstreakData.txt";
            Debug.Log("high: " + highScore + "score: " + score);

            if (highScore < score)
            {
                highScore = score;
                File.WriteAllText(scorePath, highScore.ToString());
                return true;
            }

            return false;
        }

        public static List<Color> winstreakColors1 = new (50);
        public static List<Color> winstreakColors2 = new(50);

        public static List<int> winstreakRanges1 = new(50);
        public static List<int> winstreakRanges2 = new(50);

        public static int highScore;
    }
}
