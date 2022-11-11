﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            if (Helper.IsOwMode && __instance.HasControl)
            {
                var randWord = Helper.OuchPhrases[Random.Range(0, Helper.OuchPhrases.Length)]; // The max is exclusive, hence no len(OuchPhrases) - 1
                Helper.SendChatMsg(randWord, ChatCommands.LogLevel.Success, true, ChatCommands.CmdOutputVisibility["ouch"]);
            }
        }

        public static void OnDeathMethodMethodPostfix(Controller __instance) // Postfix method for OnDeath()
        {
                if (Helper.AutoGG && __instance.HasControl)
                    Helper.SendChatMsg("gg", ChatCommands.LogLevel.Success, true, ChatCommands.CmdOutputVisibility["gg"]);
        }

        public static void LateUpdateMethodPrefix(Controller __instance)
        {
            if (__instance.inactive || __instance.playerID != Helper.localNetworkPlayer.NetworkSpawnID || !GameManager.inFight)
                return;

            if (__instance.GetComponent<CharacterInformation>().isDead)
            {
                GameManagerPatches.TimeDead += Time.deltaTime;
                return;
            }

            GameManagerPatches.TimeAlive += Time.deltaTime;
        }
    }
}
