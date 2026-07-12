using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 5f;

    [SerializeField, Min(0f)]
    private float rotationSpeed = 12f;

    [SerializeField, Range(0f, 0.9f)]
    private float joystickDeadZone = 0.15f;

    [Header("References")]
    [SerializeField]
    private FloatingJoystick joystick;

    private Rigidbody rb;
    private Vector3 moveDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        ReadMovementInput();
    }

    private void FixedUpdate()
    {
        MovePlayer();
        RotatePlayer();
    }

    private void ReadMovementInput()
    {
        if (joystick == null)
        {
            moveDirection = Vector3.zero;
            return;
        }

        Vector2 joystickInput = new Vector2(
            joystick.Horizontal,
            joystick.Vertical
        );

        // Joystick merkezdeyken oluşabilecek küçük değerleri yok sayar.
        if (joystickInput.magnitude < joystickDeadZone)
        {
            joystickInput = Vector2.zero;
        }
        else
        {
            joystickInput = Vector2.ClampMagnitude(
                joystickInput,
                1f
            );
        }

        moveDirection = new Vector3(
            joystickInput.x,
            0f,
            joystickInput.y
        );
    }

    private void MovePlayer()
    {
        Vector3 horizontalVelocity =
            moveDirection * moveSpeed;

        rb.linearVelocity = new Vector3(
            horizontalVelocity.x,
            rb.linearVelocity.y,
            horizontalVelocity.z
        );
    }

    private void RotatePlayer()
    {
        // Çarpışmalardan veya önceki hareketten kalan dönüşü durdurur.
        rb.angularVelocity = Vector3.zero;

        // Joystick bırakıldıysa karakterin dönüşünü değiştirme.
        if (moveDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                moveDirection,
                Vector3.up
            );

        Quaternion smoothRotation =
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );

        rb.MoveRotation(smoothRotation);
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);

        joystickDeadZone = Mathf.Clamp(
            joystickDeadZone,
            0f,
            0.9f
        );
    }
}