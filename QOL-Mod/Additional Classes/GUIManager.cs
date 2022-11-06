using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using UnityEngine;

namespace QOL
{
    public class GUIManager : MonoBehaviour
    {
        private bool mShowMenu;
        private bool mStatsShown;
        public static float[] QOLMenuPos;
        public static float[] StatMenuPos;

        private bool mShowStatMenu;

        private Rect MenuRect = new(QOLMenuPos[0], QOLMenuPos[1], 350f, 375f);

        private Rect StatMenuRect = new(StatMenuPos[0], StatMenuPos[1], 510f, 350f);

        private int WindowId = 100;

        private string[] playerStats = new string[4];

        private string playerNamesStr = "Players in Room: \n";

        private string theLobbyHost;

        private KeyCode QOLMenuKey1;
        private KeyCode QOLMenuKey2;
        private bool singleMenuKey;

        private KeyCode statWindowKey1;
        private KeyCode statWindowKey2;
        private bool singleStatKey;

        private void Start() => Debug.Log("Started GUI in GUIManager!");
        
        private void Awake()
        {
            QOLMenuKey1 = Plugin.ConfigQolMenuKeybind.Value.MainKey;
            QOLMenuKey2 = Plugin.ConfigQolMenuKeybind.Value.Modifiers.LastOrDefault();
            if (QOLMenuKey2 == KeyCode.None) singleMenuKey = true;

            statWindowKey1 = Plugin.ConfigStatMenuKeybind.Value.MainKey;
            statWindowKey2 = Plugin.ConfigStatMenuKeybind.Value.Modifiers.LastOrDefault();
            if (statWindowKey2 == KeyCode.None) singleStatKey = true;
        }

