using UnityEngine;

// Control modular de movimiento de Valentina (plataforma lateral o vista cenital).
// Momento 3: correr con Shift, pasos con sonido y animaciones disparadas por eventos
// (interactuar, éxito y fallo) a través del bus GameEvents.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private bool isTopDown = false; // Activar si tu juego es vista cenital

    [Header("Sonido de pasos")]
    [SerializeField] private float stepInterval = 0.32f;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    private bool facingRight = true;
    private bool running;
    private float stepTimer;

    // IDs de parámetros del Animator (se calculan una vez, no en cada Update)
    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int WalkSpeedId = Animator.StringToHash("WalkSpeed");
    private static readonly int InteractId = Animator.StringToHash("Interactuar");
    private static readonly int SuccessId = Animator.StringToHash("Exito");
    private static readonly int FailId = Animator.StringToHash("Fallo");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        GameInput.ResetState();
    }

    private void OnEnable()
    {
        GameEvents.OnInteract += PlayInteract;
        GameEvents.OnCorrectAnswer += PlaySuccess;
        GameEvents.OnLevelCompleted += PlaySuccessLevel;
        GameEvents.OnWrongAnswer += PlayFail;
        GameEvents.OnDefeat += PlayFail;
    }

    private void OnDisable()
    {
        GameEvents.OnInteract -= PlayInteract;
        GameEvents.OnCorrectAnswer -= PlaySuccess;
        GameEvents.OnLevelCompleted -= PlaySuccessLevel;
        GameEvents.OnWrongAnswer -= PlayFail;
        GameEvents.OnDefeat -= PlayFail;
    }

    private void PlayInteract() { if (animator != null) animator.SetTrigger(InteractId); }
    private void PlaySuccess() { if (animator != null) animator.SetTrigger(SuccessId); }
    private void PlaySuccessLevel(int _) => PlaySuccess();
    private void PlayFail() { if (animator != null) animator.SetTrigger(FailId); }

    private void Update()
    {
        // 1. Captura de entradas (Teclado: A/D, Flechas o Joystick) a través de GameInput
        float horizontal = GameInput.Horizontal;
        float vertical = isTopDown ? GameInput.Vertical : 0f;
        moveInput = new Vector2(horizontal, vertical).normalized;
        running = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // 2. Control de orientación visual (Flip del Sprite)
        if (horizontal > 0 && !facingRight) Flip();
        else if (horizontal < 0 && facingRight) Flip();

        // 3. Envío de datos al Animator (al correr, la animación de caminar va más rápido)
        float currentSpeed = moveInput.sqrMagnitude;
        if (animator != null)
        {
            animator.SetFloat(SpeedId, currentSpeed);
            animator.SetFloat(WalkSpeedId, running ? 1.6f : 1f);
        }

        // 4. Pasos
        if (currentSpeed > 0.01f)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                stepTimer = running ? stepInterval * 0.65f : stepInterval;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayStep();
            }
        }
        else stepTimer = 0f;
    }

    private void FixedUpdate()
    {
        // Movimiento por físicas en el ciclo FixedUpdate.
        // En Unity 6 la propiedad "velocity" se llama "linearVelocity".
        float speed = running ? runSpeed : moveSpeed;
        if (isTopDown)
        {
            rb.linearVelocity = moveInput * speed;
        }
        else
        {
            rb.linearVelocity = new Vector2(moveInput.x * speed, rb.linearVelocity.y);
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
