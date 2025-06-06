using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    public enum PlayerState
    {
        Idle,
        Running,
        Rolling,
    }
    public DetectorScript detectorScript;
    public ThrowController throwController;
    public ShootController shootController;

    public float speed; // Speed of the character
    public float rollDuration; // Total duration of the roll
    public float desiredRollDistance;
    public float rotationSpeed;
    public LayerMask mask;
    public bool roofed;
    public Vector3 move;
    public float normalHeight = 2.0f;
    public float crouchHeight = 0.5f;


    [SerializeField] private PlayerState currentState;
    [SerializeField] private CapsuleCollider capsuleCollider;

    private Rigidbody rb;
    private float rollTime;
    private float rollAcceleration;
    private float rollSpeed;
    private float currentRollSpeed;
    private Vector3 rollDirection;
    private GameObject lastHitTable = null;
    private Vector3 aimDirection;
    private double originalCenterY;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // Disable gravity
        originalCenterY = capsuleCollider.center.y;
        currentState = PlayerState.Idle; // Start in Idle state
    }

    void Update()
    {
        // Get input for movement
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
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    StartRoll(transform.forward);
                }

                if (Physics.Raycast(transform.position, transform.forward, out RaycastHit standingHitInfo, desiredRollDistance - 1f))
                {
                    if (standingHitInfo.collider.gameObject.layer == 7)
                    {
                        standingHitInfo.collider.gameObject.GetComponentInParent<Outline>().enabled = true;
                        lastHitTable = standingHitInfo.collider.gameObject.transform.parent.gameObject;
                    }
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

                if (Physics.Raycast(transform.position, move, out RaycastHit hitInfo, desiredRollDistance - 1f))
                {
                    if (hitInfo.collider.gameObject.layer == 7)
                    {
                        hitInfo.collider.gameObject.GetComponentInParent<Outline>().enabled = true;
                        lastHitTable = hitInfo.collider.gameObject.transform.parent.gameObject;
                    }
                    else if (lastHitTable != null)
                    {
                        lastHitTable.GetComponent<Outline>().enabled = false;
                    }
                }
                else if (lastHitTable != null)
                {
                    lastHitTable.GetComponent<Outline>().enabled = false;
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
        rollDirection = moveDirection.normalized;

        // Calculate the required roll speed to cover the desired distance
        rollSpeed = desiredRollDistance / rollDuration; // Speed needed to cover the desired distance

        currentRollSpeed = rollSpeed; // Start the roll with the calculated speed
        rollAcceleration = rollSpeed * 2;
    }

    private void Roll()
    {
        capsuleCollider.height = 0.1f;
        rollTime += Time.deltaTime;

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
        }
        else if (currentRollSpeed < 10) { currentRollSpeed = 18; }
        Debug.Log(capsuleCollider.height);
    }


    private void ChangeState(PlayerState newState)
    {
        currentState = newState;
    }
    private Vector3 GetMouseWorldPosition()
    {
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (groundPlane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return Vector3.zero;
    }
    private Vector3 GetClampedTarget(Vector3 start, Vector3 target, float maxRange)
    {
        Vector3 direction = target - start;
        float distance = direction.magnitude;

        if (distance > maxRange)
            return start + direction.normalized * maxRange;
        else
            return target;
    }
    bool TryGetMouseWorldPosition(out Vector3 worldPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
        {
            worldPosition = hitInfo.point;
            return true;
        }

        worldPosition = Vector3.zero;
        return false;
    }

    public void RotatePlayerOverTime(GameObject ObjectToLookAt, float RotationSpeed)
    {
            Vector3 direction = ObjectToLookAt.transform.position - transform.position;
            Vector3 LeveledDirection = new Vector3(direction.x, 0, direction.z);
            Quaternion targetRotation = Quaternion.LookRotation(LeveledDirection.normalized);


            rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
