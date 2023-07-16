using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace QOL;

public static class MapPresetHandler
{
    public static List<SaveableMapPreset> MapPresets { get; private set; }
    public static List<string> MapPresetNames { get; private set; }
    
    private static bool _defaultPresetsAlreadyExist;
    private static int _highestMapIndex = -1;
    private static List<MapSelectionHandler.MapCategoryUI> _mapCategories;
    private static readonly string[] StockCategoryNames = Enum.GetNames(typeof(MapWorldsEnum))
        .Select(name => name.ToLower())
        .ToArray();

    private static void RefreshHighestMapIndex()
    {
        var max = -1;
        for (var i = 0; i < (int) MapWorldsEnum.CustomLocal; i++)
        {
            foreach (var map in _mapCategories[i].CategoryMaps)
            {
                var mapIntIndex = int.Parse(map.MapIndex);
                
                if (mapIntIndex > max)
                    max = mapIntIndex;
            }
        }

        _highestMapIndex = max;
    }   

    public static void InitializeMapPresets()
    {
        MapPresets = new List<SaveableMapPreset>();
        var mapPresetsJson = JSONNode.Parse(File.ReadAllText(Plugin.MapPresetsPath))["savedPresets"];

        foreach (var presetJson in mapPresetsJson)
        {
            var newPreset = new SaveableMapPreset(presetJson.Value);
            MapPresets.Add(newPreset);
        }
        
        MapPresetNames = MapPresets.Select(preset => preset.PresetName).ToList(); // Add all names from the preset list
        MapPresetNames.Add("save");
        MapPresetNames.Add("remove");
        MapPresetNames.Sort();
        
        Debug.Log("Loaded " + MapPresets.Count + " map presets!");
    }
    
    public static List<string> GetAllStockMapIndexes(bool activeMapsOnly)
    {
        var allMaps = new List<string>();
        
        for (var i = 0; i < (int) MapWorldsEnum.CustomLocal; i++) // Only want to include stock Landfall maps
        {
            var mapType = (MapWorldsEnum) i;

            allMaps.AddRange(GetSpecificCategoryMaps(mapType, activeMapsOnly)
                .Where(map => map.MapToggle is not null) // Don't want hidden/unused maps, they don't have a toggle
                .Select(map => map.MapIndex).ToList());
        }

        return allMaps;
    }

    public static void RefreshMutables()
    {
        RefreshCategoriesObj();
        
        if (_highestMapIndex == -1)
            RefreshHighestMapIndex();
    }

    private static void RefreshCategoriesObj()
    {
        _mapCategories = Traverse.Create(MapSelectionHandler.Instance)
            .Field("m_Categories")
            .GetValue<List<MapSelectionHandler.MapCategoryUI>>();
    }

    public static List<SingleMapUI> GetSpecificCategoryMaps(MapWorldsEnum categoryType, bool activeMapsOnly)
    {
        var mapCategoryUI = _mapCategories
            .Find(cat => string.
                Equals(cat.CategoryName, categoryType.ToString(), StringComparison.CurrentCultureIgnoreCase));

        if (!mapCategoryUI.IsActive && activeMapsOnly)
            return new List<SingleMapUI>();
        
        return activeMapsOnly ? mapCategoryUI.CategoryMaps.FindAll(map => map.IsLocallyActive) : mapCategoryUI.CategoryMaps;
    }

    public static bool DefaultPresetsExist()
    {
        var rawPresetNames = MapPresets.Select(preset => preset.PresetName).ToList();

        if (_defaultPresetsAlreadyExist)
            return true;

        for (var i = 0; i < (int) MapWorldsEnum.CustomLocal; i++) // Only want to include stock Landfall maps
        {
            if (!rawPresetNames.Contains(StockCategoryNames[i]))
                return false;
        }

        return true;    
    }

    public static void GenerateDefaultPresets()
    {
        var allMaps = new List<string>();
        Debug.Log("Generating default map presets...");

        SaveableMapPreset newPreset;
        for (var i = 0; i < (int) MapWorldsEnum.CustomLocal; i++) // Only want to include stock Landfall maps
        {
            var mapType = (MapWorldsEnum) i;

            var categoryMaps = GetSpecificCategoryMaps(mapType, false)
                .Where(map => map.MapToggle is not null) // Don't want hidden/unused maps, they don't have a toggle
                .Select(map => map.MapIndex).ToList();

            var categoryName = StockCategoryNames[i];
            newPreset = new SaveableMapPreset(categoryMaps, categoryName);

            AddNewPreset(newPreset);
            allMaps.AddRange(categoryMaps); // For the final 'all' preset later
        }
        
        newPreset = new SaveableMapPreset(allMaps, "all");
        AddNewPreset(newPreset);
        newPreset = new SaveableMapPreset(new List<string>(), "none");
        AddNewPreset(newPreset);
        
        _defaultPresetsAlreadyExist = true;
    }

    public static void AddNewPreset(SaveableMapPreset preset)
    {
        MapPresets.Add(preset);
        MapPresetNames.Add(preset.PresetName);
        MapPresetNames.Sort();
        
        SavePreset(preset);
    }

    private static void SavePreset(SaveableMapPreset preset)
    {
        var savedMapPresetsJson = JSONNode.Parse(File.ReadAllText(Plugin.MapPresetsPath));
        savedMapPresetsJson["savedPresets"].Add(preset.ToJson());

        File.WriteAllText(Plugin.MapPresetsPath, savedMapPresetsJson.ToString());
    }

    public static void DeletePreset(int presetIndex, string presetName)
    {
        var savedMapPresetsJson = JSONNode.Parse(File.ReadAllText(Plugin.MapPresetsPath));
        
        MapPresets.RemoveAt(presetIndex);
        MapPresetNames.Remove(presetName);
        savedMapPresetsJson["savedPresets"].Remove(presetIndex);
        
        File.WriteAllText(Plugin.MapPresetsPath, savedMapPresetsJson.ToString());
    }
    
    public static void LoadPreset(SaveableMapPreset preset) 
    {
        var mapSelector = MapSelectionHandler.Instance;

        for (var i = 1; i <= _highestMapIndex; i++) // Maps start at one
        {
            var curMapIndex = i.ToString();
            var map = mapSelector.FindSingleMapByIndex(curMapIndex);
            var mapToggle = map?.MapToggle;
            
            if (mapToggle is not null)
                mapToggle.isOn = preset.Maps.Contains(curMapIndex); // Enable map if in preset else disable it
        }

        for (var i = 0; i < (int) MapWorldsEnum.CustomLocal; i++) 
            _mapCategories[i].CategoryToggle.isOn = true; // All maps have the proper state, so enable all categories
    }
}