using UnityEngine;

[DefaultExecutionOrder(100)]
public class AutoShooter : MonoBehaviour
{
    public enum TargetingMode
    {
        ForwardCone,
        FullCircle
    }

    [Header("References")]

    [Tooltip("Düşmana doğru dönecek silah veya karakter pivotu.")]
    [SerializeField]
    private Transform weaponPivot;

    [Tooltip("Merminin çıkacağı nokta.")]
    [SerializeField]
    private Transform firePoint;

    [Tooltip("Karakterin ön yönünü belirleyen obje.")]
    [SerializeField]
    private Transform facingReference;


    [Header("Targeting Mode")]

    [Tooltip("Player için Forward Cone, takım üyeleri için Full Circle seç.")]
    [SerializeField]
    private TargetingMode targetingMode =
        TargetingMode.ForwardCone;


    [Header("Target Settings")]

    [Tooltip("Hedef olarak algılanacak Enemy katmanı.")]
    [SerializeField]
    private LayerMask enemyLayer;

    [Tooltip("Düşmanların algılanacağı maksimum mesafe.")]
    [SerializeField, Min(0.1f)]
    private float fireRange = 6f;

    [Tooltip("Forward Cone modunda sağ ve sol hedefleme açısı.")]
    [SerializeField, Range(0f, 180f)]
    private float maximumAimAngle = 65f;

    [Tooltip("Geçerli hedef yokken kaç saniyede bir düşman aranacak?")]
    [SerializeField, Min(0.02f)]
    private float targetScanInterval = 0.15f;

    [Tooltip("Tek taramada kontrol edilebilecek maksimum collider sayısı.")]
    [SerializeField, Min(1)]
    private int maximumDetectedEnemies = 32;


    [Header("Weapon Settings")]

    [Tooltip("İki atış arasındaki bekleme süresi.")]
    [SerializeField, Min(0.05f)]
    private float fireInterval = 0.5f;

    [Tooltip("Bir merminin vereceği hasar.")]
    [SerializeField, Min(1)]
    private int bulletDamage = 1;

    [Tooltip("Merminin hareket hızı.")]
    [SerializeField, Min(0.1f)]
    private float bulletSpeed = 16f;

    [Tooltip("Merminin havuzda geri dönmeden önceki ömrü.")]
    [SerializeField, Min(0.1f)]
    private float bulletLifetime = 3f;

    [Tooltip("WeaponPivot veya CombatPivot dönüş hızı.")]
    [SerializeField, Min(0f)]
    private float aimRotationSpeed = 18f;

    [Tooltip("Silah hedefe bu açı kadar yaklaşınca ateş edebilir.")]
    [SerializeField, Range(0.1f, 45f)]
    private float fireAlignmentTolerance = 6f;

    [Tooltip("Hedef olmadığında pivotu başlangıç yönüne döndürür.")]
    [SerializeField]
    private bool returnForwardWhenNoTarget = true;


    private Collider[] detectionResults;

    private EnemyHealth currentTarget;

    private Quaternion defaultPivotLocalRotation;

    private float nextScanTime;
    private float nextFireTime;


    private void Awake()
    {
        if (facingReference == null)
        {
            facingReference = transform;
        }

        if (weaponPivot != null)
        {
            defaultPivotLocalRotation =
                weaponPivot.localRotation;
        }

        detectionResults =
            new Collider[Mathf.Max(1, maximumDetectedEnemies)];
    }


    private void LateUpdate()
    {
        UpdateTarget();

        if (!IsCurrentTargetValid())
        {
            currentTarget = null;

            ReturnPivotForward();

            return;
        }

        RotatePivotTowardsTarget();
        TryShoot();
    }


    /// <summary>
    /// Geçerli hedef varsa onu korur.
    /// Hedef yoksa belirli aralıklarla yeni düşman arar.
    /// </summary>
    private void UpdateTarget()
    {
        if (IsCurrentTargetValid())
        {
            return;
        }

        currentTarget = null;

        if (Time.time < nextScanTime)
        {
            return;
        }

        nextScanTime =
            Time.time + targetScanInterval;

        FindClosestEnemy();
    }


