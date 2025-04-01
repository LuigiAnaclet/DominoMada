using System.Text;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoomUI : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI roomCodeText;
    public Button copyCodeButton;
    public Button startGameButton;
    public Button leaveRoomButton;
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI playerListText;
    public AudioSource joinSound;
    public AudioSource leaveSound;

    private string roomCode;
    private bool roomCreated = false;

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            Debug.Log("🔌 Connexion à Photon en cours...");
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Connecté au Master Server !");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("✅ Connecté au lobby, prêt à créer la room");

        roomCode = GenerateRandomCode(6);
        RoomOptions options = new RoomOptions { MaxPlayers = 4 };
        PhotonNetwork.CreateRoom(roomCode, options);

        roomCodeText.text = "Code : " + roomCode;
        copyCodeButton.onClick.AddListener(() => {
            GUIUtility.systemCopyBuffer = roomCode;
        });

        leaveRoomButton.onClick.AddListener(() => {
            PhotonNetwork.LeaveRoom();
        });

        startGameButton.gameObject.SetActive(false);
    }

    public override void OnJoinedRoom()
    {
        roomCreated = true;
        UpdateStartButtonVisibility();
        UpdatePlayerDisplay();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (joinSound != null) joinSound.Play();
        UpdateStartButtonVisibility();
        UpdatePlayerDisplay();
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player newPlayer)
    {
        if (leaveSound != null) leaveSound.Play();
        UpdateStartButtonVisibility();
        UpdatePlayerDisplay();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Leave room button clicked");
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    void UpdateStartButtonVisibility()
    {
        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= 3);
        startGameButton.onClick.RemoveAllListeners();
        startGameButton.onClick.AddListener(() => {
            PhotonNetwork.LoadLevel("PVPScene");
        });
    }

    void UpdatePlayerDisplay()
    {
        if (PhotonNetwork.CurrentRoom != null)
        {
            playerCountText.text = $"Joueurs : {PhotonNetwork.CurrentRoom.PlayerCount}/4";

            StringBuilder listBuilder = new StringBuilder();
            foreach (var player in PhotonNetwork.PlayerList)
            {
                listBuilder.AppendLine("- " + player.NickName);
            }
            playerListText.text = listBuilder.ToString();
        }
    }

    string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();
        char[] code = new char[length];
        for (int i = 0; i < length; i++)
            code[i] = chars[random.Next(chars.Length)];
        return new string(code);
    }
}
