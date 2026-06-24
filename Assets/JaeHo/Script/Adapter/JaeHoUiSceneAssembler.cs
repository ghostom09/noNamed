using System;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class JaeHoUiSceneAssembler : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private SkillSwitcher skillSwitcher;
    [SerializeField] private GameObject playerRoot;

    [Header("ChaeWoon UI")]
    [SerializeField] private Skill skillHud;
    [SerializeField] private MutationSelectUI mutationSelectUI;
    [SerializeField] private CharacterStatUpgradeUI statUpgradeUI;
    [SerializeField] private MutationPickup[] mutationPickups = Array.Empty<MutationPickup>();
    [SerializeField] private MutationData defaultMutationData;

    [Header("Options")]
    [SerializeField] private bool assembleOnValidate = true;
    [SerializeField] private bool assembleOnPlay = true;
    [SerializeField] private bool addMissingPlayerAdapters = true;

#if UNITY_EDITOR
    private bool editorAssembleQueued;
#endif

    private void OnValidate()
    {
        if (!assembleOnValidate)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            QueueEditorAssemble();
            return;
        }
#endif

        Assemble();
    }

    private void Awake()
    {
        if (assembleOnPlay || !Application.isPlaying)
        {
            Assemble();
        }
    }

    private void OnEnable()
    {
        if (assembleOnPlay || !Application.isPlaying)
        {
            Assemble();
        }
    }

    [ContextMenu("Assemble JaeHo UI Scene")]
    public void Assemble()
    {
        BindSceneObjects();

        if (skillSwitcher == null)
        {
            Debug.LogWarning("[JaeHoUiSceneAssembler] SkillSwitcher was not found.", this);
            return;
        }

        if (playerRoot == null)
        {
            playerRoot = skillSwitcher.transform.root != null
                ? skillSwitcher.transform.root.gameObject
                : skillSwitcher.gameObject;
        }

        SkillBase[] skills = GetSwitcherSkills(skillSwitcher);
        SkillMutationLoadoutBinder loadoutBinder = EnsureLoadoutBinder(skills);

        if (addMissingPlayerAdapters)
        {
            EnsurePlayerInputAdapter();
        }

        WireSkillHud();
        WireMutationSelectUi(loadoutBinder, skills);
        WireInitialStatUi();
        WireMutationPickups();

        SetDirtyInEditor(this);
    }

    private void BindSceneObjects()
    {
        if (skillSwitcher == null)
        {
            skillSwitcher = FindAnyObjectByType<SkillSwitcher>(FindObjectsInactive.Include);
        }

        if (playerRoot == null && skillSwitcher != null)
        {
            playerRoot = skillSwitcher.transform.root != null
                ? skillSwitcher.transform.root.gameObject
                : skillSwitcher.gameObject;
        }

        if (skillHud == null)
        {
            skillHud = FindAnyObjectByType<Skill>(FindObjectsInactive.Include);
        }

        if (mutationSelectUI == null)
        {
            mutationSelectUI = FindAnyObjectByType<MutationSelectUI>(FindObjectsInactive.Include);
        }

        if (statUpgradeUI == null)
        {
            statUpgradeUI = FindAnyObjectByType<CharacterStatUpgradeUI>(FindObjectsInactive.Include);
        }

        if (mutationPickups == null || mutationPickups.Length == 0)
        {
            mutationPickups = FindObjectsByType<MutationPickup>(FindObjectsInactive.Include);
        }
    }

    private SkillMutationLoadoutBinder EnsureLoadoutBinder(SkillBase[] skills)
    {
        if (playerRoot == null)
        {
            return null;
        }

        SkillMutationLoadoutBinder binder = playerRoot.GetComponent<SkillMutationLoadoutBinder>();
        if (binder == null)
        {
            binder = playerRoot.AddComponent<SkillMutationLoadoutBinder>();
        }

        SetField(binder, "autoFindSkillsInChildren", true);
        SetField(binder, "addMissingLoadouts", true);
        SetField(binder, "skills", ToComponentArray(skills));
        binder.Rebuild();
        SetDirtyInEditor(binder);
        return binder;
    }

    private void EnsurePlayerInputAdapter()
    {
        if (playerRoot == null)
        {
            return;
        }

        PlayerSkillInputAdapter adapter = playerRoot.GetComponent<PlayerSkillInputAdapter>();
        if (adapter == null)
        {
            adapter = playerRoot.AddComponent<PlayerSkillInputAdapter>();
        }

        SetField(adapter, "skillSwitcher", skillSwitcher);
        SetDirtyInEditor(adapter);
    }

    private void WireSkillHud()
    {
        if (skillHud == null)
        {
            return;
        }

        SkillSelectionBinder binder = skillHud.GetComponent<SkillSelectionBinder>();
        if (binder == null)
        {
            binder = skillHud.gameObject.AddComponent<SkillSelectionBinder>();
        }

        SetField(binder, "skillView", skillHud);
        SetField(binder, "skillSwitcher", skillSwitcher);
        SetField(binder, "syncFromSwitcher", true);
        SetDirtyInEditor(binder);
    }

    private void WireMutationSelectUi(SkillMutationLoadoutBinder loadoutBinder, SkillBase[] skills)
    {
        if (mutationSelectUI == null)
        {
            return;
        }

        SetField(mutationSelectUI, "mutationLoadoutBinder", loadoutBinder);
        SetField(mutationSelectUI, "targetSkills", ToComponentArray(skills));
        SetDirtyInEditor(mutationSelectUI);
    }

    private void WireInitialStatUi()
    {
        if (statUpgradeUI == null)
        {
            return;
        }

        SetField(statUpgradeUI, "showOnStart", false);
        statUpgradeUI.Hide();
        SetDirtyInEditor(statUpgradeUI);
    }

    private void WireMutationPickups()
    {
        if (mutationPickups == null)
        {
            return;
        }

        for (int i = 0; i < mutationPickups.Length; i++)
        {
            MutationPickup pickup = mutationPickups[i];
            if (pickup == null)
            {
                continue;
            }

            if (defaultMutationData != null && GetField<MutationData>(pickup, "mutationData") == null)
            {
                SetField(pickup, "mutationData", defaultMutationData);
            }

            if (pickup.GetComponent<MutationPickupRuntimeGuard>() == null)
            {
                pickup.gameObject.AddComponent<MutationPickupRuntimeGuard>();
            }

            SetDirtyInEditor(pickup);
        }
    }

    private static SkillBase[] GetSwitcherSkills(SkillSwitcher switcher)
    {
        SkillBase[] skills = GetField<SkillBase[]>(switcher, "skills");
        if (skills != null && skills.Length > 0)
        {
            return skills;
        }

        return switcher.GetComponentsInChildren<SkillBase>(true);
    }

    private static Component[] ToComponentArray(SkillBase[] skills)
    {
        if (skills == null)
        {
            return Array.Empty<Component>();
        }

        Component[] components = new Component[skills.Length];
        for (int i = 0; i < skills.Length; i++)
        {
            components[i] = skills[i];
        }

        return components;
    }

    private static T GetField<T>(object target, string fieldName)
    {
        if (target == null)
        {
            return default;
        }

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null || !typeof(T).IsAssignableFrom(field.FieldType))
        {
            return default;
        }

        return (T)field.GetValue(target);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        if (target == null)
        {
            return;
        }

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
        {
            return;
        }

        field.SetValue(target, value);
    }

    private static void SetDirtyInEditor(UnityEngine.Object target)
    {
#if UNITY_EDITOR
        if (target != null && !Application.isPlaying)
        {
            EditorUtility.SetDirty(target);
        }
#endif
    }

#if UNITY_EDITOR
    private void QueueEditorAssemble()
    {
        if (editorAssembleQueued)
        {
            return;
        }

        editorAssembleQueued = true;
        EditorApplication.delayCall += RunQueuedEditorAssemble;
    }

    private void RunQueuedEditorAssemble()
    {
        editorAssembleQueued = false;

        if (this == null)
        {
            return;
        }

        Assemble();
    }
#endif
}
