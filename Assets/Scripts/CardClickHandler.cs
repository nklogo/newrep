using UnityEngine;
using UnityEngine.EventSystems;

public class CardClickHandler : MonoBehaviour, IPointerClickHandler
{
    public UIPanelNavigation panelNavigation;
    public ObjectVisibilityController objectController;

public bool isBackButton = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (panelNavigation == null)
            return;

        if (isBackButton)
        {
            if (objectController != null)
            {
                objectController.HideObject();
            }

            panelNavigation.GoBack();
        }
        else
        {
            panelNavigation.OpenPanel();
        }
    }

}
