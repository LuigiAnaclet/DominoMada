using UnityEngine;
using UnityEngine.SocialPlatforms;

public class Domino : MonoBehaviour
{
    public int[] sides = new int[2]; // Représente les côtés du domino (par exemple, [3, 5])
    public int[] originalSides = new int[2]; // Garde une copie des valeurs originales

    /*void Awake()
    {
        Debug.Log($"Awake - Avant copie : Domino [{sides[0]}|{sides[1]}]");

        if (sides[0] != 0 || sides[1] != 0) // On enregistre seulement si sides est défini
        {
            originalSides = (int[])sides.Clone();
        }

        Debug.Log($"Awake - Après copie : Original [{originalSides[0]}|{originalSides[1]}]");
    }


    void Start()
    {
        Debug.Log($"Start - Sides : [{sides[0]}|{sides[1]}], Original : [{originalSides[0]}|{originalSides[1]}]");

        if (originalSides[0] == 0 && originalSides[1] == 0 && (sides[0] != 0 || sides[1] != 0))
        {
            Debug.LogError("⚠ Correction forcée : originalSides était [0,0], on le met à jour !");
            originalSides = (int[])sides.Clone();
        }

        Debug.Log($"Start - Après correction : Original [{originalSides[0]}|{originalSides[1]}]");
    }*/

    // Initialise les côtés du domino
    public void Initialize(int side1, int side2)
    {
        //Debug.Log($"Initialize - Avant : sides [{sides[0]}|{sides[1]}], Original [{originalSides[0]}|{originalSides[1]}]");

        sides[0] = side1;
        sides[1] = side2;
        originalSides[0] = side1;
        originalSides[1] = side2;

        //Debug.Log($"Initialize - Après : sides [{sides[0]}|{sides[1]}], Original [{originalSides[0]}|{originalSides[1]}]");
    }

    // Méthode pour vérifier si le domino peut être joué sur un certain numéro
    public bool CanPlay(int number)
    {
        return number == sides[0] || number == sides[1];
    }

    // Méthode pour jouer le domino en fonction du numéro joué
    public int Play(int number)
    {
        if (sides[0] == number)
        {
            return sides[1]; // Retourne l'autre côté du domino
        }
        else
        {
            return sides[0]; // Retourne l'autre côté du domino
        }
    }

    // Méthode pour inverser les côtés du domino
    public void Reverse()
    {
        int temp = sides[0];
        sides[0] = sides[1];
        sides[1] = temp; // Échange les valeurs des côtés

        if (sides[0] == 0 && sides[1] == 0)
        {
            Debug.LogError("⚠ PROBLÈME : Reverse a créé un domino [0|0] !");
        }
    }
    public void ResetDomino()
    {
        //Debug.Log($"côté 1 actuel {sides[0]}, côté 2 actuel {sides[1]}");

        if (originalSides[0] == 0 && originalSides[1] == 0 && sides[0] != 0 && sides[1] != 0)
        {
            Debug.LogError("❌ ERREUR : originalSides est [0,0] alors que sides est différent !");
        }

        gameObject.SetActive(false);
        transform.SetParent(null);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        sides[0] = originalSides[0]; // Restaure le côté gauche
        sides[1] = originalSides[1]; // Restaure le côté droit

        //Debug.Log($"côté 1 original {originalSides[0]}, côté 2 original {originalSides[1]}");
    }

    // Méthode pour obtenir une représentation textuelle du domino
    public override string ToString()
    {
        return $"[{sides[0]}|{sides[1]}]";
    }
}
