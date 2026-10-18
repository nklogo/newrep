using UnityEngine;
using UnityEngine.EventSystems;

public class CardClickHandler : MonoBehaviour, IPointerClickHandler
{
    public OpenGeneralAwareness generalAwareness;

    public void OnPointerClick(PointerEventData eventData)
    {
        generalAwareness.OpenPanel();
    }
}