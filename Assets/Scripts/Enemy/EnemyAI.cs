using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target")]

    [Tooltip("Düşmanın takip edeceği oyuncu.")]
    [SerializeField]
    private Transform target;


    [Header("Stopping Distance")]

    [Tooltip("Düşmanın oyuncuya yaklaşabileceği en yakın mesafe.")]
    [SerializeField, Min(0.5f)]
    private float minimumStoppingDistance = 3.5f;

    [Tooltip("Düşmanın oyuncudan durabileceği en uzak mesafe.")]
    [SerializeField, Min(0.5f)]
    private float maximumStoppingDistance = 4.5f;

    [Tooltip("Düşman durduktan sonra tekrar yürümeye başlaması için gereken ek mesafe.")]
    [SerializeField, Min(0f)]
    private float resumeDistanceBuffer = 0.5f;


    [Header("Path Settings")]

    [Tooltip("Düşmanın oyuncunun konumunu kaç saniyede bir güncelleyeceği.")]
    [SerializeField, Min(0.02f)]
    private float pathRefreshInterval = 0.15f;

    [Tooltip("Oyuncu bu mesafeden fazla hareket ederse yol yeniden hesaplanır.")]
    [SerializeField, Min(0f)]
    private float targetPositionThreshold = 0.1f;


    [Header("Pool Settings")]

    [Tooltip("Düşman oyuncudan bu mesafeden fazla uzaklaşırsa havuza döner.")]
    [SerializeField, Min(1f)]
    private float maximumDistanceFromTarget = 30f;


    private NavMeshAgent agent;
    private EnemyHealth enemyHealth;

    private Action<EnemyAI> releaseAction;

    private float currentStoppingDistance;
    private float nextPathRefreshTime;

    private Vector3 lastRequestedPosition;

    private bool hasRequestedPath;
    private bool isSpawned;
    private bool isHoldingPosition;


    public bool IsSpawned => isSpawned;

    public float CurrentStoppingDistance =>
        currentStoppingDistance;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();

        ApplyAgentSettings();
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

        if (!agent.enabled)
        {
            return;
        }

        if (!agent.isOnNavMesh)
        {
            return;
        }

        if (IsTooFarFromTarget())
        {
            ReleaseToPool();
            return;
        }

        UpdateMovement();
    }


    /// <summary>
    /// EnemyPool tarafından çağrılır.
    /// Düşmanı verilen konumda etkinleştirir.
    /// </summary>
    public void Activate(
        Vector3 spawnPosition,
        Transform newTarget)
    {
        target = newTarget;

        isSpawned = true;
        isHoldingPosition = false;
        hasRequestedPath = false;

        nextPathRefreshTime = 0f;

        lastRequestedPosition =
            Vector3.positiveInfinity;

        transform.position = spawnPosition;

        gameObject.SetActive(true);

        /*
         * Her düşmana biraz farklı durma mesafesi verir.
         * Böylece tüm düşmanlar tek çizgi üzerinde toplanmaz.
         */
        currentStoppingDistance =
            UnityEngine.Random.Range(
                minimumStoppingDistance,
                maximumStoppingDistance
            );

        ApplyAgentSettings();

        /*
         * Havuzdan tekrar çıkan düşmanın canını yeniler.
         */
        if (enemyHealth != null)
        {
            enemyHealth.ResetHealth();
        }

        /*
         * Spawn noktası NavMesh üzerinde değilse yakınındaki
         * geçerli NavMesh noktasını bulmayı dener.
         */
        if (!agent.isOnNavMesh)
        {
            bool navMeshPointFound =
                NavMesh.SamplePosition(
                    spawnPosition,
                    out NavMeshHit hit,
                    2f,
                    NavMesh.AllAreas
                );

            if (navMeshPointFound)
            {
                agent.Warp(hit.position);
            }
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning(
                $"{gameObject.name} NavMesh üzerine yerleştirilemedi.",
                this
            );

            ReleaseToPool();
            return;
        }

        agent.isStopped = false;
        agent.ResetPath();

        /*
         * Düşmanlara farklı kaçınma öncelikleri verir.
         * Aynı değere sahip olmaları üst üste binmeyi artırabilir.
         */
        agent.avoidancePriority =
            UnityEngine.Random.Range(30, 70);

        RotateTowardsTarget();
        UpdateDestinationImmediately();
    }


    /// <summary>
    /// EnemyPool, düşmanın havuza dönüş metodunu buradan verir.
    /// </summary>
    public void SetReleaseAction(
        Action<EnemyAI> newReleaseAction)
    {
        releaseAction = newReleaseAction;
    }


    /// <summary>
    /// Düşmanın oyuncuya olan uzaklığına göre
    /// hareket edip etmeyeceğini belirler.
    /// </summary>
    private void UpdateMovement()
    {
        Vector3 directionToTarget =
            target.position - transform.position;

        directionToTarget.y = 0f;

        float distanceSquared =
            directionToTarget.sqrMagnitude;

        float stoppingDistanceSquared =
            currentStoppingDistance *
            currentStoppingDistance;

        float resumeDistance =
            currentStoppingDistance +
            resumeDistanceBuffer;

        float resumeDistanceSquared =
            resumeDistance * resumeDistance;


        /*
         * Düşman hareket ediyorsa ve durma mesafesine ulaştıysa dur.
         */
        if (!isHoldingPosition &&
            distanceSquared <= stoppingDistanceSquared)
        {
            HoldPosition();
            return;
        }

        /*
         * Düşman durmuşsa, oyuncu yeterince uzaklaşmadan
         * tekrar yürümeye başlamaz.
         */
        if (isHoldingPosition)
        {
            if (distanceSquared >= resumeDistanceSquared)
            {
                ResumeMovement();
            }
            else
            {
                return;
            }
        }

        UpdateDestination();
    }


    /// <summary>
    /// Düşmanı bulunduğu noktada durdurur.
    /// </summary>
    private void HoldPosition()
    {
        if (isHoldingPosition)
        {
            return;
        }

        isHoldingPosition = true;

        agent.isStopped = true;

        if (agent.hasPath)
        {
            agent.ResetPath();
        }

        agent.velocity = Vector3.zero;
    }


    /// <summary>
    /// Oyuncu uzaklaşınca düşmanı tekrar harekete geçirir.
    /// </summary>
    private void ResumeMovement()
    {
        isHoldingPosition = false;

        agent.isStopped = false;

        hasRequestedPath = false;
        nextPathRefreshTime = 0f;
    }


    /// <summary>
    /// Belirli aralıklarla oyuncunun konumuna yeni yol hesaplar.
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

        float thresholdSquared =
            targetPositionThreshold *
            targetPositionThreshold;

        bool targetMovedEnough =
            targetMovement.sqrMagnitude >=
            thresholdSquared;

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


    /// <summary>
    /// Hedef konumunu NavMeshAgent'a hemen gönderir.
    /// </summary>
    private void UpdateDestinationImmediately()
    {
        if (target == null)
        {
            return;
        }

        if (!agent.enabled ||
            !agent.isOnNavMesh)
        {
            return;
        }

        bool destinationAccepted =
            agent.SetDestination(
                target.position
            );

        if (!destinationAccepted)
        {
            return;
        }

        lastRequestedPosition =
            target.position;

        hasRequestedPath = true;
    }


    /// <summary>
    /// Düşmanın oyuncudan aşırı uzaklaşıp uzaklaşmadığını kontrol eder.
    /// </summary>
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


    /// <summary>
    /// Düşman oluşturulduğunda yüzünü oyuncuya çevirir.
    /// </summary>
    private void RotateTowardsTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 directionToTarget =
            target.position - transform.position;

        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude < 0.001f)
        {
            return;
        }

        transform.rotation =
            Quaternion.LookRotation(
                directionToTarget,
                Vector3.up
            );
    }


    /// <summary>
    /// Düşmanı yok etmek yerine EnemyPool'a geri gönderir.
    /// </summary>
    public void ReleaseToPool()
    {
        if (!isSpawned)
        {
            return;
        }

        isSpawned = false;
        isHoldingPosition = false;
        hasRequestedPath = false;

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        target = null;

        if (releaseAction != null)
        {
            releaseAction.Invoke(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }


    /// <summary>
    /// Daha sonra hedef değiştirmek gerekirse kullanılabilir.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        hasRequestedPath = false;
        nextPathRefreshTime = 0f;
    }


    /// <summary>
    /// NavMeshAgent ayarlarını uygular.
    /// </summary>
    private void ApplyAgentSettings()
    {
        if (agent == null)
        {
            return;
        }

        agent.stoppingDistance =
            currentStoppingDistance > 0f
                ? currentStoppingDistance
                : minimumStoppingDistance;

        agent.autoBraking = true;

        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType
                .HighQualityObstacleAvoidance;
    }


    private void OnValidate()
    {
        minimumStoppingDistance =
            Mathf.Max(
                0.5f,
                minimumStoppingDistance
            );

        maximumStoppingDistance =
            Mathf.Max(
                minimumStoppingDistance,
                maximumStoppingDistance
            );

        resumeDistanceBuffer =
            Mathf.Max(
                0f,
                resumeDistanceBuffer
            );

        pathRefreshInterval =
            Mathf.Max(
                0.02f,
                pathRefreshInterval
            );

        targetPositionThreshold =
            Mathf.Max(
                0f,
                targetPositionThreshold
            );

        maximumDistanceFromTarget =
            Mathf.Max(
                maximumStoppingDistance + 1f,
                maximumDistanceFromTarget
            );

        if (agent == null)
        {
            agent =
                GetComponent<NavMeshAgent>();
        }

        ApplyAgentSettings();
    }
}