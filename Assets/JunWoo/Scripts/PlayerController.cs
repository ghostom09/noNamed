using UnityEngine;
using UnityEngine.InputSystem;

namespace JunWoo
{
    
    public class PlayerController : MonoBehaviour
    {
        private Rigidbody2D _rb;
        [SerializeField] private float moveSpeed = 5f;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        void Update()
        {
            var x = 0f;
            var y = 0f;

            if (Keyboard.current.aKey.isPressed) x = -1f;
            else if (Keyboard.current.dKey.isPressed) x = 1f;
            if (Keyboard.current.wKey.isPressed) y = 1f;
            else if (Keyboard.current.sKey.isPressed) y = -1f;

            _rb.linearVelocity = new Vector2(x, y) * moveSpeed;
        }
    }
}
