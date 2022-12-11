using HarmonyLib;
using UnityEngine;

namespace QOL;

class CharacterInformationPatch
{
    public static void Patch(Harmony harmonyInstance)
    {
        var startMethod = AccessTools.Method(typeof(CharacterInformation), "Start");
        var startMethodPostfix = new HarmonyMethod(typeof(CharacterInformationPatch)
            .GetMethod(nameof(StartMethodPostfix)));
        harmonyInstance.Patch(startMethod, postfix: startMethodPostfix);
    }

    public static void StartMethodPostfix(CharacterInformation __instance)
    {
        if (MatchmakingHandler.Instance.IsInsideLobby) return;
        
        var customPlayerColor = ConfigHandler.GetEntry<Color>("CustomColor");
        var isCustomPlayerColor = customPlayerColor != ConfigHandler.GetEntry<Color>("CustomColor", true);
            
        var colorWanted = isCustomPlayerColor ? customPlayerColor : ConfigHandler.DefaultColors[0];
        MultiplayerManagerPatches.ChangeAllCharacterColors(colorWanted, __instance.gameObject);
    }
}