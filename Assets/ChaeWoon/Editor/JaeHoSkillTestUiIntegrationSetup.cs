using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class JaeHoSkillTestUiIntegrationSetup
{
    private static readonly string[] ScenePaths =
    {
        "Assets/JaeHo/Scenes/Jaeho_SkillTest.unity",
        "Assets/Scenes/Jaeho_SkillTest.unity"
    };

    private const string ChaeWoonUiScenePath = "Assets/ChaeWoon/scene/ui.unity";
    private const string CombatHudPrefabPath = "Assets/ChaeWoon/Prefabs/UI/CombatHUDView.prefab";
    private const string TestMutationDataPath = "Assets/ChaeWoon/MutationData/TestMutationData.asset";
    private const string TestSkillDataFolder = "Assets/ChaeWoon/SkillData";
    private const string MutationItemSpritePath = "Assets/ChaeWoon/Sprites/ExampleMutationItem.png";

    [MenuItem("Tools/ChaeWoon/Setup JaeHo SkillTest UI")]
    public static void SetupAllSkillTestScenes()
    {
        foreach (string scenePath in ScenePaths)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                continue;
            }

            SetupScene(scenePath);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[JaeHoSkillTestUiIntegrationSetup] Setup complete.");
    }

    public static void SetupAndValidateBatch()
    {
        try
        {
            SetupAllSkillTestScenes();
            ValidateAllSkillTestScenes();
            EditorApplication.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/ChaeWoon/Validate JaeHo SkillTest UI")]
    public static void ValidateAllSkillTestScenes()
    {
        foreach (string scenePath in ScenePaths)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                continue;
            }

            ValidateScene(scenePath);
        }

        Debug.Log("[JaeHoSkillTestUiIntegrationSetup] Validation complete.");
    }

    private static void SetupScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        SkillSwitcher skillSwitcher = Object.FindFirstObjectByType<SkillSwitcher>(FindObjectsInactive.Include);

        if (skillSwitcher == null)
        {
            Debug.LogWarning($"[JaeHoSkillTestUiIntegrationSetup] SkillSwitcher not found: {scenePath}");
            return;
        }

        GameObject playerRoot = skillSwitcher.transform.root.gameObject;
        SkillMutationLoadoutBinder loadoutBinder = EnsureComponent<SkillMutationLoadoutBinder>(playerRoot);
        ConfigureLoadoutBinder(loadoutBinder);
        ConfigurePlayerSkillInputAdapter(playerRoot, skillSwitcher);

        Canvas canvas = EnsureCanvas(scene);
        EnsureEventSystem(scene);

        SkillBase[] orderedSkills = GetSwitcherSkills(skillSwitcher);
        EnsureCombatHud(canvas, orderedSkills);
        EnsureMutationUi(scene, canvas, loadoutBinder, orderedSkills);
        EnsureTestMutationPickup(scene, playerRoot);

        loadoutBinder.Rebuild();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[JaeHoSkillTestUiIntegrationSetup] Wired ChaeWoon UI to {scenePath}.");
    }

    private static void ConfigureLoadoutBinder(SkillMutationLoadoutBinder binder)
    {
        SerializedObject serializedBinder = new(binder);
        SetBool(serializedBinder, "autoFindSkillsInChildren", true);
        SetBool(serializedBinder, "addMissingLoadouts", true);
        serializedBinder.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurePlayerSkillInputAdapter(GameObject playerRoot, SkillSwitcher skillSwitcher)
    {
        PlayerSkillInputAdapter inputAdapter = playerRoot.GetComponent<PlayerSkillInputAdapter>();
        if (inputAdapter == null)
        {
            return;
        }

        SerializedObject serializedAdapter = new(inputAdapter);
        SerializedProperty switcherProperty = serializedAdapter.FindProperty("skillSwitcher");
        if (switcherProperty != null)
        {
            switcherProperty.objectReferenceValue = skillSwitcher;
        }

        serializedAdapter.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(inputAdapter);
    }

    private static Canvas EnsureCanvas(Scene scene)
    {
        Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate != null && candidate.gameObject.scene == scene);

        if (canvas == null)
        {
            GameObject canvasObject = new("ChaeWoonCombatCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            EditorSceneManager.MoveGameObjectToScene(canvasObject, scene);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        else if (canvas.name == "ChaeWoonCombatHUD")
        {
            canvas.name = "ChaeWoonCombatCanvas";
        }

        if (canvas.GetComponent<CanvasScaler>() == null)
        {
            canvas.gameObject.AddComponent<CanvasScaler>();
        }

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void EnsureEventSystem(Scene scene)
    {
        EventSystem eventSystem = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate != null && candidate.gameObject.scene == scene);

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem));
            EditorSceneManager.MoveGameObjectToScene(eventSystemObject, scene);
            eventSystem = eventSystemObject.GetComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
#else
        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
#endif
    }

    private static void EnsureCombatHud(Canvas canvas, SkillBase[] orderedSkills)
    {
        RemoveDuplicateSkillHud(canvas);

        Skill existingSkillView = Object.FindObjectsByType<Skill>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate != null && candidate.gameObject.scene == canvas.gameObject.scene);

        GameObject hudObject = existingSkillView == null
            ? InstantiatePrefab(CombatHudPrefabPath, canvas.transform)
            : FindTopLevelUnderParent(existingSkillView.transform, canvas.transform).gameObject;

        hudObject.name = "ChaeWoonCombatHUD";
        SetStretchToParent(hudObject.GetComponent<RectTransform>());

        Skill skillView = hudObject.GetComponentInChildren<Skill>(true);
        if (skillView != null)
        {
            ConfigureSkillTooltipData(skillView, orderedSkills);
            if (skillView.GetComponent<SkillSelectionBinder>() == null)
            {
                skillView.gameObject.AddComponent<SkillSelectionBinder>();
            }
        }
    }

    private static void RemoveDuplicateSkillHud(Canvas canvas)
    {
        Skill[] skillViews = Object.FindObjectsByType<Skill>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(candidate => candidate != null && candidate.gameObject.scene == canvas.gameObject.scene)
            .ToArray();

        if (skillViews.Length <= 1)
        {
            return;
        }

        for (int i = 1; i < skillViews.Length; i++)
        {
            Transform hudRoot = FindTopLevelUnderParent(skillViews[i].transform, canvas.transform);
            if (hudRoot != null && hudRoot != canvas.transform)
            {
                Object.DestroyImmediate(hudRoot.gameObject);
            }
        }
    }

    private static Transform FindTopLevelUnderParent(Transform child, Transform parent)
    {
        if (child == null || parent == null)
        {
            return child;
        }

        Transform current = child;
        while (current.parent != null && current.parent != parent)
        {
            current = current.parent;
        }

        return current;
    }

    private static void EnsureMutationUi(Scene targetScene, Canvas canvas, SkillMutationLoadoutBinder loadoutBinder, SkillBase[] orderedSkills)
    {
        MutationDescriptionUI mutationDescriptionUi = EnsureUiComponentCopied<MutationDescriptionUI>(targetScene, canvas.transform);
        MutationSelectUI mutationSelectUi = EnsureUiComponentCopied<MutationSelectUI>(targetScene, canvas.transform);
        RemoveDuplicateComponents<MutationDescriptionUI>(targetScene);
        RemoveDuplicateComponents<MutationSelectUI>(targetScene);

        if (mutationDescriptionUi != null)
        {
            mutationDescriptionUi.gameObject.name = "MutationDescriptionUIRoot";
            mutationDescriptionUi.transform.localScale = Vector3.one;
        }

        if (mutationSelectUi == null)
        {
            return;
        }

        mutationSelectUi.gameObject.name = "MutationSelectUIRoot";
        mutationSelectUi.transform.localScale = Vector3.one;

        SerializedObject serializedUi = new(mutationSelectUi);
        SetObject(serializedUi, "mutationLoadoutBinder", loadoutBinder);
        ConfigureTargetSkills(serializedUi, orderedSkills);
        ConfigureSkillDisplayData(serializedUi);
        serializedUi.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(mutationSelectUi);
    }

    private static T EnsureUiComponentCopied<T>(Scene targetScene, Transform parent) where T : Component
    {
        T existing = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate != null && candidate.gameObject.scene == targetScene);

        if (existing != null)
        {
            existing.transform.SetParent(parent, false);
            return existing;
        }

        Scene sourceScene = EditorSceneManager.OpenScene(ChaeWoonUiScenePath, OpenSceneMode.Additive);
        T source = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate != null && candidate.gameObject.scene == sourceScene);

        if (source == null)
        {
            EditorSceneManager.CloseScene(sourceScene, true);
            Debug.LogWarning($"[JaeHoSkillTestUiIntegrationSetup] {typeof(T).Name} source not found.");
            return null;
        }

        GameObject copy = Object.Instantiate(source.gameObject);
        copy.name = source.gameObject.name;
        EditorSceneManager.MoveGameObjectToScene(copy, targetScene);
        copy.transform.SetParent(parent, false);
        EditorSceneManager.CloseScene(sourceScene, true);
        return copy.GetComponent<T>();
    }

    private static void RemoveDuplicateComponents<T>(Scene targetScene) where T : Component
    {
        T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(candidate => candidate != null && candidate.gameObject.scene == targetScene)
            .ToArray();

        if (components.Length <= 1)
        {
            return;
        }

        for (int i = 1; i < components.Length; i++)
        {
            Object.DestroyImmediate(components[i].gameObject);
        }
    }

    private static void EnsureTestMutationPickup(Scene scene, GameObject playerRoot)
    {
        MutationPickup existing = Object.FindObjectsByType<MutationPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate != null && candidate.gameObject.scene == scene);

        MutationData mutationData = AssetDatabase.LoadAssetAtPath<MutationData>(TestMutationDataPath);
        Sprite itemSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MutationItemSpritePath);

        if (existing == null)
        {
            GameObject pickupObject = new("ChaeWoonMutationPickup_Test", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(MutationPickup));
            EditorSceneManager.MoveGameObjectToScene(pickupObject, scene);
            pickupObject.transform.position = playerRoot.transform.position + new Vector3(2f, 0f, 0f);

            SpriteRenderer spriteRenderer = pickupObject.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = itemSprite;
            spriteRenderer.sortingOrder = 50;

            CircleCollider2D trigger = pickupObject.GetComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.8f;

            existing = pickupObject.GetComponent<MutationPickup>();
        }

        SerializedObject serializedPickup = new(existing);
        SetObject(serializedPickup, "mutationData", mutationData);
        serializedPickup.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(existing);
    }

    private static SkillBase[] GetSwitcherSkills(SkillSwitcher skillSwitcher)
    {
        SerializedObject serializedSwitcher = new(skillSwitcher);
        SerializedProperty skillsProperty = serializedSwitcher.FindProperty("skills");

        if (skillsProperty == null || !skillsProperty.isArray)
        {
            return skillSwitcher.GetComponentsInChildren<SkillBase>(true);
        }

        List<SkillBase> orderedSkills = new();
        for (int i = 0; i < skillsProperty.arraySize; i++)
        {
            Object value = skillsProperty.GetArrayElementAtIndex(i).objectReferenceValue;
            if (value is SkillBase skill)
            {
                orderedSkills.Add(skill);
            }
        }

        return orderedSkills.Count > 0 ? orderedSkills.ToArray() : skillSwitcher.GetComponentsInChildren<SkillBase>(true);
    }

    private static void ConfigureTargetSkills(SerializedObject serializedUi, SkillBase[] orderedSkills)
    {
        SerializedProperty targetSkills = serializedUi.FindProperty("targetSkills");
        if (targetSkills == null || !targetSkills.isArray)
        {
            return;
        }

        int size = Mathf.Max(4, orderedSkills.Length);
        targetSkills.arraySize = size;

        for (int i = 0; i < size; i++)
        {
            targetSkills.GetArrayElementAtIndex(i).objectReferenceValue = i < orderedSkills.Length ? orderedSkills[i] : null;
        }
    }

    private static void ConfigureSkillDisplayData(SerializedObject serializedUi)
    {
        SerializedProperty displayData = serializedUi.FindProperty("skillDisplayData");
        if (displayData == null || !displayData.isArray)
        {
            return;
        }

        SkillData[] skillData = LoadSkillDataAssets();
        int size = Mathf.Max(4, skillData.Length);
        displayData.arraySize = size;

        for (int i = 0; i < size; i++)
        {
            displayData.GetArrayElementAtIndex(i).objectReferenceValue = i < skillData.Length ? skillData[i] : null;
        }
    }

    private static void ConfigureSkillTooltipData(Skill skillView, SkillBase[] orderedSkills)
    {
        SerializedObject serializedSkill = new(skillView);
        SerializedProperty tooltipData = serializedSkill.FindProperty("skillTooltipData");

        if (tooltipData == null || !tooltipData.isArray)
        {
            return;
        }

        SkillData[] displayData = LoadSkillDataAssets();
        int size = Mathf.Max(orderedSkills.Length, displayData.Length);
        tooltipData.arraySize = size;

        for (int i = 0; i < size; i++)
        {
            SerializedProperty item = tooltipData.GetArrayElementAtIndex(i);
            string skillName = i < orderedSkills.Length && orderedSkills[i] != null
                ? orderedSkills[i].name
                : i < displayData.Length ? displayData[i].skillName : $"Skill {i + 1}";

            string description = i < displayData.Length ? displayData[i].skillDescription : string.Empty;
            item.FindPropertyRelative("skillName").stringValue = skillName;
            item.FindPropertyRelative("description").stringValue = description;
            item.FindPropertyRelative("detailStats").stringValue = "JaeHo SkillSwitcher와 연결됨";
        }

        serializedSkill.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(skillView);
    }

    private static SkillData[] LoadSkillDataAssets()
    {
        return AssetDatabase.FindAssets("t:SkillData", new[] { TestSkillDataFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path)
            .Select(AssetDatabase.LoadAssetAtPath<SkillData>)
            .Where(asset => asset != null)
            .ToArray();
    }

    private static GameObject InstantiatePrefab(string prefabPath, Transform parent)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            throw new System.InvalidOperationException($"Prefab not found: {prefabPath}");
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.SetParent(parent, false);
        return instance;
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void SetStretchToParent(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void ValidateScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        List<string> errors = new();

        SkillSwitcher switcher = Object.FindFirstObjectByType<SkillSwitcher>(FindObjectsInactive.Include);
        SkillMutationLoadoutBinder binder = Object.FindFirstObjectByType<SkillMutationLoadoutBinder>(FindObjectsInactive.Include);
        Skill skillView = Object.FindFirstObjectByType<Skill>(FindObjectsInactive.Include);
        SkillSelectionBinder selectionBinder = Object.FindFirstObjectByType<SkillSelectionBinder>(FindObjectsInactive.Include);
        MutationSelectUI mutationSelectUi = Object.FindFirstObjectByType<MutationSelectUI>(FindObjectsInactive.Include);
        MutationDescriptionUI mutationDescriptionUi = Object.FindFirstObjectByType<MutationDescriptionUI>(FindObjectsInactive.Include);
        MutationPickup pickup = Object.FindFirstObjectByType<MutationPickup>(FindObjectsInactive.Include);
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        if (switcher == null) errors.Add("SkillSwitcher missing");
        if (binder == null) errors.Add("SkillMutationLoadoutBinder missing");
        if (skillView == null) errors.Add("ChaeWoon Skill HUD missing");
        if (selectionBinder == null) errors.Add("SkillSelectionBinder missing");
        if (mutationSelectUi == null) errors.Add("MutationSelectUI missing");
        if (mutationDescriptionUi == null) errors.Add("MutationDescriptionUI missing");
        if (pickup == null) errors.Add("MutationPickup missing");
        if (eventSystem == null) errors.Add("EventSystem missing");
        if (canvas == null) errors.Add("Canvas missing");
        AddDuplicateError<MutationSelectUI>(scene, errors);
        AddDuplicateError<MutationDescriptionUI>(scene, errors);
        AddDuplicateError<Skill>(scene, errors);

        if (switcher != null)
        {
            PlayerSkillInputAdapter inputAdapter = switcher.GetComponentInParent<PlayerSkillInputAdapter>();
            if (inputAdapter == null)
            {
                errors.Add("PlayerSkillInputAdapter missing near SkillSwitcher");
            }
            else if (GetObjectReference(inputAdapter, "skillSwitcher") != switcher)
            {
                errors.Add("PlayerSkillInputAdapter.skillSwitcher is not wired");
            }
        }

        if (mutationSelectUi != null)
        {
            if (GetObjectReference(mutationSelectUi, "mutationLoadoutBinder") == null)
            {
                errors.Add("MutationSelectUI.mutationLoadoutBinder is not wired");
            }

            if (CountObjectReferences(mutationSelectUi, "targetSkills") == 0)
            {
                errors.Add("MutationSelectUI.targetSkills has no runtime skills");
            }
        }

        if (pickup != null && GetObjectReference(pickup, "mutationData") == null)
        {
            errors.Add("MutationPickup.mutationData is not wired");
        }

        if (switcher != null && binder != null)
        {
            binder.Rebuild();
            SkillBase[] skills = GetSwitcherSkills(switcher);
            if (skills.Length == 0)
            {
                errors.Add("SkillSwitcher has no SkillBase entries");
            }

            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] == null)
                {
                    errors.Add($"SkillSwitcher skill slot {i} is null");
                    continue;
                }

                if (skills[i].GetComponent<MutationLoadout>() == null)
                {
                    errors.Add($"{skills[i].name} has no MutationLoadout");
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new System.InvalidOperationException($"{scenePath} validation failed: {string.Join(", ", errors)}");
        }

        Debug.Log($"[JaeHoSkillTestUiIntegrationSetup] Validation passed: {scenePath}");
    }

    private static void AddDuplicateError<T>(Scene scene, List<string> errors) where T : Component
    {
        int count = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(candidate => candidate != null && candidate.gameObject.scene == scene);

        if (count > 1)
        {
            errors.Add($"{typeof(T).Name} duplicate count: {count}");
        }
    }

    private static Object GetObjectReference(Object target, string propertyName)
    {
        SerializedObject serializedObject = new(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        return property == null ? null : property.objectReferenceValue;
    }

    private static int CountObjectReferences(Object target, string propertyName)
    {
        SerializedObject serializedObject = new(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null || !property.isArray)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < property.arraySize; i++)
        {
            if (property.GetArrayElementAtIndex(i).objectReferenceValue != null)
            {
                count++;
            }
        }

        return count;
    }
}
