using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Running,
        Rolling,
    }

    public float speed = 6.0f; // Speed of the character
    public float rollSpeed = 12.0f; // Maximum speed of the roll
    public float rollDuration = 1.0f; // Total duration of the roll
    public float rollAcceleration = 10.0f; // Acceleration during roll
    public float rollDeceleration = 10.0f; // Deceleration after roll
    public float rotationSpeed;

    [SerializeField] private PlayerState currentState;
    [SerializeField] private CapsuleCollider capsuleCollider;

    private Rigidbody rb;
    private float rollTime;
    private float currentRollSpeed;
    private Vector3 rollDirection;
    private float colliderHeightOG;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Disable gravity
        currentState = PlayerState.Idle; // Start in Idle state
        colliderHeightOG = capsuleCollider.height;
    }

    void Update()
    {
        // Get input for movement
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        Vector3 move = new Vector3(moveX, 0, moveZ).normalized;

        switch (currentState)
        {
            case PlayerState.Idle:
                if (move != Vector3.zero)
                {
                    ChangeState(PlayerState.Running);
                }
                else
                {
                    rb.velocity = Vector3.zero;
                }
                break;

            case PlayerState.Running:
                if (move == Vector3.zero)
                {
                    ChangeState(PlayerState.Idle);
                }
                else if (Input.GetKeyDown(KeyCode.Space))
                {
                    StartRoll(move);
                }
                else
                {
                    MoveCharacter(move);
                }
                break;

            case PlayerState.Rolling:

                Roll();
                break;
        }
    }

    private void MoveCharacter(Vector3 moveDirection)
    {
        rb.velocity = moveDirection * speed;

        // Rotate character to face movement direction
        if (moveDirection != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            rb.rotation = Quaternion.RotateTowards(rb.rotation, toRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void StartRoll(Vector3 moveDirection)
    {
        ChangeState(PlayerState.Rolling);
        rollTime = 0f;
        currentRollSpeed = rb.velocity.magnitude; // Start the roll with the current speed
        rollDirection = moveDirection;
    }

    private void Roll()
    {
        capsuleCollider.height *= 0.3f;
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
            capsuleCollider.height = colliderHeightOG;
            ChangeState(PlayerState.Idle);
        }
    }

    private void ChangeState(PlayerState newState)
    {
        currentState = newState;
    }
}
