using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyPool))]
public class EnemySpawner : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Düşmanların çevresinde oluşacağı oyuncu.")]
    [SerializeField]
    private Transform player;

    [Header("Wave Settings")]
    [Tooltip("Bu odada toplam kaç düşman oluşturulacak?")]
    [SerializeField, Min(1)]
    private int totalEnemiesToSpawn = 20;

    [Tooltip("Aynı anda sahnede bulunabilecek maksimum düşman sayısı.")]
    [SerializeField, Min(1)]
    private int maximumActiveEnemies = 6;

    [Header("Spawn Timing")]
    [Tooltip("Oda başladıktan kaç saniye sonra ilk düşman oluşacak?")]
    [SerializeField, Min(0f)]
    private float startingDelay = 1f;

    [Tooltip("İki düşmanın oluşturulması arasındaki süre.")]
    [SerializeField, Min(0.05f)]
    private float spawnInterval = 1.2f;

    [Header("Spawn Area")]
    [Tooltip("Düşmanın oyuncuya en yakın oluşabileceği mesafe.")]
    [SerializeField, Min(1f)]
    private float minimumSpawnDistance = 7f;

    [Tooltip("Düşmanın oyuncuya en uzak oluşabileceği mesafe.")]
    [SerializeField, Min(1f)]
    private float maximumSpawnDistance = 10f;

    [Tooltip("Rastgele noktanın yakınında NavMesh arama mesafesi.")]
    [SerializeField, Min(0.1f)]
    private float navMeshSearchDistance = 2f;

    [Tooltip("Geçerli bir doğma noktası bulmak için yapılacak deneme sayısı.")]
    [SerializeField, Min(1)]
    private int spawnPositionAttempts = 15;

    private EnemyPool enemyPool;

    private float nextSpawnTime;
    private int spawnedEnemyCount;

    private bool isWaveActive;
    private bool waveCompleted;

    public int SpawnedEnemyCount => spawnedEnemyCount;

    public int ActiveEnemyCount =>
        enemyPool != null
            ? enemyPool.ActiveEnemyCount
            : 0;

    public int RemainingEnemiesToSpawn =>
        Mathf.Max(
            0,
            totalEnemiesToSpawn - spawnedEnemyCount
        );

    public bool IsWaveCompleted => waveCompleted;

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

        CheckWaveCompletion();

        if (waveCompleted)
        {
            return;
        }

        TrySpawnNextEnemy();
    }

    /// <summary>
    /// Yeni düşman dalgasını başlatır.
    /// </summary>
    public void StartWave()
    {
        spawnedEnemyCount = 0;

        isWaveActive = true;
        waveCompleted = false;

        nextSpawnTime =
            Time.time + startingDelay;

        Debug.Log(
            $"Dalga başladı. Toplam düşman: {totalEnemiesToSpawn}",
            this
        );
    }

    /// <summary>
    /// Belirlenen düşman sayısıyla yeni dalga başlatır.
    /// Daha sonra RoomManager tarafından kullanılabilir.
    /// </summary>
    public void StartWave(int enemyAmount)
    {
        totalEnemiesToSpawn =
            Mathf.Max(1, enemyAmount);

        StartWave();
    }

    private void TrySpawnNextEnemy()
    {
        // Bu oda için bütün düşmanlar oluşturulduysa yenisini üretme.
        if (spawnedEnemyCount >= totalEnemiesToSpawn)
        {
            return;
        }

        // Henüz doğma zamanı gelmediyse bekle.
        if (Time.time < nextSpawnTime)
        {
            return;
        }

        // Sahnedeki aktif düşman limiti doluysa bekle.
        if (enemyPool.ActiveEnemyCount >=
            maximumActiveEnemies)
        {
            return;
        }

        bool spawnSuccessful =
            TrySpawnEnemy();

        if (!spawnSuccessful)
        {
            // Geçerli nokta bulunamadığında kısa süre sonra tekrar dene.
            nextSpawnTime =
                Time.time + 0.25f;

            return;
        }

        spawnedEnemyCount++;

        nextSpawnTime =
            Time.time + spawnInterval;
    }

    private bool TrySpawnEnemy()
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

            return false;
        }

        EnemyAI spawnedEnemy =
            enemyPool.SpawnEnemy(
                spawnPosition,
                player
            );

        return spawnedEnemy != null;
    }

    private void CheckWaveCompletion()
    {
        // Önce bu oda için bütün düşmanların oluşturulması gerekir.
        bool allEnemiesSpawned =
            spawnedEnemyCount >= totalEnemiesToSpawn;

        if (!allEnemiesSpawned)
        {
            return;
        }

        // Oluşturulmuş düşmanlardan hâlâ hayatta olan varsa oda bitmez.
        bool hasActiveEnemies =
            enemyPool.ActiveEnemyCount > 0;

        if (hasActiveEnemies)
        {
            return;
        }

        CompleteWave();
    }

    private void CompleteWave()
    {
        if (waveCompleted)
        {
            return;
        }

        waveCompleted = true;
        isWaveActive = false;

        Debug.Log(
            "Odadaki bütün düşmanlar öldürüldü. Oda tamamlandı!",
            this
        );

        // Daha sonra burada RoomManager çağrılacak:
        // RoomManager.Instance.CompleteRoom();
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

            if (!navMeshPointFound)
            {
                continue;
            }

            spawnPosition = hit.position;
            return true;
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
        totalEnemiesToSpawn =
            Mathf.Max(1, totalEnemiesToSpawn);

        maximumActiveEnemies =
            Mathf.Clamp(
                maximumActiveEnemies,
                1,
                totalEnemiesToSpawn
            );

        startingDelay =
            Mathf.Max(0f, startingDelay);

        spawnInterval =
            Mathf.Max(0.05f, spawnInterval);

        minimumSpawnDistance =
            Mathf.Max(1f, minimumSpawnDistance);

        maximumSpawnDistance =
            Mathf.Max(
                minimumSpawnDistance,
                maximumSpawnDistance
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