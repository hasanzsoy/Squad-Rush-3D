using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class BulletProjectile : MonoBehaviour
{
    [Header("Hit Detection")]
    [Tooltip("Merminin sadece hangi katmandaki objelere hasar vereceği.")]
    [SerializeField]
    private LayerMask enemyLayer;

    [Tooltip("Merminin çarpışma tarama yarıçapı.")]
    [SerializeField, Min(0.01f)]
    private float hitRadius = 0.12f;

    [Header("Debug")]
    [SerializeField]
    private bool showHitLogs = true;

    private Rigidbody rb;
    private SphereCollider bulletCollider;

    private Action<BulletProjectile> releaseAction;

    private Vector3 moveDirection;
    private float moveSpeed;
    private float remainingLifetime;
    private int damage;

    private bool isActive;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bulletCollider = GetComponent<SphereCollider>();

        // Mermiyi fizik kuvvetleri değil, kod hareket ettirecek.
        rb.isKinematic = true;

        // Trigger yedek çarpışma kontrolü olarak kalır.
        bulletCollider.isTrigger = true;
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f)
        {
            ReleaseToPool();
        }
    }

    private void FixedUpdate()
    {
        if (!isActive)
        {
            return;
        }

        MoveAndCheckCollision();
    }

    public void Launch(
        Vector3 spawnPosition,
        Quaternion spawnRotation,
        Vector3 direction,
        float speed,
        int damageAmount,
        float lifetime)
    {
        damage = Mathf.Max(1, damageAmount);
        moveSpeed = Mathf.Max(0.1f, speed);

        remainingLifetime =
            Mathf.Max(0.1f, lifetime);

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = spawnRotation * Vector3.forward;
        }

        moveDirection = direction.normalized;

        transform.SetPositionAndRotation(
            spawnPosition,
            spawnRotation
        );

        isActive = true;

        if (bulletCollider != null)
        {
            bulletCollider.enabled = true;
        }

        gameObject.SetActive(true);

        rb.position = spawnPosition;
        rb.rotation = spawnRotation;
    }

    private void MoveAndCheckCollision()
    {
        float moveDistance =
            moveSpeed * Time.fixedDeltaTime;

        Vector3 startPosition =
            rb.position;

        bool enemyHit = Physics.SphereCast(
            startPosition,
            hitRadius,
            moveDirection,
            out RaycastHit hit,
            moveDistance,
            enemyLayer,
            QueryTriggerInteraction.Collide
        );

        if (enemyHit)
        {
            EnemyHealth enemyHealth =
                hit.collider.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null &&
                enemyHealth.IsAlive)
            {
                DamageEnemy(enemyHealth);
                return;
            }
        }

        Vector3 nextPosition =
            startPosition +
            moveDirection * moveDistance;

        rb.MovePosition(nextPosition);
    }

    // SphereCast dışında Trigger çalışırsa yedek kontrol.
    private void OnTriggerEnter(Collider other)
    {
        if (!isActive)
        {
            return;
        }

        EnemyHealth enemyHealth =
            other.GetComponentInParent<EnemyHealth>();

        if (enemyHealth == null ||
            !enemyHealth.IsAlive)
        {
            return;
        }

        DamageEnemy(enemyHealth);
    }

    private void DamageEnemy(
        EnemyHealth enemyHealth)
    {
        if (!isActive)
        {
            return;
        }

        if (showHitLogs)
        {
            Debug.Log(
                $"Mermi {enemyHealth.name} objesine vurdu. Hasar: {damage}",
                enemyHealth
            );
        }

        enemyHealth.TakeDamage(damage);

        ReleaseToPool();
    }

    public void SetReleaseAction(
        Action<BulletProjectile> newReleaseAction)
    {
        releaseAction = newReleaseAction;
    }

    public void ReleaseToPool()
    {
        if (!isActive)
        {
            return;
        }

        isActive = false;
        moveSpeed = 0f;
        moveDirection = Vector3.zero;

        if (releaseAction != null)
        {
            releaseAction.Invoke(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        isActive = false;
        moveSpeed = 0f;
        moveDirection = Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            hitRadius
        );
    }

    private void OnValidate()
    {
        hitRadius = Mathf.Max(
            0.01f,
            hitRadius
        );
    }
}