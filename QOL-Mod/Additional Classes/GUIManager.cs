using System;
using System.IO;
using System.Linq;
using Steamworks;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace QOL
{
    public class GUIManager : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("Started GUI in GUIManager!");
            Helper.matchmaking = FindObjectOfType<MatchmakingHandler>();
            Helper.gameManager = FindObjectOfType<GameManager>();
        }
        private void Awake()
        {
            QOLMenuKey1 = Plugin.configQOLMenuKeybind.Value.MainKey;

            if (Plugin.configQOLMenuKeybind.Value.Modifiers.Any())
            {
                foreach (var modifier in Plugin.configQOLMenuKeybind.Value.Modifiers)
                {
                    Debug.Log("Assigning secondary key, menu: " + modifier);
                    QOLMenuKey2 = modifier;
                }
            }
            else
            {
                Debug.Log("Anykeymenu true");
                anyKeyMenu = true;
            }

            statWindowKey1 = Plugin.configStatMenuKeybind.Value.MainKey;

            if (Plugin.configStatMenuKeybind.Value.Modifiers.Any())
            {
                foreach (var modifier in Plugin.configStatMenuKeybind.Value.Modifiers)
                {
                    Debug.Log("Assigning secondary key, stat: " + modifier);
                    statWindowKey2 = modifier;
                    return;
                }
            }
            else
            {
                Debug.Log("Anykeystat true");
                anyKeyStat = true;
            }
        }
        private void Update()
        {
            if (Input.GetKey(QOLMenuKey1) && Input.GetKeyDown(QOLMenuKey2) || Input.GetKeyDown(QOLMenuKey1) && anyKeyMenu)
            {
                Debug.Log("Trying to open GUI menu!");
                Debug.Log("chatText.richText : " + Helper.tmpText.richText);

                mShowMenu = !mShowMenu;
                playerNamesStr = string.Empty;
                foreach (NetworkPlayer player in FindObjectsOfType<NetworkPlayer>())
                {
                    string str = string.Concat(new object[]
                    {
                        "[",
                        Helper.GetColorFromID(player.NetworkSpawnID),
                        "] ",
                        Helper.GetPlayerName(Helper.GetSteamID(player.NetworkSpawnID))
                    });
                    playerNamesStr = playerNamesStr + "\n" + str;
                }
                Debug.Log("Helper.LobbyID : " + Helper.lobbyID);
                Debug.Log(FindObjectOfType<MatchmakingHandler>().LobbyOwner);
                Debug.Log("findobject lobbyowner: " + Helper.matchmaking.LobbyOwner);
                theLobbyHost = Helper.GetPlayerName(Helper.matchmaking.LobbyOwner);
                Debug.Log("this.theLobbyHost : " + theLobbyHost);
            }

            if (Input.GetKey(statWindowKey1) && Input.GetKeyDown(statWindowKey2) || Input.GetKeyDown(statWindowKey1) && anyKeyStat)
            {
                //UpdateStatMenuFields();
                mStatsShown = true;
                mShowStatMenu = !mShowStatMenu;
            }

            if (mShowStatMenu && mStatsShown)
            {
                yellowStatsText = "";
                blueStatsText = "";
                redStatsText = "";
                greenStatsText = "";
                Array.Clear(playersInLobby, 0, playersInLobby.Length);

                foreach (var stat in FindObjectsOfType<CharacterStats>())
                {
                    switch (stat.GetComponentInParent<NetworkPlayer>().NetworkSpawnID)
                    {
                        case 0:
                            yellowStatsText = stat.GetString();
                            playersInLobby[0] = "Yellow";
                            break;
                        case 1:
                            blueStatsText = stat.GetString();
                            playersInLobby[1] = "Blue";
                            break;
                        case 2:
                            redStatsText = stat.GetString();
                            playersInLobby[2] = "Red";
                            break;
                        default:
                            greenStatsText = stat.GetString();
                            playersInLobby[3] = "Green";
                            break;
                    }
                }
                Debug.Log("show stats being set to false via update");
                mStatsShown = false;
            }

        }
        public void OnGUI() 
        {
            if (mShowMenu) MenuRect = GUILayout.Window(WindowId, MenuRect, KickWindow, "<color=red><b><i>Monk's QOL Menu</i></b></color>\t[v" + Plugin.VersionNumber + "]");
            if (mShowStatMenu)
            {
                StatMenuRect = GUILayout.Window(101, StatMenuRect, StatWindow, "Stat Window");
                //if (!mStatsShown) 
            }
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
                Helper.localNetworkPlayer.OnTalked("Yellow HP: " + Helper.GetHPOfPlayer("yellow"));
            }
            if (GUI.Button(new Rect(89f, 300f, 80f, 30f), "<color=blue>HP Blue</color>"))
            {
                Helper.localNetworkPlayer.OnTalked("Blue HP: " + Helper.GetHPOfPlayer("blue"));
            }
            if (GUI.Button(new Rect(176f, 300f, 80f, 30f), "<color=red>HP Red</color>"))
            {
                Helper.localNetworkPlayer.OnTalked("Red HP: " + Helper.GetHPOfPlayer("red"));
            }
            if (GUI.Button(new Rect(263f, 300f, 80f, 30f), "<color=green>HP Green</color>"))
            {
                Helper.localNetworkPlayer.OnTalked("Green HP: " + Helper.GetHPOfPlayer("green"));
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
                Debug.Log("stats being changed with stat men button: " + mStatsShown);
            }
            if (GUI.Button(new Rect(263f, 265f, 80f, 30f), "Shrug"))
            {
                Helper.localNetworkPlayer.OnTalked($" \u00af\\_{Plugin.configEmoji.Value}_/\u00af");
            }
            if (GUI.Button(new Rect(2f, 265f, 80f, 30f), "Help"))
            {
                SteamFriends.ActivateGameOverlayToWebPage("https://github.com/Mn0ky/QOL-Mod#chat-commands");
            }
            if (GUI.Button(new Rect(133f, 335f, 80f, 30f), "Private"))
            {
                if (Helper.matchmaking.IsHost)
                {
                    MethodInfo ChangeLobbyTypeMethod = typeof(MatchmakingHandler).GetMethod("ChangeLobbyType", BindingFlags.NonPublic | BindingFlags.Instance);
                    ChangeLobbyTypeMethod.Invoke(Helper.matchmaking, new object[] { ELobbyType.k_ELobbyTypeFriendsOnly });
                    Helper.localNetworkPlayer.OnTalked("Lobby made private!");
                }
                else
                {
                    Helper.localNetworkPlayer.OnTalked("Need to be host!");
                }
            }
            if (GUI.Button(new Rect(263f, 335f, 80f, 30f), "Public"))
            {
                if (Helper.matchmaking.IsHost)
                {
                    MethodInfo ChangeLobbyTypeMethod = typeof(MatchmakingHandler).GetMethod("ChangeLobbyType", BindingFlags.NonPublic | BindingFlags.Instance);
                    ChangeLobbyTypeMethod.Invoke(Helper.matchmaking, new object[] { ELobbyType.k_ELobbyTypePublic});
                    Helper.localNetworkPlayer.OnTalked("Lobby made public!");
                }
                else
                {
                    Helper.localNetworkPlayer.OnTalked("Need to be host!");
                }
            }
            Helper.autoGG = GUI.Toggle(new Rect(6f, 188f, 100f, 30f), Helper.autoGG, "AutoGG");
            Helper.isTranslating = GUI.Toggle(new Rect(100f, 220f, 106f, 30f), Helper.isTranslating, "AutoTranslations");
            Helper.tmpText.richText = GUI.Toggle(new Rect(6f, 220f, 115f, 30f), Helper.tmpText.richText, "RichText");
            Helper.chatCensorshipBypass = GUI.Toggle(new Rect(100, 188f, 150f, 30f), Helper.chatCensorshipBypass, "ChatCensorshipBypass");
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void StatWindow(int window)
        {
            var normAlignment = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.UpperCenter;
            GUI.skin.button.alignment = TextAnchor.LowerCenter;
            GUILayout.Label("<color=#228f69>(Click To Drag)</color>");
            if (GUI.Button(new Rect(237.5f, 310f, 80f, 25f), "Close"))
            {
                mShowStatMenu = !mShowStatMenu;
            }

            GUI.skin.label.alignment = normAlignment;
            GUILayout.BeginHorizontal();
            foreach (string color in playersInLobby)
            {
                GUILayout.Label($"<color={color}>{color}</color>");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(yellowStatsText);
            GUILayout.Label(blueStatsText);
            GUILayout.Label(redStatsText);
            GUILayout.Label(greenStatsText);
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private bool mShowMenu;
        private bool mStatsShown;
        public static float[] QOLMenuPos;
        public static float[] StatMenuPos;

        private bool mShowStatMenu;

        private string yellowStatsText;
        private string blueStatsText;
        private string redStatsText;
        private string greenStatsText;

        private Rect MenuRect = new(QOLMenuPos[0], QOLMenuPos[1], 350f, 375f);

        private Rect StatMenuRect = new(StatMenuPos[0], StatMenuPos[1], 510f, 350f);

        private int WindowId = 100;

        private string[] playersInLobby = {"", "", "", ""};

        private string playerNamesStr = "Players in Room: \n";

        private string theLobbyHost;

        private KeyCode QOLMenuKey1;
        private KeyCode QOLMenuKey2 = KeyCode.Joystick8Button19;
        private bool anyKeyMenu;

        private KeyCode statWindowKey1;
        private KeyCode statWindowKey2 = KeyCode.Joystick8Button19;
        private bool anyKeyStat;
    }
}
