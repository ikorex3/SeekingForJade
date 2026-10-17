using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 200, 160));

        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null)
        {
            GUILayout.EndArea();
            return;
        }

        if (!manager.IsClient && !manager.IsServer)
        {
            if (GUILayout.Button("Host", GUILayout.Height(30)))
            {
                manager.StartHost();
            }

            if (GUILayout.Button("Client", GUILayout.Height(30)))
            {
                manager.StartClient();
            }
        }
        else
        {
            string status;
            if (manager.IsHost)
            {
                int count = manager.ConnectedClientsIds.Count;
                status = $"Host Mode ({count} {(count == 1 ? "player" : "players")})";
            }
            else if (manager.IsServer)
            {
                int count = manager.ConnectedClientsIds.Count;
                status = $"Server Mode ({count} {(count == 1 ? "player" : "players")})";
            }
            else
            {
                status = "Client Connected";
            }

            GUILayout.Label(status);

            if (GUILayout.Button("Disconnect", GUILayout.Height(30)))
            {
                manager.Shutdown();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        GUILayout.EndArea();
    }
}