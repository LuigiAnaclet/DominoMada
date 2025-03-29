// DQNAgent.cs - Ajout de logs et de vérifications supplémentaires
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using static GameManager;
using System.Runtime.ConstrainedExecution;
using System.Linq;

public class DQNAgent : Agent, IPlayable
{
    public List<Domino> hand = new List<Domino>();
    private float[] lastGameState;
    private float epsilon = 1.0f;
    private float epsilonMin = 0.01f;
    private float epsilonDecay = 0.995f;
    public GameManager gameManager;
    public bool isAI { get; set; } = true;
    public string name { get; set; }

    private bool isDecisionInProgress = false;
    public bool useTrainedModel = true;

    public override void OnEpisodeBegin()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        lastGameState = null;
        isDecisionInProgress = false;
        //Debug.Log($"[OnEpisodeBegin] Réinitialisation de l'agent {name}");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(hand.Count);

        foreach (var domino in hand)
        {
            sensor.AddObservation(domino.sides[0]);
            sensor.AddObservation(domino.sides[1]);
        }

        int missingDominoCount = 7 - hand.Count;
        for (int i = 0; i < missingDominoCount; i++)
        {
            sensor.AddObservation(0);
            sensor.AddObservation(0);
        }
    }

    /*public override void Heuristic(in ActionBuffers actionsOut)
    {
        //Debug.Log("[Heuristic] Appelée pour " + this.name);
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = 0;
    }*/

    // Calcul des probabilités des autres joueurs



    public void RequestAgentDecision()
    {
        if (!isDecisionInProgress)
        {
            isDecisionInProgress = true;
            //Debug.Log($"[RequestAgentDecision] Demande de décision pour {name}");
            RequestDecision();
        }
        else
        {
            Debug.LogWarning($"[RequestAgentDecision] Demande ignorée car une décision est déjà en cours pour {name}");
        }
    }

    public bool IsDecisionInProgress()
    {
        return isDecisionInProgress;
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        isDecisionInProgress = false;
        bool isFirstMove = gameManager.playedDominos.Count == 0 && gameManager.players[gameManager.currentPlayerIndex] == this;
        // Analyse de l'état du jeu avant de jouer
        var probabilities = AnalyzeGameState();

        int actionIndex = actionBuffers.DiscreteActions[0] % hand.Count;
        int attempts = 0; // Ajout pour éviter une boucle infinie

        //Debug.Log($"🎲 [OnActionReceived] Action reçue par {name}. isFirstMove = {isFirstMove}");

        //Debug.Log($"[OnActionReceived] Action reçue par {name} avec index {actionIndex}");

        if (isFirstMove)
        {
            // 🔥 L'IA est le premier joueur, elle peut poser n'importe quel domino
            Domino selectedDomino = hand[actionIndex];
            bool playRight = true; // Au premier coup playRight doit toujours être égal à true
            
            Debug.Log($"🔥 {name} commence avec { selectedDomino}!");
            gameManager.PlayDomino(actionIndex,this, playRight);
            AddReward(2.0f); // Récompense pour le premier coup joué
            return;
        }


        while (attempts < hand.Count)
        {
            if (actionIndex >= 0 && actionIndex < hand.Count)
            {
                Domino selectedDomino = hand[actionIndex];
                PlaySide playSide = gameManager.GetValidPlaySides(selectedDomino);

                if (playSide != PlaySide.None)
                {
                    bool playRight = playSide == PlaySide.Both ? ChoosePlaySide(selectedDomino, probabilities) : playSide == PlaySide.Right;
                    Debug.Log($"[OnActionReceived] Domino choisi et joué {selectedDomino}, côté droit : {playRight}");
                    gameManager.PlayDomino(actionIndex, this, playRight);

                    AddReward(2.0f); // Récompense pour un coup valide

                    // Appelle CheckBlockedPlayers pour vérifier les joueurs bloqués
                    int blockedPlayers = CheckBlockedPlayers();
                    if (blockedPlayers > 0)
                    {
                        AddReward(0.2f * blockedPlayers); // Récompense pour chaque joueur bloqué
                        //Debug.Log($"[OnActionReceived] {blockedPlayers} joueur(s) bloqué(s). Récompense ajoutée !");
                    }

                    return; // Quitte la méthode après un coup valide
                }
                else
                {
                    //Debug.LogWarning($"[OnActionReceived] Coup invalide pour le domino {selectedDomino}");
                    AddReward(-0.5f);
                }
            }
            else
            {
                //Debug.LogWarning($"[OnActionReceived] Action invalide : {actionIndex}. Main actuelle : {string.Join(", ", hand)}");
                AddReward(-0.5f);
            }

            // Essaye une autre action
            actionIndex = (actionIndex + 1) % hand.Count;
            attempts++;
        }

        // Si aucun coup valide n'est trouvé
        //Debug.LogWarning($"[OnActionReceived] Aucun coup valide trouvé après {attempts} tentatives.");
        AddReward(-1.0f);
        //gameManager.NextTurn();
    }

    public int CheckBlockedPlayers()
    {
        int blockedPlayersCount = 0;

        // Parcours des joueurs suivants dans l'ordre des tours
        for (int i = 1; i < gameManager.players.Count; i++)
        {
            // Calcule l'index du joueur suivant
            int nextPlayerIndex = (gameManager.currentPlayerIndex + i) % gameManager.players.Count;

            // Récupère le joueur suivant
            IPlayable nextPlayer = gameManager.players[nextPlayerIndex];

            // Vérifie si le joueur suivant est bloqué
            if (!gameManager.HasValidPlay(nextPlayer))
            {
                blockedPlayersCount++;
            }
        }

        //Debug.Log($"[CheckBlockedPlayers] {blockedPlayersCount} joueur(s) bloqué(s) après le tour de {name}.");
        return blockedPlayersCount;
    }


    private Dictionary<int, float> AnalyzeGameState()
    {
        var probabilities = EstimateOtherPlayersDominos();
        var playableValues = gameManager.GetPlayableValues();
        /*Debug.Log($"[AnalyzeGameState] Valeurs jouables sur la table : {string.Join(", ", playableValues)}");
        Debug.Log($"[AnalyzeGameState] Probabilités des dominos des autres joueurs : {string.Join(", ", probabilities.Select(p => $"{p.Key}:{p.Value:F2}"))}");
        Debug.Log($"[AnalyzeGameState] Main actuelle : {string.Join(", ", hand)}");*/

        return probabilities;
    }


    private Dictionary<int, float> EstimateOtherPlayersDominos()
    {
        var probabilities = new Dictionary<int, float>();
        var playedValues = gameManager.GetPlayableValues();

        foreach (var domino in gameManager.playedDominos.Concat(hand))
        {
            playedValues.Add(domino.sides[0]);
            playedValues.Add(domino.sides[1]);
        }

        foreach (int value in Enumerable.Range(0, 7))
        {
            if (!playedValues.Contains(value))
            {
                probabilities[value] = 1.0f / (7 - hand.Count);
            }
        }

        return probabilities;
    }



    private bool ChoosePlaySide(Domino domino, Dictionary<int, float> probabilities)
    {
        int leftValue = gameManager.playedDominos[0].sides[0];
        int rightValue = gameManager.playedDominos[^1].sides[1];

        float leftProbability = probabilities.ContainsKey(leftValue) ? probabilities[leftValue] : 0;
        float rightProbability = probabilities.ContainsKey(rightValue) ? probabilities[rightValue] : 0;

        //Debug.Log($"[ChoosePlaySide] Probabilités - Gauche : {leftProbability:F2}, Droite : {rightProbability:F2}");

        return rightProbability > leftProbability; // Joue du côté avec la probabilité la plus basse
    }

    public int Act(float[] gameState)
    {
        lastGameState = gameState;

        if (Random.value < epsilon)
        {
            return Random.Range(0, hand.Count);
        }

        RequestDecision();
        ActionBuffers actionBuffers = this.GetStoredActionBuffers();
        int actionIndex = actionBuffers.DiscreteActions[0] % hand.Count;

        epsilon = Mathf.Max(epsilonMin, epsilon * epsilonDecay);
        return actionIndex;
    }

    public void SetHand(List<Domino> newHand)
    {
        hand = newHand;
    }

    public List<Domino> GetHand()
    {
        return hand;
    }

    public void RemoveDominoFromHand(Domino domino)
    {
        hand.Remove(domino);
    }
}
