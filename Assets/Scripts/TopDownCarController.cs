using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopDownCarController : MonoBehaviour
{
    [Header("Car Settings")]
    public float driftFactor = 0.95f;
    public float accelerationFactor = 30.0f;
    public float turnFactor = 3.5f;
    public float maxSpeed = 20.0f;

    [Header("Sprites")]
    public SpriteRenderer carSpriteRenderer;
    public SpriteRenderer carShadowRenderer;

    [Header("Jumping")]
    public AnimationCurve jumpCurve;
    public ParticleSystem landingParticleSystem;

    // Local variables
    private float accelerationInput = 0f;
    private float steeringInput = 0f;
    private float rotationAngle = 0f;
    private float velocityVsUp = 0f;
    private bool isJumping = false;

    // Components
    Rigidbody2D carRigidbody2D;
    Collider2D carCollider;
    CarSFXHandler carSFXHandler;

    private void Awake()
    {
        carRigidbody2D = GetComponent<Rigidbody2D>();
        carCollider = GetComponentInChildren<Collider2D>();
        carSFXHandler = GetComponent<CarSFXHandler>();
    }

    void Start()
    {
        rotationAngle = transform.rotation.eulerAngles.z;
    }

    // Frame-rate independent for physics calculations
    private void FixedUpdate()
    {
        ApplyEngineForce();
        KillOrthogonalVelocity();
        ApplySteering();
    }
    private void ApplyEngineForce()
    {
        // Don't let the player brake while in the air, but we still allow some drag so it can be slowed slightly
        if (isJumping && accelerationInput < 0)
            accelerationInput = 0;
        // Calculate how much "forward" we are going in terms of the direction of our velocity
        velocityVsUp = Vector2.Dot(transform.up, carRigidbody2D.velocity);

        // Limit so we cannot go faster than the max speed in the "forward" direction
        if (velocityVsUp > maxSpeed && accelerationInput > 0) 
            return;

        // Limit so we cannot go faster than the 50% of max speed in the "reverse" direction
        if (velocityVsUp < -maxSpeed * 0.5f && accelerationInput < 0)
            return;

        // Limit so we cannot go faster in any direction while accelerating
        if (carRigidbody2D.velocity.sqrMagnitude > maxSpeed * maxSpeed && accelerationInput > 0 && !isJumping)
            return;

        // Apply drag if there is no accelerationInput so the car stops when the player lets go of the accelerator
        if (accelerationInput == 0)
        {
            carRigidbody2D.drag = Mathf.Lerp(carRigidbody2D.drag, 3.0f, Time.fixedDeltaTime * 3); // Yucky magic numbers
        }
        else
        {
            carRigidbody2D.drag = 0;
        }

        // Create a force for the engine
        Vector2 engineForceVector = transform.up * accelerationInput * accelerationFactor;

        // Apply the force created above to push the car forward
        carRigidbody2D.AddForce(engineForceVector, ForceMode2D.Force);
    }

    private void ApplySteering()
    {
        // Limit the cars ability to turn when moving slowly
        float minSpeedBeforeAllowTurningFactor = (carRigidbody2D.velocity.magnitude / 8); // Yucky magic number
        minSpeedBeforeAllowTurningFactor = Mathf.Clamp01(minSpeedBeforeAllowTurningFactor);

        // Update the rotation angle based on input
        rotationAngle -= steeringInput * turnFactor * minSpeedBeforeAllowTurningFactor;

        // Apply steering by rotating the car object
        carRigidbody2D.MoveRotation(rotationAngle);
    }

    private void KillOrthogonalVelocity()
    {
        Vector2 forwardVelocity = transform.up * Vector2.Dot(carRigidbody2D.velocity, transform.up);
        Vector2 rightVelocity = transform.right * Vector2.Dot(carRigidbody2D.velocity, transform.right);

        carRigidbody2D.velocity = forwardVelocity + rightVelocity * driftFactor;
    }

    private float GetLateralVelocity()
    {
        // Returns how fast the car is moving sideways
        return Vector2.Dot(transform.right, carRigidbody2D.velocity);
    }

    public float GetVelocityMagnitude()
    {
        return carRigidbody2D.velocity.magnitude;
    }

    public bool IsTireScreeching(out float lateralVelocity, out bool isBreaking)
    {
        lateralVelocity = GetLateralVelocity();
        isBreaking = false;

        if (isJumping)
            return false;

        // Check if we are moving forward and if the player is hitting the brakes. In that case the tires should screech.
        if (accelerationInput < 0 && velocityVsUp > 0)
        {
            isBreaking = true;
            return true;
        }

        // If we have a lot of side movement then the tires should be screeching.
        if (Mathf.Abs(GetLateralVelocity()) > 4.0f)
            return true;

        return false;
    }

    public void SetInputVector(Vector2 inputVector)
    {
        steeringInput = inputVector.x;
        accelerationInput = inputVector.y;
    }

    public void Jump(float jumpHeightScale, float jumpPushScale)
    {
        if (!isJumping)
        {
            StartCoroutine(JumpCo(jumpHeightScale, jumpPushScale));
        }
    }

    private IEnumerator JumpCo(float jumpHeightScale, float jumpPushScale)
    {
        isJumping = true;

        float jumpStartTime = Time.time;
        float jumpDuration = carRigidbody2D.velocity.magnitude * 0.05f;

        jumpHeightScale = jumpHeightScale * carRigidbody2D.velocity.magnitude * 0.05f;
        jumpHeightScale = Mathf.Clamp(jumpHeightScale, 0.0f, 1.0f);

        // Disable collisions
        carCollider.enabled = false;

        carSFXHandler.PlayJumpSFX();

        // Change sorting layer to flying
        carSpriteRenderer.sortingLayerName = "Flying";
        carShadowRenderer.sortingLayerName = "Flying";

        // Push the object forward as we passed a jump
        carRigidbody2D.AddForce(carRigidbody2D.velocity.normalized * jumpPushScale * 10, ForceMode2D.Impulse); // Yucky Magic number

        while (isJumping)
        {
            // Percentage 0 - 1.0 of where we are in the jumping process
            float jumpCompletePercentage = (Time.time - jumpStartTime) / jumpDuration;
            jumpCompletePercentage = Mathf.Clamp01(jumpCompletePercentage);

            // Take the base scale of 1 and add how much we should increase the scale with.
            carSpriteRenderer.transform.localScale = Vector3.one + Vector3.one * jumpCurve.Evaluate(jumpCompletePercentage) * jumpHeightScale;

            // Change the shadow scale also but make it a bit smaller. In the real world this should be the opposite.
            carShadowRenderer.transform.localScale = carSpriteRenderer.transform.localScale * 0.75f;

            // Offset the shadow a bit. This is not 100% either, but works good enough in our game.
            carShadowRenderer.transform.localPosition = new Vector3(1, -1, 0.0f) * 3 * jumpCurve.Evaluate(jumpCompletePercentage) * jumpHeightScale; // Yucky Magic number

            // When we reach 100% we are done
            if (jumpCompletePercentage == 1.0f)
                break;

            yield return null;
        }

        // Check if landing is OK or not
        if (Physics2D.OverlapCircle(transform.position, 1.5f))
        {
            // Something is below the car so we need to jump again
            isJumping = false;

            // Add a small jump and push the car forward a bit
            Jump(0.2f, 0.6f);
        }
        else
        {
            // Handle landing, scale back the object
            carSpriteRenderer.transform.localScale = Vector3.one;

            // Reset the shadows position and scale
            carShadowRenderer.transform.localPosition = Vector3.zero;
            carShadowRenderer.transform.localScale = carSpriteRenderer.transform.localScale;

            // We are safe to land, so enable the collider
            carCollider.enabled = true;

            // Change sorting layer to regular layer
            carSpriteRenderer.sortingLayerName = "Default";
            carShadowRenderer.sortingLayerName = "Default";

            // Play the landing particle system if it is a bigger jump
            if (jumpHeightScale > 0.2f)
            {
                landingParticleSystem.Play();
                carSFXHandler.PlayLandingSFX();
            }

            // Change state
            isJumping = false;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Jump"))
        {
            // Get the jump data from the jump
            JumpData jumpData = collision.GetComponent<JumpData>();
            Jump(jumpData.jumpHeightScale, jumpData.jumpPushScale);
        }
    }
}
