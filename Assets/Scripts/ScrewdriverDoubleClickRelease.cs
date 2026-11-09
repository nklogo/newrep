using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ScrewdriverDoubleClickRelease : MonoBehaviour
{
    public float doubleClickTime = 0.35f;
    public bool releaseOnDoubleClick = true;

    private XRGrabInteractable grabInteractable;
    private IXRSelectInteractor selectingInteractor;
    private float firstActivationTime;
    private bool hasFirstActivation;

    private void OnEnable()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
            return;

        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);
        grabInteractable.activated.AddListener(OnActivated);

        if (grabInteractable.isSelected)
            selectingInteractor = grabInteractable.firstInteractorSelecting as IXRSelectInteractor;
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
            grabInteractable.activated.RemoveListener(OnActivated);
        }

        selectingInteractor = null;
        hasFirstActivation = false;
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        selectingInteractor = args.interactorObject;
        hasFirstActivation = false;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (args.interactorObject != selectingInteractor)
            return;

        selectingInteractor = null;
        hasFirstActivation = false;
    }

    private void OnActivated(ActivateEventArgs args)
    {
        if (!releaseOnDoubleClick || !grabInteractable.isSelected)
            return;

        var activatingInteractor = args.interactorObject as IXRSelectInteractor;
        if (activatingInteractor == null || activatingInteractor != selectingInteractor)
            return;

        var now = Time.unscaledTime;
        if (!hasFirstActivation || now - firstActivationTime > doubleClickTime)
        {
            firstActivationTime = now;
            hasFirstActivation = true;
            return;
        }

        var interactionManager = grabInteractable.interactionManager;
        if (interactionManager == null)
            return;

        hasFirstActivation = false;
        interactionManager.SelectExit(selectingInteractor, grabInteractable);
    }
}