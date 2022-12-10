using HarmonyLib;
using UnityEngine;

namespace QOL;

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
        var spawnID = __instance.transform.root.GetComponent<NetworkPlayer>().NetworkSpawnID;

        if (Helper.IsCustomPlayerColor && spawnID == Helper.localNetworkPlayer.NetworkSpawnID)
        {
            ChangeBossUIColor(Helper.CustomPlayerColor, __instance.transform.root.gameObject);
            return;
        }

        ChangeBossUIColor(Plugin.DefaultColors[spawnID], __instance.transform.root.gameObject);
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