using System;
using System.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;

public class Player : MonoBehaviour
{
    [Header("Player stats")]
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float acceleration = 1.0f;
    [SerializeField] private float stationaryAcceleration = 0.2f;
    [SerializeField] private float turnDegreesPerSecond = 720f;
    [SerializeField] private float directionDebuffValue = 10;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float movementYOffset = -0.03f;

    [Header("Player objects")]
    [SerializeField] private Transform rotationObject;
    [SerializeField] private Transform flashlightObject;
    [SerializeField] private BoxCollider2D hitbox;
    
    private static readonly int AnimatorSpeed = Animator.StringToHash("speed");
    private static readonly int AnimatorDirX = Animator.StringToHash("dirX");
    private static readonly int AnimatorDirY = Animator.StringToHash("dirY");

    private float _animAngle = 0f;
    private float _targetAngle = 0f;
    
    private Rigidbody2D _rb;
    private Animator _animator;
    
    private Vector2 _currentInput;
    private bool _isMoving;
    private float _directionDebuffCalculated;

    private Vector2 _lastMove;
    private Vector2 _move;
    private Vector2 _animDir;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();
        
        _directionDebuffCalculated = movementSpeed / directionDebuffValue;
        
        InputManager.OnPlayerMoveEvent += OnMove;
    }

    private void OnDestroy()
    {
        InputManager.OnPlayerMoveEvent -= OnMove;
    }

    private void FixedUpdate()
    {
        _isMoving = _currentInput != Vector2.zero || _rb.linearVelocity.sqrMagnitude > 0.0001f;

        Vector2 targetVelocity;
        if (IsInView())
        {
            targetVelocity = _currentInput * movementSpeed;
        }
        else
        {
            targetVelocity = _currentInput * _directionDebuffCalculated;
        }
        
        _rb.linearVelocity = Vector2.MoveTowards(
            _rb.linearVelocity, 
            targetVelocity, 
            (_isMoving? acceleration : stationaryAcceleration) * Time.fixedDeltaTime
            );

        _animator.SetFloat(AnimatorSpeed, _rb.linearVelocity.sqrMagnitude);
    }

    private void Update()
    {
        // animation turning angle calculations
        if (Mathf.Abs(Mathf.DeltaAngle(_animAngle, _targetAngle)) < 0.01f)
            return;
        
        _animAngle = Mathf.MoveTowardsAngle(_animAngle, _targetAngle, turnDegreesPerSecond * Time.deltaTime);
        Debug.Log(_animAngle);
        RotateObjectsOnMove();
        
        float rad = _animAngle * Mathf.Deg2Rad;
        _animator.SetFloat(AnimatorDirX, Mathf.Sin(rad));
        _animator.SetFloat(AnimatorDirY, Mathf.Cos(rad));
    }
    
    private void OnMove(Vector2 input)
    {
        _currentInput = input;

        if (_currentInput != Vector2.zero)
        {
            _targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
        }
        else _targetAngle = _animAngle;
        
        Debug.Log(input);
    }

    private bool IsInView()
    {
        float diff = Mathf.DeltaAngle(_animAngle, _targetAngle);
        
        return Mathf.Abs(diff) < viewAngle;
    }

    private void RotateObjectsOnMove()
    {
        rotationObject.rotation = Quaternion.Euler(0, 0, -_animAngle);
    }
}
