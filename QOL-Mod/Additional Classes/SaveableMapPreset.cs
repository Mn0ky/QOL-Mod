using System.Collections.Generic;
using System.Linq;

namespace QOL;

public class SaveableMapPreset
{
    public string PresetName { get; }
    public List<string> Maps { get; }

    public SaveableMapPreset(List<string> activeMaps, string presetName)
    {
        PresetName = presetName;
        Maps = activeMaps;
    }

    public SaveableMapPreset(JSONNode presetJson)
    {
        PresetName = presetJson["name"];
        Maps = new List<string>();

        foreach (var mapJson in presetJson["maps"])
        {
            string mapIndex = mapJson.Value;
            Maps.Add(mapIndex);
        }
    }

    public JSONNode ToJson()
    {
        var jsonObj = new JSONObject();
        jsonObj.Add("name", PresetName);

        var jsonMaps = new JSONArray();
        foreach (var map in Maps) 
            jsonMaps.Add(map);

        jsonObj.Add("maps", jsonMaps);
        return jsonObj;
    }

    public override string ToString()
    {
        var objStr = "Preset Name: " + PresetName + "\nMaps:\n\t{ ";
        objStr = Maps.Aggregate(objStr, (current, map) => current + map + ", ");

        return objStr.Remove(objStr.Length-2) + "}";
    }
}