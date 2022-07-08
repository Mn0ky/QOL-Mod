using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;

namespace QOL
{
    class OnlinePlayerUIPatch
    {
        public static void Patch(Harmony harmonyInstance)
        {
            var updateMethod = AccessTools.Method(typeof(OnlinePlayerUI), "Update");
            var updateMethodPrefix = new HarmonyMethod(typeof(OnlinePlayerUIPatch).GetMethod(nameof(UpdateMethodPrefix)));
            harmonyInstance.Patch(updateMethod, prefix: updateMethodPrefix);
        }

        public static bool UpdateMethodPrefix(ref bool ___mIsStaying, ref ConnectedClientData[] ___mClients, ref TextMeshProUGUI[] ___mPlayerTexts)
        {
			if (!___mIsStaying) return false;

			for (int i = 0; i < ___mClients.Length; i++)
			{
				ConnectedClientData client = ___mClients[i];
				if (client == null || !client.ClientID.IsValid() || client.PlayerObject == null) ___mPlayerTexts[i].text = string.Empty;

				else
				{
                    if (Helper.IsCustomName && i == Helper.localNetworkPlayer.NetworkSpawnID) ___mPlayerTexts[i].text = Plugin.configCustomName.Value;
                    
                    else 
                    {
                        Debug.Log("assigning normal name");
                        ___mPlayerTexts[i].text = client.PlayerName;
                    }

                    CodeStateAnimation component = ___mPlayerTexts[i].GetComponent<CodeStateAnimation>();
					GameObject gameObject = client.PlayerObject.GetComponentInChildren<Torso>().gameObject;
					component.state1 = true;

					if (gameObject == null) break;
                    component.transform.position = gameObject.transform.position + Vector3.up * 1.5f;

					if (gameObject == null) component.state1 = false;
				}
			}

            return false;
		}
    }
}
