using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyPool))]
public class EnemySpawner : MonoBehaviour
{
    [Header("Player")]
    [SerializeField]
    private Transform player;

    [Header("Wave Settings")]
    [SerializeField, Min(1)]
    private int totalEnemiesToSpawn = 20;

    [SerializeField, Min(1)]
    private int maximumActiveEnemies = 6;

    [Header("Spawn Timing")]
    [SerializeField, Min(0f)]
    private float startingDelay = 1f;

    [SerializeField, Min(0.05f)]
    private float spawnInterval = 1.2f;

    [Tooltip("Bir düşman öldükten sonra yenisinin gelme süresi.")]
    [SerializeField, Min(0f)]
    private float replacementSpawnDelay = 0.75f;

    [Header("Spawn Area")]
    [Tooltip("Düşmanın Player'a en yakın doğma uzaklığı.")]
    [SerializeField, Min(1f)]
    private float minimumSpawnDistance = 9f;

    [Tooltip("Düşmanın Player'a en uzak doğma uzaklığı.")]
    [SerializeField, Min(1f)]
    private float maximumSpawnDistance = 12f;

    [Tooltip("Yeni doğma noktası önceki noktadan en az bu kadar uzak olsun.")]
    [SerializeField, Min(0f)]
    private float minimumDistanceFromLastSpawn = 4f;

    [Tooltip("Yeni düşman, mevcut düşmanların üzerine doğmasın.")]
    [SerializeField, Min(0f)]
    private float minimumDistanceFromOtherEnemies = 1.5f;

    [SerializeField]
    private LayerMask enemyLayer;

    [SerializeField, Min(0.1f)]
    private float navMeshSearchDistance = 2f;

    [SerializeField, Min(1)]
    private int spawnPositionAttempts = 25;

    private EnemyPool enemyPool;

    private float nextSpawnTime;

    private int spawnedEnemyCount;
    private int previousActiveEnemyCount;

    private bool isWaveActive;
    private bool waveCompleted;

    private bool hasLastSpawnPosition;
    private Vector3 lastSpawnPosition;

    private void Awake()
    {
        enemyPool = GetComponent<EnemyPool>();
    }

    private void Start()
    {
        FindPlayerIfNecessary();
        StartWave();
    }

    private void Update()
    {
        if (!isWaveActive)
        {
            return;
        }

        if (player == null)
        {
            FindPlayerIfNecessary();
            return;
        }

        DetectEnemyDeath();
        CheckWaveCompletion();

        if (waveCompleted)
        {
            return;
        }

        TrySpawnNextEnemy();
    }

    public void StartWave()
    {
        spawnedEnemyCount = 0;
        previousActiveEnemyCount = 0;

        isWaveActive = true;
        waveCompleted = false;

        hasLastSpawnPosition = false;

        nextSpawnTime =
            Time.time + startingDelay;
    }

    private void DetectEnemyDeath()
    {
        int currentActiveEnemyCount =
            enemyPool.ActiveEnemyCount;

        // Aktif sayı azaldıysa bir düşman havuza dönmüştür.
        if (currentActiveEnemyCount <
            previousActiveEnemyCount)
        {
            nextSpawnTime = Mathf.Max(
                nextSpawnTime,
                Time.time + replacementSpawnDelay
            );
        }

        previousActiveEnemyCount =
            currentActiveEnemyCount;
    }

    private void TrySpawnNextEnemy()
    {
        if (spawnedEnemyCount >=
            totalEnemiesToSpawn)
        {
            return;
        }

        if (Time.time < nextSpawnTime)
        {
            return;
        }

        if (enemyPool.ActiveEnemyCount >=
            maximumActiveEnemies)
        {
            return;
        }

        if (!TryGetSpawnPosition(
                out Vector3 spawnPosition))
        {
            nextSpawnTime =
                Time.time + 0.25f;

            return;
        }

        EnemyAI spawnedEnemy =
            enemyPool.SpawnEnemy(
                spawnPosition,
                player
            );

        if (spawnedEnemy == null)
        {
            return;
        }

        spawnedEnemyCount++;

        lastSpawnPosition =
            spawnPosition;

        hasLastSpawnPosition = true;

        previousActiveEnemyCount =
            enemyPool.ActiveEnemyCount;

        nextSpawnTime =
            Time.time + spawnInterval;
    }

