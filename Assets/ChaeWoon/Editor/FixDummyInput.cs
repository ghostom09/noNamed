using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 더미 플레이어가 안 움직이는 문제 해결:
/// ChangYong PlayerTest 씬의 검증된 Input System "PlayerInput" 컴포넌트를 복사해
/// 더미에 붙이고, 입력 이벤트(Move/Dash 등)를 더미의 PlayerInputReader 로 재배선한다.
/// 메뉴: Tools > ChaeWoon > Fix Dummy Player Input
/// </summary>
public static class FixDummyInput
{
    private const string PlayerTestScenePath = "Assets/Scenes/PlayerTest.unity";
    private const string SourcePlayerName = "player";
    private const string DummyPlayerName = "MutationTestPlayer";

    [MenuItem("Tools/ChaeWoon/Fix Dummy Player Input")]
    private static void Fix()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("Fix Dummy Input", "활성 씬이 없습니다. ui 씬을 먼저 열어주세요.", "확인");
            return;
        }

        GameObject dummy = FindInScene(scene, DummyPlayerName);
        if (dummy == null)
        {
            EditorUtility.DisplayDialog("Fix Dummy Input",
                $"활성 씬에서 '{DummyPlayerName}' 더미를 찾지 못했습니다.", "확인");
            return;
        }

        Component reader = GetByTypeName(dummy, "PlayerInputReader");
        if (reader == null)
        {
            EditorUtility.DisplayDialog("Fix Dummy Input",
                "더미에 PlayerInputReader 가 없습니다. 먼저 ChangYong 플레이어 스크립트를 붙여주세요.", "확인");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(dummy, "Fix Dummy Player Input");

        // 이미 PlayerInput 이 있으면 중복 추가하지 않음
        Component existingInput = GetByTypeName(dummy, "PlayerInput");
        if (existingInput == null)
        {
            Scene playerScene = EditorSceneManager.OpenScene(PlayerTestScenePath, OpenSceneMode.Additive);
            GameObject source = playerScene.GetRootGameObjects().FirstOrDefault(go => go.name == SourcePlayerName);
            Component srcInput = source == null ? null : GetByTypeName(source, "PlayerInput");

            if (srcInput == null)
            {
                EditorSceneManager.CloseScene(playerScene, true);
                EditorUtility.DisplayDialog("Fix Dummy Input",
                    $"'{PlayerTestScenePath}' 의 '{SourcePlayerName}' 에서 PlayerInput 을 찾지 못했습니다.", "확인");
                return;
            }

            ComponentUtility.CopyComponent(srcInput);
            ComponentUtility.PasteComponentAsNew(dummy);
            EditorSceneManager.CloseScene(playerScene, true);
            Debug.Log("[FixDummyInput] Input System PlayerInput 컴포넌트 복사 부착 완료.");
        }

        // 입력 이벤트 타깃을 더미의 PlayerInputReader 로 재배선
        Component playerInput = GetByTypeName(dummy, "PlayerInput");
        RetargetActionEvents(playerInput, reader);

        // 중복 이동 스크립트 제거
        MonoBehaviour testController = dummy.GetComponents<MonoBehaviour>()
            .FirstOrDefault(c => c != null && c.GetType().Name == "MutationTestPlayerController");
        if (testController != null)
        {
            Object.DestroyImmediate(testController);
            Debug.Log("[FixDummyInput] 중복 이동 스크립트 제거: MutationTestPlayerController");
        }

        // 물리 안정화
        Rigidbody2D rb = dummy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        EditorUtility.SetDirty(dummy);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixDummyInput] 완료 — 더미 입력 배선 후 저장. 플레이해서 WASD/Shift 확인하세요.");
    }

    private static void RetargetActionEvents(Component playerInput, Component reader)
    {
        if (playerInput == null)
        {
            return;
        }

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
        Debug.Log($"[FixDummyInput] 입력 이벤트 {rewired}건 재배선 (→ 더미 PlayerInputReader).");
    }

    private static Component GetByTypeName(GameObject go, string typeName)
    {
        return go.GetComponents<Component>().FirstOrDefault(c => c != null && c.GetType().Name == typeName);
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
