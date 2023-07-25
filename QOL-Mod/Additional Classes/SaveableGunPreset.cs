using System.Collections.Generic;
using System.Linq;

namespace QOL;

public class SaveableGunPreset
{
    public string PresetName { get; }
    public List<int> Weapons { get; }

    public SaveableGunPreset(List<int> activeWeapons, string presetName)
    {
        PresetName = presetName;
        Weapons = activeWeapons;
    }

    public SaveableGunPreset(JSONNode presetJson)
    {
        PresetName = presetJson["name"];
        Weapons = new List<int>();

        foreach (var weaponJson in presetJson["guns"])
        {
            int weaponIndex = weaponJson.Value;
            Weapons.Add(weaponIndex);
        }
    }

    public JSONNode ToJson()
    {
        var jsonObj = new JSONObject();
        jsonObj.Add("name", PresetName);

        var jsonMaps = new JSONArray();
        foreach (var weapon in Weapons) 
            jsonMaps.Add(weapon);

        jsonObj.Add("guns", jsonMaps);
        return jsonObj;
    }

    public override string ToString()
    {
        var objStr = "Preset Name: " + PresetName + "\nGuns:\n\t{ ";
        objStr = Weapons.Aggregate(objStr, (current, weapon) => current + weapon + ", ");

        return objStr.Remove(objStr.Length-2) + "}";
    }
}