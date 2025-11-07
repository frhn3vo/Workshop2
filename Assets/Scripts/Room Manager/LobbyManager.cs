using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public void OnCreateRoom()
    {
        // Go to RoomOptions scene to set room settings
        SceneManager.LoadScene("RoomOptions");
    }

    public void OnJoinRoom()
    {
        // Go to JoinRoom scene to enter room ID
        SceneManager.LoadScene("JoinRoom");
    }

    public void OnBackToMainMenu()
    {
        // Disconnect from Photon when returning to main menu
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        // Only load main menu after successfully disconnected
        SceneManager.LoadScene("MainMenu");
    }
}