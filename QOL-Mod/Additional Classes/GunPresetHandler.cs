using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace QOL;

public static class GunPresetHandler
{
    public static List<string> GunPresetNames { get; private set; }
    
    private static List<SaveableGunPreset> _weaponPresets;
    private static List<WeaponSelectionHandler.WeaponCategoryUI> _weaponCategories;
    private static List<WeaponSelectionHandler.SingleWeaponUI> _weapons;
    private static List<Toggle> _categoryToggles;
    private static List<Toggle> _weaponToggles;
    private static bool _defaultPresetsAlreadyExist;

    public static void InitializeGunPresets()
    {
        _weaponPresets = new List<SaveableGunPreset>();
        var mapPresetsJson = JSONNode.Parse(File.ReadAllText(Plugin.GunPresetsPath))["savedPresets"];

        foreach (var presetJson in mapPresetsJson)
        {
            var newPreset = new SaveableGunPreset(presetJson.Value);
            _weaponPresets.Add(newPreset);
        }
        
        GunPresetNames = _weaponPresets.Select(preset => preset.PresetName).ToList(); // Add all names from the preset list
        GunPresetNames.Add("save");
        GunPresetNames.Add("remove");
        GunPresetNames.Sort();
        
        Debug.Log("Loaded " + _weaponPresets.Count + " gun presets!");
    }

    public static void RefreshMutables()
    {
        var selector = GameManager.Instance.GetComponent<WeaponSelectionHandler>();
        var selectorReflector = Traverse.Create(GameManager.Instance.GetComponent<WeaponSelectionHandler>());
        
        _weapons = selectorReflector.Field("m_Weapons")
            .GetValue<List<WeaponSelectionHandler.SingleWeaponUI>>();
        
        _weaponCategories = selectorReflector.Field("m_Categories")
            .GetValue<List<WeaponSelectionHandler.WeaponCategoryUI>>();

        var categoryObjs = selector.transform.GetComponentsInChildren<WeaponCategoryTAG>();

        _categoryToggles = categoryObjs
            .Select(category => category.transform.Find("Toggle").GetComponent<Toggle>())
            .ToList();
        
        _weaponToggles = categoryObjs
            .Select(category => category.transform.Find("Grid").GetComponentsInChildren<Toggle>())
            .SelectMany(toggleArr => toggleArr).ToList();
    }
    
    public static bool DefaultPresetsExist() 
        => _defaultPresetsAlreadyExist || 
           _weaponCategories.All(category => GunPresetNames.Contains(category.Category.ToLower()));

    public static void GenerateDefaultPresets()
    {
        var allGunIndexes = new List<int>();
        
        Debug.Log("Generating default gun presets...!");

        SaveableGunPreset newPreset;
        List<int> newPresetWeapons = new();
        var curCategoryName = _weaponCategories[0].Category;

        for (var index = 0; index < _weapons.Count; index++)
        {
            var weapon = _weapons[index];
            var categoryName = weapon.Category;

            if (curCategoryName != categoryName || index == _weapons.Count - 1)
            {
                newPreset = new SaveableGunPreset(newPresetWeapons, curCategoryName.ToLower());
                AddNewPreset(newPreset);

                newPresetWeapons.Clear();
                curCategoryName = categoryName;
            }

            var weaponIndex = weapon.WeaponIndex;
            newPresetWeapons.Add(weaponIndex);
            allGunIndexes.Add(weaponIndex);
        }

        newPreset = new SaveableGunPreset(allGunIndexes, "all");
        AddNewPreset(newPreset);
        newPreset = new SaveableGunPreset(new List<int>(), "none");
        AddNewPreset(newPreset);
        
        _defaultPresetsAlreadyExist = true;
    }
    
    public static List<int> GetAllActiveWeapons() =>
        _weapons.Where(weapon => weapon.IsLocallyActive && // Verify both weapon and its parent category is active
                                 Helper.WeaponSelectHandler.FindCategoryByName(weapon.Category).IsActive)
            .Select(weapon => (int)weapon.WeaponIndex)
            .ToList();

    public static void AddNewPreset(SaveableGunPreset preset)
    {
        _weaponPresets.Add(preset);
        GunPresetNames.Add(preset.PresetName);
        GunPresetNames.Sort();
        
        SavePreset(preset);
    }

    private static void SavePreset(SaveableGunPreset preset)
    {
        var savedMapPresetsJson = JSONNode.Parse(File.ReadAllText(Plugin.GunPresetsPath));
        savedMapPresetsJson["savedPresets"].Add(preset.ToJson());

        File.WriteAllText(Plugin.GunPresetsPath, savedMapPresetsJson.ToString());
    }

    public static void DeletePreset(int presetIndex, string presetName)
    {
        var savedMapPresetsJson = JSONNode.Parse(File.ReadAllText(Plugin.GunPresetsPath));
        
        _weaponPresets.RemoveAt(presetIndex);
        GunPresetNames.Remove(presetName);
        savedMapPresetsJson["savedPresets"].Remove(presetIndex);
        
        File.WriteAllText(Plugin.GunPresetsPath, savedMapPresetsJson.ToString());
    }

    public static int FindIndexOfPreset(string presetName) 
        => _weaponPresets.FindIndex(preset => preset.PresetName == presetName);
    
    public static SaveableGunPreset FindPreset(string presetName) 
        => _weaponPresets.FirstOrDefault(preset => preset.PresetName == presetName);

    public static void LoadPreset(SaveableGunPreset preset)
    {
        for (var index = 0; index < _weapons.Count; index++)
        {
            var weapon = _weapons[index];
            var weaponIndex = weapon.WeaponIndex;
            var state = preset.Weapons.Contains(weaponIndex);
            
            weapon.IsLocallyActive = state;
            _weaponToggles[index].isOn = state;
        }
        
        // All weapons have the proper state, so enable all categories
        for (var index = 0; index < _weaponCategories.Count; index++)
        {
            var category = _weaponCategories[index];
            var categoryToggle = _categoryToggles[index];
            
            category.IsActive = true; 
            categoryToggle.isOn = true;
        }
    }
}