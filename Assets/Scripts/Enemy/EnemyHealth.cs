using UnityEngine;

[RequireComponent(typeof(EnemyAI))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField, Min(1)]
    private int maximumHealth = 3;

    [Header("Target Settings")]
    [Tooltip("Mermilerin hedef alacağı yükseklik.")]
    [SerializeField]
    private float aimHeight = 1f;

    private EnemyAI enemyAI;
    private int currentHealth;
    private bool isAlive;

    public int CurrentHealth => currentHealth;
    public int MaximumHealth => maximumHealth;
    public bool IsAlive => isAlive;

    public Vector3 AimPosition =>
        transform.position + Vector3.up * aimHeight;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
    }

    private void OnEnable()
    {
        ResetHealth();
    }

    /// <summary>
    /// Düşmanın canını başlangıç değerine getirir.
    /// Pool'dan tekrar çıktığında yeniden çalışır.
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maximumHealth;
        isAlive = true;
    }

    /// <summary>
    /// Düşmana belirtilen miktarda hasar verir.
    /// </summary>
    public void TakeDamage(int damageAmount)
    {
        if (!isAlive)
        {
            return;
        }

        damageAmount = Mathf.Max(0, damageAmount);

        if (damageAmount == 0)
        {
            return;
        }

        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (!isAlive)
        {
            return;
        }

        isAlive = false;
        currentHealth = 0;

        if (enemyAI != null)
        {
            enemyAI.ReleaseToPool();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnValidate()
    {
        maximumHealth = Mathf.Max(1, maximumHealth);
        aimHeight = Mathf.Max(0f, aimHeight);
    }
}