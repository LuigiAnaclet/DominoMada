using UnityEngine;

public class ClickableZone : MonoBehaviour
{
    public enum ZoneSide { Left, Right }
    public ZoneSide side; // � d�finir dans l'Inspector pour chaque zone

    private void OnMouseDown()
    {
        if (side == ZoneSide.Left)
        {
            Debug.Log("Zone gauche cliqu�e");
            FindAnyObjectByType<GameManager>().OnLeftZoneClicked();
        }
        else if (side == ZoneSide.Right)
        {
            Debug.Log("Zone droite cliqu�e");
            FindAnyObjectByType<GameManager>().OnRightZoneClicked();
        }
    }
}