    /// <summary>
    /// Menzildeki uygun düşmanlar arasından en yakını seçer.
    /// </summary>
    private void FindClosestEnemy()
    {
        int detectedCount =
            Physics.OverlapSphereNonAlloc(
                transform.position,
                fireRange,
                detectionResults,
                enemyLayer,
                QueryTriggerInteraction.Ignore
            );

        EnemyHealth closestEnemy = null;

        float closestDistanceSquared =
            float.PositiveInfinity;

        for (int i = 0; i < detectedCount; i++)
        {
            Collider detectedCollider =
                detectionResults[i];

            if (detectedCollider == null)
            {
                continue;
            }

            EnemyHealth enemyHealth =
                detectedCollider.GetComponentInParent<EnemyHealth>();

            if (enemyHealth == null)
            {
                continue;
            }

            if (!enemyHealth.IsAlive)
            {
                continue;
            }

            if (!enemyHealth.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!CanTargetPosition(enemyHealth.AimPosition))
            {
                continue;
            }

            Vector3 directionToEnemy =
                enemyHealth.AimPosition -
                transform.position;

            directionToEnemy.y = 0f;

            float distanceSquared =
                directionToEnemy.sqrMagnitude;

            if (distanceSquared >= closestDistanceSquared)
            {
                continue;
            }

            closestDistanceSquared =
                distanceSquared;

            closestEnemy =
                enemyHealth;
        }

        currentTarget = closestEnemy;
    }


    /// <summary>
    /// Mevcut hedefin hâlâ canlı, aktif,
    /// menzilde ve hedefleme alanında olup olmadığını kontrol eder.
    /// </summary>
    private bool IsCurrentTargetValid()
    {
        if (currentTarget == null)
        {
            return false;
        }

        if (!currentTarget.IsAlive)
        {
            return false;
        }

        if (!currentTarget.gameObject.activeInHierarchy)
        {
            return false;
        }

        Vector3 directionToTarget =
            currentTarget.AimPosition -
            transform.position;

        directionToTarget.y = 0f;

        float fireRangeSquared =
            fireRange * fireRange;

        if (directionToTarget.sqrMagnitude >
            fireRangeSquared)
        {
            return false;
        }

        return CanTargetPosition(
            currentTarget.AimPosition
        );
    }


    /// <summary>
    /// FullCircle modunda bütün yönlere izin verir.
    /// ForwardCone modunda yalnızca ön taraftaki hedeflere izin verir.
    /// </summary>
    private bool CanTargetPosition(Vector3 targetPosition)
    {
        if (targetingMode ==
            TargetingMode.FullCircle)
        {
            return true;
        }

        return IsInsideForwardCone(
            targetPosition
        );
    }


    /// <summary>
    /// Hedefin karakterin ön görüş açısında olup olmadığını kontrol eder.
    /// </summary>
    private bool IsInsideForwardCone(Vector3 targetPosition)
    {
        if (facingReference == null)
        {
            return false;
        }

        Vector3 directionToTarget =
            targetPosition -
            facingReference.position;

        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude < 0.001f)
        {
            return true;
        }

        Vector3 forwardDirection =
            facingReference.forward;

        forwardDirection.y = 0f;

        if (forwardDirection.sqrMagnitude < 0.001f)
        {
            return false;
        }

        float targetAngle =
            Vector3.Angle(
                forwardDirection,
                directionToTarget
            );

