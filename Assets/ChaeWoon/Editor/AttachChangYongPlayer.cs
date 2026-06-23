using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ChangYong의 플레이어 로직 컴포넌트(PlayerStatManager / PlayerMove / PlayerAttack /
/// PlayerInputReader / Input System PlayerInput)를 ui 씬의 더미 플레이어
/// (MutationTestPlayer)에 복사해 붙이고, Input System 이벤트 배선을 더미의
/// PlayerInputReader로 다시 연결해 실제로 움직이게 만든다.
/// 메뉴: Tools > ChaeWoon > Attach ChangYong Player To Dummy
/// </summary>
public static class AttachChangYongPlayer
{
    private const string PlayerTestScenePath = "Assets/Scenes/PlayerTest.unity";
    private const string SourcePlayerName = "player";
    private const string DummyPlayerName = "MutationTestPlayer";

    // ChangYong 플레이어에서 더미로 옮길 컴포넌트 타입명 (붙일 순서)
    private static readonly string[] CopyComponentTypeNames =
    {
        "PlayerStatManager",
        "PlayerMove",
        "PlayerAttack",
        "PlayerInputReader",
        "PlayerInput", // UnityEngine.InputSystem.PlayerInput
    };

    [MenuItem("Tools/ChaeWoon/Attach ChangYong Player To Dummy")]
    private static void Attach()
    {
        Scene uiScene = SceneManager.GetActiveScene();
        if (!uiScene.IsValid() || !uiScene.isLoaded)
        {
            EditorUtility.DisplayDialog("Attach Player", "활성 씬이 없습니다. ui 씬을 먼저 열어주세요.", "확인");
            return;
        }

        GameObject dummy = FindInScene(uiScene, DummyPlayerName);
        if (dummy == null)
        {
            EditorUtility.DisplayDialog("Attach Player",
                $"활성 씬에서 '{DummyPlayerName}' 더미 플레이어를 찾지 못했습니다.", "확인");
            return;
        }

        if (dummy.GetComponent<PlayerMove>() != null)
        {
            Debug.Log("[AttachChangYongPlayer] 더미에 이미 PlayerMove가 있어 중단합니다 (이미 적용됨).");
            return;
        }

        // 1) ChangYong player 가져오기
        Scene playerScene = EditorSceneManager.OpenScene(PlayerTestScenePath, OpenSceneMode.Additive);
        GameObject source = playerScene.GetRootGameObjects().FirstOrDefault(go => go.name == SourcePlayerName);
        if (source == null)
        {
            EditorSceneManager.CloseScene(playerScene, true);
            EditorUtility.DisplayDialog("Attach Player",
                $"'{PlayerTestScenePath}' 에서 '{SourcePlayerName}' 오브젝트를 찾지 못했습니다.", "확인");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(dummy, "Attach ChangYong Player");

        // 2) 컴포넌트 복사 → 붙여넣기 (직렬화된 값/에셋 참조/이벤트 구조 보존)
        foreach (string typeName in CopyComponentTypeNames)
        {
            Component src = source.GetComponents<Component>()
                .FirstOrDefault(c => c != null && c.GetType().Name == typeName);

            if (src == null)
            {
                Debug.LogWarning($"[AttachChangYongPlayer] source에 {typeName} 컴포넌트가 없어 건너뜀.");
                continue;
            }

            ComponentUtility.CopyComponent(src);
            if (!ComponentUtility.PasteComponentAsNew(dummy))
            {
                Debug.LogWarning($"[AttachChangYongPlayer] {typeName} 붙여넣기 실패.");
            }
        }

        EditorSceneManager.CloseScene(playerScene, true);

        // 3) 더미의 PlayerInputReader 로 입력 이벤트 재배선
        Component reader = dummy.GetComponents<Component>()
            .FirstOrDefault(c => c != null && c.GetType().Name == "PlayerInputReader");
        Component playerInput = dummy.GetComponents<Component>()
            .FirstOrDefault(c => c != null && c.GetType().Name == "PlayerInput");

        if (playerInput != null && reader != null)
        {
            RetargetActionEvents(playerInput, reader);
        }

        // 4) ChangYong 전용 스탯 패널 참조 끊기 (ui 씬엔 없음)
        if (reader != null)
        {
            ClearObjectField(reader, "_statPanel");
        }

        // 5) 중복 이동 스크립트 제거 (더미 자체 이동 로직과 충돌)
        MonoBehaviour testController = dummy.GetComponents<MonoBehaviour>()
            .FirstOrDefault(c => c != null && c.GetType().Name == "MutationTestPlayerController");
        if (testController != null)
        {
            Object.DestroyImmediate(testController);
            Debug.Log("[AttachChangYongPlayer] 더미 이동 스크립트 제거: MutationTestPlayerController");
        }

        // 6) 물리 안정화 (회전 고정)
        Rigidbody2D rb = dummy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        // 7) 스탯 UI 연동
        PlayerStatManager statManager = dummy.GetComponent<PlayerStatManager>();
        WireStatUi(statManager);

        EditorUtility.SetDirty(dummy);
        EditorSceneManager.MarkSceneDirty(uiScene);
        EditorSceneManager.SaveScene(uiScene);
        Debug.Log("[AttachChangYongPlayer] 완료 — 더미 플레이어에 ChangYong 로직 부착 + 입력 배선 + 스탯 UI 연동 후 저장.");
    }

    /// <summary>PlayerInput.m_ActionEvents 의 모든 호출 타깃을 더미의 PlayerInputReader 로 교체.</summary>
    private static void RetargetActionEvents(Component playerInput, Component reader)
    {
        SerializedObject so = new SerializedObject(playerInput);
        SerializedProperty events = so.FindProperty("m_ActionEvents");
        if (events == null)
        {
            return;
        }

        int rewired = 0;
        for (int i = 0; i < events.arraySize; i++)
        {
            SerializedProperty calls = events.GetArrayElementAtIndex(i)
                .FindPropertyRelative("m_PersistentCalls.m_Calls");
            if (calls == null)
            {
                continue;
            }

            for (int j = 0; j < calls.arraySize; j++)
            {
                SerializedProperty target = calls.GetArrayElementAtIndex(j).FindPropertyRelative("m_Target");
                if (target != null && target.objectReferenceValue != null)
                {
                    target.objectReferenceValue = reader;
                    rewired++;
                }
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[AttachChangYongPlayer] 입력 이벤트 {rewired}건 재배선 (→ 더미 PlayerInputReader).");
    }

    private static void ClearObjectField(Component component, string fieldName)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
        {
            prop.objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void WireStatUi(PlayerStatManager statManager)
    {
        if (statManager == null)
        {
            Debug.LogWarning("[AttachChangYongPlayer] PlayerStatManager 가 없어 UI 연동을 건너뜁니다.");
            return;
        }

        CharacterStatUpgradeUI ui = Object.FindFirstObjectByType<CharacterStatUpgradeUI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(ui);
        SerializedProperty prop = so.FindProperty("playerStatManager");
        if (prop != null)
        {
            prop.objectReferenceValue = statManager;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
            Debug.Log("[AttachChangYongPlayer] CharacterStatUpgradeUI.playerStatManager ← 더미 PlayerStatManager");
        }
    }

    private static GameObject FindInScene(Scene scene, string targetName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform found = FindRecursive(root.transform, targetName);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindRecursive(Transform root, string targetName)
    {
        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindRecursive(root.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
