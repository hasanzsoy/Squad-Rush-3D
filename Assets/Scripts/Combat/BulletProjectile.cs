using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class BulletProjectile : MonoBehaviour
{
    private Rigidbody rb;

    private Action<BulletProjectile> releaseAction;

    private int damage;
    private float remainingLifetime;
    private bool isActive;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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

    /// <summary>
    /// Mermi havuzdan çıkarıldığında çağrılır.
    /// </summary>
    public void Launch(
        Vector3 spawnPosition,
        Quaternion spawnRotation,
        Vector3 direction,
        float speed,
        int damageAmount,
        float lifetime)
    {
        transform.SetPositionAndRotation(
            spawnPosition,
            spawnRotation
        );

        damage = Mathf.Max(0, damageAmount);
        remainingLifetime = Mathf.Max(0.1f, lifetime);

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = transform.forward;
        }

        direction.Normalize();

        gameObject.SetActive(true);

        rb.linearVelocity = direction * speed;
        rb.angularVelocity = Vector3.zero;

        isActive = true;
    }

    /// <summary>
    /// BulletPool tarafından geri dönüş metodu atanır.
    /// </summary>
    public void SetReleaseAction(
        Action<BulletProjectile> newReleaseAction)
    {
        releaseAction = newReleaseAction;
    }

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

        enemyHealth.TakeDamage(damage);

        ReleaseToPool();
    }

    public void ReleaseToPool()
    {
        if (!isActive)
        {
            return;
        }

        isActive = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

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
        if (rb == null)
        {
            return;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}