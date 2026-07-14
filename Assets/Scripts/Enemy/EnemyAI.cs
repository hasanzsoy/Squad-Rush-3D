using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Düşmanın takip edeceği hedef.")]
    [SerializeField]
    private Transform target;

    [Header("Path Settings")]
    [Tooltip("Oyuncunun konumu kaç saniyede bir güncellenecek?")]
    [SerializeField, Min(0.02f)]
    private float pathRefreshInterval = 0.15f;

    [Tooltip("Oyuncu bu mesafeden fazla hareket ederse yol yenilenir.")]
    [SerializeField, Min(0f)]
    private float targetPositionThreshold = 0.1f;

    [Header("Pool Settings")]
    [Tooltip("Düşman oyuncudan fazla uzaklaşırsa havuza döner.")]
    [SerializeField, Min(1f)]
    private float maximumDistanceFromTarget = 30f;

    private NavMeshAgent agent;

    private Action<EnemyAI> releaseAction;

    private float nextPathRefreshTime;
    private Vector3 lastRequestedPosition;

    private bool hasRequestedPath;
    private bool isSpawned;

    public bool IsSpawned => isSpawned;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!isSpawned)
        {
            return;
        }

        if (target == null)
        {
            ReleaseToPool();
            return;
        }

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        if (IsTooFarFromTarget())
        {
            ReleaseToPool();
            return;
        }

        UpdateDestination();
    }

    /// <summary>
    /// EnemyPool tarafından çağrılır.
    /// Düşmanı verilen konumda aktif eder.
    /// </summary>
    public void Activate(
        Vector3 spawnPosition,
        Transform newTarget)
    {
        target = newTarget;

        nextPathRefreshTime = 0f;
        hasRequestedPath = false;
        lastRequestedPosition = Vector3.positiveInfinity;

        isSpawned = true;

        transform.position = spawnPosition;

        RotateTowardsTarget();

        gameObject.SetActive(true);

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            Debug.LogWarning(
                $"{name} NavMesh üzerine yerleştirilemedi.",
                this
            );

            ReleaseToPool();
            return;
        }

        agent.isStopped = false;
        agent.ResetPath();

        UpdateDestinationImmediately();
    }

    /// <summary>
    /// EnemyPool bu metotla düşmanın geri dönüş fonksiyonunu verir.
    /// </summary>
    public void SetReleaseAction(
        Action<EnemyAI> newReleaseAction)
    {
        releaseAction = newReleaseAction;
    }

    /// <summary>
    /// Düşmanı yok etmek yerine havuza gönderir.
    /// </summary>
    public void ReleaseToPool()
    {
        if (!isSpawned)
        {
            return;
        }

        isSpawned = false;

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        target = null;
        hasRequestedPath = false;

        if (releaseAction != null)
        {
            releaseAction.Invoke(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void UpdateDestination()
    {
        if (Time.time < nextPathRefreshTime)
        {
            return;
        }

        nextPathRefreshTime =
            Time.time + pathRefreshInterval;

        Vector3 targetMovement =
            target.position - lastRequestedPosition;

        float thresholdSquared =
            targetPositionThreshold *
            targetPositionThreshold;

        bool targetMovedEnough =
            targetMovement.sqrMagnitude >= thresholdSquared;

        bool needsNewPath =
            !hasRequestedPath ||
            !agent.hasPath ||
            targetMovedEnough;

        if (!needsNewPath)
        {
            return;
        }

        UpdateDestinationImmediately();
    }

    private void UpdateDestinationImmediately()
    {
        if (target == null ||
            !agent.enabled ||
            !agent.isOnNavMesh)
        {
            return;
        }

        bool destinationAccepted =
            agent.SetDestination(target.position);

        if (destinationAccepted)
        {
            lastRequestedPosition = target.position;
            hasRequestedPath = true;
        }
    }

    private bool IsTooFarFromTarget()
    {
        Vector3 directionToTarget =
            target.position - transform.position;

        directionToTarget.y = 0f;

        float maximumDistanceSquared =
            maximumDistanceFromTarget *
            maximumDistanceFromTarget;

        return directionToTarget.sqrMagnitude >
               maximumDistanceSquared;
    }

    private void RotateTowardsTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 direction =
            target.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(
            direction,
            Vector3.up
        );
    }

    private void OnValidate()
    {
        pathRefreshInterval =
            Mathf.Max(0.02f, pathRefreshInterval);

        targetPositionThreshold =
            Mathf.Max(0f, targetPositionThreshold);

        maximumDistanceFromTarget =
            Mathf.Max(1f, maximumDistanceFromTarget);
    }
}