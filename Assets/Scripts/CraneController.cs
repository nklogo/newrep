using UnityEngine;

public class CraneController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform upperRod;   // Left & Right
    [SerializeField] private Transform holder;     // Up/Down & Forward/Back

    [Header("Movement Speed")]
    [SerializeField] private float horizontalSpeed = 2f;
    [SerializeField] private float verticalSpeed = 2f;
    [SerializeField] private float depthSpeed = 2f;

    private Vector3 upperRodDirection = Vector3.zero;
    private Vector3 holderVerticalDirection = Vector3.zero;
    private Vector3 holderDepthDirection = Vector3.zero;

    private void Update()
    {
        // Upper Rod Movement (Left / Right)
        upperRod.position += upperRodDirection * horizontalSpeed * Time.deltaTime;

        // Holder Vertical Movement (Local)
        holder.localPosition += holderVerticalDirection * verticalSpeed * Time.deltaTime;

        // Holder Forward / Backward Movement (Local)
        holder.localPosition += holderDepthDirection * depthSpeed * Time.deltaTime;
    }

    #region Upper Rod Left / Right

    public void StartLeft()
    {
        upperRodDirection = Vector3.left;
    }

    public void StopLeft()
    {
        if (upperRodDirection == Vector3.left)
            upperRodDirection = Vector3.zero;
    }

    public void StartRight()
    {
        upperRodDirection = Vector3.right;
    }

    public void StopRight()
    {
        if (upperRodDirection == Vector3.right)
            upperRodDirection = Vector3.zero;
    }

    #endregion

    #region Holder Up / Down

    public void StartUp()
    {
        holderVerticalDirection = Vector3.up;
    }

    public void StopUp()
    {
        if (holderVerticalDirection == Vector3.up)
            holderVerticalDirection = Vector3.zero;
    }

    public void StartDown()
    {
        holderVerticalDirection = Vector3.down;
    }

    public void StopDown()
    {
        if (holderVerticalDirection == Vector3.down)
            holderVerticalDirection = Vector3.zero;
    }

    #endregion

    #region Holder Forward / Backward

    public void StartForward()
    {
        holderDepthDirection = Vector3.forward;
    }

    public void StopForward()
    {
        if (holderDepthDirection == Vector3.forward)
            holderDepthDirection = Vector3.zero;
    }

    public void StartBackward()
    {
        holderDepthDirection = Vector3.back;
    }

    public void StopBackward()
    {
        if (holderDepthDirection == Vector3.back)
            holderDepthDirection = Vector3.zero;
    }

    #endregion

    public void StopAll()
    {
        upperRodDirection = Vector3.zero;
        holderVerticalDirection = Vector3.zero;
        holderDepthDirection = Vector3.zero;
    }
}