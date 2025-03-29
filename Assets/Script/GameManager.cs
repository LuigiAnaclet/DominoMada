using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Actuators;
using UnityEngine.XR;
using System;
using System.Collections;
using System.Linq;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;



public class GameManager : MonoBehaviour
{

    public List<IPlayable> players;
    public List<Domino> dominoObjects;
    public Transform board;
    public UIManager uiManager;
    //public Button playButton;
    public Transform playerHandPanel;
    public enum PlaySide { None, Left, Right, Both }

    public List<Domino> playedDominos = new List<Domino>();
    public int currentPlayerIndex = 0;
    private int passes = 0;
    public GameObject leftClickZone;  // Assigner dans l'Inspector
    public GameObject rightClickZone;
    public int selectedDominoIndex = -1;
    private Dictionary<IPlayable, int> playerScores = new Dictionary<IPlayable, int>();
    private Dictionary<IPlayable, int> playerCochons = new Dictionary<IPlayable, int>();
    private Dictionary<IPlayable, Dictionary<IPlayable, int>> cochonsDonnés = new Dictionary<IPlayable, Dictionary<IPlayable, int>>();
    private IPlayable lastWinner = null; // Stocke le dernier gagnant
    private List<IPlayable> lastBallePlayers = new List<IPlayable>(); // Stocke si il y a eu balle
    private int startingPlayerIndex = -1;
    private const int MaxHorizontalDominosPerSide = 8;
    private int leftDominoCount = 0;
    private int rightDominoCount = 0;
    private IPlayable localPlayer;




    private void Start()
    {

        //SetupAgents();
        /*int numPlayers = 0; // Par exemple, un joueur humain
        int numAI = 3;      // Et deux IA*/
        //InitializePlayers(numPlayers, numAI);

        if (uiManager == null)
        {
            uiManager = FindAnyObjectByType<UIManager>();
        }

        InitializePlayersFromScene();
        foreach (var player in players)
        {
            if (!playerScores.ContainsKey(player))
            {
                playerScores[player] = 0;
                playerCochons[player] = 0;
                cochonsDonnés[player] = new Dictionary<IPlayable, int>();
            }
        }

        InitializeGame();
        
        /*playButton.gameObject.SetActive(true);
        playButton.onClick.AddListener(StartGame);*/
    }

    private void StartGame()
    {
        //playButton.gameObject.SetActive(false);
        //uiManager.HideScores();
        //InitializeGame();
    }
    private void ConfigureBehavior(DQNAgent aiAgent)
    {
        var behaviorParams = aiAgent.GetComponent<BehaviorParameters>();
        if (behaviorParams != null)
        {
            behaviorParams.BehaviorType = BehaviorType.Default;
            //behaviorParams.BrainParameters.VectorObservationSize = 15; // Ajustez à votre nombre exact d'observations
            behaviorParams.BrainParameters.NumStackedVectorObservations = 1;
            behaviorParams.BrainParameters.ActionSpec = ActionSpec.MakeDiscrete(7); // Nombre d'actions disponibles
        }
    }

    private void SyncAgentHand(IPlayable player)
    {
        if (player is DQNAgent agent)
        {
            agent.SetHand(new List<Domino>(player.GetHand()));
        }
    }


    private void InitializePlayersFromScene()
    {
        players = new List<IPlayable>();

        // Trouve le joueur humain dans la scène
        Player humanPlayer = FindAnyObjectByType<Player>();
        if (humanPlayer != null)
        {
            humanPlayer.gameManager = this;
            players.Add(humanPlayer);
        }
        localPlayer = humanPlayer;

        // Utiliser FindObjectsByType à la place de FindObjectsOfType
        DQNAgent[] aiAgents = FindObjectsByType<DQNAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None); // Utilise un tri non trié pour des performances optimales
        int i = 1;
        foreach (DQNAgent agent in aiAgents)
        {
            agent.gameManager = this; // Associe le GameManager à chaque agent
            ConfigureBehavior(agent); // Configure les paramètres de comportement
            agent.SetHand(new List<Domino>());
            agent.name = $"IA_{i}";
            players.Add(agent);
            i++;
        }

