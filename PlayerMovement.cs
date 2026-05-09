using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))] // *** NEW: Added required AudioSource
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Footstep Sound Settings")]
    // Assign your footstep sound files here in the Inspector
    public AudioClip[] footstepSounds;
    public float walkStepInterval = 0.5f; // Time in seconds between footsteps when walking
    public float runStepInterval = 0.3f;  // Time in seconds between footsteps when running
    public float volume = 0.5f; // Volume of the footstep sound

    private CharacterController controller;
    private Vector3 moveDirection;
    private float verticalVelocity;
    private Animator animator;
    // *** NEW: Audio Source component
    private AudioSource audioSource;

    // *** NEW: Footstep timer variables
    private float nextFootstepTime;
    private float timeBetweenSteps;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        // *** NEW: Get the AudioSource component
        audioSource = GetComponent<AudioSource>();
        // Initialize the footstep timer
        nextFootstepTime = Time.time;
    }

    void Update()
    {
        // --- Original Movement Logic ---
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Arrow keys
        float vertical = Input.GetAxis("Vertical");      // W/S or Arrow keys
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        moveDirection = forward * vertical + right * horizontal;
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        float speedValue = 0f;
        float horizontalValue = 0f;

        if (moveDirection.magnitude > 0f)
        {
            moveDirection = moveDirection.normalized;

            if (isRunning)
            {
                speedValue = vertical * 2f;
                horizontalValue = horizontal * 1f;
            }
            else
            {
                speedValue = vertical * 1f;
                horizontalValue = horizontal * 1f;
            }
        }

        // --- Handle Footsteps (NEW) ---
        // Set the appropriate interval based on movement state
        timeBetweenSteps = isRunning ? runStepInterval : walkStepInterval;
        
        // Only call the footstep logic if the player is actively moving on the ground
        if (controller.isGrounded && moveDirection.magnitude > 0.1f)
        {
            HandleFootsteps();
        }


        // --- Original CharacterController Logic ---
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -1f;

            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

                if (animator != null)
                {
                    animator.SetTrigger("Jump");
                }
            }
        }

        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);

        if (animator != null)
        {
            animator.SetFloat("Speed", speedValue);
            animator.SetFloat("Horizontal", horizontalValue);
        }
    }

    // *** NEW: Method to handle the footstep sound logic
    private void HandleFootsteps()
    {
        // Check if the current time has passed the time we set for the next footstep
        if (Time.time > nextFootstepTime)
        {
            // Set the time for the *next* footstep
            nextFootstepTime = Time.time + timeBetweenSteps;

            // Only proceed if we have sounds assigned
            if (footstepSounds != null && footstepSounds.Length > 0)
            {
                // Pick a random footstep sound from the array
                int index = Random.Range(0, footstepSounds.Length);
                AudioClip clip = footstepSounds[index];

                // Play the sound
                // PlayOneShot is better for short, non-looping sounds like footsteps
                audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}