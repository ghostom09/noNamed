using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ui 씬에서 더미 플레이어(MutationTestPlayer)를 제거하고,
/// ChangYong의 실제 캐릭터(PlayerTest.unity의 "player")를 가져와
/// CharacterStatUpgradeUI 와 연동해 하이어라키에 올린다.
/// 메뉴: Tools > ChaeWoon > Setup Player + Stat UI
/// </summary>
public static class UiScenePlayerSetup
{
    private const string PlayerTestScenePath = "Assets/Scenes/PlayerTest.unity";
    private const string SourcePlayerName = "player";
    private const string DummyPlayerName = "MutationTestPlayer";
    private const string DummyRootName = "MutationTestObjects";
    private const string PlacedPlayerName = "Player";

    [MenuItem("Tools/ChaeWoon/Setup Player + Stat UI")]
    private static void Setup()
    {
        Scene uiScene = SceneManager.GetActiveScene();

        if (!uiScene.IsValid() || !uiScene.isLoaded)
        {
            EditorUtility.DisplayDialog("Setup Player", "활성 씬이 없습니다. ui 씬을 먼저 열어주세요.", "확인");
            return;
        }

        // 이미 배치된 플레이어가 있는지 검사 (중복 실행 방지)
        PlayerStatManager existing = Object.FindObjectsByType<PlayerStatManager>(FindObjectsSortMode.None)
            .FirstOrDefault();

        // 1) 더미 플레이어 제거
        RemoveDummy(uiScene);

        // 2) ChangYong 캐릭터 가져오기 (이미 있으면 그대로 재사용)
        PlayerStatManager statManager = existing;
        if (statManager == null)
        {
            GameObject placed = ImportChangYongPlayer(uiScene);
            if (placed == null)
            {
                return;
            }

            statManager = placed.GetComponent<PlayerStatManager>();
            Undo.RegisterCreatedObjectUndo(placed, "Setup Player + Stat UI");
        }

        // 3) CharacterStatUpgradeUI 와 연동
        WireStatUi(statManager);

        EditorSceneManager.MarkSceneDirty(uiScene);
        EditorSceneManager.SaveScene(uiScene);
        Debug.Log("[UiScenePlayerSetup] 완료 — 더미 제거 + ChangYong 캐릭터 배치 + 스탯 UI 연동 후 씬 저장됨.");
    }

    private static void RemoveDummy(Scene uiScene)
    {
        foreach (GameObject root in uiScene.GetRootGameObjects())
        {
            // 더미 플레이어만 정확히 지목해 제거
            Transform dummy = FindInChildrenByName(root.transform, DummyPlayerName);
            if (dummy != null)
            {
                Transform parent = dummy.parent;
                Undo.DestroyObjectImmediate(dummy.gameObject);
                Debug.Log("[UiScenePlayerSetup] 더미 플레이어 제거: " + DummyPlayerName);

                // 더미 전용 컨테이너가 비었으면 같이 정리
                if (parent != null && parent.name == DummyRootName && parent.childCount == 0)
                {
                    Undo.DestroyObjectImmediate(parent.gameObject);
                    Debug.Log("[UiScenePlayerSetup] 빈 컨테이너 제거: " + DummyRootName);
                }
                return;
            }
        }

        Debug.Log("[UiScenePlayerSetup] 제거할 더미 플레이어가 없습니다 (이미 정리됨).");
    }

    private static GameObject ImportChangYongPlayer(Scene uiScene)
    {
        Scene playerScene = EditorSceneManager.OpenScene(PlayerTestScenePath, OpenSceneMode.Additive);

        GameObject source = playerScene.GetRootGameObjects()
            .FirstOrDefault(go => go.name == SourcePlayerName);

        if (source == null)
        {
            EditorSceneManager.CloseScene(playerScene, true);
            EditorUtility.DisplayDialog("Setup Player",
                $"'{PlayerTestScenePath}' 에서 '{SourcePlayerName}' 오브젝트를 찾지 못했습니다.", "확인");
            return null;
        }

        GameObject copy = Object.Instantiate(source);
        copy.name = PlacedPlayerName;
        EditorSceneManager.MoveGameObjectToScene(copy, uiScene);

        // PlayerTest 씬은 변경 없이 닫는다
        EditorSceneManager.CloseScene(playerScene, true);

        // ChangYong 전용 스탯 패널 참조는 끊는다 (ui 씬에는 그 패널이 없고, CharacterStatUpgradeUI 를 쓴다)
        ClearStatPanelReference(copy);

        Debug.Log("[UiScenePlayerSetup] ChangYong 캐릭터 배치 완료: " + PlacedPlayerName);
        return copy;
    }

    private static void ClearStatPanelReference(GameObject player)
    {
        foreach (MonoBehaviour mb in player.GetComponents<MonoBehaviour>())
        {
            if (mb == null || mb.GetType().Name != "PlayerInputReader")
            {
                continue;
            }

            SerializedObject so = new SerializedObject(mb);
            SerializedProperty prop = so.FindProperty("_statPanel");
            if (prop != null)
            {
                prop.objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }

    private static void WireStatUi(PlayerStatManager statManager)
    {
        if (statManager == null)
        {
            Debug.LogWarning("[UiScenePlayerSetup] PlayerStatManager 가 없어 UI 연동을 건너뜁니다.");
            return;
        }

        CharacterStatUpgradeUI ui = Object.FindFirstObjectByType<CharacterStatUpgradeUI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            Debug.LogWarning("[UiScenePlayerSetup] CharacterStatUpgradeUI 인스턴스를 찾지 못했습니다.");
            return;
        }

        SerializedObject so = new SerializedObject(ui);
        SerializedProperty prop = so.FindProperty("playerStatManager");
        if (prop != null)
        {
            prop.objectReferenceValue = statManager;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);
            Debug.Log("[UiScenePlayerSetup] CharacterStatUpgradeUI.playerStatManager ← " + statManager.name);
        }
    }

    private static Transform FindInChildrenByName(Transform root, string targetName)
    {
        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindInChildrenByName(root.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
