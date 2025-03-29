using UnityEngine;
using System.Collections.Generic;

public interface IPlayable
{
    public string name { get; set; }    
    List<Domino> GetHand();
    void SetHand(List<Domino> hand);
    void RemoveDominoFromHand(Domino domino);
    public bool isAI { get; set; }
}
