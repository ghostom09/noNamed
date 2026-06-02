using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeightedRoomDefinition
{
    [SerializeField] private RoomDefinition room;
    [SerializeField, Min(1)] private int weight = 1;

    public RoomDefinition Room => room;
    public int Weight => Mathf.Max(1, weight);
}

[CreateAssetMenu(menuName = "Dungeon/Floor Rule")]
public class FloorRule : ScriptableObject
{
    [SerializeField, Min(1)] private int floor = 1;
    [SerializeField, Min(1)] private int mainPathRoomCount = 4;
    [SerializeField, Range(0f, 1f)] private float branchChance = 0.5f;
    [SerializeField, Range(0f, 1f)] private float childBranchChance = 0.3f;
    [SerializeField, Range(0, 2)] private int maxSideBranchesPerMainRoom = 2;
    [SerializeField, Min(0)] private int maxChildBranchRoomCount = 1;
    [SerializeField] private bool hasBoss;

    [Header("Required Room Types")]
    [SerializeField] private RoomDefinition corridorRoom;
    [SerializeField] private RoomDefinition passageRoom;
    [SerializeField] private RoomDefinition bossRoom;
    [SerializeField] private RoomDefinition exitRoom;

    [Header("Branch Room Pool")]
    [SerializeField] private List<WeightedRoomDefinition> branchRoomPool = new List<WeightedRoomDefinition>();

    public int Floor => floor;
    public int MainPathRoomCount => Mathf.Max(1, mainPathRoomCount);
    public float BranchChance => branchChance;
    public float ChildBranchChance => childBranchChance;
    public int MaxSideBranchesPerMainRoom => Mathf.Clamp(maxSideBranchesPerMainRoom, 0, 2);
    public int MaxChildBranchRoomCount => Mathf.Max(0, maxChildBranchRoomCount);

    public bool HasBoss => hasBoss;
    public RoomDefinition CorridorRoom => corridorRoom;
    public RoomDefinition PassageRoom => passageRoom;
    public RoomDefinition BossRoom => bossRoom;
    public RoomDefinition ExitRoom => exitRoom;

    public bool TryGetBranchRoom(RoomType type, out RoomDefinition room)
    {
        room = null;

        for (var i = 0; i < branchRoomPool.Count; i++)
        {
            var candidate = branchRoomPool[i].Room;
            if (candidate == null || candidate.Type != type)
                continue;

            room = candidate;
            return true;
        }

        return false;
    }

    public bool TryGetRandomBranchRoom(out RoomDefinition room)
    {
        room = null;

        var totalWeight = 0;
        for (var i = 0; i < branchRoomPool.Count; i++)
        {
            if (branchRoomPool[i].Room == null) continue;
            totalWeight += branchRoomPool[i].Weight;
        }

        if (totalWeight <= 0)
            return false;

        var randomValue = Random.Range(0, totalWeight);
        for (var i = 0; i < branchRoomPool.Count; i++)
        {
            var candidate = branchRoomPool[i];
            if (candidate.Room == null) continue;

            if (randomValue < candidate.Weight)
            {
                room = candidate.Room;
                return true;
            }

            randomValue -= candidate.Weight;
        }

        return false;
    }
}
