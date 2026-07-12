using UnityEngine;

public class TopDownCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField]
    private Transform target;

    [Header("Camera Position")]
    [SerializeField]
    private Vector3 positionOffset = new Vector3(0f, 10f, -8f);

    [SerializeField]
    private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

    [Header("Smoothing")]
    [SerializeField, Min(0.01f)]
    private float positionSmoothTime = 0.15f;

    [SerializeField, Min(0f)]
    private float rotationSpeed = 10f;

    private Vector3 currentVelocity;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError(
                "TopDownCameraFollow: Kamera hedefi atanmadı!",
                this
            );

            return;
        }

        // Oyun başladığında kamerayı doğrudan doğru konuma yerleştirir.
        transform.position = target.position + positionOffset;

        LookAtTargetImmediately();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        FollowTarget();
        RotateTowardsTarget();
    }

    private void FollowTarget()
    {
        Vector3 desiredPosition =
            target.position + positionOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            positionSmoothTime
        );
    }

    private void RotateTowardsTarget()
    {
        Vector3 targetLookPosition =
            target.position + lookAtOffset;

        Vector3 lookDirection =
            targetLookPosition - transform.position;

        if (lookDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(
            lookDirection,
            Vector3.up
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void LookAtTargetImmediately()
    {
        Vector3 targetLookPosition =
            target.position + lookAtOffset;

        Vector3 lookDirection =
            targetLookPosition - transform.position;

        if (lookDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(
            lookDirection,
            Vector3.up
        );
    }

    private void OnValidate()
    {
        positionSmoothTime = Mathf.Max(
            0.01f,
            positionSmoothTime
        );

        rotationSpeed = Mathf.Max(
            0f,
            rotationSpeed
        );
    }
}