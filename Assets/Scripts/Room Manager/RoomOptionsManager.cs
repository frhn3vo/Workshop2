using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class RoomOptionsManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    public Dropdown difficultyDropdown;
    public Dropdown terrainDropdown;
    public Button createRoomButton;
    public Button exitButton;
    public Text roomIdDisplayText;

    [Header("Room Settings")]
    public string roomId;
    private RoomOptions roomOptions;

    void Start()
    {
        // Generate random 6-digit room ID
        roomId = GenerateSixDigitRoomID();
        Debug.Log("Generated Room ID: " + roomId);

        // Display the room ID to the host
        if (roomIdDisplayText != null)
        {
            roomIdDisplayText.text = $"Room ID: {roomId}";
        }

        createRoomButton.onClick.AddListener(CreateRoomWithOptions);
        exitButton.onClick.AddListener(OnExitToLobby);
    }

    private string GenerateSixDigitRoomID()
    {
        // Generate a 6-digit numeric room ID
        System.Random random = new System.Random();
        return random.Next(100000, 999999).ToString();
    }

    public void OnExitToLobby()
    {
        Debug.Log("Returning to lobby without creating room");
        SceneManager.LoadScene("Lobby");
    }

    public void CreateRoomWithOptions()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            return;
        }

        // Get selected options
        string difficulty = difficultyDropdown.options[difficultyDropdown.value].text;
        int terrainTypeIndex = terrainDropdown.value; // 0=Grass, 1=Desert, 2=Black Soil

        // Store room options in custom properties
        roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;
        roomOptions.IsVisible = false; // Make room private (join by ID only)

        // Store room properties including the generated room ID AND terrain type
        roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
        {
            { "Difficulty", difficulty },
            { "TerrainType", terrainTypeIndex }, // Store terrain type index
            { "RoomID", roomId } // Store the 6-digit room ID
        };
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "Difficulty", "TerrainType", "RoomID" };

        Debug.Log($"Creating room with - Difficulty: {difficulty}, Terrain Type: {terrainTypeIndex}, RoomID: {roomId}");

        // Use the generated 6-digit roomId as the room name
        PhotonNetwork.CreateRoom(roomId, roomOptions);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room created successfully! Room ID: " + roomId);
        // Set the host as the master client who can start the game
        PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);

        // Store the room ID and terrain type in room properties for all players to access
        if (PhotonNetwork.IsMasterClient)
        {
            // Get the selected terrain type from dropdown
            int terrainTypeIndex = terrainDropdown.value;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { "RoomID", roomId },
                { "TerrainType", terrainTypeIndex } // Ensure terrain type is stored
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            Debug.Log($"Master client set terrain type to index: {terrainTypeIndex}");
        }

        // Go to waiting room
        SceneManager.LoadScene("WaitingRoom");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Room creation failed: " + message);
        // Regenerate room ID and try again
        roomId = GenerateSixDigitRoomID();
        Debug.Log("New Room ID: " + roomId);

        // Update the display
        if (roomIdDisplayText != null)
        {
            roomIdDisplayText.text = $"Room ID: {roomId}";
        }
    }

    public override void OnConnectedToMaster()
    {
        // If we reconnected after being disconnected, retry room creation
        if (!string.IsNullOrEmpty(roomId))
        {
            CreateRoomWithOptions();
        }
    }
}