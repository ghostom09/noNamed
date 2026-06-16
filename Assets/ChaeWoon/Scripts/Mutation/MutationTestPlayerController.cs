using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class MutationTestPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rigid;
    private Vector2 movementInput;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        rigid.gravityScale = 0f;
        rigid.freezeRotation = true;
    }

    private void Update()
    {
        movementInput = ReadMovementInput();

        if (movementInput.sqrMagnitude > 1f)
        {
            movementInput.Normalize();
        }
    }

    private void FixedUpdate()
    {
        rigid.linearVelocity = Vector2.zero;

        if (movementInput == Vector2.zero)
        {
            return;
        }

        Vector3 delta = (Vector3)(movementInput * moveSpeed * Time.fixedDeltaTime);
        transform.position += delta;
        rigid.position = transform.position;
        Physics2D.SyncTransforms();
    }

    private void OnDisable()
    {
        if (rigid != null)
        {
            rigid.linearVelocity = Vector2.zero;
        }
    }

    private Vector2 ReadMovementInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            input.x -= 1f;
        }

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            input.x += 1f;
        }

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            input.y -= 1f;
        }

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            input.y += 1f;
        }

        return input;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
        return Vector2.zero;
#endif
    }
}