    private bool TryGetSpawnPosition(
        out Vector3 spawnPosition)
    {
        for (int i = 0;
             i < spawnPositionAttempts;
             i++)
        {
            Vector2 randomCircle =
                Random.insideUnitCircle.normalized;

            float randomDistance =
                Random.Range(
                    minimumSpawnDistance,
                    maximumSpawnDistance
                );

            Vector3 candidatePosition =
                player.position +
                new Vector3(
                    randomCircle.x,
                    0f,
                    randomCircle.y
                ) * randomDistance;

            bool pointFound =
                NavMesh.SamplePosition(
                    candidatePosition,
                    out NavMeshHit navMeshHit,
                    navMeshSearchDistance,
                    NavMesh.AllAreas
                );

            if (!pointFound)
            {
                continue;
            }

            Vector3 validPosition =
                navMeshHit.position;

            Vector3 directionFromPlayer =
                validPosition - player.position;

            directionFromPlayer.y = 0f;

            // NavMesh araması noktayı Player'a fazla yaklaştırmışse reddet.
            if (directionFromPlayer.sqrMagnitude <
                minimumSpawnDistance *
                minimumSpawnDistance)
            {
                continue;
            }

            if (hasLastSpawnPosition)
            {
                Vector3 directionFromLastSpawn =
                    validPosition -
                    lastSpawnPosition;

                directionFromLastSpawn.y = 0f;

                float minimumLastDistanceSquared =
                    minimumDistanceFromLastSpawn *
                    minimumDistanceFromLastSpawn;

                if (directionFromLastSpawn.sqrMagnitude <
                    minimumLastDistanceSquared)
                {
                    continue;
                }
            }

            bool enemyAlreadyNearby =
                Physics.CheckSphere(
                    validPosition,
                    minimumDistanceFromOtherEnemies,
                    enemyLayer,
                    QueryTriggerInteraction.Ignore
                );

            if (enemyAlreadyNearby)
            {
                continue;
            }

            spawnPosition = validPosition;
            return true;
        }

        spawnPosition = Vector3.zero;

        Debug.LogWarning(
            "EnemySpawner: Uygun uzak doğma noktası bulunamadı.",
            this
        );

        return false;
    }

    private void CheckWaveCompletion()
    {
        bool allEnemiesSpawned =
            spawnedEnemyCount >= totalEnemiesToSpawn;

        if (!allEnemiesSpawned)
        {
            return;
        }

        if (enemyPool.ActiveEnemyCount > 0)
        {
            return;
        }

        waveCompleted = true;
        isWaveActive = false;

        Debug.Log(
            "Bütün düşmanlar öldü. Oda tamamlandı!",
            this
        );
    }

    private void FindPlayerIfNecessary()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (playerObject != null)
        {
            player =
                playerObject.transform;
        }
    }

    private void OnValidate()
    {
        totalEnemiesToSpawn =
            Mathf.Max(1, totalEnemiesToSpawn);

        maximumActiveEnemies = Mathf.Clamp(
            maximumActiveEnemies,
            1,
            totalEnemiesToSpawn
        );

        startingDelay =
            Mathf.Max(0f, startingDelay);

        spawnInterval =
            Mathf.Max(0.05f, spawnInterval);

        replacementSpawnDelay =
            Mathf.Max(0f, replacementSpawnDelay);

        minimumSpawnDistance =
            Mathf.Max(1f, minimumSpawnDistance);

        maximumSpawnDistance = Mathf.Max(
            minimumSpawnDistance,
            maximumSpawnDistance
        );

        minimumDistanceFromLastSpawn =
            Mathf.Max(
                0f,
                minimumDistanceFromLastSpawn
            );

        minimumDistanceFromOtherEnemies =
            Mathf.Max(
                0f,
                minimumDistanceFromOtherEnemies
            );

        navMeshSearchDistance =
            Mathf.Max(
                0.1f,
                navMeshSearchDistance
            );

        spawnPositionAttempts =
            Mathf.Max(
                1,
                spawnPositionAttempts
            );
    }
}