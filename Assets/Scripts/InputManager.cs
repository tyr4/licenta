using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Serialization;

public class InputManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset playerInput;
    [SerializeField] private InputActionAsset uiInput;
    public static InputManager Instance;
    
    public static event Action<Vector2> OnClickEvent;
    public static event Action<Vector2> OnPlayerMoveEvent;
    // public event Action<InputAction.CallbackContext> OnMouseMoveEvent;

    private List<Action> _unsubscribe = new();
    
    private InputAction _moveAction;
    
    private Camera _mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _mainCamera = Camera.main;
    }

    private void Start()
    {
        WirePlayerEvents();
    }

    private void OnDestroy()
    {
        foreach (var undo in _unsubscribe) undo();
        _unsubscribe.Clear();
    }
    
    private InputAction Bind(InputActionMap map, string nameOrId, Action<InputAction.CallbackContext> handler, bool includeCanceled = true)
    {
        var action = map.FindAction(nameOrId);
        if (action == null)
        {
            Debug.LogError($"{nameOrId} action doesn't exist!");
            return null;
        }
        
        action.performed += handler;
        if (includeCanceled) action.canceled += handler;
        
        _unsubscribe.Add(() =>
        {
            action.performed -= handler;
            if (includeCanceled) action.canceled -= handler;
        });

        return action;
    }

    private void WirePlayerEvents()
    {
        var map = playerInput.FindActionMap("Player");
    
        _moveAction = Bind(map, "Move", OnPlayerMove);

        map.Enable();
    }

    private void OnPlayerMove(InputAction.CallbackContext ctx)
    {
        var moveInput = ctx.ReadValue<Vector2>();
        OnPlayerMoveEvent?.Invoke(moveInput);
    }

    public void OnClick(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        Vector2 worldPos = GetMouseWorldPosition();
        
        OnClickEvent?.Invoke(worldPos);
    }

    public Vector2 GetMouseWorldPosition()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 screenPos = new Vector3(mousePos.x, mousePos.y, -_mainCamera.transform.position.z);
        Vector3 world = _mainCamera.ScreenToWorldPoint(screenPos);
        Vector2 worldPos = new Vector2(world.x, world.y);

        return worldPos;
    }

    public Vector3 GetMouseScreenPosition()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 screenPos = new Vector3(mousePos.x, mousePos.y, -_mainCamera.transform.position.z);

        return screenPos;
    }
}