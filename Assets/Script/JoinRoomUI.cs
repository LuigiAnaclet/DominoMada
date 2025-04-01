using Photon.Pun;
using TMPro;
using UnityEngine.UI;

public class JoinRoomUI : MonoBehaviourPunCallbacks
{
    public TMP_InputField codeInputField;
    public Button joinButton;
    public TextMeshProUGUI feedbackText;
    public Button leaveRoomButton;

    private void Start()
    {
        joinButton.onClick.AddListener(() => {
            string code = codeInputField.text.ToUpper();
            PhotonNetwork.JoinRoom(code);
        });
    }

    public override void OnJoinedRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("PVPScene");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        feedbackText.text = "❌ Code invalide ou salle pleine.";
    }
    public override void OnLeftRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}