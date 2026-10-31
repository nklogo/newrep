using UnityEngine;
using UnityEngine.EventSystems;

public class BlockCardClickHandler : MonoBehaviour, IPointerClickHandler
{
    public ObjectVisibilityController objectManager;

public void OnPointerClick(PointerEventData eventData)
    {
        if (objectManager != null)
        {
            objectManager.ShowObject();
        }
    }

}
