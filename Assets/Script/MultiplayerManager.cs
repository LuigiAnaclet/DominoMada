using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class MultiplayerManager : MonoBehaviourPunCallbacks
{
    public static MultiplayerManager Instance;

    public GameManager gameManager;
    public UIManager uiManager;
    public GameObject playerPrefab;

    private Dictionary<int, Player> photonPlayers = new Dictionary<int, Player>();
    private int playersReady = 0;
    private int expectedPlayerCount = 0;
    private int handsReceived = 0;
    private HashSet<int> confirmedPlayers = new HashSet<int>();


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterGameManager(GameManager gm)
    {
        this.gameManager = gm;
        //Debug.Log("✅ GameManager enregistré dans MultiplayerManager.");
    }

    private IEnumerator Start()
    {
        while (!PhotonNetwork.IsConnectedAndReady)
            yield return null;

        expectedPlayerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        //PhotonNetwork.AutomaticallySyncScene = true;

        StartCoroutine(WaitAndInstantiatePlayer());
    }


    IEnumerator WaitAndInstantiatePlayer()
    {
        yield return new WaitForSeconds(0.5f);

        // S'assurer que le joueur local n'a pas déjà été instancié
        if (PhotonNetwork.LocalPlayer.TagObject != null)
        {
            Debug.Log("⚠️ Le joueur a déjà été instancié, on saute.");
            yield break;
        }

        GameObject playerObj = PhotonNetwork.Instantiate("NetworkPlayer", Vector3.zero, Quaternion.identity);
        Player playerScript = playerObj.GetComponent<Player>();
        //playerScript.gameManager = gameManager;
        playerScript.uiManager = uiManager;
        playerScript.name = PhotonNetwork.NickName;

        // Marque ce client comme instancié
        PhotonNetwork.LocalPlayer.TagObject = playerObj;

        gameManager.localPlayer = playerScript;


        PhotonView view = playerObj.GetComponent<PhotonView>();
        if (view != null)
        {
            photonView.RPC("RPC_RegisterPlayer", RpcTarget.AllBuffered, view.ViewID);
            view.RPC("SetPlayerDisplayName", RpcTarget.AllBuffered, PhotonNetwork.NickName);
            playerScript.SetPlayerDisplayName(PhotonNetwork.NickName);
            Debug.Log($"👤 Nouveau joueur instancié : {playerScript.name}, Owner: {view.Owner.ActorNumber}, IsMine: {view.IsMine}");
            photonView.RPC("RPC_PlayerReady", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            Debug.LogError("❌ PhotonView manquant sur le prefab PlayerNetwork !");
        }

        photonPlayers[PhotonNetwork.LocalPlayer.ActorNumber] = playerScript;
    }


    public void RegisterNetworkPlayer(GameObject playerObj)
{
    if (gameManager == null) return;

    Player playerScript = playerObj.GetComponent<Player>();
    if (playerScript == null) return;

    gameManager.players.Add(playerScript);

    // ✅ Récupération directe du PhotonView depuis l’objet
    PhotonView view = playerObj.GetComponent<PhotonView>();

    // ✅ TRI de la liste pour garantir un ordre identique sur tous les clients
    gameManager.players = gameManager.players
        .OrderBy(p =>
        {
            if (p is Player pl && pl.photonView != null && pl.photonView.Owner != null)
                return pl.photonView.Owner.ActorNumber;
            return int.MaxValue;
        }).ToList();

    foreach (var p in gameManager.players)
    {
        gameManager.RegisterPlayerInDictionaries(p);
    }


    Debug.Log($"✅ Player enregistré : {playerScript.name} / ActorNumber = {view?.OwnerActorNr}");
}




    private IEnumerator WaitAndRegister(Player player)
    {
        int attempts = 0;
        while (gameManager == null && attempts < 100)
        {
            attempts++;
            yield return new WaitForSeconds(0.1f);
        }

        if (gameManager != null)
        {
            RegisterNetworkPlayer(player.gameObject);
        }
        else
        {
            Debug.LogError("❌ gameManager toujours null après attente. Impossible d’enregistrer le joueur.");
        }
    }

    [PunRPC]
    public void RPC_RegisterPlayer(int viewID)
    {

        PhotonView view = PhotonView.Find(viewID);
       // Debug.Log($"ℹ️ view.CreatorActorNr = {view.CreatorActorNr}, view.Owner.ActorNumber = {view.Owner.ActorNumber}, local Actor = {PhotonNetwork.LocalPlayer.ActorNumber}");

        if (view == null)
        {
            Debug.LogError($"❌ Impossible de trouver le PhotonView avec l’ID {viewID} !");
            return;
        }

        GameObject playerObj = view.gameObject;
        Player playerScript = playerObj.GetComponent<Player>();

        if (playerScript == null)
        {
            Debug.LogError("❌ Le script Player est manquant sur l’objet instancié !");
            return;
        }

        // Évite d'ajouter plusieurs fois le même joueur
        foreach (var p in gameManager.players)
        {
            if (p is Player existing && existing.photonView != null &&
                existing.photonView.ViewID == playerScript.photonView.ViewID)
            {
                Debug.Log("🔁 Joueur déjà enregistré, on ignore.");
                return;
            }
        }

       // Debug.Log("📥 Enregistrement du joueur via RPC...");
        RegisterNetworkPlayer(playerScript.gameObject);
        if (playerScript.name == "")
        {
            playerScript.SetPlayerDisplayName(PhotonNetwork.NickName);
        }
        //Debug.Log($"✅ Joueur {playerScript.name} enregistré. Total : {gameManager.players.Count}");
    }



    [PunRPC]
    void RPC_PlayerReady(int actorNumber)
    {
        playersReady++;
        //Debug.Log($"📥 Joueur prêt : {actorNumber} ({playersReady}/{expectedPlayerCount})");
        foreach (Player player in gameManager.players)
        {
            gameManager.RegisterPlayerInDictionaries(player);
        }


        if (PhotonNetwork.IsMasterClient && playersReady >= expectedPlayerCount)
        {
           // Debug.Log("✅ Tous les joueurs sont prêts. Distribution des dominos...");
            if (!PhotonNetwork.IsMasterClient) return;
            // S'assure que le GameManager est bien prêt avant d'appeler InitializeGameMultiplayer
            StartCoroutine(WaitAndLaunchGame());
        }
    }

    private IEnumerator WaitAndLaunchGame()
    {
        int tries = 0;
        while ((gameManager == null || gameManager.players.Count != expectedPlayerCount) && tries < 100)
        {
            Debug.Log("Attente que le GameManager soit prêt avant de lancer la partie...");
            tries++;
            yield return new WaitForSeconds(0.5f);
        }

        if (gameManager != null && gameManager.players.Count == expectedPlayerCount)
        {
            Debug.Log("✅ Tous les joueurs sont prêts. Initialisation du jeu multijoueur.");
            gameManager.InitializeGameMultiplayer();
        }
        else
        {
            Debug.LogError(" GameManager ou joueurs toujours indisponibles après attente !");
        }
    }

    [PunRPC]
    public void RPC_ApplyDominoOrder(int[] order)
    {
        if (gameManager == null)
        {
            Debug.LogError(" GameManager est null dans RPC_ApplyDominoOrder");
            return;
        }

        List<Domino> newOrder = new List<Domino>();
        foreach (int index in order)
        {
            if (index >= 0 && index < gameManager.dominoObjects.Count)
            {
                newOrder.Add(gameManager.dominoObjects[index]);
            }
            else
            {
                Debug.LogWarning($"⚠️ Index hors limites dans la permutation de dominos : {index}");
            }
        }

        gameManager.dominoObjects = newOrder;
        //Debug.Log("🔀 Ordre des dominos appliqué avec succès !");
    }

    [PunRPC]
    public void RPC_SetPlayerHand(int[] indices, int actorNumber)
    {
        photonView.RPC("RPC_UpdateUI", RpcTarget.All);
        if (gameManager == null || gameManager.players == null)
        {
            Debug.LogError("❌ gameManager ou ses joueurs sont null !");
            return;
        }
       // Debug.Log($"📋 Vérification des joueurs pour RPC_SetPlayerHand : info.Sender.ActorNumber = {actorNumber}");
        foreach (var p in gameManager.players)
        {
            if (p is Player player && player.photonView != null && player.photonView.Owner.ActorNumber == actorNumber)
            {
                List<Domino> hand = new List<Domino>();
                string handLog = "";

                foreach (int index in indices)
                {
                    Domino domino = gameManager.dominoObjects[index];
                    domino.gameObject.SetActive(false);
                    hand.Add(domino);
                    handLog += $"[{domino.sides[0]}|{domino.sides[1]}], ";
                }

                player.SetHand(hand);
                if (player.photonView.IsMine)
                {
                    player.DisplayPlayerHand(); // ✅ Le joueur local affiche sa main
                    
                }

                PhotonView.Get(this).RPC("RPC_PlayerHandReceived", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
                Debug.Log($"🃏 {player.name} (Owner: {player.photonView.Owner.NickName}, ActorNumber: {player.photonView.Owner.ActorNumber}) a reçu {hand.Count} dominos : {handLog.TrimEnd(',', ' ')}");
                //photonView.RPC("RPC_UpdateUI", RpcTarget.All);
                // Debug.Log($"ℹ️ info.Sender.ActorNumber = {actorNumber}, info.Sender.NickName = {actorNumber.NickName}");
                return;
            }
        }
        
        Debug.LogWarning("⚠ Aucun joueur correspondant trouvé pour RPC_SetPlayerHand !");
    }

    [PunRPC]
    public void RPC_PlayerHandReceived(int actorNumber)
    {
        //Debug.Log("RPC_PlayerHandReceived appelé");
        if (!PhotonNetwork.IsMasterClient) return;

        if (!confirmedPlayers.Contains(actorNumber))
        {
            confirmedPlayers.Add(actorNumber);
           // Debug.Log($"✅ Confirmation main reçue par le joueur {actorNumber} ({confirmedPlayers.Count}/{PhotonNetwork.CurrentRoom.PlayerCount})");
        }

        if (PhotonNetwork.IsMasterClient && confirmedPlayers.Count == PhotonNetwork.CurrentRoom.PlayerCount && !gameStarted)
        {
            Debug.Log("✅ TOUS les joueurs ont confirmé leur main. Lancement du jeu...");
            photonView.RPC("RPC_StartAfterDistribution", RpcTarget.All);
        }
    }

    bool gameStarted = false;

    [PunRPC]
    public void RPC_StartAfterDistribution()
    {
        if (gameStarted) return;
        gameStarted = true;

        Debug.Log("🔁 [RPC_StartAfterDistribution] Initialisation du tour...");
        gameManager.ContinueAfterDistributionMultiplayer();
    }

    [PunRPC]
    public void RPC_SetCurrentPlayerIndex(int index)
    {
        if (gameManager == null) return;

        gameManager.currentPlayerIndex = index;
        IPlayable currentPlayer = gameManager.players[index];

        Debug.Log($"[SYNC] SetCurrentPlayerIndex reçu : {index}, localPlayer = {PhotonNetwork.LocalPlayer.ActorNumber}");
        // 🔁 Boucle sur tous les joueurs pour bloquer les interactions
        foreach (IPlayable p in gameManager.players)
        {
            if (p is Player otherPlayer)
            {
                otherPlayer.SetDominosInteractable(false);
            }
        }

        if (currentPlayer is Player player && player.photonView != null)
        {

            // ✅ CORRECTION : comparaison fiable via ActorNumber
            bool isLocalPlayer = player.photonView.Owner.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber;

            if (isLocalPlayer)
            {
                Debug.Log($"🟢 [LOCAL] C’est à moi de jouer ({player.name})");
                player.SetDominosInteractable(true);
                player.StartTurnTimer(15f);
                gameManager.uiManager?.DisplayPlayerTurn("C’est mon tour !");
                //Debug.Log($"🟢 [LOCAL] C’est à moi de jouer ({player.name})");
            }
            else
            {
                Debug.Log($"🔵 [REMOTE] Ce n’est pas à moi ({player.name})");
                gameManager.uiManager?.DisplayPlayerTurn($"C’est le tour de {player.playerDisplayName}");
            }
        }
    }




    public void SendDominoPlayByData(int sideA, int sideB, bool playRight)
    {
        photonView.RPC("RPC_PlaceDominoByData", RpcTarget.All, sideA, sideB, playRight);
    }

    [PunRPC]
    public void RPC_PlaceDominoByData(int sideA, int sideB, bool playRight)
    {
        if (gameManager == null)
        {
            Debug.LogError("GameManager is null in RPC_PlaceDominoByData");
            return;
        }

        // 1. Ajoute la data côté clients
        if (!gameManager.playedDominosData.Contains((sideA, sideB)) && !gameManager.playedDominosData.Contains((sideB, sideA)))
            gameManager.playedDominosData.Add((sideA, sideB));

        // 2. Trouve le domino correspondant
        Domino domino = gameManager.dominoObjects.FirstOrDefault(d =>
            (d.sides[0] == sideA && d.sides[1] == sideB) ||
            (d.sides[0] == sideB && d.sides[1] == sideA));

        if (domino == null)
        {
            Debug.LogError($"Domino [{sideA}|{sideB}] not found!");
            return;
        }

        // 3. Place le domino
        domino.gameObject.SetActive(true);
        domino.transform.SetParent(null);
        /*if (!gameManager.playedDominos.Contains(domino))
            gameManager.playedDominos.Add(domino);*/
       // Debug.Log($"[DEBUG MULTI] Domino [{sideA}|{sideB}] : ID={domino.GetInstanceID()}, transform.position = {domino.transform.position}, parent = {(domino.transform.parent != null ? domino.transform.parent.name : "null")}");
        gameManager.PlaceDomino(domino, playRight);
        //Debug.Log($"[RPC_PlaceDominoByData] playedDominos.Count = {gameManager.playedDominos.Count}");
        
        //Debug.Log($"✅ Domino [{sideA}|{sideB}] ajouté au plateau !");



        // 4. Supprime le domino de la main du joueur courant
        IPlayable current = gameManager.players[gameManager.currentPlayerIndex];
        current.RemoveDominoFromHand(domino);

        // 5. Update UI
        photonView.RPC("RPC_UpdateUI", RpcTarget.All);

        if (PhotonNetwork.IsMasterClient)
        {
            gameManager.NextTurn();
        }
    }

    public void RequestNextTurn()
    {
        photonView.RPC("RPC_NextTurn", RpcTarget.MasterClient);
    }


    [PunRPC]
    public void RPC_NextTurn()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            gameManager.NextTurn();
        }
    }

    [PunRPC]
    public void RPC_UpdateUI()
    {
        if (gameManager == null || gameManager.uiManager == null)
        {
            Debug.LogError("GameManager ou uiManager est null dans RPC_UpdateUI !");
            return;
        }

        uiManager.UpdateScoresDisplay(gameManager.players,
                                                           gameManager.playerScores,
                                                           gameManager.playerCochons,
                                                           gameManager.cochonsDonnés);

        uiManager.UpdateIADominoCounts(gameManager.players,
                                                            gameManager.localPlayer);
    }

    public void NotifyPlayerPassed(string playerName)
    {
        photonView.RPC("RPC_PlayerPassed", RpcTarget.Others, playerName);
    }

    [PunRPC]
    public void RPC_EventMessage(string message)
    {
        if (gameManager?.uiManager != null)
        {
            gameManager.uiManager.EventMessage(message);
        }
        else
        {
            Debug.LogWarning($"❌ [RPC_EventMessage] UIManager manquant. Message non affiché : {message}");
        }
    }

    [PunRPC]
    public void RPC_WinnerMessage(string message)
    {
        if (gameManager?.uiManager != null)
        {
            gameManager.uiManager.ShowWinnerMessage(message);
        }
        else
        {
            Debug.LogWarning($"❌ [RPC_WinnerMessage] UIManager manquant. Message non affiché : {message}");
        }
    }

    public void RequestRestartGame()
    {
        photonView.RPC("RPC_RestartGameMultiplayer", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_RestartGameMultiplayer()
    {
        gameStarted = false;
        confirmedPlayers.Clear();
        gameManager.ResetDominos(); // ✅ Réinitialise sur TOUS les clients AVANT de redistribuer
        if (PhotonNetwork.IsMasterClient)
        {
            gameManager.RestartGame(); // Distribue les mains ensuite
        }
    }

    [PunRPC]
    public void RPC_SyncAllStats(object[] data)
    {
        int index = 0;
        var newScores = new Dictionary<IPlayable, int>();
        var newCochons = new Dictionary<IPlayable, int>();
        var newCochonsDonnés = new Dictionary<IPlayable, Dictionary<IPlayable, int>>();

        foreach (var player in gameManager.players)
        {
            int score = (int)data[index++];
            int cochons = (int)data[index++];
            int innerCount = (int)data[index++];

            newScores[player] = score;
            newCochons[player] = cochons;

            var innerDict = new Dictionary<IPlayable, int>();
            for (int j = 0; j < innerCount; j++)
            {
                string keyName = (string)data[index++];
                int value = (int)data[index++];

                var target = gameManager.players.FirstOrDefault(p => p.name == keyName);
                if (target != null)
                {
                    innerDict[target] = value;
                }
            }
            newCochonsDonnés[player] = innerDict;
        }

        gameManager.playerScores = newScores;
        gameManager.playerCochons = newCochons;
        gameManager.cochonsDonnés = newCochonsDonnés;

        uiManager?.UpdateScoresDisplay(gameManager.players, gameManager.playerScores, gameManager.playerCochons, gameManager.cochonsDonnés);
    }





}
