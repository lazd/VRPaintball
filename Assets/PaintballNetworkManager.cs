using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PaintballNetworkManager : NetworkManager {
    public Color[] playerColors = { Color.green, Color.red, Color.blue, Color.yellow };
    private int lastColor = 0;

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        base.OnServerAddPlayer(conn, playerControllerId);
        if (conn.playerControllers.Count > 0)
        {
            GameObject player = conn.playerControllers[0].gameObject;
            player.GetComponent<PlayerController>().color = playerColors[lastColor];
            Debug.Log("Settingh color to "+ lastColor +" = "+ playerColors[lastColor]);
            lastColor++;
        }
    }
}
