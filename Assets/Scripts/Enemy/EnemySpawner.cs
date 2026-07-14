using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyPool))]
public class EnemySpawner : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Düşmanların çevresinde doğacağı oyuncu.")]
    [SerializeField]
    private Transform player;

    [Header("Spawn Timing")]
    [Tooltip("İlk düşmanın kaç saniye sonra doğacağı.")]
    [SerializeField, Min(0f)]
    private float startingDelay = 1f;

    [Tooltip("Düşmanlar kaç saniyede bir doğacak?")]
    [SerializeField, Min(0.05f)]
    private float spawnInterval = 1.2f;

    [Header("Enemy Limit")]
    [Tooltip("Aynı anda sahnede bulunabilecek maksimum düşman sayısı.")]
    [SerializeField, Min(1)]
    private int maximumActiveEnemies = 12;

    [Header("Spawn Area")]
    [Tooltip("Düşmanın oyuncuya en yakın doğabileceği mesafe.")]
    [SerializeField, Min(1f)]
    private float minimumSpawnDistance = 7f;

    [Tooltip("Düşmanın oyuncuya en uzak doğabileceği mesafe.")]
    [SerializeField, Min(1f)]
    private float maximumSpawnDistance = 10f;

    [Tooltip("Rastgele noktanın çevresinde NavMesh arama mesafesi.")]
    [SerializeField, Min(0.1f)]
    private float navMeshSearchDistance = 3f;

    [Tooltip("Geçerli doğma noktası bulmak için kaç kez denenecek?")]
    [SerializeField, Min(1)]
    private int spawnPositionAttempts = 10;

    private EnemyPool enemyPool;

    private float nextSpawnTime;

    private void Awake()
    {
        enemyPool = GetComponent<EnemyPool>();
    }

    private void Start()
    {
        FindPlayerIfNecessary();

        nextSpawnTime =
            Time.time + startingDelay;
    }

    private void Update()
    {
        if (player == null)
        {
            FindPlayerIfNecessary();
            return;
        }

        if (Time.time < nextSpawnTime)
        {
            return;
        }

        nextSpawnTime =
            Time.time + spawnInterval;

        if (enemyPool.ActiveEnemyCount >=
            maximumActiveEnemies)
        {
            return;
        }

        TrySpawnEnemy();
    }

    private void TrySpawnEnemy()
    {
        bool positionFound =
            TryGetSpawnPosition(
                out Vector3 spawnPosition
            );

        if (!positionFound)
        {
            Debug.LogWarning(
                "EnemySpawner: Geçerli NavMesh doğma noktası bulunamadı.",
                this
            );

            return;
        }

        enemyPool.SpawnEnemy(
            spawnPosition,
            player
        );
    }

    private bool TryGetSpawnPosition(
        out Vector3 spawnPosition)
    {
        for (int i = 0;
             i < spawnPositionAttempts;
             i++)
        {
            float randomAngle =
                Random.Range(0f, 360f);

            float angleInRadians =
                randomAngle * Mathf.Deg2Rad;

            Vector3 randomDirection =
                new Vector3(
                    Mathf.Cos(angleInRadians),
                    0f,
                    Mathf.Sin(angleInRadians)
                );

            float randomDistance =
                Random.Range(
                    minimumSpawnDistance,
                    maximumSpawnDistance
                );

            Vector3 candidatePosition =
                player.position +
                randomDirection * randomDistance;

            bool navMeshPointFound =
                NavMesh.SamplePosition(
                    candidatePosition,
                    out NavMeshHit hit,
                    navMeshSearchDistance,
                    NavMesh.AllAreas
                );

            if (navMeshPointFound)
            {
                spawnPosition = hit.position;
                return true;
            }
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    private void FindPlayerIfNecessary()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void OnValidate()
    {
        startingDelay =
            Mathf.Max(0f, startingDelay);

        spawnInterval =
            Mathf.Max(0.05f, spawnInterval);

        maximumActiveEnemies =
            Mathf.Max(1, maximumActiveEnemies);

        minimumSpawnDistance =
            Mathf.Max(1f, minimumSpawnDistance);

        maximumSpawnDistance =
            Mathf.Max(
                minimumSpawnDistance,
                maximumSpawnDistance
            );

        navMeshSearchDistance =
            Mathf.Max(0.1f, navMeshSearchDistance);

        spawnPositionAttempts =
            Mathf.Max(1, spawnPositionAttempts);
    }
}