using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Steamworks;

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
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F1) && !ChatManager.isTyping)
            {
                Debug.Log("Trying to open GUI menu!");
                this.mShowMenu = !this.mShowMenu;
                this.networkPlayerArray = UnityEngine.Object.FindObjectsOfType<NetworkPlayer>();
                Debug.Log("playerarray: ");
                foreach (NetworkPlayer owo in this.networkPlayerArray)
                {
                    Debug.Log(owo);
                }

                this.theLobbyID = Helper.lobbyID;
                this.theLobbyHost = Helper.GetPlayerName(Helper.lobbyID);
            }
        }
        public void OnGUI()
        {
            if (!this.mShowMenu)
            {
                return;
            }
            this.MenuRect = GUILayout.Window(this.WindowId, this.MenuRect, new GUI.WindowFunction(this.KickWindow), "<color=red><b><i>Monk's QOL Menu</i></b></color>\t[v1.0.6]", new GUILayoutOption[0]);
        }
		private void KickWindow(int window)
		{
			GUILayout.Label("\t<color=#228f69>Show / Hide Menu (Q)</color>", new GUILayoutOption[0]);
			GUILayout.Label("<color=red>Lobby ID:</color> " + this.theLobbyID, new GUILayoutOption[0]);
			GUILayout.Label("Host: " + this.theLobbyHost, new GUILayoutOption[0]);
			string text = "Players in Room: \n";
			foreach (NetworkPlayer networkPlayer in networkPlayerArray)
			{
				string str = string.Concat(new object[]
				{
				"[",
				Helper.GetColorFromID(networkPlayer.NetworkSpawnID),
				"] ",
				Helper.GetPlayerName(Helper.GetSteamID(networkPlayer.NetworkSpawnID))
				});
				text = text + "\n" + str;
			}
			GUILayout.Label(text, new GUILayoutOption[0]);
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
        }
        private MultiplayerManager mManager = UnityEngine.Object.FindObjectOfType<MultiplayerManager>();
        private MatchmakingHandler mMatchmaking = UnityEngine.Object.FindObjectOfType<MatchmakingHandler>();
        private bool mShowMenu;
        private Rect MenuRect = new Rect(0f, 100f, 350f, 375f);
        private int WindowId = 100;
        private NetworkPlayer[] networkPlayerArray;
        // private string[] playerNamesArray;
        private string theLobbyHost;
        private CSteamID theLobbyID;
    }
}
