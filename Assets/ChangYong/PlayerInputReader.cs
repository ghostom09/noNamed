using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    private PlayerMove _playerMove;
    private PlayerAttack _playerAttack;
    private PlayerStatManager _statManager;
    [SerializeField] private PlayerStatPanel _statPanel;

    private void Start()
    {
        _playerMove = GetComponent<PlayerMove>();
        _playerAttack = GetComponent<PlayerAttack>();
        _statManager = GetComponent<PlayerStatManager>();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _playerMove.moveDirection = context.ReadValue<Vector2>().normalized;
    }
    
    public void OnDash(InputAction.CallbackContext context)
    {
        _playerMove.Dash();
    }

    public void OnAttack()
    {
        
    }
}
