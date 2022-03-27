using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;

namespace QOL
{
    // UNUSED, left for reference purposes
    class CharacterStatsPatch
    {
        public static void Patch(Harmony harmonyInstance) // CharacterStats methods to patch with the harmony __instance
        {
            var GetStringMethod = AccessTools.Method(typeof(CharacterStats), "GetString");
            var GetStringMethodPrefix = new HarmonyMethod(typeof(CharacterStatsPatch).GetMethod(nameof(CharacterStatsPatch.GetStringMethodPrefix))); // Patches GetString() with prefix method
            harmonyInstance.Patch(GetStringMethod, prefix: GetStringMethodPrefix);
        }

        // Messing around with box-drawing characters, didn't work out
        public static bool GetStringMethodPrefix(ref string __result, CharacterStats __instance)
        {
            __result = string.Concat("──────────\nWins: ", __instance.wins, 
                                     "│\nKills: ", __instance.kills, 
                                     "│\nDeaths: ", __instance.deaths, 
                                     "│\nSuicides: ", __instance.suicides, 
                                     "│\nFalls: ", __instance.falls, 
                                     "│\nCrownSteals: ", __instance.crownSteals,
                                     "│\nBulletsHit: ", __instance.bulletsHit, 
                                     "│\nBulletsMissed: ", __instance.bulletsMissed,
                                     "│\nBulletsShot: ", __instance.bulletsShot, 
                                     "│\nBlocks: ", __instance.blocks,
                                     "│\nPunchesLanded: ", __instance.punchesLanded, 
                                     "│\nWeaponsPickedUp: ", __instance.weaponsPickedUp,
                                     "│\nWeaponsThrown: ", __instance.weaponsThrown);

            return false;
        }
    }
}
