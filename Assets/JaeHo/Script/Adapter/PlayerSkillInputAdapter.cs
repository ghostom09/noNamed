using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerSkillInputAdapter : MonoBehaviour
{
    [Header("Skill Targets")]
    [SerializeField] private SkillSwitcher skillSwitcher;
    [SerializeField] private FirePointRotator firePointRotator;

    [Header("Player Input")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string attackActionName = "Attack";
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string nextSkillActionName = "NextSkill";
    [SerializeField] private string previousSkillActionName = "PreviousSkill";

    private InputAction _attackAction;
    private InputAction _lookAction;
    private InputAction _nextSkillAction;
    private InputAction _previousSkillAction;

    private void Awake()
    {
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (skillSwitcher == null)
            skillSwitcher = GetComponentInChildren<SkillSwitcher>();

        if (firePointRotator == null)
            firePointRotator = GetComponentInChildren<FirePointRotator>();
    }

    private void OnEnable()
    {
        BindInputActions();
    }

    private void OnDisable()
    {
        UnbindInputActions();
        skillSwitcher?.SetAttackHeld(false);
    }

    private void BindInputActions()
    {
        if (playerInput == null || playerInput.actions == null) return;

        _attackAction = FindAction(attackActionName);
        _lookAction = FindAction(lookActionName);
        _nextSkillAction = FindAction(nextSkillActionName);
        _previousSkillAction = FindAction(previousSkillActionName);

        if (_attackAction != null)
        {
            _attackAction.started += OnAttackStarted;
            _attackAction.canceled += OnAttackCanceled;
        }

        if (_lookAction != null)
            _lookAction.performed += OnLookPerformed;

        if (_nextSkillAction != null)
            _nextSkillAction.performed += OnNextSkillPerformed;

        if (_previousSkillAction != null)
            _previousSkillAction.performed += OnPreviousSkillPerformed;
    }

    private void UnbindInputActions()
    {
        if (_attackAction != null)
        {
            _attackAction.started -= OnAttackStarted;
            _attackAction.canceled -= OnAttackCanceled;
        }

        if (_lookAction != null)
            _lookAction.performed -= OnLookPerformed;

        if (_nextSkillAction != null)
            _nextSkillAction.performed -= OnNextSkillPerformed;

        if (_previousSkillAction != null)
            _previousSkillAction.performed -= OnPreviousSkillPerformed;

        _attackAction = null;
        _lookAction = null;
        _nextSkillAction = null;
        _previousSkillAction = null;
    }

    private InputAction FindAction(string actionName)
    {
        if (string.IsNullOrWhiteSpace(actionName)) return null;
        return playerInput.actions.FindAction(actionName, false);
    }

    private void OnAttackStarted(InputAction.CallbackContext context)
    {
        skillSwitcher?.SetAttackHeld(true);
    }

    private void OnAttackCanceled(InputAction.CallbackContext context)
    {
        skillSwitcher?.SetAttackHeld(false);
    }

    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        firePointRotator?.SetLookScreenPosition(context.ReadValue<Vector2>());
    }

    private void OnNextSkillPerformed(InputAction.CallbackContext context)
    {
        skillSwitcher?.SwitchNext();
    }

    private void OnPreviousSkillPerformed(InputAction.CallbackContext context)
    {
        skillSwitcher?.SwitchPrevious();
    }
}