        //Debug.Log($"Nombre d'agents IA trouvés : {aiAgents.Length}");
    }

    public void InitializeGame()
    {
        leftDominoCount = 0;
        rightDominoCount = 0;
        Debug.Log("🆕 [InitializeGame] Nouvelle partie en cours...");
        DisplayScoresAndCochons();
        DisplayCochonsHistory();
        if (uiManager != null)
        {
            uiManager.UpdateScoresDisplay(players, playerScores, playerCochons, cochonsDonnés);
        }
        dominoObjects.Shuffle();
        int startIndex = 0;
        int dominosPerPlayer = 7;
        foreach (IPlayable player in players)
        {
            if (player != null)
            {
                InitializePlayerHand(player, startIndex, dominosPerPlayer);
                SyncAgentHand(player);
                startIndex += dominosPerPlayer;

                // Met à jour l'affichage pour le joueur humain
                if (player is Player humanPlayer)
                {
                    humanPlayer.DisplayPlayerHand();
                    //Debug.Log("Au tour du joueur humain.");
                    humanPlayer.SetDominosInteractable(false);
                }
            }
        }
        GetStartingPlayerIndex();
        currentPlayerIndex = startingPlayerIndex;
        IPlayable currentPlayer = players[currentPlayerIndex];
        //Debug.Log($"🎯 [InitializeGame] Le premier joueur est {currentPlayer.name}");
        //Debug.Log($"📌 Nombre total de joueurs : {players.Count}, currentPlayerIndex = {currentPlayerIndex}");

        // Si le dernier gagnant commence sans contrainte, ne rien faire ici
        if (lastWinner != null && players.Contains(lastWinner))
        {
            currentPlayer = players[currentPlayerIndex];
            Debug.Log("🎯 Tour libre pour le dernier gagnant, pas de domino imposé.");

            SyncAgentHand(currentPlayer);

            if (currentPlayer is DQNAgent aiAgent)
            {
                StartCoroutine(WaitAndRequestDecision(aiAgent));
            }
            else if (currentPlayer is Player player)
            {
                player.SetDominosInteractable(true);
                player.StartTurnTimer(15f);
            }

            return; // 🔁 Surtout ne pas appeler NextTurn après !
        }
        else
        {
            // Sinon, on applique la logique classique (plus grand double)
            if (playedDominos.Count == 0)
            {
                currentPlayerIndex = (currentPlayerIndex - 1 + players.Count) % players.Count;
                currentPlayer = players[currentPlayerIndex];
            }
            else
            {
                currentPlayer = players[currentPlayerIndex];
            }
        }

        SyncAgentHand(currentPlayer);
        NextTurn();
    }


    private void InitializePlayerHand(IPlayable playable, int startIndex, int dominosPerPlayer)
    {
        List<Domino> hand = new List<Domino>(); // Crée une nouvelle liste pour chaque joueur

        for (int i = startIndex; i < startIndex + dominosPerPlayer; i++)
        {
            Domino domino = dominoObjects[i];// Instancie une copie unique
            domino.gameObject.SetActive(false);
            hand.Add(domino);
        }

        playable.SetHand(hand); // Utilise une méthode SetHand pour définir la main
        //Debug.Log($"Main initialisée pour {playable.name}: {string.Join(", ", hand)}");
    }

    private void GetStartingPlayerIndex()
    {
        if (players.Count == 0)
        {
            Debug.LogError("❌ [GetStartingPlayerIndex] Aucun joueur trouvé !");
            //return 0;
        }
        //int startingPlayerIndex = -1;
        if (lastWinner != null && players.Contains(lastWinner))
        {
            startingPlayerIndex = players.IndexOf(lastWinner);
            Debug.Log($"Le dernier gagnant {lastWinner.name} commence sans contrainte.");
            //return startingPlayerIndex; // 🔥 Ce joueur commence avec le domino qu'il veut
        }

        else if (lastBallePlayers.Count > 1)
        {
            Debug.Log($"balle la partie d'avant");
            PlayHighestDouble(lastBallePlayers);
        }
        else
        {
            PlayHighestDouble(players);
        }
         
        //return startingPlayerIndex;
    }
    private void PlayHighestDouble(List<IPlayable> candidates)
    {
        Domino highestDouble = null;
        //int startingPlayerIndex = -1;

        // 🔍 Recherche du plus grand double parmi les joueurs donnés
        for (int i = 0; i < candidates.Count; i++)
        {
            IPlayable playable = candidates[i];
            foreach (var domino in playable.GetHand())
            {
                if (domino.sides[0] == domino.sides[1]) // C'est un double
                {
                    if (highestDouble == null || domino.sides[0] > highestDouble.sides[0])
                    {
                        highestDouble = domino;
                        startingPlayerIndex = players.IndexOf(playable);
                    }
                }
            }
        }

        if (highestDouble != null && startingPlayerIndex != -1)
        {
            IPlayable startingPlayer = players[startingPlayerIndex];

            Debug.Log($"🔥 {startingPlayer.name} commence avec [{highestDouble.sides[0]}|{highestDouble.sides[1]}] !");

            // 🛠 Retirer le domino de la main du joueur
            startingPlayer.RemoveDominoFromHand(highestDouble);
            SyncAgentHand(startingPlayer);

            // 📌 Activer et placer le domino sur le plateau
            highestDouble.gameObject.SetActive(true);
            PlaceDomino(highestDouble, true);

            // ✅ Mettre à jour le premier joueur actuel
            currentPlayerIndex = startingPlayerIndex;

            return;
            //return startingPlayerIndex;
        }
        // 🚨 Aucun double trouvé → Rebrassage et retour `-1`
        Debug.LogWarning("⚠ Aucun double trouvé. Les dominos sont rebattus et la partie recommence.");
        RestartGame();
        //return 0; // Indicateur que la partie a été relancée
    }

    public void NextTurn()
    {
        uiManager.UpdateIADominoCounts(players, localPlayer);
        if (CheckIfGameEnded())
        {
            Debug.Log("La partie est terminée !");
            RestartGame();
            return;
        }
        if (players[currentPlayerIndex] is Player previousPlayer)
        {
            previousPlayer.SetDominosInteractable(false);

        }
        //Debug.Log($"Nombre de dominos restants : {GetTotalDominosRemaining()}");

        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        IPlayable currentPlayer = players[currentPlayerIndex];

        Debug.Log($"C'est au tour du joueur {currentPlayerIndex + 1} : {currentPlayer.name}");
        

        if (currentPlayer != null)
        {
            SyncAgentHand(currentPlayer);
            if (HasValidPlay(players[currentPlayerIndex]))
            {
                uiManager.DisplayPlayerTurn(currentPlayer.name+" joue ");
                if (currentPlayer is DQNAgent aiAgent)
                {
                    StartCoroutine(WaitAndRequestDecision(aiAgent));
                }
                if (currentPlayer is Player player)
                {
                    player.SetDominosInteractable(true);
                    player.StartTurnTimer(15f); // ⏱ démarrer le compte à rebours
                    //PositionClickZonesAtEnds();
                }
                passes = 0;
            }
            else
            {
                Debug.Log($"Le joueur {currentPlayerIndex + 1} est boudé...");
                if (currentPlayer is DQNAgent aiAgent)
                {
                    aiAgent.AddReward(-2.0f);
                }
                passes++;
                HideClickZones();
                if (uiManager != null)
                {
                    uiManager.DisplayPlayerTurn("");
                    uiManager.EventMessage($"{currentPlayer.name} est boudé...");
                }

                StartCoroutine(DelayBeforeNextTurn(3f));
            }
        }
    }

    private IEnumerator DelayBeforeNextTurn(float delay)
    {
        yield return new WaitForSeconds(delay);
        NextTurn();
    }



    private IEnumerator WaitAndRequestDecision(DQNAgent aiAgent)
    {
        //Debug.Log($"⏳ Attente avant décision de {aiAgent.name}...");
        yield return new WaitForSeconds(2f); // Pause avant que l'IA joue

        if (!this.enabled) // Vérifie si GameManager est désactivé
        {
            Debug.LogError("❌ [WaitAndRequestDecision] Annulée car GameManager a été désactivé !");
            yield break;
        }

        //Debug.Log($"🚀 Demande de décision pour {aiAgent.name} !");
        aiAgent.RequestAgentDecision();
    }

    public bool HasValidPlay(IPlayable player)
    {
        if (playedDominos.Count == 0) {
            return true;
        }

        foreach (var domino in player.GetHand())
        {
            PlaySide validSides = GetValidPlaySides(domino);
            if (validSides != PlaySide.None)
            {
                return true;
            }
        }
        return false;
    }

