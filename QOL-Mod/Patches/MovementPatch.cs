using HarmonyLib;

namespace QOL;

public class MovementPatch
{
    public static void Patch(Harmony harmonyInstance) // MatchmakingHandler method to patch with the harmony instance
    {
        var jumpMethod = AccessTools.Method(typeof(Movement), "Jump");
        var jumpMethodPostfix = new HarmonyMethod(typeof(MovementPatch).GetMethod(nameof(JumpMethodPostfix)));
        harmonyInstance.Patch(jumpMethod, postfix: jumpMethodPostfix);
    }

    public static void JumpMethodPostfix() => GameManagerPatches.RoundJumpCount += 1;
}