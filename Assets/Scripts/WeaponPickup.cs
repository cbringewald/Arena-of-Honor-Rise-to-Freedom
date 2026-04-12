using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public GameObject weaponModelPrefab;
    public GameObject promptWorldText;

    void Awake()
    {
        if (promptWorldText != null)
            promptWorldText.SetActive(false);
    }

    public void ShowPrompt(bool show)
    {
        if (promptWorldText != null)
            promptWorldText.SetActive(show);
    }
}