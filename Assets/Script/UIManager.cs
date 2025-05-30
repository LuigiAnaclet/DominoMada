using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class UIManager : MonoBehaviour
{
     public TextMeshProUGUI playerTurnText;
     public TextMeshProUGUI winnerText;
     public TextMeshProUGUI scoreText;
    public TextMeshProUGUI eventMessage;
    public List<TextMeshProUGUI> iaDominoCounters;

    public void UpdateScoresDisplay(List<IPlayable> players, Dictionary<IPlayable, int> scores, Dictionary<IPlayable, int> cochonsPris, Dictionary<IPlayable, Dictionary<IPlayable, int>> cochonsDonnés)
     {
         string display = "<b> Scores :</b>\n";
         foreach (var player in players)
         {
             int points = scores.ContainsKey(player) ? scores[player] : 0;
             int pris = cochonsPris.ContainsKey(player) ? cochonsPris[player] : 0;

             int donnés = 0;
             if (cochonsDonnés.ContainsKey(player))
             {
                 foreach (var val in cochonsDonnés[player].Values)
                 {
                     donnés += val;
                 }
             }

             display += $"{player.name} : {points} pts | +{pris} cochons | -{donnés} cochons\n";
         }

         if (scoreText != null)
         {
             scoreText.text = display;
         }
         else
         {
             Debug.LogWarning("⚠️ UIManager : Le champ scoreText n'est pas assigné !");
         }
     }

    public void ShowWinnerMessage(string message, float duration = 3f)
    {
        if (winnerText == null) return;

        winnerText.text = message;
        winnerText.gameObject.SetActive(true);
        StartCoroutine(HideWinnerAfterDelay(duration));
    }

    private IEnumerator HideWinnerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (winnerText != null)
        {
            winnerText.gameObject.SetActive(false);
        }
    }

    public void EventMessage(string message, float duration = 3f)
    {
        if (eventMessage == null) return;

        eventMessage.text = message;
        eventMessage.gameObject.SetActive(true);
        StartCoroutine(HideEventAfterDelay(duration));
    }

    private IEnumerator HideEventAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (eventMessage != null)
        {
            eventMessage.gameObject.SetActive(false);
        }
    }
    public void DisplayPlayerTurn(string message)
    {
        playerTurnText.text = $"{message}";
    }

    public void UpdateIADominoCounts(List<IPlayable> players, IPlayable localPlayer)
    {
        int counterIndex = 0;

        foreach (var player in players)
        {
            if (player != localPlayer)
            {
                if (counterIndex < iaDominoCounters.Count)
                {
                    int count = player.GetHand().Count;
                    string playerName = player.name;
                    iaDominoCounters[counterIndex].text = $"{playerName} : x{count}";
                }
                counterIndex++;
            }
        }
    }


    /* public void DisplayPlayerTurn(string playerName)
     {
         playerTurnText.text = $"{playerName} joue";
         playerTurnText.gameObject.SetActive(true);
     }

     public void HidePlayerTurn()
     {
         playerTurnText.gameObject.SetActive(false);
     }

     public void DisplayWinner(string playerName)
     {
         winnerText.text = $"{playerName} a gagné!";
         winnerText.gameObject.SetActive(true);
     }

     public void HideScores()
     {
         winnerText.gameObject.SetActive(false);
     }*/
}
