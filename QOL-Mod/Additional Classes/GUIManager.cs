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
            Debug.Log("Started GUI or called class?");
            this.mTestNetworkPlayer = base.gameObject.GetComponent<NetworkPlayer>();
        }
        private void Awake()
        {
        }
        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F1) && !ChatManager.isTyping)
            {
                this.mShowMenu = !this.mShowMenu;
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
			GUILayout.Label("<color=red>Lobby ID:</color> " + MatchmakingHandlerPatch.lobbyID, new GUILayoutOption[0]);
			GUILayout.Label("Host: " + GUIManager.GetPlayerName(this.mMatchmaking.LobbyOwner), new GUILayoutOption[0]);
			string text = "Players in Room: \n";
			foreach (NetworkPlayer networkPlayer in UnityEngine.Object.FindObjectsOfType<NetworkPlayer>())
			{
				string str = string.Concat(new object[]
				{
				"[",
				this.GetColor(networkPlayer.NetworkSpawnID),
				"] ",
				GUIManager.GetPlayerName(ChatManagerPatches.GetSteamID(networkPlayer.NetworkSpawnID))
				});
				text = text + "\n" + str;
			}
			GUILayout.Label(text, new GUILayoutOption[0]);
			if (GUI.Button(new Rect(120f, 335f, 100f, 30f), "Get Lobby Link"))
			{
				ChatManagerPatches.GetJoinGameLink(MatchmakingHandlerPatch.lobbyID, ChatManagerPatches.localPlayerSteamID);
			}
		}
        public string GetColor(ushort x)
        {
            switch (x)
            {
                case 1:
                    return "Blue";
                case 2:
                    return "Red";
                case 3:
                    return "Green";
                default:
                    return "Yellow";
            }
        }
        public static string GetPlayerName(CSteamID passedClientID)
        {
            return SteamFriends.GetFriendPersonaName(passedClientID);
        }
        private MultiplayerManager mManager = UnityEngine.Object.FindObjectOfType<MultiplayerManager>();
        private MatchmakingHandler mMatchmaking = UnityEngine.Object.FindObjectOfType<MatchmakingHandler>();
        private NetworkPlayer mTestNetworkPlayer;
        private bool mShowMenu;
        private Rect MenuRect = new Rect(0f, 100f, 350f, 375f);
        private int WindowId = 100;
    }
}
