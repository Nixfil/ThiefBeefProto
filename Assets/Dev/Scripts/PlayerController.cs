using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 6.0f; // Speed of the character
    public float rollSpeed = 12.0f; // Maximum speed of the roll
    public float rollDuration = 1.0f; // Total duration of the roll
    public float rollAcceleration = 10.0f; // Acceleration during roll
    public float rollDeceleration = 10.0f; // Deceleration after roll

    private Rigidbody rb;
    private bool isRolling;
    private float rollTime;
    private float currentRollSpeed;
    private Vector3 rollDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Disable gravity
    }

    void Update()
    {
        // Get input for movement
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        if (isRolling)
        {
            Roll();
        }
        else
        {
            // Calculate the movement direction
            Vector3 move = new Vector3(moveX, 0, moveZ).normalized;

            // Move the character
            rb.velocity = move * speed;

            // Check for roll input (space bar)
            if (Input.GetKeyDown(KeyCode.Space) && move != Vector3.zero)
            {
                StartRoll(move);
            }
        }
    }

    private void StartRoll(Vector3 moveDirection)
    {
        isRolling = true;
        rollTime = 0f;
        currentRollSpeed = rb.velocity.magnitude; // Start the roll with the current speed
        rollDirection = moveDirection;
    }

    private void Roll()
    {
        rollTime += Time.deltaTime;

        // Gradually increase the speed
        if (rollTime <= rollDuration / 2)
        {
            currentRollSpeed += rollAcceleration * Time.deltaTime;
        }
        // Gradually decrease the speed
        else if (rollTime <= rollDuration)
        {
            currentRollSpeed -= rollDeceleration * Time.deltaTime;
        }

        // Cap the speed at rollSpeed
        currentRollSpeed = Mathf.Clamp(currentRollSpeed, 0, rollSpeed);

        // Move the character in the roll direction
        rb.velocity = rollDirection * currentRollSpeed;

        // End the roll
        if (rollTime >= rollDuration)
        {
            isRolling = false;
        }
    }
}
