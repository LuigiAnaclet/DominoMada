using UnityEngine;

public class ClickableZone : MonoBehaviour
{
    public enum ZoneSide { Left, Right }
    public ZoneSide side; // À définir dans l'Inspector pour chaque zone

    private void OnMouseDown()
    {
        if (side == ZoneSide.Left)
        {
            Debug.Log("Zone gauche cliquée");
            FindAnyObjectByType<GameManager>().OnLeftZoneClicked();
        }
        else if (side == ZoneSide.Right)
        {
            Debug.Log("Zone droite cliquée");
            FindAnyObjectByType<GameManager>().OnRightZoneClicked();
        }
    }
}
