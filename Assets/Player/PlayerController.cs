using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Camera playerCamera;
    public Transform rotateCenter;

    CharacterController characterController;

    [Header("Movement")]
    public bool canMove = true;
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    Vector3 moveDirection = Vector3.zero;

    public float lookSpeed = 2f;
    public float lookXLimit = 45f;

    float rotationX = 0;

    [Header("Panning")]
    public bool isPanning = false;
    public float panDuration = 2f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Toggle movement.
        if (Input.GetKeyDown(KeyCode.Y))
        {
            canMove = !canMove;
        }

        // Toggle cursor lock.
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        // Trigger camera panning.
        if (Input.GetKeyDown(KeyCode.Alpha0) && !isPanning)
        {
            StartCoroutine(PanCamera360());
        }

        #region Handles Movement
        if (!isPanning)
        {
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);

            // Press Left Shift to run
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float curSpeedX = canMove
                ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical")
                : 0;
            float curSpeedY = canMove
                ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal")
                : 0;

            moveDirection = (forward * curSpeedX) + (right * curSpeedY);

            characterController.Move(moveDirection * Time.deltaTime);

            // Handle player rotation (mouse movement)
            if (canMove)
            {
                // Get mouse input
                float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
                float mouseY = -Input.GetAxis("Mouse Y") * lookSpeed;

                // Rotate the player horizontally
                transform.rotation *= Quaternion.Euler(0, mouseX, 0);

                // Rotate the camera vertically
                rotationX += mouseY;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
                rotateCenter.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            }
        }
        #endregion

        // Get terrain height.
        if (Input.GetKeyDown(KeyCode.R))
        {
            float height = FindObjectOfType<GetHeightScript>()
                .GetHeight(new Vector2(transform.localPosition.x, transform.localPosition.z));
            transform.position = new Vector3(transform.position.x, height, transform.position.z);
            print("Terrain height: " + height);
        }
    }

    // Coroutine to pan the camera 360 degrees.
    IEnumerator PanCamera360()
    {
        isPanning = true;
        float curDuration = 0f;
        float startRotY = transform.eulerAngles.y;

        while (curDuration < panDuration)
        {
            float newRotY = Mathf.Lerp(startRotY, startRotY + 360f, curDuration / panDuration);
            transform.rotation = Quaternion.Euler(0, newRotY, 0);
            curDuration += Time.deltaTime;
            yield return null;
        }

        // Ensure exact 360 degrees rotation at the end.
        transform.rotation = Quaternion.Euler(0, startRotY + 360f, 0);
        isPanning = false;
    }
}
