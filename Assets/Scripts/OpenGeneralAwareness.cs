using UnityEngine;

public class OpenGeneralAwareness : MonoBehaviour
{
    public GameObject Cards;
    public GameObject panelCanvas1;

    public void OpenPanel()
    {
        Cards.SetActive(false);
        panelCanvas1.SetActive(true);
    }
}
