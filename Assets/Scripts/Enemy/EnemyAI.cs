using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Düşmanın takip edeceği hedef. Boş bırakılırsa Player Tag'i aranır.")]
    [SerializeField]
    private Transform target;

    [Header("Path Settings")]
    [Tooltip("Düşmanın hedef yolunu kaç saniyede bir güncelleyeceği.")]
    [SerializeField, Min(0.02f)]
    private float pathRefreshInterval = 0.15f;

    [Tooltip("Hedef bu mesafeden fazla hareket ederse yol yeniden hesaplanır.")]
    [SerializeField, Min(0f)]
    private float targetPositionThreshold = 0.1f;

    private NavMeshAgent agent;

    private float nextPathRefreshTime;
    private Vector3 lastRequestedPosition;
    private bool hasRequestedPath;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        nextPathRefreshTime = 0f;
        hasRequestedPath = false;

        if (target == null)
        {
            FindPlayer();
        }
    }

    private void Update()
    {
        if (target == null)
        {
            FindPlayer();
            return;
        }

        // Agent kapalıysa veya NavMesh üzerinde değilse işlem yapma.
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            return;
        }

        CheckStoppingDistance();
        UpdateDestination();
    }

    /// <summary>
    /// Düşman oyuncuya yeterince yaklaşınca mevcut yolu temizler.
    /// </summary>
    private void CheckStoppingDistance()
    {
        Vector3 directionToTarget =
            target.position - transform.position;

        directionToTarget.y = 0f;

        float stoppingDistanceSquared =
            agent.stoppingDistance *
            agent.stoppingDistance;

        if (directionToTarget.sqrMagnitude <=
            stoppingDistanceSquared)
        {
            if (agent.hasPath)
            {
                agent.ResetPath();
            }
        }
    }

    /// <summary>
    /// Belirli aralıklarla oyuncunun konumuna yeni yol ister.
    /// </summary>
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

        bool targetMovedEnough =
            targetMovement.sqrMagnitude >=
            targetPositionThreshold *
            targetPositionThreshold;

        bool needsNewPath =
            !hasRequestedPath ||
            !agent.hasPath ||
            targetMovedEnough;

        if (!needsNewPath)
        {
            return;
        }

        bool destinationAccepted =
            agent.SetDestination(target.position);

        if (destinationAccepted)
        {
            lastRequestedPosition =
                target.position;

            hasRequestedPath = true;
        }
    }

    /// <summary>
    /// Player Tag'ine sahip objeyi bulur.
    /// </summary>
    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            return;
        }

        target = playerObject.transform;
    }

    /// <summary>
    /// Daha sonra farklı bir hedef vermek için kullanılabilir.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        hasRequestedPath = false;
        nextPathRefreshTime = 0f;
    }

    private void OnValidate()
    {
        pathRefreshInterval =
            Mathf.Max(0.02f, pathRefreshInterval);

        targetPositionThreshold =
            Mathf.Max(0f, targetPositionThreshold);
    }
}