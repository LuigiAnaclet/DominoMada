using UnityEngine;

public class ClickableDomino : MonoBehaviour
{
    private Player player;

    void Start()
    {
        // Trouve le script Player dans la sc�ne
        player = FindAnyObjectByType<Player>();
    }

    void OnMouseDown()
    {
       if (player != null)
        {
            // Passe le domino cliqu� au script Player pour qu'il soit jou�
            player.OnDominoSelected(this.gameObject);
        }
    }
}
