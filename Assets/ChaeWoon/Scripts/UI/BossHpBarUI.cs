using System;
using System.Text;
using BossSystem.Boss;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 보스 HP UI 를 JunMo 의 BossBase 와 이벤트 기반으로 연결한다.
// - BossBase.HealthChanged 로 체력 표시 갱신 (폴링/리플렉션 없음)
// - BossBase.OnDeath 로 사망 처리
// - BossBase.BossSpawned/BossDespawned (static) 로 현재 보스를 자동 추적
[DisallowMultipleComponent]
public class BossHpBarUI : MonoBehaviour
{
    private const string PanelName = "BossHpBarPanel";
    private const string NameTextName = "BossNameText";
    private const string FillName = "BossHpFill";
    private const string HpTextName = "BossHpText";

    [SerializeField] private BossBase boss;
    [SerializeField] private string bossNameOverride;
    [SerializeField] private bool autoFindBoss = true;
    [SerializeField] private bool hideWhenNoBoss = true;
    [SerializeField] private bool hideWhenBossDead = true;
    [SerializeField] private bool showHpNumbers;

    [Header("Prefab References")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TextMeshProUGUI hpText;

    private bool warnedMissingReferences;

    /// <summary>살아있는 보스를 표시 중인지. 통합 부트스트랩이 중복 배선을 막는 데 사용.</summary>
    public bool HasLiveBoss => boss != null && !boss.IsDead;

    private void Awake()
    {
        BindReferences();
        SetVisible(false);
    }

    private void OnEnable()
    {
        BindReferences();

        BossBase.BossSpawned += HandleBossSpawned;
        BossBase.BossDespawned += HandleBossDespawned;

        if (boss == null && autoFindBoss)
            boss = FindLiveBoss();

        BossBase target = boss;
        boss = null;          // Attach 가 구독을 정상적으로 걸도록 초기화
        Attach(target);
    }

    private void OnDisable()
    {
        BossBase.BossSpawned -= HandleBossSpawned;
        BossBase.BossDespawned -= HandleBossDespawned;
        Unsubscribe(boss);
    }

    public void SetBoss(BossBase targetBoss)
    {
        Attach(targetBoss);
    }

    public void SetBossName(string displayName)
    {
        bossNameOverride = displayName;
        RefreshName();
    }

    // ── 이벤트 핸들러 ─────────────────────────────────────────

    private void HandleBossSpawned(BossBase spawned)
    {
        if (!autoFindBoss || spawned == null)
            return;

        // 표시 중인 보스가 없거나 죽었으면 새로 등장한 보스로 교체.
        if (boss == null || boss.IsDead)
            Attach(spawned);
    }

    private void HandleBossDespawned(BossBase despawned)
    {
        if (despawned != boss)
            return;

        Unsubscribe(boss);
        boss = null;
        Attach(autoFindBoss ? FindLiveBoss() : null);
    }

    private void HandleHealthChanged(float currentHp, float maxHp)
    {
        if (!HasRequiredReferences())
            return;

        SetVisible(true);
        RefreshHp(currentHp, maxHp);
    }

    private void HandleBossDeath()
    {
        if (hideWhenBossDead)
            SetVisible(false);
    }

    // ── 구독/표시 ─────────────────────────────────────────────

    private void Attach(BossBase target)
    {
        if (boss != target)
        {
            Unsubscribe(boss);
            boss = target;
            Subscribe(boss);
        }

        Refresh();
    }

    private void Subscribe(BossBase target)
    {
        if (target == null)
            return;

        target.HealthChanged += HandleHealthChanged;
        target.OnDeath += HandleBossDeath;
    }

    private void Unsubscribe(BossBase target)
    {
        if (target == null)
            return;

        target.HealthChanged -= HandleHealthChanged;
        target.OnDeath -= HandleBossDeath;
    }

    private void Refresh()
    {
        BindReferences();

        if (!HasRequiredReferences())
        {
            WarnMissingReferences();
            return;
        }

        if (boss == null)
        {
            SetVisible(!hideWhenNoBoss);
            if (!hideWhenNoBoss)
            {
                RefreshName();
                RefreshHp(1f, 1f);
            }

            return;
        }

        if (boss.IsDead && hideWhenBossDead)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        RefreshName();
        RefreshHp(boss.CurrentHP, boss.MaxHP);
    }

    private void RefreshName()
    {
        if (bossNameText != null)
            bossNameText.text = GetDisplayName();
    }

    private void RefreshHp(float currentHp, float maxHp)
    {
        maxHp = Mathf.Max(1f, maxHp);
        currentHp = Mathf.Clamp(currentHp, 0f, maxHp);

        if (hpFillImage != null)
            hpFillImage.fillAmount = Mathf.Clamp01(currentHp / maxHp);

        if (hpText != null)
        {
            hpText.gameObject.SetActive(showHpNumbers);
            hpText.text = $"{currentHp:0} / {maxHp:0}";
        }
    }

    private void SetVisible(bool visible)
    {
        if (panelRoot != null && panelRoot.gameObject.activeSelf != visible)
            panelRoot.gameObject.SetActive(visible);
    }

    private static BossBase FindLiveBoss()
    {
        var bosses = BossBase.ActiveBosses;
        for (int i = 0; i < bosses.Count; i++)
        {
            if (bosses[i] != null && !bosses[i].IsDead)
                return bosses[i];
        }

        return null;
    }

    private void BindReferences()
    {
        if (panelRoot == null)
            panelRoot = FindDescendant<RectTransform>(PanelName);

        if (bossNameText == null)
            bossNameText = FindDescendant<TextMeshProUGUI>(NameTextName);

        if (hpFillImage == null)
            hpFillImage = FindDescendant<Image>(FillName);

        if (hpText == null)
            hpText = FindDescendant<TextMeshProUGUI>(HpTextName);

        if (hpText != null)
            hpText.gameObject.SetActive(showHpNumbers);
    }

    private bool HasRequiredReferences()
    {
        return panelRoot != null && bossNameText != null && hpFillImage != null;
    }

    private void WarnMissingReferences()
    {
        if (warnedMissingReferences)
            return;

        warnedMissingReferences = true;
        Debug.LogWarning("[BossHpBarUI] Boss HP prefab references are missing.", this);
    }

    private T FindDescendant<T>(string childName) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].name == childName)
                return components[i];
        }

        return null;
    }

    private string GetDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(bossNameOverride))
            return bossNameOverride;

        if (boss == null)
            return "Boss";

        string rawName = boss.gameObject.name.Replace("(Clone)", string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(rawName) || rawName.Equals("Boss", StringComparison.OrdinalIgnoreCase))
            rawName = boss.GetType().Name;

        rawName = StripSuffix(rawName, "Controller");
        return SplitPascalCase(rawName);
    }

    private static string StripSuffix(string value, string suffix)
    {
        if (value.EndsWith(suffix, StringComparison.Ordinal))
            return value.Substring(0, value.Length - suffix.Length);

        return value;
    }

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Boss";

        StringBuilder builder = new();

        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];

            if (current == '_' || current == '-')
            {
                AppendSpaceIfNeeded(builder);
                continue;
            }

            if (i > 0 && char.IsUpper(current) && NeedsWordBreak(value, i))
                AppendSpaceIfNeeded(builder);

            builder.Append(current);
        }

        return builder.ToString().Trim();
    }

    private static bool NeedsWordBreak(string value, int index)
    {
        char previous = value[index - 1];

        if (char.IsLower(previous) || char.IsDigit(previous))
            return true;

        return index + 1 < value.Length && char.IsLower(value[index + 1]);
    }

    private static void AppendSpaceIfNeeded(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[^1] != ' ')
            builder.Append(' ');
    }
}
