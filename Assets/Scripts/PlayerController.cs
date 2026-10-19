using UnityEngine;

// Control modular de movimiento de Valentina (plataforma lateral o vista cenital).
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private bool isTopDown = false; // Activar si tu juego es vista cenital

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private bool facingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        GameInput.ResetState();
    }

    private void Update()
    {
        // 1. Captura de entradas (Teclado: A/D, Flechas o Joystick) a través de GameInput
        float horizontal = GameInput.Horizontal;
        float vertical = isTopDown ? GameInput.Vertical : 0f;
        moveInput = new Vector2(horizontal, vertical).normalized;

        // 2. Control de orientación visual (Flip del Sprite)
        if (horizontal > 0 && !facingRight) Flip();
        else if (horizontal < 0 && facingRight) Flip();

        // 3. Envío de datos al Animator
        float currentSpeed = moveInput.sqrMagnitude;
        if (animator != null)
        {
            animator.SetFloat("Speed", currentSpeed);
        }
    }

    private void FixedUpdate()
    {
        // Movimiento por físicas en el ciclo FixedUpdate.
        // En Unity 6 la propiedad "velocity" se llama "linearVelocity".
        if (isTopDown)
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
        else
        {
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        }
    }

    // Permite que otros scripts (por ejemplo el NPC) hagan que Valentina mire hacia un punto
    public void FaceTowards(float worldX)
    {
        bool shouldFaceRight = worldX > transform.position.x;
        if (shouldFaceRight != facingRight) Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
