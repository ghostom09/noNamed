using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ChangYong의 스탯 시스템(PlayerStatManager + PlayerStat 에셋)을 현재 씬에 보장하고
/// 상태 UI(CharacterStatUpgradeUI)와 연결한다.
/// - 씬에 PlayerStatManager 가 없으면 더미 플레이어에 추가하고 PlayerStat 에셋을 할당
/// - CharacterStatUpgradeUI.playerStatManager 참조를 그 매니저로 배선
/// 메뉴: Tools > ChaeWoon > Connect Status UI
/// </summary>
public static class ConnectStatusUi
{
    private const string DummyPlayerName = "MutationTestPlayer";
    private const string PlayerStatAssetPath = "Assets/ChangYong/TestStat.asset";

    [MenuItem("Tools/ChaeWoon/Connect Status UI")]
    private static void Connect()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog("Connect Status UI", "활성 씬이 없습니다. ui 씬을 먼저 열어주세요.", "확인");
            return;
        }

        CharacterStatUpgradeUI ui = Object.FindFirstObjectByType<CharacterStatUpgradeUI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            EditorUtility.DisplayDialog("Connect Status UI",
                "씬에서 CharacterStatUpgradeUI 를 찾지 못했습니다.", "확인");
            return;
        }

        // 1) 씬에 PlayerStatManager 확보 (없으면 더미에 추가)
        PlayerStatManager manager = Object.FindFirstObjectByType<PlayerStatManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            GameObject host = FindInScene(scene, DummyPlayerName);
            if (host == null)
            {
                EditorUtility.DisplayDialog("Connect Status UI",
                    $"PlayerStatManager 를 붙일 '{DummyPlayerName}' 더미를 찾지 못했습니다.", "확인");
                return;
            }

            manager = Undo.AddComponent<PlayerStatManager>(host);
            Debug.Log("[ConnectStatusUi] PlayerStatManager 추가 → " + host.name);
        }

        // 2) PlayerStat 에셋 할당 (비어 있으면)
        AssignPlayerStatAsset(manager);

        // 3) 상태 UI 에 매니저 연결
        SerializedObject so = new SerializedObject(ui);
        SerializedProperty prop = so.FindProperty("playerStatManager");
        if (prop != null)
        {
            prop.objectReferenceValue = manager;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
            Debug.Log("[ConnectStatusUi] CharacterStatUpgradeUI.playerStatManager ← " + manager.name);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ConnectStatusUi] 완료 — 스탯 매니저와 상태 UI 연결 후 저장.");
    }

    private static void AssignPlayerStatAsset(PlayerStatManager manager)
    {
        SerializedObject so = new SerializedObject(manager);
        SerializedProperty prop = so.FindProperty("playerStat");
        if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
        {
            return;
        }

        if (prop.objectReferenceValue != null)
        {
            return; // 이미 할당됨
        }

        PlayerStat statAsset = AssetDatabase.LoadAssetAtPath<PlayerStat>(PlayerStatAssetPath);
        if (statAsset == null)
        {
            Debug.LogWarning($"[ConnectStatusUi] PlayerStat 에셋을 찾지 못했습니다: {PlayerStatAssetPath}");
            return;
        }

        prop.objectReferenceValue = statAsset;
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[ConnectStatusUi] PlayerStat 에셋 할당: " + statAsset.name);
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
