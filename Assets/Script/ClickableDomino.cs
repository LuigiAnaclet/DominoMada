using UnityEngine;

public class ClickableDomino : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (GameManager.Instance.localPlayer is Player player)
        {
            player.OnDominoSelected(gameObject);
        }
        else
        {
            Debug.LogWarning("❌ localPlayer n'est pas de type Player.");
        }
    }
}