        private void Update()
        {
            if (Input.GetKey(QOLMenuKey1) && Input.GetKeyDown(QOLMenuKey2) || Input.GetKeyDown(QOLMenuKey1) && singleMenuKey)
            {
                Debug.Log("Trying to open GUI menu!");

                mShowMenu = !mShowMenu;
                playerNamesStr = "";

                foreach (NetworkPlayer player in FindObjectsOfType<NetworkPlayer>())
                {
                    var str = string.Concat(
                        "[",
                        Helper.GetColorFromID(player.NetworkSpawnID),
                        "] ",
                        Helper.GetPlayerName(Helper.GetSteamID(player.NetworkSpawnID)));
                    
                    playerNamesStr +=  "\n" + str;
                }

                theLobbyHost = Helper.GetPlayerName(MatchmakingHandler.Instance.LobbyOwner);
            }

            if (Input.GetKey(statWindowKey1) && Input.GetKeyDown(statWindowKey2) || Input.GetKeyDown(statWindowKey1) && singleStatKey)
            {
                mStatsShown = true;
                mShowStatMenu = !mShowStatMenu;
            }

            if (mShowStatMenu && mStatsShown)
            {
                foreach (var stat in FindObjectsOfType<CharacterStats>())
                {
                    switch (stat.GetComponentInParent<NetworkPlayer>().NetworkSpawnID)
                    {
                        case 0:
                            playerStats[0] = stat.GetString();
                            Debug.Log(playerStats[0]);
                            break;
                        case 1:
                            playerStats[1] = stat.GetString();
                            Debug.Log(playerStats[1]);
                            break;
                        case 2:
                            playerStats[2] = stat.GetString();
                            Debug.Log(playerStats[2]);
                            break;
                        default:
                            playerStats[3] = stat.GetString();
                            Debug.Log(playerStats[3]);
                            break;
                    }
                }

                Debug.Log("show stats being set to false via update");
                mStatsShown = false;
            }

        }
        public void OnGUI() 
        {
            if (mShowMenu)
                MenuRect = GUILayout.Window(WindowId, MenuRect, KickWindow,
                    $"<color=red>Monky's QOL Menu</color>\t[v{Plugin.VersionNumber}]");
            if (mShowStatMenu) StatMenuRect = GUILayout.Window(101, StatMenuRect, StatWindow, "Stat Menu");
        }
		private void KickWindow(int window)
        {
            var normAlignment = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("<color=#228f69>(Click To Drag)</color>");
            GUI.skin.label.alignment = normAlignment;

			GUILayout.Label("Host: " + theLobbyHost);
            GUILayout.Label(playerNamesStr);

            if (GUI.Button(new Rect(2f, 300f, 80f, 30f), "<color=yellow>HP Yellow</color>"))
            {
                var yellowHP = new PlayerHP("yellow");
                Helper.localNetworkPlayer.OnTalked(yellowHP.FullColor + " HP: " + yellowHP.HP);
            }

            if (GUI.Button(new Rect(89f, 300f, 80f, 30f), "<color=blue>HP Blue</color>"))
            {
                var blueHP = new PlayerHP("blue");
                Helper.localNetworkPlayer.OnTalked(blueHP.FullColor + " HP: " + blueHP.HP);
            }

            if (GUI.Button(new Rect(176f, 300f, 80f, 30f), "<color=red>HP Red</color>"))
            {
                var redHP = new PlayerHP("red");
                Helper.localNetworkPlayer.OnTalked(redHP.FullColor + " HP: " + redHP.HP);
            }

            if (GUI.Button(new Rect(263f, 300f, 80f, 30f), "<color=green>HP Green</color>"))
            {
                var greenHP = new PlayerHP("green");
                Helper.localNetworkPlayer.OnTalked(greenHP.FullColor + " HP: " + greenHP.HP);
            }

            if (GUI.Button(new Rect(3f, 335f, 80f, 30f), "Lobby Link"))
			{
                GUIUtility.systemCopyBuffer = Helper.GetJoinGameLink();
                Helper.localNetworkPlayer.OnTalked("Join link copied to clipboard!");
			}

            if (GUI.Button(new Rect(133f, 265f, 80f, 30f), "Stat Menu"))
            {
                mShowStatMenu = !mShowStatMenu;
                mStatsShown = true;
            }

            if (GUI.Button(new Rect(263f, 265f, 80f, 30f), "Shrug"))
                Helper.localNetworkPlayer.OnTalked($" \u00af\\_{Plugin.ConfigEmoji.Value}_/\u00af");
            
            if (GUI.Button(new Rect(2f, 265f, 80f, 30f), "Help"))
                SteamFriends.ActivateGameOverlayToWebPage("https://github.com/Mn0ky/QOL-Mod#chat-commands");
            
            if (GUI.Button(new Rect(133f, 335f, 80f, 30f), "Private")) 
                Helper.ToggleLobbyVisibility(false);
            
            if (GUI.Button(new Rect(263f, 335f, 80f, 30f), "Public")) 
                Helper.ToggleLobbyVisibility(true);

            Helper.AutoGG = GUI.Toggle(new Rect(6f, 188f, 100f, 30f), 
                Helper.AutoGG, "AutoGG");
            
            Helper.IsTranslating = GUI.Toggle(new Rect(100f, 220f, 106f, 30f), 
                Helper.IsTranslating, "AutoTranslations");
            
            Helper.TMPText.richText = GUI.Toggle(new Rect(6f, 220f, 115f, 30f), 
                Helper.TMPText.richText, "RichText");
            
            Helper.ChatCensorshipBypass = GUI.Toggle(new Rect(100, 188f, 150f, 30f), 
                Helper.ChatCensorshipBypass, "ChatCensorshipBypass");

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void StatWindow(int window)
        {
            var normAlignment = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.UpperCenter;
            GUI.skin.button.alignment = TextAnchor.LowerCenter;
            GUILayout.Label("<color=#228f69>(Click To Drag)</color>");

            if (GUI.Button(new Rect(237.5f, 310f, 80f, 25f), "Close")) 
                mShowStatMenu = !mShowStatMenu;
            GUI.skin.label.alignment = normAlignment;

            GUILayout.BeginHorizontal();
            for (ushort i = 0; i < playerStats.Length; i++)
            {
                var stat = playerStats[i];
                var color = Helper.GetColorFromID(i);

                GUILayout.BeginVertical();
                GUILayout.Label("<color=" + color + ">" + color + "</color>");
                GUILayout.Label(stat);  
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
    }
}
