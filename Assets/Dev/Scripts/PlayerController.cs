using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    public enum PlayerState { Idle, Running, Rolling }
    public DetectorScript detectorScript;

    public float speed; // Speed of the character
    public float rollDuration; // Total duration of the roll
    public float idleRollDuration;
    public float desiredRollDistance;
    public float idleRollDistance;
    public float runningRollDistance;
    public float rotationSpeed;

    public LayerMask mask;
    public bool roofed;
    public Vector3 move;

    [SerializeField] private PlayerState currentState;
    [SerializeField] private CapsuleCollider capsuleCollider;

    private Rigidbody rb;
    private float rollTime;
    private float rollAcceleration;
    private float rollSpeed;
    private float currentRollSpeed;
    private Vector3 rollDirection;
    private GameObject lastHitTable = null;

    public PlayerState CurrentState { get; internal set; }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Disable gravity
        // Freeze rotation on X and Z axes to prevent unwanted rotations
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (capsuleCollider == null)
            capsuleCollider = GetComponent<CapsuleCollider>();

        currentState = PlayerState.Idle;
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        move = new Vector3(moveX, 0, moveZ).normalized;

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

                // Detect table in the direction the player is facing
                DetectTable(transform.forward);

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    // Roll in the opposite direction of current facing
                    Vector3 backDirection = -transform.forward;
                    StartRoll(backDirection, currentState);
                }
                break;

            case PlayerState.Running:
                if (move == Vector3.zero)
                {
                    ChangeState(PlayerState.Idle);
                    rb.velocity = Vector3.zero;
                }
                else if (Input.GetKeyDown(KeyCode.Space))
                {
                    StartRoll(move, currentState);
                }
                else
                {
                    rb.velocity = move * speed;
                    Quaternion toRotation = Quaternion.LookRotation(move, Vector3.up);
                    rb.rotation = Quaternion.RotateTowards(rb.rotation, toRotation, rotationSpeed * Time.deltaTime);
                }

                // Detect table in the direction the player is moving
                DetectTable(move != Vector3.zero ? move : transform.forward);
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

    public void StartRoll(Vector3 moveDirection, PlayerState fromState)
    {
        ChangeState(PlayerState.Rolling);
        rollTime = 0f;
        rollDirection = moveDirection.normalized;

        float rollDistance = fromState == PlayerState.Idle ? idleRollDistance : runningRollDistance;
        float duration = fromState == PlayerState.Idle ? idleRollDuration : rollDuration;

        rollSpeed = rollDistance / duration;
        currentRollSpeed = rollSpeed;
        rollAcceleration = rollSpeed * 2;
    }

    private void Roll()
    {
        capsuleCollider.height = 0.3f * capsuleCollider.height;
        rollTime += Time.deltaTime;
        rb.velocity = rollDirection * currentRollSpeed;

        // Adjust speed
        currentRollSpeed += (rollTime <= rollDuration / 2 ? rollAcceleration : -rollAcceleration) * Time.deltaTime;
        currentRollSpeed = Mathf.Clamp(currentRollSpeed, 0, rollSpeed);

        // Move the character in the roll direction
        rb.velocity = rollDirection * currentRollSpeed;

        // End the roll
        if (rollTime >= rollDuration && detectorScript.ObjectsAboveMyHead.Count == 0)
        {
            capsuleCollider.height = 2;
            ChangeState(PlayerState.Idle);
            // Force exit direction and stop spinning
            rb.velocity = transform.forward * speed;
            rb.angularVelocity = Vector3.zero;
        }
        else if (currentRollSpeed < 10) { currentRollSpeed = 18; }
    }

    private void DetectTable(Vector3 direction)
    {
        GameObject currentHitTable = null;

        if (Physics.Raycast(transform.position, direction, out RaycastHit hitInfo, desiredRollDistance - 1f))
        {
            if (hitInfo.collider.gameObject.layer == 7)
            {
                currentHitTable = hitInfo.collider.gameObject.transform.parent.gameObject;

                if (currentHitTable != lastHitTable)
                {
                    // Disable outline on the last one
                    if (lastHitTable != null)
                    {
                        var lastOutline = lastHitTable.GetComponent<Outline>();
                        if (lastOutline != null) lastOutline.enabled = false;
                    }

                    // Enable outline on the current one
                    var outline = currentHitTable.GetComponent<Outline>();
                    if (outline != null) outline.enabled = true;

                    lastHitTable = currentHitTable;
                }
            }
        }
        else
        {
            // Nothing hit — disable previous outline
            if (lastHitTable != null)
            {
                var outline = lastHitTable.GetComponent<Outline>();
                if (outline != null) outline.enabled = false;
                lastHitTable = null;
            }
        }
    }

    private void ChangeState(PlayerState newState)
    {
        currentState = newState;
    }
}
