using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using SimpleJson;
using SimpleJSON;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QOL
{
    class GameManagerPatches
    {
        public static void Patch(Harmony harmonyInstance) // GameManager methods to patch with the harmony __instance
        {
            var networkAllPlayersDiedButOneMethod = AccessTools.Method(typeof(GameManager), "NetworkAllPlayersDiedButOne");
            var networkAllPlayersDiedButOnePostfix = new HarmonyMethod(typeof(GameManagerPatches)
                .GetMethod(nameof(NetworkAllPlayersDiedButOnePostfix))); // Patches NetworkAllPlayersDiedButOne() with postfix method
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
                Helper.SendChatMsg("Winner HP: " + winnerHP.HP, ChatCommands.LogLevel.Success, true, 
                    ChatCommands.CmdOutputVisibility["winnerhp"]);
            }
            
            var isUserWinner = winner == Helper.localNetworkPlayer.NetworkSpawnID;
            UpdateGlobalStats(isUserWinner);
            
            if (isUserWinner)
            {
                Debug.Log("Winner is me :D");
                Helper.WinStreak++;
                var isStreakHigher = Helper.WinStreak > WinstreakHighScore;
                WinstreakHighScore = isStreakHigher ? Helper.WinStreak : WinstreakHighScore;

                if (!Helper.WinStreakEnabled) return;

                __instance.winText.color = DetermineStreakColor();
                
                __instance.winText.fontSize = Plugin.ConfigWinStreakFontsize.Value;
                __instance.winText.text = isStreakHigher ? 
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

        private static void UpdateGlobalStats(bool isUserWinner)
        {
            try
            { 
                var statFields = typeof(CharacterStats).GetFields(); // Using reflection to get vanilla stats
                var playerCurrentStats = Helper.localNetworkPlayer.GetComponent<CharacterStats>();

                if (!Plugin.StatsFileExists)
                {
                    Debug.Log("Stats file doesn't exist");
                    var globalStats = new JSONObject();
                    foreach (var stat in statFields) 
                        globalStats.Add(stat.Name, (int) stat.GetValue(playerCurrentStats));
                    
                    globalStats.Add("winstreakHighscore", 0);
                    globalStats.Add("totalDamageTaken", 0);
                    globalStats.Add("totalJumps", 0);
                    globalStats.Add("roundsPlayed", 0);
                    globalStats.Add("lobbiesJoined", 0);
                    globalStats.Add("avgTimeAlive", 0);
                    globalStats.Add("avgTimeDead", 0);
                    globalStats.Add("avgRoundDuration", 0);
                    globalStats.Add("killDeathRatio", 0.0);
                    globalStats.Add("win%", 0);
                    
                    File.WriteAllText(Plugin.StatsPath,globalStats.ToString());
                    Plugin.StatsFileExists = true;
                    return;
                }
            
                var globalStatsJson = JSONNode.Parse(File.ReadAllText(Plugin.StatsPath));
                var incrementor = 0;
                foreach (var stat in statFields)
                {
                    var localStatValue = (int) stat.GetValue(playerCurrentStats);
                    int globalStatValue = globalStatsJson[stat.Name];
                    // Subtraction so we're not continuously adding the previous value as well
                    globalStatsJson[stat.Name] = globalStatValue + localStatValue - RoundStatValues[incrementor];
                    RoundStatValues[incrementor] = localStatValue;
                    incrementor++;
                }

                if (isUserWinner && Helper.WinStreak + 1 > WinstreakHighScore)
                    globalStatsJson["winstreakHighscore"] = Helper.WinStreak + 1;
                
                var damageTaken = (int) Traverse.Create(Helper.localNetworkPlayer.GetComponent<Controller>())
                    .Field("TransientMemory")
                    .GetValue<MemoryBucket>()
                    .Copy<float>("DamageTaken")
                    .GetValue(0f);
                
                // Added TryParse() protection in case sf's dmg calc gives an integer to too large (perhaps from a purposefully sent packet)
                if (int.TryParse(globalStatsJson["totalDamageTaken"], out var totalDamageTaken))
                {
                    globalStatsJson["totalDamageTaken"] =  totalDamageTaken
                        + damageTaken - RoundStatValues[13];
                    
                    RoundStatValues[13] = damageTaken;
                }

                globalStatsJson["totalJumps"] += RoundJumpCount;
                globalStatsJson["roundsPlayed"] += 1;
                if (LobbiesJoined > 0) globalStatsJson["lobbiesJoined"] += LobbiesJoined;
                
                var newAvgTimeAlive= (globalStatsJson["avgTimeAlive"] + (int)TimeAlive) / 2;
                var newAvgTimeDead= (globalStatsJson["avgTimeDead"] + (int)TimeDead) / 2;
                int oldAvgRoundDuration = globalStatsJson["avgRoundDuration"];

                globalStatsJson["avgTimeAlive"] = newAvgTimeAlive;
                globalStatsJson["avgTimeDead"] = newAvgTimeDead;
                globalStatsJson["avgRoundDuration"] = (oldAvgRoundDuration + newAvgTimeAlive + newAvgTimeDead) / 2;

                globalStatsJson["killDeathRatio"] = Math.Round(globalStatsJson["kills"] / (float)globalStatsJson["deaths"], 2);
                
                int winTotal = globalStatsJson["wins"];
                globalStatsJson["win%"] = Math.Round(winTotal / (float)(globalStatsJson["deaths"] + winTotal) * 100, 2) + "%";

                TimeDead = TimeAlive = 0;
                LobbiesJoined = RoundJumpCount = 0;
                globalStatsJson.Inline = false; // Needs to be false for indentation to work
                File.WriteAllText(Plugin.StatsPath ,globalStatsJson.ToString(4)); // 4 spaces = 1 tab
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception when trying to update global stats: " + e);
                Console.WriteLine("Renaming old stats file as backup in order to generate new one...");
                File.Move(Plugin.StatsPath, Plugin.StatsPath + ".backup");
            }
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

        public static List<Color> WinstreakColors1 = new (50);
        public static List<Color> WinstreakColors2 = new (50);

        public static List<int> WinstreakRanges1 = new (50);
        public static List<int> WinstreakRanges2 = new (50);

        private static readonly int[] RoundStatValues = new int[14];

        public static int WinstreakHighScore;
        public static int RoundJumpCount;
        public static int LobbiesJoined;
        public static float TimeAlive;
        public static float TimeDead;
    }
}
