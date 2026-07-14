using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemyPool : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Havuzda kullanılacak düşman prefabı.")]
    [SerializeField]
    private EnemyAI enemyPrefab;

    [Tooltip("Havuzdaki düşmanların tutulacağı ana obje.")]
    [SerializeField]
    private Transform poolParent;

    [Header("Pool Settings")]
    [Tooltip("Oyun başlarken hazırlanacak düşman sayısı.")]
    [SerializeField, Min(0)]
    private int prewarmCount = 12;

    [Tooltip("Havuzun başlangıç kapasitesi.")]
    [SerializeField, Min(1)]
    private int defaultCapacity = 12;

    [Tooltip("Havuzun saklayabileceği maksimum pasif düşman sayısı.")]
    [SerializeField, Min(1)]
    private int maximumPoolSize = 40;

    private ObjectPool<EnemyAI> enemyPool;

    private int activeEnemyCount;

    public int ActiveEnemyCount => activeEnemyCount;

    private void Awake()
    {
        if (poolParent == null)
        {
            poolParent = transform;
        }

        enemyPool = new ObjectPool<EnemyAI>(
            createFunc: CreateEnemy,
            actionOnGet: OnTakeFromPool,
            actionOnRelease: OnReturnedToPool,
            actionOnDestroy: OnDestroyPoolObject,
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maximumPoolSize
        );

        PrewarmPool();
    }

    /// <summary>
    /// Yeni bir düşman oluşturur.
    /// Bu metot yalnızca havuz boş olduğunda çalışır.
    /// </summary>
    private EnemyAI CreateEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError(
                "EnemyPool: Enemy Prefab alanı boş!",
                this
            );

            return null;
        }

        EnemyAI newEnemy = Instantiate(
            enemyPrefab,
            poolParent
        );

        newEnemy.name = enemyPrefab.name;

        newEnemy.SetReleaseAction(
            ReleaseEnemy
        );

        newEnemy.gameObject.SetActive(false);

        return newEnemy;
    }

    /// <summary>
    /// Düşman havuzdan çıkarıldığında çalışır.
    /// </summary>
    private void OnTakeFromPool(EnemyAI enemy)
    {
        if (enemy == null)
        {
            return;
        }

        activeEnemyCount++;
    }

    /// <summary>
    /// Düşman havuza geri gönderildiğinde çalışır.
    /// </summary>
    private void OnReturnedToPool(EnemyAI enemy)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.gameObject.SetActive(false);

        activeEnemyCount = Mathf.Max(
            0,
            activeEnemyCount - 1
        );
    }

    /// <summary>
    /// Havuz maksimum kapasiteyi aşarsa fazlalık objeyi siler.
    /// </summary>
    private void OnDestroyPoolObject(EnemyAI enemy)
    {
        if (enemy == null)
        {
            return;
        }

        Destroy(enemy.gameObject);
    }

    /// <summary>
    /// Havuzdan düşman alır ve sahneye yerleştirir.
    /// </summary>
    public EnemyAI SpawnEnemy(
        Vector3 spawnPosition,
        Transform target)
    {
        if (enemyPool == null)
        {
            Debug.LogError(
                "EnemyPool henüz oluşturulmadı.",
                this
            );

            return null;
        }

        EnemyAI enemy = enemyPool.Get();

        if (enemy == null)
        {
            return null;
        }

        enemy.Activate(
            spawnPosition,
            target
        );

        return enemy;
    }

    /// <summary>
    /// Düşmanı havuza geri gönderir.
    /// </summary>
    public void ReleaseEnemy(EnemyAI enemy)
    {
        if (enemy == null || enemyPool == null)
        {
            return;
        }

        enemyPool.Release(enemy);
    }

    /// <summary>
    /// Oyun başında belirli sayıda düşmanı önceden oluşturur.
    /// </summary>
    private void PrewarmPool()
    {
        if (enemyPrefab == null ||
            prewarmCount <= 0)
        {
            return;
        }

        int amountToPrepare = Mathf.Min(
            prewarmCount,
            maximumPoolSize
        );

        List<EnemyAI> preparedEnemies =
            new List<EnemyAI>(amountToPrepare);

        for (int i = 0; i < amountToPrepare; i++)
        {
            EnemyAI enemy = enemyPool.Get();

            if (enemy != null)
            {
                preparedEnemies.Add(enemy);
            }
        }

        for (int i = 0; i < preparedEnemies.Count; i++)
        {
            enemyPool.Release(
                preparedEnemies[i]
            );
        }
    }

    private void OnValidate()
    {
        defaultCapacity =
            Mathf.Max(1, defaultCapacity);

        maximumPoolSize =
            Mathf.Max(
                defaultCapacity,
                maximumPoolSize
            );

        prewarmCount = Mathf.Clamp(
            prewarmCount,
            0,
            maximumPoolSize
        );
    }
}