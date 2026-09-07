using Unity.Netcode;
using UnityEngine;

public class NetworkUI : MonoBehaviour
{
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 20, 150, 150));

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
            GUILayout.Label(manager.IsHost ? "Host mode" : "Client mode");
        }

        GUILayout.EndArea();
    }
}