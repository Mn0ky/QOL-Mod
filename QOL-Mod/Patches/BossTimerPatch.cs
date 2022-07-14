using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace QOL
{
    class BossTimerPatch
    {
        public static void Patch(Harmony harmonyInstance)
        {
            var startMethod = AccessTools.Method(typeof(BossTimer), "Start");
            var startMethodPostfix = new HarmonyMethod(typeof(BossTimerPatch).GetMethod(nameof(StartMethodPostfix)));
            harmonyInstance.Patch(startMethod, postfix: startMethodPostfix);
        }

        public static void StartMethodPostfix(BossTimer __instance)
        {
            Color colorWanted = Plugin.configCustomColor.Value != new Color(1, 1, 1) ? Plugin.configCustomColor.Value : Plugin.defaultColors[0];
            ChangeBossUIColor(colorWanted, __instance.transform.root.gameObject);
        }

        private static void ChangeBossUIColor(Color colorWanted, GameObject character)
        {
            var bossTimer = character.GetComponentInChildren<BossTimer>(true);
            bossTimer.text.color = colorWanted;
            bossTimer.phaseRing.color = colorWanted;

            var bossHealth = character.GetComponentInChildren<BossHealth>(true);
            bossHealth.red.color = colorWanted;
        }
    }
}
