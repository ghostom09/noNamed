using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class RoomClearMutationRewardSystem : MonoBehaviour
{
    private enum RewardUiKind
    {
        None,
        Mutation,
        StatUpgrade
    }

    private static RoomClearMutationRewardSystem instance;
    private static readonly HashSet<string> RewardedRoomKeys = new();

    [Header("Mutation Pool")]
    [SerializeField] private MutationData[] mutationPool = Array.Empty<MutationData>();
    [SerializeField] private bool includeLoadedMutationAssets = true;

    [Header("Stat Reward")]
    [SerializeField, Min(1)] private int statRewardPoints = 1;

    [Header("Pause")]
    [SerializeField] private bool pauseWhileRewardUiIsOpen = true;

    private bool pausedByRewardUi;
    private float timeScaleBeforePause = 1f;
    private bool rewardInProgress;
    private RewardUiKind pendingRewardKind;
    private Action pendingRewardCompleted;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            ClearPendingReward();
            EndPause();
            instance = null;
        }
    }

    public static void ResetRewardHistory()
    {
        RewardedRoomKeys.Clear();
    }

    public static bool TryPlayReward(RoomNode room, Action onRewardCompleted)
    {
        RoomClearMutationRewardSystem system = GetOrCreateInstance();
        return system != null && system.BeginReward(room, onRewardCompleted);
    }

    private static RoomClearMutationRewardSystem GetOrCreateInstance()
    {
        if (instance != null)
            return instance;

        instance = FindAnyObjectByType<RoomClearMutationRewardSystem>(FindObjectsInactive.Include);
        if (instance != null)
            return instance;

        GameObject rewardSystemObject = new GameObject("[Room Clear Reward System]");
        DontDestroyOnLoad(rewardSystemObject);
        instance = rewardSystemObject.AddComponent<RoomClearMutationRewardSystem>();
        return instance;
    }

    private bool BeginReward(RoomNode room, Action onRewardCompleted)
    {
        if (room == null)
            return false;

        RewardUiKind rewardKind = GetRewardUiKind(room.RewardType);
        if (rewardKind == RewardUiKind.None)
            return false;

        string roomKey = GetRoomRewardKey(room);
        if (RewardedRoomKeys.Contains(roomKey))
            return false;

        if (rewardInProgress)
        {
            Debug.LogWarning("[RoomClearMutationRewardSystem] Reward is already in progress.");
            return false;
        }

        bool started = rewardKind switch
        {
            RewardUiKind.Mutation => BeginMutationReward(onRewardCompleted),
            RewardUiKind.StatUpgrade => BeginStatReward(onRewardCompleted),
            _ => false
        };

        if (started)
            RewardedRoomKeys.Add(roomKey);

        return started;
    }

    private static RewardUiKind GetRewardUiKind(RewardType rewardType)
    {
        return rewardType switch
        {
            RewardType.LabReward => RewardUiKind.Mutation,
            RewardType.ArchiveReward => RewardUiKind.Mutation,
            RewardType.ContainmentReward => RewardUiKind.Mutation,
            RewardType.BossClear => RewardUiKind.Mutation,
            RewardType.RestReward => RewardUiKind.StatUpgrade,
            _ => RewardUiKind.None
        };
    }

    private bool BeginMutationReward(Action onRewardCompleted)
    {
        MutationData mutation = PickApplicableMutation();
        if (mutation == null)
        {
            Debug.LogWarning("[RoomClearMutationRewardSystem] No applicable MutationData found.");
            return false;
        }

        MutationSelectUI ui = MutationSelectUI.Instance;
        if (ui == null)
        {
            Debug.LogWarning("[RoomClearMutationRewardSystem] MutationSelectUI.Instance was not found.");
            return false;
        }

        rewardInProgress = true;
        pendingRewardKind = RewardUiKind.Mutation;
        pendingRewardCompleted = onRewardCompleted;

        ui.SelectionCompleted -= HandleMutationRewardCompleted;
        ui.SelectionCompleted += HandleMutationRewardCompleted;
        ui.Show(mutation);

        BeginPause();
        return true;
    }

    private bool BeginStatReward(Action onRewardCompleted)
    {
        CharacterStatUpgradeUI ui = FindAnyObjectByType<CharacterStatUpgradeUI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            Debug.LogWarning("[RoomClearMutationRewardSystem] CharacterStatUpgradeUI was not found.");
            return false;
        }

        GrantStatRewardPoint();

        rewardInProgress = true;
        pendingRewardKind = RewardUiKind.StatUpgrade;
        pendingRewardCompleted = onRewardCompleted;

        ui.UpgradeConfirmed -= HandleStatRewardCompleted;
        ui.UpgradeConfirmed += HandleStatRewardCompleted;
        ui.Show();

        return true;
    }

    private void GrantStatRewardPoint()
    {
        PlayerStatManager statManager = FindAnyObjectByType<PlayerStatManager>(FindObjectsInactive.Include);
        if (statManager != null)
            statManager.AddAvailablePoints(statRewardPoints);
    }

    private void HandleMutationRewardCompleted()
    {
        if (pendingRewardKind != RewardUiKind.Mutation)
            return;

        MutationSelectUI ui = MutationSelectUI.Instance;
        if (ui != null)
        {
            ui.SelectionCompleted -= HandleMutationRewardCompleted;
            ui.Hide();
        }

        CompletePendingReward();
    }

    private void HandleStatRewardCompleted()
    {
        if (pendingRewardKind != RewardUiKind.StatUpgrade)
            return;

        CharacterStatUpgradeUI ui = FindAnyObjectByType<CharacterStatUpgradeUI>(FindObjectsInactive.Include);
        if (ui != null)
        {
            ui.UpgradeConfirmed -= HandleStatRewardCompleted;
            ui.Hide();
        }

        CompletePendingReward();
    }

    private void CompletePendingReward()
    {
        Action rewardCompleted = pendingRewardCompleted;
        ClearPendingReward();
        EndPause();
        rewardCompleted?.Invoke();
    }

    private void ClearPendingReward()
    {
        if (pendingRewardKind == RewardUiKind.Mutation && MutationSelectUI.Instance != null)
            MutationSelectUI.Instance.SelectionCompleted -= HandleMutationRewardCompleted;

        CharacterStatUpgradeUI statUi = FindAnyObjectByType<CharacterStatUpgradeUI>(FindObjectsInactive.Include);
        if (statUi != null)
            statUi.UpgradeConfirmed -= HandleStatRewardCompleted;

        rewardInProgress = false;
        pendingRewardKind = RewardUiKind.None;
        pendingRewardCompleted = null;
    }

    private static string GetRoomRewardKey(RoomNode room)
    {
        int floor = FloorManager.Instance != null ? FloorManager.Instance.GetCurrentFloor() : 0;
        return $"{floor}:{room.Id}";
    }

    private MutationData PickApplicableMutation()
    {
        List<MutationData> candidates = GetMutationCandidates();
        if (candidates.Count == 0) return null;

        SkillMutationLoadoutBinder binder = FindAnyObjectByType<SkillMutationLoadoutBinder>(FindObjectsInactive.Include);
        if (binder == null) return null;

        binder.Rebuild();

        List<MutationData> applicable = new();
        for (int i = 0; i < candidates.Count; i++)
        {
            MutationData mutation = candidates[i];
            if (mutation != null && CanApplyToAnySkill(mutation, binder))
                applicable.Add(mutation);
        }

        if (applicable.Count == 0) return null;

        int index = UnityEngine.Random.Range(0, applicable.Count);
        return applicable[index];
    }

    private static bool CanApplyToAnySkill(MutationData mutation, SkillMutationLoadoutBinder binder)
    {
        for (int i = 0; i < binder.SkillCount; i++)
        {
            Component skill = binder.GetSkill(i);
            if (skill != null && mutation.CanApplyTo(skill))
                return true;
        }

        return false;
    }

    private List<MutationData> GetMutationCandidates()
    {
        List<MutationData> candidates = new();
        AddCandidates(candidates, mutationPool);

        if (includeLoadedMutationAssets)
        {
            AddCandidates(candidates, Resources.LoadAll<MutationData>(string.Empty));
            AddCandidates(candidates, Resources.FindObjectsOfTypeAll<MutationData>());
        }

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:MutationData");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            MutationData mutation = AssetDatabase.LoadAssetAtPath<MutationData>(path);
            AddCandidate(candidates, mutation);
        }
#endif

        return candidates;
    }

    private static void AddCandidates(List<MutationData> candidates, MutationData[] mutations)
    {
        if (mutations == null) return;

        for (int i = 0; i < mutations.Length; i++)
            AddCandidate(candidates, mutations[i]);
    }

    private static void AddCandidate(List<MutationData> candidates, MutationData mutation)
    {
        if (mutation == null || candidates.Contains(mutation)) return;

        candidates.Add(mutation);
    }

    private void BeginPause()
    {
        if (!pauseWhileRewardUiIsOpen || pausedByRewardUi)
            return;

        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        pausedByRewardUi = true;
    }

    private void EndPause()
    {
        if (!pausedByRewardUi)
            return;

        Time.timeScale = timeScaleBeforePause;
        pausedByRewardUi = false;
    }
}
