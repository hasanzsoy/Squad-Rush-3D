using UnityEngine;

public class AutoShooter : MonoBehaviour
{
    public enum TargetingMode
    {
        ForwardCone, // Yalnızca baktığı yöndeki düşmanlar
        FullCircle   // 360 derece çevredeki düşmanlar
    }

    [Header("References")]

    [Tooltip("Düşmana doğru dönecek silah objesi.")]
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

    [SerializeField]
    private LayerMask enemyLayer;

    [SerializeField, Min(0.1f)]
    private float fireRange = 6f;

    [Tooltip("Forward Cone modunda sağ ve sol hedefleme açısı.")]
    [SerializeField, Range(0f, 180f)]
    private float maximumAimAngle = 80f;

    [SerializeField, Min(0.02f)]
    private float targetScanInterval = 0.15f;

    [SerializeField, Min(1)]
    private int maximumDetectedEnemies = 32;


    [Header("Weapon Settings")]

    [SerializeField, Min(0.05f)]
    private float fireInterval = 0.5f;

    [SerializeField, Min(1)]
    private int bulletDamage = 1;

    [SerializeField, Min(0.1f)]
    private float bulletSpeed = 16f;

    [SerializeField, Min(0.1f)]
    private float bulletLifetime = 3f;

    [SerializeField, Min(0f)]
    private float aimRotationSpeed = 15f;

    [Tooltip("Silah hedefe bu açı kadar yaklaşınca ateş eder.")]
    [SerializeField, Range(0.1f, 45f)]
    private float fireAlignmentTolerance = 7f;

    [SerializeField]
    private bool returnWeaponForwardWhenNoTarget = true;


    private Collider[] detectionResults;
    private EnemyHealth currentTarget;

    private float nextScanTime;
    private float nextFireTime;

    private Quaternion defaultWeaponLocalRotation;


    private void Awake()
    {
        if (facingReference == null)
        {
            facingReference = transform;
        }

        if (weaponPivot != null)
        {
            defaultWeaponLocalRotation =
                weaponPivot.localRotation;
        }

        detectionResults =
            new Collider[Mathf.Max(1, maximumDetectedEnemies)];
    }


    private void Update()
    {
        UpdateTargetScanning();

        if (!IsCurrentTargetValid())
        {
            currentTarget = null;
            ReturnWeaponToForward();
            return;
        }

        RotateWeaponTowardsTarget();
        TryShoot();
    }


    private void UpdateTargetScanning()
    {
        if (Time.time < nextScanTime)
        {
            return;
        }

        nextScanTime =
            Time.time + targetScanInterval;

        FindClosestEnemy();
    }


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

            if (enemyHealth == null ||
                !enemyHealth.IsAlive ||
                !enemyHealth.gameObject.activeInHierarchy)
            {
                continue;
            }

            // Player modundaysa yalnızca ön taraftaki düşmanları alır.
            // Takım üyesi modundaysa bütün yönleri kabul eder.
            if (!CanTargetEnemy(enemyHealth.AimPosition))
            {
                continue;
            }

            Vector3 directionToEnemy =
                enemyHealth.AimPosition -
                transform.position;

            directionToEnemy.y = 0f;

            float distanceSquared =
                directionToEnemy.sqrMagnitude;

            if (distanceSquared <
                closestDistanceSquared)
            {
                closestDistanceSquared =
                    distanceSquared;

                closestEnemy =
                    enemyHealth;
            }
        }

        currentTarget = closestEnemy;
    }


    private bool IsCurrentTargetValid()
    {
        if (currentTarget == null ||
            !currentTarget.IsAlive ||
            !currentTarget.gameObject.activeInHierarchy)
        {
            return false;
        }

        Vector3 directionToTarget =
            currentTarget.AimPosition -
            transform.position;

        directionToTarget.y = 0f;

        float rangeSquared =
            fireRange * fireRange;

        if (directionToTarget.sqrMagnitude >
            rangeSquared)
        {
            return false;
        }

        return CanTargetEnemy(
            currentTarget.AimPosition
        );
    }


    private bool CanTargetEnemy(Vector3 enemyPosition)
    {
        // Takım üyeleri için 360 derece hedefleme.
        if (targetingMode == TargetingMode.FullCircle)
        {
            return true;
        }

        // Player için baktığı yön kontrolü.
        return IsInsideForwardCone(enemyPosition);
    }


    private bool IsInsideForwardCone(Vector3 enemyPosition)
    {
        if (facingReference == null)
        {
            return false;
        }

        Vector3 directionToEnemy =
            enemyPosition -
            facingReference.position;

        directionToEnemy.y = 0f;

        if (directionToEnemy.sqrMagnitude < 0.001f)
        {
            return true;
        }

        Vector3 forwardDirection =
            facingReference.forward;

        forwardDirection.y = 0f;

        float angleToEnemy = Vector3.Angle(
            forwardDirection,
            directionToEnemy
        );

        return angleToEnemy <= maximumAimAngle;
    }


    private void RotateWeaponTowardsTarget()
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

        float alignmentAngle =
            Vector3.Angle(
                firePoint.forward,
                directionToTarget
            );

        // WeaponPivot henüz hedefe dönmediyse ateş etme.
        if (alignmentAngle > fireAlignmentTolerance)
        {
            return;
        }

        Vector3 bulletDirection =
            firePoint.forward;

        bulletDirection.y = 0f;

        if (bulletDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        bulletDirection.Normalize();

        nextFireTime =
            Time.time + fireInterval;

        Quaternion bulletRotation =
            Quaternion.LookRotation(
                bulletDirection,
                Vector3.up
            );

        BulletPool.Instance.SpawnBullet(
            firePoint.position,
            bulletRotation,
            bulletDirection,
            bulletSpeed,
            bulletDamage,
            bulletLifetime
        );
    }


    private void ReturnWeaponToForward()
    {
        if (!returnWeaponForwardWhenNoTarget ||
            weaponPivot == null)
        {
            return;
        }

        weaponPivot.localRotation =
            Quaternion.Slerp(
                weaponPivot.localRotation,
                defaultWeaponLocalRotation,
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

        if (targetingMode != TargetingMode.ForwardCone)
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
            Mathf.Clamp(maximumAimAngle, 0f, 180f);

        targetScanInterval =
            Mathf.Max(0.02f, targetScanInterval);

        maximumDetectedEnemies =
            Mathf.Max(1, maximumDetectedEnemies);

        fireInterval =
            Mathf.Max(0.05f, fireInterval);

        bulletDamage =
            Mathf.Max(1, bulletDamage);

        bulletSpeed =
            Mathf.Max(0.1f, bulletSpeed);

        bulletLifetime =
            Mathf.Max(0.1f, bulletLifetime);

        aimRotationSpeed =
            Mathf.Max(0f, aimRotationSpeed);

        fireAlignmentTolerance =
            Mathf.Clamp(
                fireAlignmentTolerance,
                0.1f,
                45f
            );
    }
}