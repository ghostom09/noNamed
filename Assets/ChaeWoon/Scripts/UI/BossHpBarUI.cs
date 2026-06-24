using System;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BossHpBarUI : MonoBehaviour
{
    private const string PanelName = "BossHpBarPanel";
    private const string NameTextName = "BossNameText";
    private const string FillName = "BossHpFill";
    private const string HpTextName = "BossHpText";

    [SerializeField] private Component boss;
    [SerializeField] private string bossNameOverride;
    [SerializeField] private bool autoFindBoss = true;
    [SerializeField] private bool hideWhenNoBoss = true;
    [SerializeField] private bool hideWhenBossDead = true;
    [SerializeField] private bool showHpNumbers;
    [SerializeField, Min(0f)] private float refreshInterval = 0.05f;

    [Header("Prefab References")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TextMeshProUGUI hpText;

    private float nextRefreshTime;
    private float nextFindTime;
    private bool warnedMissingReferences;

    private void Awake()
    {
        BindReferences();
        SetVisible(false);
    }

    private void OnEnable()
    {
        BindReferences();
        FindBossIfNeeded(true);
        Refresh(true);
    }

    private void Update()
    {
        FindBossIfNeeded(false);
        Refresh(false);
    }

    public void SetBoss(Component targetBoss)
    {
        boss = targetBoss;
        Refresh(true);
    }

    public void SetBossName(string displayName)
    {
        bossNameOverride = displayName;
        Refresh(true);
    }

    private void FindBossIfNeeded(bool force)
    {
        if (!autoFindBoss || boss != null)
        {
            return;
        }

        if (!force && Time.unscaledTime < nextFindTime)
        {
            return;
        }

        nextFindTime = Time.unscaledTime + 0.5f;
        boss = FindActiveBoss();
    }

    private void Refresh(bool force)
    {
        if (!force && refreshInterval > 0f && Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + Mathf.Max(0f, refreshInterval);
        BindReferences();

        if (!HasRequiredReferences())
        {
            WarnMissingReferences();
            return;
        }

        if (boss == null)
        {
            SetName("Boss");
            SetHp(1f, 1f);
            SetVisible(!hideWhenNoBoss);
            return;
        }

        if (ReadIsDead(boss))
        {
            SetVisible(!hideWhenBossDead);
            if (hideWhenBossDead)
            {
                return;
            }
        }

        float maxHp = Mathf.Max(1f, ReadMaxHp(boss));
        float currentHp = Mathf.Clamp(ReadCurrentHp(boss, maxHp), 0f, maxHp);

        SetVisible(true);
        SetName(GetDisplayName(boss));
        SetHp(currentHp, maxHp);
    }

    private void SetName(string displayName)
    {
        if (bossNameText != null)
        {
            bossNameText.text = string.IsNullOrWhiteSpace(displayName) ? "Boss" : displayName;
        }
    }

    private void SetHp(float currentHp, float maxHp)
    {
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = maxHp <= 0f ? 0f : Mathf.Clamp01(currentHp / maxHp);
        }

        if (hpText != null)
        {
            hpText.gameObject.SetActive(showHpNumbers);
            hpText.text = $"{currentHp:0} / {maxHp:0}";
        }
    }

    private void SetVisible(bool visible)
    {
        if (panelRoot != null && panelRoot.gameObject.activeSelf != visible)
        {
            panelRoot.gameObject.SetActive(visible);
        }
    }

    private void BindReferences()
    {
        if (panelRoot == null)
        {
            panelRoot = FindDescendant<RectTransform>(PanelName);
        }

        if (bossNameText == null)
        {
            bossNameText = FindDescendant<TextMeshProUGUI>(NameTextName);
        }

        if (hpFillImage == null)
        {
            hpFillImage = FindDescendant<Image>(FillName);
        }

        if (hpText == null)
        {
            hpText = FindDescendant<TextMeshProUGUI>(HpTextName);
        }

        if (hpText != null)
        {
            hpText.gameObject.SetActive(showHpNumbers);
        }
    }

    private bool HasRequiredReferences()
    {
        return panelRoot != null && bossNameText != null && hpFillImage != null;
    }

    private void WarnMissingReferences()
    {
        if (warnedMissingReferences)
        {
            return;
        }

        warnedMissingReferences = true;
        Debug.LogWarning("[BossHpBarUI] Boss HP prefab references are missing.", this);
    }

    private T FindDescendant<T>(string childName) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].name == childName)
            {
                return components[i];
            }
        }

        return null;
    }

    private static Component FindActiveBoss()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];

            if (behaviour == null || !IsOrInherits(behaviour.GetType(), "BossBase"))
            {
                continue;
            }

            if (!ReadIsDead(behaviour))
            {
                return behaviour;
            }
        }

        return null;
    }

    private string GetDisplayName(Component target)
    {
        if (!string.IsNullOrWhiteSpace(bossNameOverride))
        {
            return bossNameOverride;
        }

        string rawName = target == null ? string.Empty : target.gameObject.name;
        rawName = rawName.Replace("(Clone)", string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(rawName) || rawName.Equals("Boss", StringComparison.OrdinalIgnoreCase))
        {
            rawName = target == null ? "Boss" : target.GetType().Name;
        }

        rawName = StripSuffix(rawName, "Controller");
        return SplitPascalCase(rawName);
    }

    private static float ReadCurrentHp(Component target, float fallback)
    {
        return TryReadFloat(target, new[] { "CurrentHP", "CurrentHp", "CurrentHealth" },
            new[] { "currentHP", "currentHp", "currentHealth", "hp" }, out float value)
            ? value
            : fallback;
    }

    private static float ReadMaxHp(Component target)
    {
        return TryReadFloat(target, new[] { "MaxHP", "MaxHp", "MaxHealth" },
            new[] { "maxHP", "maxHp", "maxHealth" }, out float value)
            ? value
            : 1f;
    }

    private static bool ReadIsDead(Component target)
    {
        if (TryReadBool(target, new[] { "IsDead" }, new[] { "isDead", "_isDead" }, out bool value))
        {
            return value;
        }

        return target == null;
    }

    private static bool TryReadFloat(Component target, string[] propertyNames, string[] fieldNames, out float value)
    {
        value = 0f;

        if (target == null)
        {
            return false;
        }

        Type type = target.GetType();

        for (int i = 0; i < propertyNames.Length; i++)
        {
            PropertyInfo property = FindProperty(type, propertyNames[i]);
            if (property != null && TryConvertFloat(property.GetValue(target), out value))
            {
                return true;
            }
        }

        for (int i = 0; i < fieldNames.Length; i++)
        {
            FieldInfo field = FindField(type, fieldNames[i]);
            if (field != null && TryConvertFloat(field.GetValue(target), out value))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadBool(Component target, string[] propertyNames, string[] fieldNames, out bool value)
    {
        value = false;

        if (target == null)
        {
            return false;
        }

        Type type = target.GetType();

        for (int i = 0; i < propertyNames.Length; i++)
        {
            PropertyInfo property = FindProperty(type, propertyNames[i]);
            if (property != null && property.GetValue(target) is bool propertyValue)
            {
                value = propertyValue;
                return true;
            }
        }

        for (int i = 0; i < fieldNames.Length; i++)
        {
            FieldInfo field = FindField(type, fieldNames[i]);
            if (field != null && field.GetValue(target) is bool fieldValue)
            {
                value = fieldValue;
                return true;
            }
        }

        return false;
    }

    private static bool TryConvertFloat(object rawValue, out float value)
    {
        value = 0f;

        if (rawValue == null)
        {
            return false;
        }

        try
        {
            value = Convert.ToSingle(rawValue);
            return true;
        }
        catch (InvalidCastException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static PropertyInfo FindProperty(Type type, string propertyName)
    {
        while (type != null)
        {
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (property != null)
            {
                return property;
            }

            type = type.BaseType;
        }

        return null;
    }

    private static FieldInfo FindField(Type type, string fieldName)
    {
        while (type != null)
        {
            FieldInfo field = type.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null)
            {
                return field;
            }

            type = type.BaseType;
        }

        return null;
    }

    private static bool IsOrInherits(Type type, string typeName)
    {
        while (type != null)
        {
            if (type.Name == typeName)
            {
                return true;
            }

            type = type.BaseType;
        }

        return false;
    }

    private static string StripSuffix(string value, string suffix)
    {
        if (value.EndsWith(suffix, StringComparison.Ordinal))
        {
            return value.Substring(0, value.Length - suffix.Length);
        }

        return value;
    }

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Boss";
        }

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
            {
                AppendSpaceIfNeeded(builder);
            }

            builder.Append(current);
        }

        return builder.ToString().Trim();
    }

    private static bool NeedsWordBreak(string value, int index)
    {
        char previous = value[index - 1];

        if (char.IsLower(previous) || char.IsDigit(previous))
        {
            return true;
        }

        return index + 1 < value.Length && char.IsLower(value[index + 1]);
    }

    private static void AppendSpaceIfNeeded(StringBuilder builder)
    {
        if (builder.Length > 0 && builder[^1] != ' ')
        {
            builder.Append(' ');
        }
    }
}
