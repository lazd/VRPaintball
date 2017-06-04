using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PaintballNetworkManager : NetworkManager {
    private Color[] playerColors = new Color[] {
        new Color(0.9f, 0f, 0), // Red
        new Color(1, 0.55f, 0), // Orange
        new Color(0, 0.9f, 0), // Green
        new Color(0, 0.50f, 1), // Blue
        new Color(1, 0, 0.85f) // Pink
    };
    private int lastColor = 0;

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        base.OnServerAddPlayer(conn, playerControllerId);
        if (conn.playerControllers.Count > 0)
        {
            if (lastColor == playerColors.Length-1)
            {
                // Wrap around
                lastColor = 0;
            }

            GameObject player = conn.playerControllers[0].gameObject;
            player.GetComponent<PlayerController>().color = playerColors[lastColor];
            lastColor++;
        }
    }

    public void Update()
    {
        // Find all spheres
        // Increment score
        // Reset round if time > round time

    }
}
