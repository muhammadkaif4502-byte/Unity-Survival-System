using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private float lastYRotation;
    private float turnSpeed;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        lastYRotation = transform.eulerAngles.y;
    }

    void Update()
    {
        // Get input values
        float horizontal = Input.GetAxis("Horizontal"); // A/D keys
        float vertical = Input.GetAxis("Vertical");     // W/S keys
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        // Calculate turning speed
        float currentYRotation = transform.eulerAngles.y;
        float rotationDifference = Mathf.DeltaAngle(lastYRotation, currentYRotation);
        turnSpeed = rotationDifference / Time.deltaTime;
        lastYRotation = currentYRotation;

        // Calculate animation values for blend tree
        float speedValue = 0f;
        float horizontalValue = 0f;
        float turnValue = 0f;

        // Calculate input magnitude to determine if moving
        float inputMagnitude = new Vector2(horizontal, vertical).magnitude;

        if (inputMagnitude > 0.1f)
        {
            // Set horizontal value for left/right movement
            horizontalValue = horizontal;
            
            // Set speed value based on forward/backward and walking/running
            if (isRunning)
            {
                // Running: use full values for better animation triggering
                if (vertical > 0.1f) // Forward
                {
                    speedValue = 2f;
                }
                else if (vertical < -0.1f) // Backward
                {
                    speedValue = -2f;
                }
                else // Pure strafe while running
                {
                    speedValue = 1f; // Use walk speed for pure strafe
                }
            }
            else
            {
                // Walking: use standard values
                if (vertical > 0.1f) // Forward
                {
                    speedValue = 1f;
                }
                else if (vertical < -0.1f) // Backward
                {
                    speedValue = -1f;
                }
                else // Pure strafe
                {
                    speedValue = 0f;
                }
            }
        }

        // Calculate turn animation value
        if (Mathf.Abs(turnSpeed) > 30f) // Only trigger turn animations for significant turns
        {
            turnValue = Mathf.Clamp(turnSpeed / 90f, -1f, 1f); // Normalize turn speed
        }

        // Update animator parameters WITHOUT smooth transitions
        animator.SetFloat("Speed", speedValue);
        animator.SetFloat("Horizontal", horizontalValue);
        animator.SetFloat("Turn", turnValue);
        
        // Debug info
        if (inputMagnitude > 0.1f || Mathf.Abs(turnValue) > 0.1f)
        {
            Debug.Log("Speed: " + speedValue + ", Horizontal: " + horizontalValue + ", Turn: " + turnValue);
        }
    }
}