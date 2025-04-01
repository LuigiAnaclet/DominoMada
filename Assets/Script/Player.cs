using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Unity.MLAgents.Actuators;
using UnityEngine;
using UnityEngine.UI;
using static GameManager;

public class Player : MonoBehaviourPun, IPlayable
{
    public string name { get; set; } = "Humain";
    public GameManager gameManager;
    public List<Domino> hand = new List<Domino>();
    public bool isAI { get; set; } = false;
    public Camera targetCamera; // La caméra de référence
    public float distance = 2.0f; // Distance entre l'objet et la caméra
    public Coroutine playTimerCoroutine;
    public bool hasPlayed = false;
    public UIManager uiManager;

    //public Transform playerHandPanel;     // Panel où les dominos seront affichés

    // Implémentation de GetHand
    public List<Domino> GetHand()
    {
        DisplayPlayerHand();
        return hand;
    }

    public void SetHand(List<Domino> newHand)
    {
        hand = newHand;
        DisplayPlayerHand(); // Appel pour afficher la main après l'assignation
    }

    // Implémentation de RemoveDominoFromHand
    public void RemoveDominoFromHand(Domino domino)
    {
        hand.Remove(domino);
        DisplayPlayerHand(); // Met à jour l'affichage après avoir joué un domino
    }

    //Le joueur a 15 sec pour jouer
    public void StartTurnTimer(float duration)
    {
        hasPlayed = false;

        if (playTimerCoroutine != null)
            StopCoroutine(playTimerCoroutine);

        playTimerCoroutine = StartCoroutine(PlayTimer(duration));
    }

    public IEnumerator PlayTimer(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (!hasPlayed)
        {
            Debug.Log("⏱ Temps écoulé ! Un domino va être joué automatiquement.");

            List<Domino> validDominos = new List<Domino>();
            foreach (var d in hand)
            {
                if (gameManager.GetValidPlaySides(d) != GameManager.PlaySide.None)
                    validDominos.Add(d);
            }

            if (validDominos.Count > 0)
            {
                Domino randomDomino = validDominos[Random.Range(0, validDominos.Count)];
                int index = hand.IndexOf(randomDomino);
                GameManager.PlaySide side = gameManager.GetValidPlaySides(randomDomino);

                if (side == GameManager.PlaySide.Both)
                {
                    gameManager.PlayDomino(index, this, Random.value > 0.5f);
                }
                else
                {
                    gameManager.PlayDomino(index, this, side == GameManager.PlaySide.Right);
                }
            }
            else
            {
                Debug.Log("Aucun domino jouable, le joueur passe son tour.");
                gameManager.NextTurn();
            }
        }
    }


    // Affiche les dominos 
    public void DisplayPlayerHand()
    {
        if (Photon.Pun.PhotonNetwork.IsConnected && !GetComponent<PhotonView>().IsMine)
            return;
        float spacing = 0.3f; // Espacement horizontal entre les dominos
        float yOffset = -0.8f; // Décalage vertical pour placer les dominos plus bas

        int count = hand.Count;


        for (int i = 0; i < hand.Count; i++)
        {
            // Instancie le domino 3D
            GameObject dominoInstance = hand[i].gameObject;

            dominoInstance.gameObject.SetActive(true);

            // Calcul de la position pour centrer les dominos même quand le nombre diminue
            float xPos = (i - (count - 1) / 2.0f) * spacing;

            // Positionne le domino devant la caméra avec le décalage vertical
            Vector3 forwardDirection = targetCamera.transform.forward;
            Vector3 position = targetCamera.transform.position + forwardDirection * distance;
            position += targetCamera.transform.right * xPos; // Décalage horizontal proportionnel à i
            position += targetCamera.transform.up * yOffset; // Décalage vertical

            dominoInstance.transform.position = position;

            // Ajuste l'échelle du domino pour le rendre plus grand
            dominoInstance.transform.localScale = new Vector3(0.05f, 0.25f, 0.125f);

            // Ajuste la rotation du domino pour qu'il soit correctement orienté
            dominoInstance.transform.localRotation = Quaternion.Euler(0f, 90f, targetCamera.transform.rotation.eulerAngles.x); // Rotation de 90° sur l'axe Z


            // Ajoute le script ClickableDomino si ce n'est pas déjà fait
            if (dominoInstance.GetComponent<ClickableDomino>() == null)
            {
                dominoInstance.AddComponent<ClickableDomino>();
            }

            //Debug.Log("Domino ajouté dans la main du joueur avec ajustement de taille et rotation.");
        }
    }

    public void SetDominosInteractable(bool interactable)
    {
        foreach (Domino domino in hand)
        {
            if (domino != null)
            {
                // Accède au GameObject associé au Domino
                GameObject dominoObject = domino.gameObject;

                // Désactive ou active le composant Collider pour empêcher ou permettre le clic
                Collider collider = dominoObject.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = interactable;
                }

                // Désactive ou active le script ClickableDomino si tu utilises un tel script
                ClickableDomino clickable = dominoObject.GetComponent<ClickableDomino>();
                if (clickable != null)
                {
                    clickable.enabled = interactable;
                }
            }
        }
    }




    // Méthode appelée lorsque le joueur clique sur un domino
    public void OnDominoSelected(GameObject dominoObj)
    {

        Domino selectedDomino = dominoObj.GetComponent<Domino>();

        if (selectedDomino != null)
        {
            // ✅ Cas spécial : premier tour, aucune contrainte
            if (gameManager.playedDominos.Count == 0)
            {
                Debug.Log("🟢 Premier tour : le joueur peut jouer n'importe quel domino.");
                // Sauvegarde l'index du domino sélectionné
                gameManager.selectedDominoIndex = hand.IndexOf(selectedDomino);
                // Au premier coup playRight doit toujours être égal à true
                gameManager.PlayDomino(hand.IndexOf(selectedDomino), this, true);
                //gameManager.ShowClickZones(true);
                return;
            }

            var validPlaySide = gameManager.GetValidPlaySides(selectedDomino);

            if (validPlaySide == GameManager.PlaySide.Both)
            {

                uiManager.eventMessage.gameObject.SetActive(false);

                //Debug.Log("Le joueur peut jouer à gauche ou à droite. En attente du choix du joueur...");
                uiManager.EventMessage("Choisissez de jouer à droite ou à gauche...");
                gameManager.ShowClickZones(true);
                gameManager.PositionClickZonesAtEnds();

                gameManager.selectedDominoIndex = hand.IndexOf(selectedDomino);
            }
            else if (validPlaySide == GameManager.PlaySide.Left)
            {
                gameManager.PlayDomino(hand.IndexOf(selectedDomino), this, false);
            }
            else if (validPlaySide == GameManager.PlaySide.Right)
            {
                gameManager.PlayDomino(hand.IndexOf(selectedDomino), this, true);
            }
            else
            {
                Debug.Log("❌ Le domino sélectionné ne peut pas être joué.");
            }
        }
    }

}
