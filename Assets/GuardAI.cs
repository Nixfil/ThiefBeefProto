using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GuardAI : MonoBehaviour
{
    public enum GuardState
    {
        Patrol,
        ChasePlayer,
        Reseting
    }

    public GuardState currentState;

    public float PatrolSpeed;
    public float ChasePlayerSpeed;

    public Transform[] AssignedWayPoints;

    public PlayerDetectionScript PlayerDetectionScript;

    public GameObject Player;

    private void Update()
    {
        switch (currentState)
        {
            case GuardState.Patrol:
                
                break;
            case GuardState.ChasePlayer:
                Move(Player.transform);
                break;
            case GuardState.Reseting:
                break;
        }
    }

    public void ChangeState(GuardState state) 
    {
        currentState = state;
    }

    public void Move(Transform TargetTransform)
    {
        Vector3 direction = (TargetTransform.position - transform.position).normalized;
        if(currentState == GuardState.Patrol) transform.position += direction * PatrolSpeed * Time.deltaTime;
        else if (currentState == GuardState.ChasePlayer) transform.position += direction * ChasePlayerSpeed * Time.deltaTime;
    }
}
