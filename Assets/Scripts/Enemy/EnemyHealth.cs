using UnityEngine;

[RequireComponent(typeof(EnemyAI))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]

    [Tooltip("Düşmanın maksimum canı.")]
    [SerializeField, Min(1)]
    private int maximumHealth = 3;


    [Header("Aim Settings")]

    [Tooltip("Mermilerin düşmanın hangi yüksekliğine nişan alacağı.")]
    [SerializeField, Min(0f)]
    private float aimHeight = 1f;


    [Header("Debug")]

    [Tooltip("Hasar ve ölüm bilgilerini Console'a yazar.")]
    [SerializeField]
    private bool showDamageLogs = true;


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


    /// <summary>
    /// Düşman havuzdan çıkarıldığında canını yeniler.
    /// EnemyAI içerisindeki Activate metodu çağırır.
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maximumHealth;
        isAlive = true;

        if (showDamageLogs)
        {
            Debug.Log(
                $"{gameObject.name} yeniden doğdu. Can: {currentHealth}",
                this
            );
        }
    }


    /// <summary>
    /// Düşmana hasar verir.
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

        if (showDamageLogs)
        {
            Debug.Log(
                $"{gameObject.name} hasar aldı. " +
                $"Hasar: {damageAmount} | Kalan can: {currentHealth}",
                this
            );
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    /// <summary>
    /// Düşmanın canı bittiğinde çalışır.
    /// Düşmanı Destroy etmek yerine havuza gönderir.
    /// </summary>
    private void Die()
    {
        if (!isAlive)
        {
            return;
        }

        isAlive = false;
        currentHealth = 0;

        if (showDamageLogs)
        {
            Debug.Log(
                $"{gameObject.name} öldü ve havuza gönderildi.",
                this
            );
        }

        if (enemyAI != null)
        {
            enemyAI.ReleaseToPool();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }


    /// <summary>
    /// Düşman havuza dönüp pasif olduğunda hedef alınmasını engeller.
    /// </summary>
    private void OnDisable()
    {
        isAlive = false;
    }


    private void OnValidate()
    {
        maximumHealth =
            Mathf.Max(1, maximumHealth);

        aimHeight =
            Mathf.Max(0f, aimHeight);
    }
}