        return targetAngle <= maximumAimAngle;
    }


    /// <summary>
    /// WeaponPivot veya CombatPivot objesini hedefe döndürür.
    /// </summary>
    private void RotatePivotTowardsTarget()
    {
        if (weaponPivot == null ||
            currentTarget == null)
        {
            return;
        }

        Vector3 targetDirection =
            currentTarget.AimPosition -
            weaponPivot.position;

        targetDirection.y = 0f;

        if (targetDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                targetDirection,
                Vector3.up
            );

        weaponPivot.rotation =
            Quaternion.Slerp(
                weaponPivot.rotation,
                targetRotation,
                aimRotationSpeed * Time.deltaTime
            );
    }


    /// <summary>
    /// Ateş süresi geldiyse ve FirePoint hedefe bakıyorsa mermi gönderir.
    /// </summary>
    private void TryShoot()
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        if (firePoint == null ||
            currentTarget == null)
        {
            return;
        }

        if (BulletPool.Instance == null)
        {
            Debug.LogError(
                "AutoShooter: Sahnede BulletPool bulunamadı!",
                this
            );

            return;
        }

        Vector3 directionToTarget =
            currentTarget.AimPosition -
            firePoint.position;

        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude < 0.001f)
        {
            return;
        }

        directionToTarget.Normalize();

        Vector3 firePointDirection =
            firePoint.forward;

        firePointDirection.y = 0f;

        if (firePointDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        firePointDirection.Normalize();

        float alignmentAngle =
            Vector3.Angle(
                firePointDirection,
                directionToTarget
            );

        // Pivot hedefe yeterince dönmeden ateş etme.
        if (alignmentAngle >
            fireAlignmentTolerance)
        {
            return;
        }

        nextFireTime =
            Time.time + fireInterval;

        Quaternion bulletRotation =
            Quaternion.LookRotation(
                firePointDirection,
                Vector3.up
            );

        // Mermi düşmanın konumuna kilitlenmez.
        // FirePoint hangi yöne bakıyorsa o yönde ilerler.
        BulletPool.Instance.SpawnBullet(
            firePoint.position,
            bulletRotation,
            firePointDirection,
            bulletSpeed,
            bulletDamage,
            bulletLifetime
        );
    }


    /// <summary>
    /// Hedef kalmadığında pivotu başlangıç yönüne döndürür.
    /// </summary>
    private void ReturnPivotForward()
    {
        if (!returnForwardWhenNoTarget)
        {
            return;
        }

        if (weaponPivot == null)
        {
            return;
        }

        weaponPivot.localRotation =
            Quaternion.Slerp(
                weaponPivot.localRotation,
                defaultPivotLocalRotation,
                aimRotationSpeed * Time.deltaTime
            );
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;

        Gizmos.DrawWireSphere(
            transform.position,
            fireRange
        );

        if (targetingMode !=
            TargetingMode.ForwardCone)
        {
            return;
        }

        Transform directionReference =
            facingReference != null
                ? facingReference
                : transform;

        Vector3 forwardDirection =
            directionReference.forward;

        forwardDirection.y = 0f;

        if (forwardDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        forwardDirection.Normalize();

        Vector3 leftDirection =
            Quaternion.AngleAxis(
                -maximumAimAngle,
                Vector3.up
            ) * forwardDirection;

        Vector3 rightDirection =
            Quaternion.AngleAxis(
                maximumAimAngle,
                Vector3.up
            ) * forwardDirection;

        Gizmos.color = Color.yellow;

        Gizmos.DrawRay(
            directionReference.position,
            leftDirection * fireRange
        );

        Gizmos.DrawRay(
            directionReference.position,
            rightDirection * fireRange
        );
    }


    private void OnValidate()
    {
        fireRange =
            Mathf.Max(0.1f, fireRange);

        maximumAimAngle =
            Mathf.Clamp(
                maximumAimAngle,
                0f,
                180f
            );

        targetScanInterval =
            Mathf.Max(
                0.02f,
                targetScanInterval
            );

        maximumDetectedEnemies =
            Mathf.Max(
                1,
                maximumDetectedEnemies
            );

        fireInterval =
            Mathf.Max(
                0.05f,
                fireInterval
            );

        bulletDamage =
            Mathf.Max(
                1,
                bulletDamage
            );

        bulletSpeed =
            Mathf.Max(
                0.1f,
                bulletSpeed
            );

        bulletLifetime =
            Mathf.Max(
                0.1f,
                bulletLifetime
            );

        aimRotationSpeed =
            Mathf.Max(
                0f,
                aimRotationSpeed
            );

        fireAlignmentTolerance =
            Mathf.Clamp(
                fireAlignmentTolerance,
                0.1f,
                45f
            );
    }
}