using System;
using HarmonyLib;
using UnityEngine;
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
            
            var lateUpdateMethod = AccessTools.Method(typeof(Controller), "LateUpdate");
            var lateUpdateMethodPrefix = new HarmonyMethod(typeof(ControllerPatches).GetMethod(nameof(LateUpdateMethodPrefix)));
            harmonyInstance.Patch(lateUpdateMethod, prefix: lateUpdateMethodPrefix);
        }

        public static void OnTakeDamageMethodPostfix(Controller __instance) // Postfix method for OnTakeDamage()
        {
            if (ChatCommands.CmdDict["ow"].IsEnabled && __instance.HasControl)
            {
                var randWord = Helper.OuchPhrases[Random.Range(0, Helper.OuchPhrases.Length)]; // The max is exclusive, hence no len(OuchPhrases) - 1
                Helper.SendPublicOutput(randWord);
            }
        }

        public static void OnDeathMethodMethodPostfix(Controller __instance) // Postfix method for OnDeath()
        {
                if (ChatCommands.CmdDict["gg"].IsEnabled && __instance.HasControl)
                    Helper.SendPublicOutput("gg");
        }

        public static void LateUpdateMethodPrefix(Controller __instance, CharacterInformation ___info)
        {
            var localPlayerID = GameManager.Instance.mMultiplayerManager.LocalPlayerIndex;
            if (__instance.inactive || __instance.playerID != localPlayerID || !GameManager.inFight)
                return;

            if (___info.isDead)
            {
                GameManagerPatches.TimeDead += Time.deltaTime;
                return;
            }

            GameManagerPatches.TimeAlive += Time.deltaTime;
        }
    }
}
