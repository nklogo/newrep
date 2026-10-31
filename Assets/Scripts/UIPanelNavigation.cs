using UnityEngine;

public class UIPanelNavigation : MonoBehaviour
{
    public GameObject currentPanel;
    public GameObject nextPanel;


public void OpenPanel()
    {
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
        }

        if (nextPanel != null)
        {
            nextPanel.SetActive(true);
        }
    }
    public void GoBack() 
    { 
        if (nextPanel != null) 
        { 
            nextPanel.SetActive(false);
        } 
        if (currentPanel != null) 
        { 
            currentPanel.SetActive(true); 
        } 
    
    }



}
