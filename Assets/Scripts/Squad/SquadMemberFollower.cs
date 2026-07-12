using UnityEngine;

public class SquadMemberFollower : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField, Min(0.01f)]
    private float positionSmoothTime = 0.12f;

    [SerializeField, Min(0f)]
    private float maximumFollowSpeed = 12f;

    [SerializeField, Min(0f)]
    private float rotationSpeed = 12f;

    [SerializeField, Min(0f)]
    private float stoppingDistance = 0.05f;

    private Transform leader;
    private Vector3 formationOffset;
    private Vector3 currentVelocity;

    private bool isInitialized;

    public void Initialize(
        Transform newLeader,
        Vector3 newFormationOffset)
    {
        leader = newLeader;
        formationOffset = newFormationOffset;
        isInitialized = true;
    }

    public void SetFormationOffset(Vector3 newFormationOffset)
    {
        formationOffset = newFormationOffset;
    }

    private void LateUpdate()
    {
        if (!isInitialized || leader == null)
        {
            return;
        }

        FollowFormationPosition();
    }

    private void FollowFormationPosition()
    {
        // Formasyon konumunu oyuncunun baktığı yöne göre döndürür.
        Vector3 rotatedOffset =
            leader.TransformDirection(formationOffset);

        // Oyuncunun dünya konumuna formasyon uzaklığını ekler.
        Vector3 desiredPosition =
            leader.position + rotatedOffset;

        // Takım üyelerini oyuncuyla aynı yükseklikte tutar.
        desiredPosition.y = leader.position.y;

        Vector3 previousPosition = transform.position;

        float squaredDistance =
            (desiredPosition - previousPosition).sqrMagnitude;

        float squaredStoppingDistance =
            stoppingDistance * stoppingDistance;

        if (squaredDistance <= squaredStoppingDistance)
        {
            transform.position = desiredPosition;
            currentVelocity = Vector3.zero;
            return;
        }

        Vector3 nextPosition = Vector3.SmoothDamp(
            previousPosition,
            desiredPosition,
            ref currentVelocity,
            positionSmoothTime,
            maximumFollowSpeed,
            Time.deltaTime
        );

        transform.position = nextPosition;

        RotateTowardsMovement(
            nextPosition - previousPosition
        );
    }

    private void RotateTowardsMovement(Vector3 movementDirection)
    {
        movementDirection.y = 0f;

        if (movementDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                movementDirection,
                Vector3.up
            );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void OnValidate()
    {
        positionSmoothTime =
            Mathf.Max(0.01f, positionSmoothTime);

        maximumFollowSpeed =
            Mathf.Max(0f, maximumFollowSpeed);

        rotationSpeed =
            Mathf.Max(0f, rotationSpeed);

        stoppingDistance =
            Mathf.Max(0f, stoppingDistance);
    }
}