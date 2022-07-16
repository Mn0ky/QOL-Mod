using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Random = UnityEngine.Random;

namespace QOL

{
    class ControllerPatches
    {
        public static void Patch(Harmony harmonyInstance) // Controller methods to patch with the harmony instance
        {
            var onTakeDamageMethod = AccessTools.Method(typeof(Controller), "OnTakeDamage");
            var onTakeDamageMethodPostfix = new HarmonyMethod(typeof(ControllerPatches).GetMethod(nameof(OnTakeDamageMethodPostfix)));
            harmonyInstance.Patch(onTakeDamageMethod, postfix: onTakeDamageMethodPostfix);

            var onDeathMethod = AccessTools.Method(typeof(Controller), "OnDeath");
            var onDeathMethodMethodPostfix = new HarmonyMethod(typeof(ControllerPatches).GetMethod(nameof(OnDeathMethodMethodPostfix)));
            harmonyInstance.Patch(onDeathMethod, postfix: onDeathMethodMethodPostfix);
        }

        public static void OnTakeDamageMethodPostfix(Controller __instance) // Postfix method for OnTakeDamage()
        {
            if (Helper.IsOwMode && __instance.HasControl)
            {
                var randWord = Helper.OuchPhrases[Random.Range(0, Helper.OuchPhrases.Length)]; // The max is exclusive, hence no len(OuchPhrases) - 1
                Helper.localNetworkPlayer.OnTalked(randWord);
            }
        }

        public static void OnDeathMethodMethodPostfix(Controller __instance) // Postfix method for OnDeath()
        {
            if (Helper.autoGG && __instance.HasControl) Helper.localNetworkPlayer.OnTalked("gg");
        }
    }
}