public bool IsValidPlay(Domino domino)
    {

        if (domino == null)
        {
            Debug.LogError("Domino est null dans IsValidPlay.");
            return false;
        }
        int leftEndValue = GetLeftEndValue();
        int rightEndValue = GetRightEndValue();

        if (playedDominos.Count == 0)
        {
            Debug.Log("1er domino joué.");
            return true;
        }

            // Vérifie si l'une des valeurs du domino correspond aux extrémités du plateau
            return (domino.sides[0] == leftEndValue || domino.sides[1] == leftEndValue ||
                domino.sides[0] == rightEndValue || domino.sides[1] == rightEndValue);
    }


    public PlaySide GetValidPlaySides(Domino domino)
    {
        int leftEndValue = GetLeftEndValue();
        int rightEndValue = GetRightEndValue();

        bool canPlayLeft = domino.sides[0] == leftEndValue || domino.sides[1] == leftEndValue;
        bool canPlayRight = domino.sides[0] == rightEndValue || domino.sides[1] == rightEndValue;

        if (canPlayLeft && canPlayRight)
        {
            return PlaySide.Both;
        }
        else if (canPlayLeft)
        {
            return PlaySide.Left;
        }
        else if (canPlayRight)
        {
            return PlaySide.Right;
        }

        return PlaySide.None;
    }
    public void PlayDomino(int action, IPlayable player, bool playRight)
    {
        List<Domino> hand = player.GetHand();

        if (action < 0 || action >= hand.Count)
        {
            Debug.LogError("Action invalide : Index hors limites.");
            return;
        }

        Domino domino = hand[action];
        if (domino == null)
        {
            Debug.LogError("Le domino sélectionné est null.");
            return;
        }

        // Place le domino sur le plateau
        domino.transform.SetParent(board, true);
        domino.gameObject.SetActive(true);
        
        PlaceDomino(domino, playRight);

        // Retire le domino de la main
        player.RemoveDominoFromHand(domino);
        if (player is Player humanPlayer)
        {
            humanPlayer.hasPlayed = true;
            if (humanPlayer.playTimerCoroutine != null)
                humanPlayer.StopCoroutine(humanPlayer.playTimerCoroutine);
        }
        NextTurn();
    }


    public List<int> GetPlayableValues()
    {
        if (playedDominos.Count == 0)
        {
            return Enumerable.Range(0, 7).ToList();
        }

        int leftValue = playedDominos[0].sides[0];
        int rightValue = playedDominos[^1].sides[1];

        return new List<int> { leftValue, rightValue };
    }


    private float[] GetGameStateForAI(int playerIndex)
    {
        return new float[] { }; // Populate based on your game state
    }

    private int GetLeftEndValue() => playedDominos.Count > 0 ? playedDominos[0].sides[0] : -1;
    private int GetRightEndValue() => playedDominos.Count > 0 ? playedDominos[playedDominos.Count - 1].sides[1] : -1;

    public void PositionClickZonesAtEnds()
    {
        // Cache les zones de clic au début
        HideClickZones();

        // Si le plateau est vide, rien à faire
        if (playedDominos.Count == 0)
            return;

        // Récupère le premier et le dernier domino joués
        GameObject firstDomino = playedDominos[0].gameObject;
        GameObject lastDomino = playedDominos[playedDominos.Count - 1].gameObject;

        // Positionne la zone gauche à l'extrémité gauche du premier domino sur l'axe X
        Vector3 leftPosition = firstDomino.transform.position;
        if (leftDominoCount >= MaxHorizontalDominosPerSide)
        {
            leftPosition.z -= 1.9f; // zone au-dessus
        }
        else
        {
            leftPosition.x -= 1.9f; // zone à droite
        }
        leftClickZone.transform.position = leftPosition;
        leftClickZone.SetActive(true);

        // Positionne la zone droite à l'extrémité droite du dernier domino sur l'axe X
        Vector3 rightPosition = lastDomino.transform.position;
        // Si on est passé en mode vertical (après X dominos horizontaux)
        if (rightDominoCount >= MaxHorizontalDominosPerSide)
        {
            rightPosition.z += 1.9f; // zone au-dessus
        }
        else
        {
            rightPosition.x += 1.9f; // zone à droite
        }
        rightClickZone.transform.position = rightPosition;
        rightClickZone.SetActive(true);
    }

    public void OnLeftZoneClicked()
    {
        if (selectedDominoIndex != -1 && players[currentPlayerIndex] is Player player)
        {
            PlayDomino(selectedDominoIndex, player, false); // Jouer à gauche
            selectedDominoIndex = -1; // Réinitialise l'index sélectionné
            uiManager.eventMessage.gameObject.SetActive(false);
            HideClickZones();
        }
    }

    public void OnRightZoneClicked()
    {
        if (selectedDominoIndex != -1 && players[currentPlayerIndex] is Player player)
        {
            PlayDomino(selectedDominoIndex, player, true); // Jouer à droite
            selectedDominoIndex = -1; // Réinitialise l'index sélectionné
            uiManager.eventMessage.gameObject.SetActive(false);
            HideClickZones();
        }
    }

    public void CancelSelection()
    {
        selectedDominoIndex = -1;
        HideClickZones();
        Debug.Log("Sélection annulée.");
    }

    // Affiche ou masque les zones de clic
    public void ShowClickZones(bool show)
    {
        leftClickZone.SetActive(show);
        rightClickZone.SetActive(show);
    }

    public void HideClickZones()
    {
        ShowClickZones(false);
    }


    public void PlaceDomino(Domino domino, bool playRight)
    {
        domino.transform.SetParent(null);
        Vector3 position = new Vector3(0, 0.55f, 0); ;
        Quaternion rotation = Quaternion.Euler(0, 0, 90); // Couché sur le dos

        domino.transform.localScale = new Vector3(0.3f, 1.5f, 0.75f);


        if (playedDominos.Count == 0)
        {
            if (domino.sides[0] == domino.sides[1])
            {
                rotation = Quaternion.Euler(0, 90, 90);
            }
            else
            {
                // Premier domino à être placé
                rotation = Quaternion.Euler(0, 0, 90);
            }
            
        }
        else
        {
            if (playRight)
            {
                Domino lastDomino = playedDominos[playedDominos.Count - 1];
                if (rightDominoCount >= MaxHorizontalDominosPerSide)
                {
                    if (domino.sides[0] == domino.sides[1])
                    {
                        position = (rightDominoCount == MaxHorizontalDominosPerSide)
                        ? lastDomino.transform.position + new Vector3(1.15f, 0, 0)
                        : lastDomino.transform.position + new Vector3(0, 0, 1.15f);
                        rotation = (rightDominoCount == MaxHorizontalDominosPerSide)
                        ? Quaternion.Euler(0, 90, 90)
                        : rotation;
                    }
                    else
                    {
                        if (rightDominoCount == MaxHorizontalDominosPerSide)
                        {
                            if (lastDomino.sides[0] == lastDomino.sides[1])
                            {
                                position = lastDomino.transform.position + new Vector3(0, 0, 1.6f);
                            }
                            else
                            {
                                position = lastDomino.transform.position + new Vector3(0.35f, 0, 1.15f);
                            }
                            rotation = Quaternion.Euler(0, -90, 90);
                            
                        }
                        else if (rightDominoCount > MaxHorizontalDominosPerSide)
                        {
                            if (lastDomino.sides[0] == lastDomino.sides[1])
                            {
                                position = lastDomino.transform.position + new Vector3(0, 0, 1.15f);
                            }
                            else
                            {
                                position = lastDomino.transform.position + new Vector3(0, 0, 1.6f);
                            }
                            rotation = Quaternion.Euler(0, -90, 90);
                        }
                    }

                    if (domino.sides[0] != GetRightEndValue())
                    {
                        rotation = Quaternion.Euler(0, 90, 90);
                        domino.Reverse(); // Inverser le domino si nécessaire pour correspondre au chiffre à droite
                    }
                }
               else
                {
                    if (lastDomino.sides[0] == lastDomino.sides[1])
                    {
                        position = lastDomino.transform.position + new Vector3(1.15f, 0, 0);
                    }
                    else
                    {
                        position = lastDomino.transform.position + new Vector3(1.6f, 0, 0);
                    }

                    if (domino.sides[0] == domino.sides[1])
                    {
                        position = lastDomino.transform.position + new Vector3(1.15f, 0, 0);
                        rotation = Quaternion.Euler(0, 90, 90);
                    }
                    if (domino.sides[0] != GetRightEndValue())
                    {
                        rotation = Quaternion.Euler(0, 180, 90);
                        domino.Reverse(); // Inverser le domino si nécessaire pour correspondre au chiffre à droite
                    }
                }
                rightDominoCount++;

            }
            else
            {
                // Placer le domino à gauche
                Domino firstDomino = playedDominos[0];
                if (leftDominoCount >= MaxHorizontalDominosPerSide)
                {
                    if (domino.sides[0] == domino.sides[1])
                    {
                        position = (leftDominoCount == MaxHorizontalDominosPerSide)
                        ? firstDomino.transform.position + new Vector3(-1.15f, 0, 0)
                        : firstDomino.transform.position + new Vector3(0, 0, -1.15f);
                        rotation = (leftDominoCount == MaxHorizontalDominosPerSide)
                        ? Quaternion.Euler(0, 90, 90)
                        : rotation;
                    }
                    else
                    {
                        if (leftDominoCount == MaxHorizontalDominosPerSide)
                        {
                            if (firstDomino.sides[0] == firstDomino.sides[1])
                            {
                                Debug.Log("double");
                                position = firstDomino.transform.position + new Vector3(0, 0, -1.6f);
                            }
                            else
                            {
                                position = firstDomino.transform.position + new Vector3(-0.35f, 0, -1.15f);
                            }
                            rotation = Quaternion.Euler(0, -90, 90);

                        }
                        else if (leftDominoCount > MaxHorizontalDominosPerSide)
                        {
                            if (firstDomino.sides[0] == firstDomino.sides[1])
                            {
                                position = firstDomino.transform.position + new Vector3(0, 0, -1.15f);
                            }
                            else
                            {
                                position = firstDomino.transform.position + new Vector3(0, 0, -1.6f);
                            }
                            rotation = Quaternion.Euler(0, -90, 90);
                        }
                    }

                    if (domino.sides[1] != GetLeftEndValue())
                    {
                        //Debug.Log("Reverse");
                        rotation = Quaternion.Euler(0, 90, 90);
                        domino.Reverse(); // Inverser le domino si nécessaire pour correspondre au chiffre à droite
                    }
                }
                else
                {
                    if (firstDomino.sides[0] == firstDomino.sides[1])
                    {
                        position = firstDomino.transform.position + new Vector3(-1.15f, 0, 0);
                    }
                    else
                    {
                        position = firstDomino.transform.position + new Vector3(-1.6f, 0, 0);
                    }

                    //rotation = Quaternion.Euler(0, 0, 90);
                    if (domino.sides[0] == domino.sides[1])
                    {
                        position = firstDomino.transform.position + new Vector3(-1.15f, 0, 0);
                        rotation = Quaternion.Euler(0, 90, 90);
                    }
                    if (domino.sides[1] != GetLeftEndValue())
                    {
                        rotation = Quaternion.Euler(0, 180, 90);
                        domino.Reverse(); // Inverser le domino si nécessaire pour correspondre au chiffre à gauche
                    }
                }

                // Ajouter le domino au début de la liste
                playedDominos.Insert(0, domino);
                leftDominoCount++;
            }
        }

        // Définir la position et la rotation du domino
        domino.transform.position = position;
        domino.transform.rotation = rotation;

        // Ajouter le domino à la liste des dominos joués si placé à droite
        if (playRight)
        {
            playedDominos.Add(domino);
        }
        Debug.Log($"Dominos sur la table : {string.Join(", ", playedDominos)}");
        PositionClickZonesAtEnds();
        //Debug.Log($"Domino placé : [{domino.sides[0]}|{domino.sides[1]}] à la position {position}");
    }

    public bool CheckIfGameEnded()
    {
        // 🚨 Vérifie si la partie est en cours de redémarrage
        if (playedDominos.Count == 0)
        {
            Debug.LogWarning("⚠ [CheckForWinner] Partie en train de redémarrer, annulation de la vérification du gagnant.");
            return false;
        }

        foreach (IPlayable player in players)
        {
            if (player != null && player.GetHand().Count == 0)
            {
                if (uiManager != null)
                {
                    uiManager.ShowWinnerMessage($"Le joueur {player.name} a gagné !");
                }
                Debug.Log($"Le joueur {player.name} a gagné !");
                if (player is DQNAgent aiAgent)
                {
                    aiAgent.AddReward(10.0f);
                    aiAgent.EndEpisode();
                }
                OnPlayerWin(player);
                return true;
            }
        }

        if (passes >= players.Count)
        {
            Debug.Log("Tous les joueurs ont passé leur tour. La partie est terminée.");
            CheckForWinner();
            return true;
        }

        return false;
    }


    private void CheckForWinner()
    {
        

        Dictionary<IPlayable, int> playerScores = new Dictionary<IPlayable, int>();

        foreach (var player in players)
        {
            int totalPoints = 0;
            foreach (var domino in player.GetHand())
            {
                totalPoints += domino.sides[0] + domino.sides[1];
            }

            playerScores[player] = totalPoints;
            Debug.Log($"Score du joueur {player.name} : {totalPoints} points");
        }

        int minScore = playerScores.Values.Min();
        List<IPlayable> potentialWinners = playerScores.Where(p => p.Value == minScore).Select(p => p.Key).ToList();
        // ⚠ Cas d'égalité ("balle")
        if (potentialWinners.Count > 1)
        {
            if (uiManager != null)
            {
                uiManager.ShowWinnerMessage($"Égalité ! Aucun gagnant, balle entre {string.Join(", ", potentialWinners.Select(p => p.name))}");
            }
            Debug.Log($"⚠ Égalité ! Aucun gagnant, balle entre {string.Join(", ", potentialWinners.Select(p => p.name))}");

            // 🔥 Stocke les joueurs en balle pour la prochaine partie
            lastBallePlayers = new List<IPlayable>(potentialWinners);
            lastWinner = null; // Pas de gagnant officiel

            return;
        }

        // ✅ Un seul gagnant
        IPlayable winner = potentialWinners[0];
        if (uiManager != null)
        {
            uiManager.ShowWinnerMessage($"Le gagnant est {winner.name} avec {minScore} points !");
        }
        Debug.Log($"🏆 Le gagnant est {winner.name} avec {minScore} points !");
        OnPlayerWin(winner);

        // 🔥 Réinitialisation de la balle
        lastBallePlayers.Clear();
        lastWinner = winner;

        //OnPlayerWin(winner);

        // Ajouter un reward si le gagnant est une IA
        if (winner is DQNAgent aiAgent)
        {
            //Debug.Log($"L'IA {aiAgent.name} reçoit un reward pour avoir gagné !");
            aiAgent.AddReward(10.0f); // Reward positif pour avoir gagné
            aiAgent.EndEpisode();    // Termine l'épisode pour l'agent gagnant
        }
    }

    private void RestartGame()
    {
        //Debug.Log("Redémarrage du jeu...");
        Debug.Log($"🔄 [RestartGame] Redémarrage... Nombre de joueurs avant reset : {players.Count}");


        // Réinitialiser les dominos sur le plateau
        foreach (Domino domino in playedDominos)
        {
            domino.ResetDomino();
        }
        playedDominos.Clear();

        // Réinitialiser les mains des joueurs
        foreach (IPlayable player in players)
        {
            List<Domino> hand = player.GetHand();
            foreach (Domino domino in hand)
            {
                domino.ResetDomino();
            }
            hand.Clear();
        }

        // Réinitialiser l'index du joueur courant et les passes
        currentPlayerIndex = 0;
        passes = 0;

        // Masquer les zones cliquables
        HideClickZones();

        Debug.Log($"🔄 [RestartGame] Nombre de joueurs après reset : {players.Count}");

        // 🔥 Ajoute un léger délai pour éviter des conflits d'initialisation
        StartCoroutine(DelayedInitializeGame());
    }

    private IEnumerator DelayedInitializeGame()
    {
        yield return new WaitForSeconds(5f); // 🔥 Petit délai pour afficher qui à gagné
        InitializeGame();
    }

    // Appelée à la fin de chaque partie pour enregistrer les victoires
    public void OnPlayerWin(IPlayable winner)
    {

        playerScores[winner]++;
        Debug.Log($"[OnPlayerWin] {winner.name} a gagné une partie.");

        // Vérifie si tous les joueurs ont au moins 1 point → Partie chiré
        bool allPlayersHavePoints = true;
        foreach (var player in players)
        {
            Debug.Log($"[OnPlayerWin] {player.name} à : {playerScores[player]} points");
            if (playerScores[player] == 0)
            {
                allPlayersHavePoints = false;
                lastWinner = winner;  // Peu importe IA ou Humain
                Debug.Log($"✅ lastWinner est maintenant {winner.name}");
                if (player is DQNAgent aiAgent)
                {
                    aiAgent.AddReward(-5.0f);
                }
                break;
            }
        }

        if (allPlayersHavePoints)
        {
            Debug.Log("[OnPlayerWin] Partie chiré ! Tous les joueurs ont au moins 1 point. Remise des scores à zéro.");
            foreach (var player in players)
            {
                playerScores[player] = 0;
            }
            lastWinner = null;
            
        }
        else
        {
            // Si un joueur atteint 3 points et que la partie n'est pas chiré
            if (playerScores[winner] == 3)
            {
                foreach (var player in players)
                {
                    if (player is DQNAgent aiAgent)
                    {
                        if (player == winner)
                        {
                            aiAgent.AddReward(20.0f);
                            //aiAgent.EndEpisode();
                        }
                        
                    }
                    if (player != winner && playerScores[player] == 0)
                    {
                        playerCochons[player]++;
                        Debug.Log($"[OnPlayerWin] {player.name} a pris un cochon de {winner.name} ! Total cochons : {playerCochons[player]}");

                        if (player is DQNAgent aiAgent2)
                        {
                            aiAgent2.AddReward(-20.0f);
                            aiAgent2.EndEpisode();
                        }

                        // Mise à jour de l'historique des cochons donnés
                        if (!cochonsDonnés[winner].ContainsKey(player))
                        {
                            cochonsDonnés[winner][player] = 0;
                        }
                        cochonsDonnés[winner][player]++;
                    }
                    lastWinner = null;
                }

                Debug.Log("[OnPlayerWin] Remise des scores à zéro après attribution des cochons.");
                foreach (var player in players)
                {
                    playerScores[player] = 0;
                }

                //RestartGame();
            }
        }
        //RestartGame();
    }

    public void DisplayScoresAndCochons()
    {
        Debug.Log("📊 Statistiques des joueurs :");

        foreach (var player in players)
        {
            int points = playerScores.ContainsKey(player) ? playerScores[player] : 0;
            int cochonsPris = playerCochons.ContainsKey(player) ? playerCochons[player] : 0;

            // Calcul des cochons donnés par ce joueur
            int cochonsDonnésTotal = 0;
            if (cochonsDonnés.ContainsKey(player))
            {
                foreach (var receveur in cochonsDonnés[player])
                {
                    cochonsDonnésTotal += receveur.Value;
                }
            }

            Debug.Log($"- {player.name} : {points} points | {cochonsPris} cochons pris | {cochonsDonnésTotal} cochons donnés");
        }
    }

    public void DisplayCochonsHistory()
    {
        Debug.Log("\n📌 **Historique des Cochons donnés** 📌");
        foreach (var donneur in cochonsDonnés)
        {
            foreach (var receveur in donneur.Value)
            {
                Debug.Log($"{receveur.Key.name} a pris {receveur.Value} cochons de {donneur.Key.name}");
            }
        }
    }

}
