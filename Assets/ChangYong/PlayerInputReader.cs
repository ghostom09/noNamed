using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    private PlayerMove _playerMove;
    private PlayerAttack _playerAttack;
    private PlayerStatManager _statManager;
    [SerializeField] private PlayerStatPanel _statPanel;

    private InputAction _moveAction;
    private InputAction _dashAction;
    private InputAction _attackAction;

    private void Awake()
    {
        _playerMove   = GetComponent<PlayerMove>();
        _playerAttack = GetComponent<PlayerAttack>();
        _statManager  = GetComponent<PlayerStatManager>();
    }

    private void Start()
    {
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput == null || playerInput.actions == null) return;

        _moveAction   = playerInput.actions["Player/Move"];
        _dashAction   = playerInput.actions["Player/Dash"];
        _attackAction = playerInput.actions["Player/Attack"];

        _dashAction.performed += _ => _playerMove.Dash();
    }

    private void OnDisable()
    {
        if (_dashAction != null)
            _dashAction.performed -= _ => _playerMove.Dash();
    }

    private void Update()
    {
        if (_moveAction == null) return;
        var raw = _moveAction.ReadValue<Vector2>();
        _playerMove.moveDirection = raw.normalized;
    }

    // SendMessages 호환 유지 (PlayerInput behavior가 SendMessages일 때도 동작)
    public void OnMove(InputValue value)
    {
        _playerMove.moveDirection = value.Get<Vector2>().normalized;
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed) _playerMove.Dash();
    }

    public void OnAttack(InputValue value) { }
}
