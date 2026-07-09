using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField, Min(0f)]
    private float moveSpeed = 5f;

    [SerializeField, Min(0f)]
    private float rotationSpeed = 12f;

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

        float horizontalInput = joystick.Horizontal;
        float verticalInput = joystick.Vertical;

        moveDirection = new Vector3(
            horizontalInput,
            0f,
            verticalInput
        );

        // Çapraz hareketin daha hızlı olmasını engeller.
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }
    }

    private void MovePlayer()
    {
        Vector3 horizontalVelocity = moveDirection * moveSpeed;

        rb.linearVelocity = new Vector3(
            horizontalVelocity.x,
            rb.linearVelocity.y,
            horizontalVelocity.z
        );
    }

    private void RotatePlayer()
    {
        if (moveDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(moveDirection, Vector3.up);

        Quaternion smoothRotation = Quaternion.Slerp(
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
    }
}