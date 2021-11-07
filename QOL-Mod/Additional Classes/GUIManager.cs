using Steamworks;
using UnityEngine;
using HarmonyLib;
using TMPro;

namespace QOL
{
    public class GUIManager : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("Started GUI in GUIManager!");
        }
        private void Awake()
        {
        }
        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F1))
            {
                Debug.Log("Trying to open GUI menu!");
                Debug.Log("chatText.richText : " + Helper.tmpText.richText);
                mShowMenu = !mShowMenu;
                networkPlayerArray = FindObjectsOfType<NetworkPlayer>();
                playerNamesStr = string.Empty;
                foreach (NetworkPlayer player in networkPlayerArray)
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

                theLobbyID = Helper.lobbyID;
                theLobbyHost = Helper.GetPlayerName(mMatchmaking.LobbyOwner);
                Debug.Log("this.theLobbyID : " + theLobbyID);
                Debug.Log("this.theLobbyHost : " + theLobbyHost);
                Debug.Log(FindObjectOfType<MatchmakingHandler>().LobbyOwner);
            }
        }
        public void OnGUI()
        {
            if (!mShowMenu)
            {
                return;
            }
            MenuRect = GUILayout.Window(WindowId, MenuRect, KickWindow, "<color=red><b><i>Monk's QOL Menu</i></b></color>\t[v1.0.9]");
        }
		private void KickWindow(int window)
		{
			GUILayout.Label("\t<color=#228f69>Show / Hide Menu (Q)</color>");
			GUILayout.Label("<color=red>Lobby ID:</color> " + theLobbyID);
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
			if (GUI.Button(new Rect(120f, 335f, 100f, 30f), "Get Lobby Link"))
			{
				Helper.GetJoinGameLink();
                Helper.localNetworkPlayer.OnTalked("Join link copied to clipboard!");
			}
            Helper.autoGG = GUI.Toggle(new Rect(6f, 188f, 100f, 30f), Helper.autoGG, "AutoGG");
            Helper.tmpText.richText = GUI.Toggle(new Rect(6f, 220f, 115f, 30f), Helper.tmpText.richText, "RichTextInChat");
            Helper.chatCensorshipBypass = GUI.Toggle(new Rect(100, 188f, 150f, 30f), Helper.chatCensorshipBypass, "ChatCensorshipBypass");
            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }
        private MultiplayerManager mManager = FindObjectOfType<MultiplayerManager>();

        private MatchmakingHandler mMatchmaking = FindObjectOfType<MatchmakingHandler>();

        private bool mShowMenu;

        private Rect MenuRect = new(0f, 100f, 350f, 375f);

        private int WindowId = 100;

        private NetworkPlayer[] networkPlayerArray;

        private string playerNamesStr = "Players in Room: \n";

        private string theLobbyHost;

        private CSteamID theLobbyID;
    }
}
