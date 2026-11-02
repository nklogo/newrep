using UnityEngine;
using UnityEngine.EventSystems;

public class MixerCardSwitcher : MonoBehaviour, IPointerClickHandler
{
    [Header("All Mixer Cards")]
    public GameObject[] allCards;

[Header("Selected Card")]
    public GameObject selectedCard;

    [Header("Back Button")]
    public bool isBackButton;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isBackButton)
        {
            HideAllCards();
        }
        else
        {
            ShowSelectedCard();
        }
    }

    public void ShowSelectedCard()
    {
        HideAllCards();

        if (selectedCard != null)
        {
            selectedCard.SetActive(true);
        }
    }

    public void HideAllCards()
    {
        for (int i = 0; i < allCards.Length; i++)
        {
            if (allCards[i] != null)
            {
                allCards[i].SetActive(false);
            }
        }
    }

}
