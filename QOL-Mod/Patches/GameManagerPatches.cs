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
    class GameManagerPatches
    {
        public static void Patch(Harmony harmonyInstance) // GameManager methods to patch with the harmony __instance
        {
            var networkAllPlayersDiedButOneMethod = AccessTools.Method(typeof(GameManager), "NetworkAllPlayersDiedButOne");
            var networkAllPlayersDiedButOnePostfix = new HarmonyMethod(typeof(GameManagerPatches).GetMethod(nameof(NetworkAllPlayersDiedButOnePostfix))); // Patches NetworkAllPlayersDiedButOne() with postfix method
            harmonyInstance.Patch(networkAllPlayersDiedButOneMethod, postfix: networkAllPlayersDiedButOnePostfix);

            var awakeMethod = AccessTools.Method(typeof(GameManager), "Awake");
            var awakeMethodPostfix = new HarmonyMethod(typeof(GameManagerPatches).GetMethod(nameof(AwakeMethodPostfix))); // Patches NetworkAllPlayersDiedButOne() with postfix method
            harmonyInstance.Patch(awakeMethod, postfix: awakeMethodPostfix);
        }

        public static void AwakeMethodPostfix() => Plugin.InitModText();

        public static void NetworkAllPlayersDiedButOnePostfix(ref byte winner, GameManager __instance)
        {
            if (Helper.HPWinner)
            {
                var winnerHP = new PlayerHP(Helper.GetColorFromID(winner).ToLower());
                Helper.SendLocalMsg("Winner HP: " + winnerHP.HP, ChatCommands.LogLevel.Success);
            }

            Debug.Log("winner int: " + winner);
            Debug.Log("spawnID: " + Helper.localNetworkPlayer.NetworkSpawnID);

            if (winner == Helper.localNetworkPlayer.NetworkSpawnID)
            {
                Debug.Log("Winner is me :D");
                Helper.WinStreak++;
                var isHigher = DetermineNewHighScore(Helper.WinStreak);

                if (!Helper.WinStreakEnabled) return;

                __instance.winText.color = DetermineStreakColor();
                
                __instance.winText.fontSize = Plugin.ConfigWinStreakFontsize.Value;
                __instance.winText.text = isHigher ? 
                    $"New Highscore: {Helper.WinStreak}" 
                    : $"Winstreak Of {Helper.WinStreak}";

                __instance.winText.gameObject.SetActive(true);
                return;
            }

            Debug.Log("winstreak lost");
            __instance.winText.fontSize = 200;
            WinstreakRanges1 = new List<int>(WinstreakRanges2);
            WinstreakColors1 = new List<Color>(WinstreakColors2);
            Helper.WinStreak = 0;
        }

        private static Color DetermineStreakColor()
        {
            for (var i = 0; i < WinstreakRanges1.Count; i++)
            {
                i = 0;
                Debug.Log("streak range value: " + WinstreakRanges1[i]);
                switch (WinstreakRanges1[i])
                {
                    case > 0:
                        Debug.Log("Deducting!, ranges count: " + WinstreakRanges1.Count);
                        WinstreakRanges1[i]--;
                        return WinstreakColors1[i];
                    case 0 when WinstreakRanges1.Count == 2:
                        return WinstreakColors2[WinstreakColors2.Count - 1];
                    default:
                        WinstreakRanges1.RemoveAt(i);
                        WinstreakColors1.RemoveAt(i);
                        break;
                }
            }

            Debug.Log("Something went wrong with streak!");
            return Color.white;
        }

        private static bool DetermineNewHighScore(int score)
        {
            Debug.Log("high: " + HighScore + "score: " + score);
            if (score < HighScore) return false;
            
            HighScore = score;
            File.WriteAllText(Plugin.ScorePath, HighScore.ToString());
            return true;
        }

        public static List<Color> WinstreakColors1 = new (50);
        public static List<Color> WinstreakColors2 = new (50);

        public static List<int> WinstreakRanges1 = new (50);
        public static List<int> WinstreakRanges2 = new (50);

        public static int HighScore;
    }
}
