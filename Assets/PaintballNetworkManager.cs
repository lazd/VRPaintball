using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PaintballNetworkManager : NetworkManager {
    private Color[] playerColors = new Color[] {
        new Color(0.9f, 0f, 0), // Red
        new Color(1, 0.55f, 0), // Orange
        new Color(1, 0, 0.85f), // Pink
        new Color(0, 0.50f, 1) // Blue
    };
    private int lastColor = 0;

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        base.OnServerAddPlayer(conn, playerControllerId);
        if (conn.playerControllers.Count > 0)
        {
            GameObject player = conn.playerControllers[0].gameObject;
            player.GetComponent<PlayerController>().color = playerColors[lastColor];
            lastColor++;
        }
    }
}
