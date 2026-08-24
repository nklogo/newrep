using UnityEngine;

public class CraneController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform upperRod;
    [SerializeField] private Transform holder;

    [Header("Movement Speed")]
    [SerializeField] private float horizontalSpeed = 2f;
    [SerializeField] private float verticalSpeed = 2f;
    [SerializeField] private float depthSpeed = 2f;

    // Movement states
    private bool moveLeft;
    private bool moveRight;

    private bool moveUp;
    private bool moveDown;

    private bool moveForward;
    private bool moveBackward;

    private void Update()
    {
        // =====================================================
        // CRANE ROD
        // X AXIS ONLY
        // =====================================================

        float xMovement = 0f;

        if (moveLeft)
        {
            xMovement = -1f;
        }
        else if (moveRight)
        {
            xMovement = 1f;
        }

        if (xMovement != 0f)
        {
            Vector3 position = upperRod.localPosition;

            position.x += xMovement * horizontalSpeed * Time.deltaTime;

            upperRod.localPosition = position;
        }


        // =====================================================
        // HOLDER
        // Y AXIS - UP / DOWN
        // Z AXIS - FORWARD / BACKWARD
        // =====================================================

        float yMovement = 0f;
        float zMovement = 0f;

        if (moveUp)
        {
            yMovement = 1f;
        }
        else if (moveDown)
        {
            yMovement = -1f;
        }

        if (moveForward)
        {
            zMovement = 1f;
        }
        else if (moveBackward)
        {
            zMovement = -1f;
        }

        Vector3 holderPosition = holder.localPosition;

        holderPosition.y += yMovement * verticalSpeed * Time.deltaTime;
        holderPosition.z += zMovement * depthSpeed * Time.deltaTime;

        holder.localPosition = holderPosition;
    }


    // =========================================================
    // CRANE ROD - LEFT
    // =========================================================

    public void StartLeft()
    {
        Debug.Log("START LEFT");

        moveLeft = true;
        moveRight = false;
    }

    public void StopLeft()
    {
        Debug.Log("STOP LEFT");

        moveLeft = false;
    }


    // =========================================================
    // CRANE ROD - RIGHT
    // =========================================================

    public void StartRight()
    {
        Debug.Log("START RIGHT");

        moveRight = true;
        moveLeft = false;
    }

    public void StopRight()
    {
        Debug.Log("STOP RIGHT");

        moveRight = false;
    }


    // =========================================================
    // HOLDER - UP
    // =========================================================

    public void StartUp()
    {
        moveUp = true;
        moveDown = false;
    }

    public void StopUp()
    {
        moveUp = false;
    }


    // =========================================================
    // HOLDER - DOWN
    // =========================================================

    public void StartDown()
    {
        moveDown = true;
        moveUp = false;
    }

    public void StopDown()
    {
        moveDown = false;
    }


    // =========================================================
    // HOLDER - FORWARD
    // =========================================================

    public void StartForward()
    {
        moveForward = true;
        moveBackward = false;
    }

    public void StopForward()
    {
        moveForward = false;
    }


    // =========================================================
    // HOLDER - BACKWARD
    // =========================================================

    public void StartBackward()
    {
        moveBackward = true;
        moveForward = false;
    }

    public void StopBackward()
    {
        moveBackward = false;
    }


    // =========================================================
    // STOP ALL
    // =========================================================

    public void StopAll()
    {
        moveLeft = false;
        moveRight = false;

        moveUp = false;
        moveDown = false;

        moveForward = false;
        moveBackward = false;
    }
}