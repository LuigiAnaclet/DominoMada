using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class MainMenuUI : MonoBehaviourPunCallbacks
{
    private Button createRoomButton;
    private Button joinRoomButton;
    private Button coopVsIAButton;
    public TMP_InputField nicknameInput;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings(); // Connexion automatique à Photon

        // Recherche automatique des boutons par nom
        createRoomButton = GameObject.Find("CreateButton").GetComponent<Button>();
        joinRoomButton = GameObject.Find("JoinButton").GetComponent<Button>();
        coopVsIAButton = GameObject.Find("CoopVsIAButton").GetComponent<Button>();

        createRoomButton.onClick.AddListener(() => {
            UnityEngine.SceneManagement.SceneManager.LoadScene("CreateRoomScene");
            SetNickname();
            Debug.Log("Create button clicked");
        });

        joinRoomButton.onClick.AddListener(() => {
            UnityEngine.SceneManagement.SceneManager.LoadScene("JoinRoomScene");
            SetNickname();
            Debug.Log("Join button clicked");
        });

        coopVsIAButton.onClick.AddListener(() => {
            UnityEngine.SceneManagement.SceneManager.LoadScene("CoopVsIA");
            Debug.Log("CoopVsIA button clicked");
        });

        void SetNickname()
        {
            string pseudo = nicknameInput.text.Trim();
            if (string.IsNullOrEmpty(pseudo))
            {
                pseudo = "Joueur_" + Random.Range(1000, 9999);
            }
            PhotonNetwork.NickName = pseudo;
            Debug.Log("👤 Pseudo défini : " + pseudo);
        }

    }
}