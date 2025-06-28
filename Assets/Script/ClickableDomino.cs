using UnityEngine;

public class ClickableDomino : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (GameManager.Instance.localPlayer is Player player)
        {
            if (this == null || gameObject == null)
            {
                Debug.LogWarning("❌ ClickableDomino cliqué mais GameObject manquant.");
                return;
            }

            player.OnDominoSelected(gameObject);
        }
        else
        {
            Debug.LogWarning("❌ localPlayer n'est pas de type Player.");
        }
    }
}
