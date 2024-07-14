using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    private Vector2 _input;
    private CharacterController _characterController;
    //private Rigidbody _rb;

    private Vector3 _direction;
    public float speed;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        //_rb = GetComponent<Rigidbody>();
        //_rb.useGravity = false;
    }

    private void Update()
    {
        _characterController.Move(_direction * speed * Time.deltaTime);
    }

    public void Move(InputAction.CallbackContext context)
    {
        _input = context.ReadValue<Vector2>();
        _direction = new Vector3(_input.x, 0.0f, _input.y);
    }
}
