using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField]
    private BulletProjectile bulletPrefab;

    [SerializeField]
    private Transform bulletsParent;

    [Header("Pool Settings")]
    [SerializeField, Min(0)]
    private int prewarmCount = 20;

    [SerializeField, Min(1)]
    private int defaultCapacity = 20;

    [SerializeField, Min(1)]
    private int maximumPoolSize = 100;

    private ObjectPool<BulletProjectile> bulletPool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "Sahnede birden fazla BulletPool bulundu!",
                this
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (bulletsParent == null)
        {
            bulletsParent = transform;
        }

        bulletPool = new ObjectPool<BulletProjectile>(
            createFunc: CreateBullet,
            actionOnGet: OnTakeFromPool,
            actionOnRelease: OnReturnedToPool,
            actionOnDestroy: OnDestroyPoolObject,
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize: maximumPoolSize
        );

        PrewarmPool();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private BulletProjectile CreateBullet()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError(
                "BulletPool: Bullet Prefab alanı boş!",
                this
            );

            return null;
        }

        BulletProjectile newBullet = Instantiate(
            bulletPrefab,
            bulletsParent
        );

        newBullet.name = bulletPrefab.name;

        newBullet.SetReleaseAction(
            ReleaseBullet
        );

        newBullet.gameObject.SetActive(false);

        return newBullet;
    }

    private void OnTakeFromPool(
        BulletProjectile bullet)
    {
        // Bullet, Launch metodu içinde aktif edilecek.
    }

    private void OnReturnedToPool(
        BulletProjectile bullet)
    {
        if (bullet == null)
        {
            return;
        }

        bullet.gameObject.SetActive(false);
    }

    private void OnDestroyPoolObject(
        BulletProjectile bullet)
    {
        if (bullet == null)
        {
            return;
        }

        Destroy(bullet.gameObject);
    }

    /// <summary>
    /// Havuzdan mermi alır ve ateşler.
    /// </summary>
    public BulletProjectile SpawnBullet(
        Vector3 spawnPosition,
        Quaternion spawnRotation,
        Vector3 direction,
        float speed,
        int damage,
        float lifetime)
    {
        if (bulletPool == null)
        {
            Debug.LogError(
                "BulletPool henüz hazırlanmadı.",
                this
            );

            return null;
        }

        BulletProjectile bullet = bulletPool.Get();

        if (bullet == null)
        {
            return null;
        }

        bullet.Launch(
            spawnPosition,
            spawnRotation,
            direction,
            speed,
            damage,
            lifetime
        );

        return bullet;
    }

    public void ReleaseBullet(
        BulletProjectile bullet)
    {
        if (bullet == null || bulletPool == null)
        {
            return;
        }

        bulletPool.Release(bullet);
    }

    private void PrewarmPool()
    {
        if (bulletPrefab == null ||
            prewarmCount <= 0)
        {
            return;
        }

        int amountToPrepare = Mathf.Min(
            prewarmCount,
            maximumPoolSize
        );

        List<BulletProjectile> preparedBullets =
            new List<BulletProjectile>(amountToPrepare);

        for (int i = 0; i < amountToPrepare; i++)
        {
            BulletProjectile bullet =
                bulletPool.Get();

            if (bullet != null)
            {
                preparedBullets.Add(bullet);
            }
        }

        for (int i = 0;
             i < preparedBullets.Count;
             i++)
        {
            bulletPool.Release(
                preparedBullets[i]
            );
        }
    }

    private void OnValidate()
    {
        defaultCapacity =
            Mathf.Max(1, defaultCapacity);

        maximumPoolSize = Mathf.Max(
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