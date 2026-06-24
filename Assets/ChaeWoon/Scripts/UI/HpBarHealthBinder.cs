using System;
using System.Reflection;
using UnityEngine;

public class HpBarHealthBinder : MonoBehaviour
{
    [SerializeField] private HpBarView hpBarView;
    [SerializeField] private Component health;
    [SerializeField] private float fallbackMaxHp = 100f;

    private Action<float, float> hpChangedHandler;

    private void Awake()
    {
        hpChangedHandler = HandleHpChanged;
        BindReferences();
    }

    private void OnEnable()
    {
        if (hpChangedHandler == null)
        {
            hpChangedHandler = HandleHpChanged;
        }

        BindReferences();
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void SetHealth(Component targetHealth)
    {
        if (health == targetHealth)
        {
            return;
        }

        Unsubscribe();
        health = targetHealth;
        Subscribe();
        Refresh();
    }

    private void Subscribe()
    {
        AddHealthEventHandler("OnDamaged");
        AddHealthEventHandler("OnHealed");
    }

    private void Unsubscribe()
    {
        RemoveHealthEventHandler("OnDamaged");
        RemoveHealthEventHandler("OnHealed");
    }

    private void AddHealthEventHandler(string eventName)
    {
        EventInfo eventInfo = health == null ? null : health.GetType().GetEvent(eventName);

        if (eventInfo != null && hpChangedHandler != null)
        {
            eventInfo.AddEventHandler(health, hpChangedHandler);
        }
    }

    private void RemoveHealthEventHandler(string eventName)
    {
        EventInfo eventInfo = health == null ? null : health.GetType().GetEvent(eventName);

        if (eventInfo != null && hpChangedHandler != null)
        {
            eventInfo.RemoveEventHandler(health, hpChangedHandler);
        }
    }

    private void HandleHpChanged(float currentHp, float maxHp)
    {
        if (hpBarView != null)
        {
            hpBarView.SetHp(currentHp, maxHp);
        }
    }

    private void Refresh()
    {
        if (hpBarView == null || health == null)
        {
            return;
        }

        hpBarView.SetHp(ResolveCurrentHp(), ResolveMaxHp());
    }

    private float ResolveCurrentHp()
    {
        if (TryReadFloatProperty("CurrentHp", out float value))
        {
            return value;
        }

        return ResolveMaxHp();
    }

    private float ResolveMaxHp()
    {
        // PlayerHealth exposes MaxHp only as a public property (it has no private 'maxHp'
        // field, it delegates to an inner Health). Raw Health has both. Read the property
        // first so the player's max HP is correct instead of falling back to 100.
        if (TryReadFloatProperty("MaxHp", out float maxFromProperty) && maxFromProperty > 0f)
        {
            return maxFromProperty;
        }

        FieldInfo maxHpField = health == null
            ? null
            : health.GetType().GetField("maxHp", BindingFlags.Instance | BindingFlags.NonPublic);

        if (maxHpField != null && maxHpField.GetValue(health) is float value && value > 0f)
        {
            return value;
        }

        return Mathf.Max(1f, fallbackMaxHp);
    }

    private bool TryReadFloatProperty(string propertyName, out float value)
    {
        value = 0f;

        if (health == null)
        {
            return false;
        }

        PropertyInfo property = health.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

        if (property != null && property.GetValue(health) is float result)
        {
            value = result;
            return true;
        }

        return false;
    }

    private void BindReferences()
    {
        if (hpBarView == null)
        {
            hpBarView = GetComponent<HpBarView>();
        }

        if (hpBarView == null)
        {
            hpBarView = FindAnyObjectByType<HpBarView>();
        }

        if (health == null)
        {
            health = FindPlayerHealth();
        }
    }

    // Components that identify the player object. The HP bar must bind to the player's
    // Health, not to one of the many enemy/boss Health components in a combat scene.
    private static readonly string[] PlayerMarkerTypeNames =
    {
        "PlayerMove",
        "PlayerStatManager",
        "PlayerInputReader",
        "SkillSwitcher",
        "SkillMutationLoadoutBinder"
    };

    private static Component FindPlayerHealth()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
        Component fallback = null;
        Component playerCandidate = null;

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour current = behaviours[i];

            if (current == null || !IsHealthType(current.GetType().Name))
            {
                continue;
            }

            // First Health-like component seen, used only if nothing better turns up.
            fallback ??= current;

            if (!IsUnderPlayerMarker(current))
            {
                continue;
            }

            // Prefer the player's own component; PlayerHealth (the wrapper that fires the
            // correct events) wins over a raw Health on the same object/hierarchy.
            if (playerCandidate == null || PrefersHealth(current, playerCandidate))
            {
                playerCandidate = current;
            }
        }

        return playerCandidate ?? fallback;
    }

    private static bool IsHealthType(string typeName)
    {
        return typeName == "PlayerHealth" || typeName == "Health";
    }

    private static bool IsUnderPlayerMarker(Component component)
    {
        for (int i = 0; i < PlayerMarkerTypeNames.Length; i++)
        {
            if (SkillMutationLoadoutBinder.HasComponentInParentByTypeName(component, PlayerMarkerTypeNames[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool PrefersHealth(Component candidate, Component current)
    {
        return candidate.GetType().Name == "PlayerHealth" && current.GetType().Name != "PlayerHealth";
    }
}
