using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace QOL

{
    class ControllerPatch
    {
        public static void Patch(Harmony harmonyInstance) // Controller methods to patch with the harmony instance
        {
            var OnDeathMethod = AccessTools.Method(typeof(Controller), "OnDeath");
            var OnDeathMethodMethodPostfix = new HarmonyMethod(typeof(ControllerPatch).GetMethod(nameof(ControllerPatch.OnDeathMethodMethodPostfix))); // Patches OnDeathMethod with postfix method
            harmonyInstance.Patch(OnDeathMethod, postfix: OnDeathMethodMethodPostfix);
        }
        public static void OnDeathMethodMethodPostfix(Controller __instance) // Postfix method for OnDeath()
        {
            if (Helper.autoGG && __instance.HasControl)
            {
                Helper.localNetworkPlayer.OnTalked("gg");
            }
        }
    }
}
