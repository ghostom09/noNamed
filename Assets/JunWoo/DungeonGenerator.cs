using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Fallback Rules")]
    [SerializeField, Min(1)] private int fallbackMainPathRoomCount = 4;
    [SerializeField, Range(0f, 1f)] private float fallbackBranchChance = 0.5f;
    [SerializeField, Range(0f, 1f)] private float fallbackChildBranchChance = 0.3f;
    [SerializeField, Range(0, 2)] private int fallbackMaxSideBranchesPerMainRoom = 2;
    [SerializeField, Min(0)] private int fallbackMaxChildBranchRoomCount = 1;
    [SerializeField, Min(0)] private int fallbackBossInterval = 3;

    [Header("Placement")]
    [SerializeField, Min(0f)] private float roomPadding = 2f;

    private readonly List<RoomNode> _allRooms = new List<RoomNode>();
    private int _idCounter;

    public List<RoomNode> GenerateRooms(int currentFloor)
    {
        return GenerateRooms(currentFloor, null);
    }

    public List<RoomNode> GenerateRooms(int currentFloor, FloorRule rule)
    {
        _allRooms.Clear();
        _idCounter = 0;

        var mainPath = GenerateMainPath(rule);
        var exitParent = mainPath[^1];

        if (ShouldGenerateBoss(currentFloor, rule))
            exitParent = AddBoss(exitParent, rule);

        AddExit(exitParent, rule);
        GenerateBranches(mainPath, rule);
        GenerateGuaranteedRooms(mainPath, rule);

        return new List<RoomNode>(_allRooms);
    }
    private void GenerateGuaranteedRooms(List<RoomNode> mainPath, FloorRule rule)
    {
        TryForceSpawnRoomType(mainPath, RoomType.Archive, rule);
        TryForceSpawnRoomType(mainPath, RoomType.Rest, rule);
    }
    private void TryForceSpawnRoomType(List<RoomNode> mainPath, RoomType type, FloorRule rule)
    {
        if (HasGeneratedRoomType(type))
            return;

        var definition = GetBranchDefinitionByType(rule, type);
        if (definition == null && rule != null)
        {
            Debug.LogWarning($"Guaranteed room type is not in Branch Room Pool: {type}");
            return;
        }

        var shuffled = new List<RoomNode>(mainPath);
        Shuffle(shuffled);

        foreach (var corridor in shuffled)
        {
            var directions = new List<Vector2Int> { Vector2Int.up, Vector2Int.down };
            Shuffle(directions);

            foreach (var dir in directions)
            {
                if (TryCreateConnectedRoom(corridor, definition, type, dir, out _))
                    return;
            }
        }

        Debug.LogWarning($"보장 방 생성 실패: {type}");
    }

    private bool HasGeneratedRoomType(RoomType type)
    {
        for (var i = 0; i < _allRooms.Count; i++)
        {
            if (_allRooms[i].Type == type)
                return true;
        }

        return false;
    }

    public bool IsFallbackBossFloor(int currentFloor)
    {
        return fallbackBossInterval > 0 && currentFloor % fallbackBossInterval == 0;
    }

    private List<RoomNode> GenerateMainPath(FloorRule rule)
    {
        var mainPath = new List<RoomNode>();
        var current = CreateRoom(GetCorridorDefinition(rule), RoomType.Corridor, Vector2.zero, Vector2Int.zero);
        mainPath.Add(current);

        var roomCount = GetMainPathRoomCount(rule);
        for (var i = 1; i < roomCount; i++)
        {
            var passage = CreateConnectedRoom(current, GetPassageDefinition(rule), RoomType.Passage, Vector2Int.right);
            current = CreateConnectedRoom(passage, GetCorridorDefinition(rule), RoomType.Corridor, Vector2Int.right);
            mainPath.Add(current);
        }

        return mainPath;
    }

    private void GenerateBranches(List<RoomNode> mainPath, FloorRule rule)
    {
        var maxBranches = GetMaxSideBranchesPerMainRoom(rule);
        if (maxBranches <= 0)
            return;

        var maxChildBranches = GetMaxChildBranchRoomCount(rule);
        var createdChildBranchCount = 0;

        for (var i = 0; i < mainPath.Count; i++)
        {
            var branchDirections = new List<Vector2Int> { Vector2Int.up, Vector2Int.down };
            Shuffle(branchDirections);

            var createdCount = 0;
            for (var j = 0; j < branchDirections.Count; j++)
            {
                if (createdCount >= maxBranches)
                    break;
                

                if (Random.value > GetBranchChance(rule))
                    continue;
                
                

                if (!TryCreateBranchRoom(mainPath[i], branchDirections[j], rule, out var branch))
                    continue;

                createdCount++;

                if (createdChildBranchCount >= maxChildBranches)
                    continue;

                if (TryCreateChildBranch(branch, rule))
                    createdChildBranchCount++;
            }
        }
    }

    private bool TryCreateBranchRoom(RoomNode parent, Vector2Int direction, FloorRule rule, out RoomNode branch)
    {
        var definition = GetRandomBranchDefinition(rule);
        if (definition == null && rule != null)
        {
            branch = null;
            return false;
        }

        var fallbackType = definition != null ? definition.Type : GetRandomBranchType();
        return TryCreateConnectedRoom(parent, definition, fallbackType, direction, out branch);
    }

    private bool TryCreateChildBranch(RoomNode parent, FloorRule rule)
    {
        if (Random.value > GetChildBranchChance(rule))
            return false;

        var directions = new List<Vector2Int>
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        Shuffle(directions);

        for (var i = 0; i < directions.Count; i++)
        {
            if (TryCreateBranchRoom(parent, directions[i], rule, out _))
                return true;
        }

        return false;
    }

    private RoomNode AddBoss(RoomNode parent, FloorRule rule)
    {
        return CreateConnectedRoom(parent, GetBossDefinition(rule), RoomType.Boss, Vector2Int.right);
    }

    private RoomNode AddExit(RoomNode parent, FloorRule rule)
    {
        return CreateConnectedRoom(parent, GetExitDefinition(rule), RoomType.Exit, Vector2Int.right);
    }

    private RoomNode CreateConnectedRoom(
        RoomNode parent,
        RoomDefinition definition,
        RoomType fallbackType,
        Vector2Int direction)
    {
        var normalizedDirection = RoomDataUtility.NormalizeDirection(direction);
        var position = CalculateConnectedPosition(parent, definition, fallbackType, normalizedDirection);
        var gridPos = parent.GridPos + normalizedDirection;
        var room = CreateRoom(definition, fallbackType, position, gridPos);
        ConnectRooms(parent, room, normalizedDirection);
        return room;
    }

    private bool TryCreateConnectedRoom(
        RoomNode parent,
        RoomDefinition definition,
        RoomType fallbackType,
        Vector2Int direction,
        out RoomNode room)
    {
        var normalizedDirection = RoomDataUtility.NormalizeDirection(direction);
        var position = CalculateConnectedPosition(parent, definition, fallbackType, normalizedDirection);
        var size = GetRoomSize(definition, fallbackType);
        var bounds = new Rect(position - size * 0.5f, size);

        if (OverlapsExistingRoom(bounds))
        {
            room = null;
            return false;
        }

        var gridPos = parent.GridPos + normalizedDirection;
        room = CreateRoom(definition, fallbackType, position, gridPos);
        ConnectRooms(parent, room, normalizedDirection);
        return true;
    }

    private RoomNode CreateRoom(RoomDefinition definition, RoomType fallbackType, Vector2 position, Vector2Int gridPos)
    {
        var room = new RoomNode
        {
            Id = _idCounter++,
            Definition = definition,
            Type = definition != null ? definition.Type : fallbackType,
            Position = position,
            GridPos = gridPos,
            Children = new List<RoomNode>(),
            Connections = new List<RoomConnection>()
        };

        _allRooms.Add(room);
        return room;
    }

    private void ConnectRooms(RoomNode from, RoomNode to, Vector2Int direction)
    {
        from.Children.Add(to);
        to.Parent = from;

        from.Connections.Add(new RoomConnection(from, to, direction));
        to.Connections.Add(new RoomConnection(to, from, new Vector2Int(-direction.x, -direction.y)));
    }

    private Vector2 CalculateConnectedPosition(
        RoomNode parent,
        RoomDefinition childDefinition,
        RoomType childFallbackType,
        Vector2Int direction)
    {
        var parentDoor = GetDoorLocalPosition(parent.Definition, parent.Type, direction);
        var oppositeDirection = new Vector2Int(-direction.x, -direction.y);
        var childDoor = GetDoorLocalPosition(childDefinition, childFallbackType, oppositeDirection);
        var paddingOffset = new Vector2(direction.x, direction.y) * roomPadding;

        return parent.Position + parentDoor + paddingOffset - childDoor;
    }

    private Vector2 GetDoorLocalPosition(RoomDefinition definition, RoomType fallbackType, Vector2Int direction)
    {
        if (definition != null)
            return definition.GetDoorLocalPosition(direction);

        return RoomDataUtility.GetFallbackDoorLocalPosition(fallbackType, direction);
    }

    private Vector2 GetRoomSize(RoomDefinition definition, RoomType fallbackType)
    {
        return definition != null ? definition.Size : RoomDataUtility.GetFallbackSize(fallbackType);
    }

    private bool OverlapsExistingRoom(Rect bounds)
    {
        for (var i = 0; i < _allRooms.Count; i++)
        {
            if (bounds.Overlaps(_allRooms[i].Bounds))
                return true;
        }

        return false;
    }

    private bool ShouldGenerateBoss(int currentFloor, FloorRule rule)
    {
        return rule != null ? rule.HasBoss : IsFallbackBossFloor(currentFloor);
    }

    private int GetMainPathRoomCount(FloorRule rule)
    {
        return rule != null ? rule.MainPathRoomCount : Mathf.Max(1, fallbackMainPathRoomCount);
    }

    private float GetBranchChance(FloorRule rule)
    {
        return rule != null ? rule.BranchChance : fallbackBranchChance;
    }

    private float GetChildBranchChance(FloorRule rule)
    {
        return rule != null ? rule.ChildBranchChance : fallbackChildBranchChance;
    }

    private int GetMaxSideBranchesPerMainRoom(FloorRule rule)
    {
        return rule != null ? rule.MaxSideBranchesPerMainRoom : Mathf.Clamp(fallbackMaxSideBranchesPerMainRoom, 0, 2);
    }

    private int GetMaxChildBranchRoomCount(FloorRule rule)
    {
        return rule != null ? rule.MaxChildBranchRoomCount : Mathf.Max(0, fallbackMaxChildBranchRoomCount);
    }

    private RoomDefinition GetCorridorDefinition(FloorRule rule)
    {
        return rule != null ? rule.CorridorRoom : null;
    }

    private RoomDefinition GetPassageDefinition(FloorRule rule)
    {
        return rule != null ? rule.PassageRoom : null;
    }

    private RoomDefinition GetBossDefinition(FloorRule rule)
    {
        return rule != null ? rule.BossRoom : null;
    }

    private RoomDefinition GetExitDefinition(FloorRule rule)
    {
        return rule != null ? rule.ExitRoom : null;
    }

    private RoomDefinition GetRandomBranchDefinition(FloorRule rule)
    {
        if (rule != null && rule.TryGetRandomBranchRoom(out var room))
            return room;

        return null;
    }

    private RoomDefinition GetBranchDefinitionByType(FloorRule rule, RoomType type)
    {
        if (rule != null && rule.TryGetBranchRoom(type, out var room))
            return room;

        return null;
    }

    private RoomType GetRandomBranchType()
    {
        return Random.value < 0.5f ? RoomType.Lab : RoomType.Containment;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
