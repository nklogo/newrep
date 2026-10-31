using UnityEngine;
using UnityEngine.EventSystems;

public class StandardMouldsCardClick : MonoBehaviour, IPointerClickHandler
{
    public ObjectVisibilityController mouldManager;

public void OnPointerClick(PointerEventData eventData)
    {
        if (mouldManager != null)
        {
            mouldManager.ShowObject();
        }
    }

}
