using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class SquadManager : MonoBehaviour
{
    [Header("Prefab References")]

    [Tooltip("Oluşturulacak takım üyesi prefabı.")]
    [SerializeField]
    private SquadMemberFollower memberPrefab;

    [Tooltip("Oluşturulan takım üyelerinin yerleştirileceği ana obje.")]
    [SerializeField]
    private Transform membersParent;


    [Header("Squad Limit")]

    [Tooltip("Takımda bulunabilecek maksimum üye sayısı.")]
    [SerializeField, Min(1)]
    private int maximumMemberCount = 9;


    [Header("Formation Settings")]

    [Tooltip("Bir sırada bulunabilecek maksimum karakter sayısı.")]
    [SerializeField, Min(1)]
    private int maximumColumnCount = 3;

    [Tooltip("Karakterler arasındaki sağ-sol mesafesi.")]
    [SerializeField, Min(0.1f)]
    private float horizontalSpacing = 1.25f;

    [Tooltip("Formasyon sıraları arasındaki ileri-geri mesafesi.")]
    [SerializeField, Min(0.1f)]
    private float verticalSpacing = 1.6f;


    [Header("Starting Squad")]

    [Tooltip("Oyun başladığında oluşturulacak takım üyesi sayısı.")]
    [SerializeField, Min(0)]
    private int startingMemberCount = 2;


    [Header("Editor Testing")]

    [Tooltip("Space tuşuyla takım üyesi eklemeyi açar.")]
    [SerializeField]
    private bool enableSpaceKeyTest = true;


    // Oyunda oluşturulmuş takım üyelerini saklar.
    private readonly List<SquadMemberFollower> members =
        new List<SquadMemberFollower>();


    // Takımdaki mevcut üye sayısını dışarıdan okumamızı sağlar.
    public int MemberCount => members.Count;

    // Takımın dolu olup olmadığını bildirir.
    public bool IsSquadFull =>
        members.Count >= maximumMemberCount;

    // Listeyi dışarıdan yalnızca okunabilir şekilde verir.
    public IReadOnlyList<SquadMemberFollower> Members =>
        members;


    private void Start()
    {
        CreateStartingMembers();
    }

    private void Update()
    {
        TestAddMemberInput();
    }


    /// <summary>
    /// Oyun başlangıcındaki takım üyelerini oluşturur.
    /// </summary>
    private void CreateStartingMembers()
    {
        int amountToCreate = Mathf.Min(
            startingMemberCount,
            maximumMemberCount
        );

        for (int i = 0; i < amountToCreate; i++)
        {
            AddMember();
        }
    }


    /// <summary>
    /// Editörde Space tuşuyla takım üyesi eklemeyi test eder.
    /// </summary>
    private void TestAddMemberInput()
    {
        if (!enableSpaceKeyTest)
        {
            return;
        }

        if (WasAddMemberKeyPressed())
        {
            AddMember();
        }
    }


    /// <summary>
    /// Hem eski hem de yeni Input System için Space kontrolü yapar.
    /// </summary>
    private bool WasAddMemberKeyPressed()
    {
        bool keyPressed = false;

#if ENABLE_INPUT_SYSTEM

        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            keyPressed = true;
        }

#endif

#if ENABLE_LEGACY_INPUT_MANAGER

        if (Input.GetKeyDown(KeyCode.Space))
        {
            keyPressed = true;
        }

#endif

        return keyPressed;
    }


    /// <summary>
    /// Takıma yeni bir üye ekler.
    /// Upgrade butonundan da bu metot çağrılabilir.
    /// </summary>
    public void AddMember()
    {
        // Takım doluysa yeni üye oluşturma.
        if (IsSquadFull)
        {
            Debug.Log(
                $"Takım dolu! Maksimum takım üyesi: {maximumMemberCount}",
                this
            );

            return;
        }

        // Prefab bağlantısı yapılmamışsa işlemi durdur.
        if (memberPrefab == null)
        {
            Debug.LogError(
                "SquadManager: Member Prefab alanı boş!",
                this
            );

            return;
        }

        // Yeni karakteri oyuncunun biraz arkasında oluştur.
        Vector3 spawnPosition =
            transform.position -
            transform.forward * verticalSpacing;

        Quaternion spawnRotation =
            transform.rotation;

        SquadMemberFollower newMember = Instantiate(
            memberPrefab,
            spawnPosition,
            spawnRotation,
            membersParent
        );

        // Yeni üyeyi listeye ekle.
        members.Add(newMember);

        // Yeni üyeye takip edeceği lideri tanıt.
        newMember.Initialize(
            transform,
            Vector3.zero
        );

        // Yeni kişi eklendiği için formasyonu yeniden hesapla.
        RefreshFormation();
    }


    /// <summary>
    /// Takımdaki bütün üyelerin formasyon konumlarını yeniler.
    /// </summary>
    public void RefreshFormation()
    {
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] == null)
            {
                continue;
            }

            Vector3 newFormationOffset =
                CalculateFormationOffset(
                    i,
                    members.Count
                );

            members[i].SetFormationOffset(
                newFormationOffset
            );
        }
    }


    /// <summary>
    /// Karakterin takım içindeki satır ve sütun konumunu hesaplar.
    /// </summary>
    private Vector3 CalculateFormationOffset(
        int memberIndex,
        int totalMemberCount)
    {
        // Üyenin bulunduğu satır.
        int rowIndex =
            memberIndex / maximumColumnCount;

        // Üyenin satır içindeki sırası.
        int indexInsideRow =
            memberIndex % maximumColumnCount;

        // Bu satırdaki ilk üyenin liste numarası.
        int firstMemberIndexOfRow =
            rowIndex * maximumColumnCount;

        // Bu satıra yerleşebilecek kalan üye sayısı.
        int remainingMemberCount =
            totalMemberCount - firstMemberIndexOfRow;

        // Son satır tam dolu değilse mevcut üye sayısını kullanır.
        int membersInCurrentRow = Mathf.Min(
            maximumColumnCount,
            remainingMemberCount
        );

        // Satırın merkezde durmasını sağlar.
        float centeredColumnPosition =
            indexInsideRow -
            (membersInCurrentRow - 1) * 0.5f;

        float xPosition =
            centeredColumnPosition * horizontalSpacing;

        float zPosition =
            -(rowIndex + 1) * verticalSpacing;

        return new Vector3(
            xPosition,
            0f,
            zPosition
        );
    }


    /// <summary>
    /// Belirli bir üyeyi takımdan kaldırır.
    /// Daha sonraki ölüm sistemi için kullanılabilir.
    /// </summary>
    public void RemoveMember(
        SquadMemberFollower memberToRemove)
    {
        if (memberToRemove == null)
        {
            return;
        }

        if (!members.Contains(memberToRemove))
        {
            return;
        }

        members.Remove(memberToRemove);

        Destroy(memberToRemove.gameObject);

        RefreshFormation();
    }


    /// <summary>
    /// Inspector değerlerinin hatalı girilmesini engeller.
    /// </summary>
    private void OnValidate()
    {
        maximumMemberCount =
            Mathf.Max(1, maximumMemberCount);

        maximumColumnCount = Mathf.Clamp(
            maximumColumnCount,
            1,
            maximumMemberCount
        );

        startingMemberCount = Mathf.Clamp(
            startingMemberCount,
            0,
            maximumMemberCount
        );

        horizontalSpacing =
            Mathf.Max(0.1f, horizontalSpacing);

        verticalSpacing =
            Mathf.Max(0.1f, verticalSpacing);
    }
}