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
        PropertyInfo currentHp = health.GetType().GetProperty("CurrentHp", BindingFlags.Instance | BindingFlags.Public);

        if (currentHp != null && currentHp.GetValue(health) is float value)
        {
            return value;
        }

        return ResolveMaxHp();
    }

    private float ResolveMaxHp()
    {
        FieldInfo maxHp = health.GetType().GetField("maxHp", BindingFlags.Instance | BindingFlags.NonPublic);

        if (maxHp != null && maxHp.GetValue(health) is float value)
        {
            return value;
        }

        return Mathf.Max(1f, fallbackMaxHp);
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

    private static Component FindPlayerHealth()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
        Component fallback = null;

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour current = behaviours[i];

            if (current == null || current.GetType().Name != "Health")
            {
                continue;
            }

            fallback ??= current;

            if (SkillMutationLoadoutBinder.HasComponentInParentByTypeName(current, "SkillMutationLoadoutBinder") ||
                SkillMutationLoadoutBinder.HasComponentInParentByTypeName(current, "SkillSwitcher") ||
                SkillMutationLoadoutBinder.HasComponentInParentByTypeName(current, "PlayerMovement"))
            {
                return current;
            }
        }

        return fallback;
    }
}
