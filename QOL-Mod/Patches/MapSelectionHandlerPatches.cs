using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace QOL;

public static class MapSelectionHandlerPatches
{
    public static void Patch(Harmony harmonyInstance)
    {
        var updateSavedDataMethod = AccessTools.Method(typeof(MapSelectionHandler), "UpdateSavedData");
        var updateSavedDataMethodTranspiler = new HarmonyMethod(typeof(MapSelectionHandlerPatches).GetMethod(nameof(UpdateSavedDataMethodTranspiler)));
        var toggleMapActiveMethod = AccessTools.Method(typeof(MapSelectionHandler), "ToggleMapActive");
        var toggleMapActiveMethodTranspiler = new HarmonyMethod(typeof(MapSelectionHandlerPatches).GetMethod(nameof(ToggleMapActiveMethodTranspiler)));
        
        harmonyInstance.Patch(updateSavedDataMethod, transpiler: updateSavedDataMethodTranspiler);
        harmonyInstance.Patch(toggleMapActiveMethod, transpiler: toggleMapActiveMethodTranspiler);
    }
    
    // Patches out the debug text that is normally logged when toggling a map in the maps menu, removes lag spike
    // caused by loading in large map presets
    public static IEnumerable<CodeInstruction> UpdateSavedDataMethodTranspiler(
        IEnumerable<CodeInstruction> instructions)
    {
        const string debugText = "SAVED MAPS: ";

        var instructionList = instructions.ToList();

        for (var i = 0; i < instructionList.Count; i++)
        {
            if (instructionList[i].opcode != OpCodes.Ldstr || (string) instructionList[i].operand != debugText) 
                continue;
            
            instructionList.RemoveRange(i, 4); // TODO: Make more flexible, no hardcoded numbers!
            break;
        }
            
        return instructionList.AsEnumerable(); // Returns the now modified list of IL instructions
    }

    public static IEnumerable<CodeInstruction> ToggleMapActiveMethodTranspiler(
        IEnumerable<CodeInstruction> instructions)
    {
        const string debugText = "Map: ";

        var instructionList = instructions.ToList();

        for (var i = 0; i < instructionList.Count; i++)
        {
            if (instructionList[i].opcode != OpCodes.Ldstr || (string) instructionList[i].operand != debugText) 
                continue;
            
            instructionList.RemoveRange(i, 9); // TODO: Make more flexible, no hardcoded numbers!
            break;
        }
            
        return instructionList.AsEnumerable(); // Returns the now modified list of IL instructions
    }
}