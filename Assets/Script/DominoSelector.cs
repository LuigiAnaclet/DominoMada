using System.Collections.Generic;
using UnityEngine;

public class DominoSelector : MonoBehaviour
{
    /*public GameManager gameManager; // Référence à GameManager pour accéder aux informations de jeu
    public List<Domino> playableDominos = new List<Domino>(); // Liste des dominos jouables
    private Domino selectedDomino; // Domino sélectionné
    private int selectedSide; // Côté sélectionné pour jouer le domino

    // Appelé lorsqu'un joueur sélectionne un domino
    public void SelectDomino(Domino domino)
    {
        if (playableDominos.Contains(domino))
        {
            selectedDomino = domino;
            HighlightDomino(domino);
        }
    }

    // Méthode pour gérer le choix du côté
    public void ChooseSide(int side)
    {
        if (selectedDomino != null)
        {
            selectedSide = side;
            PlaySelectedDomino();
        }
    }

    // Joue le domino sélectionné en appelant GameManager
    private void PlaySelectedDomino()
    {
        if (selectedDomino != null)
        {
            int dominoIndex = gameManager.dominoObjects.IndexOf(selectedDomino);
            gameManager.PlayDomino(dominoIndex); // Appelle PlayDomino dans GameManager avec l'index du domino
            ClearSelection();
        }
    }

    // Met en évidence le domino sélectionné
    private void HighlightDomino(Domino domino)
    {
        // Logique de surbrillance du domino, par exemple en changeant sa couleur ou en ajoutant un effet visuel
        // Vous pouvez ajuster cette méthode pour définir la surbrillance souhaitée
    }

    // Réinitialise la sélection après avoir joué le domino
    private void ClearSelection()
    {
        selectedDomino = null;
        selectedSide = -1;
        playableDominos.Clear();
    }

    // Détermine les dominos jouables pour le joueur
    public void UpdatePlayableDominos(List<Domino> dominos, int endValue)
    {
        playableDominos.Clear();
        foreach (var domino in dominos)
        {
            if (domino.CanPlay(endValue))
            {
                playableDominos.Add(domino);
            }
        }
    }*/
}
