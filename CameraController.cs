using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float mouseSensitivity = 100f;
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;
    public float initialVerticalAngle = -20f; // Set this in Inspector to adjust startup angle
    
    private float verticalRotation = 0f;
    private Transform playerBody;
    private bool hasSetInitialRotation = false;

    void Start()
    {
        // Lock the cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        
        // Get player body (parent object)
        playerBody = transform.parent;
        
        // Set initial camera angle to look at player from behind, not down at ground
        verticalRotation = -20f; // Negative value to look UP at the player
        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        
        Debug.Log("Camera starting with vertical angle: " + verticalRotation);
        Debug.Log("Camera local rotation: " + transform.localRotation.eulerAngles);
    }

    void Update()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Vertical camera rotation (up/down) - applied to camera
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
        
        // Apply vertical rotation to camera
        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // Horizontal rotation (left/right) - applied to player body
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
        
        // Toggle cursor lock with Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}