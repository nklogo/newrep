using UnityEngine;

public class ObjectVisibilityController : MonoBehaviour
{
    public GameObject objectRoot;


private static GameObject currentlyVisibleObject;

    public void ShowObject()
    {
        // Hide previously visible object
        if (currentlyVisibleObject != null)
        {
            currentlyVisibleObject.SetActive(false);
        }

        // Show selected object
        if (objectRoot != null)
        {
            objectRoot.SetActive(true);
            currentlyVisibleObject = objectRoot;
        }
    }

    public void HideObject()
    {
        // Hide whichever object is currently visible
        if (currentlyVisibleObject != null)
        {
            currentlyVisibleObject.SetActive(false);
            currentlyVisibleObject = null;
        }
    }


}
