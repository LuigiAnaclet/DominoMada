using UnityEngine;

public class ClickableDomino : MonoBehaviour
{
    private Player player;

    void Start()
    {
        // Trouve le script Player dans la scène
        player = FindAnyObjectByType<Player>();
    }

    void OnMouseDown()
    {
       if (player != null)
        {
            // Passe le domino cliqué au script Player pour qu'il soit joué
            player.OnDominoSelected(this.gameObject);
        }
    }
}
