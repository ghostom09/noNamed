using System.Collections.Generic;
using UnityEngine;

public enum RoomType
{
    Corridor = 0,
    Passage = 1,
    Lab = 2,
    Archive = 3,
    Rest = 4,
    Containment = 5,
    Boss = 6,
    Exit = 7
}

public enum RewardType
{
    None = 0,
    LabReward = 1,
    ArchiveReward = 2,
    RestReward = 3,
    ContainmentReward = 4,
    BossClear = 5
}

public class RoomConnection
{
    public RoomNode From;
    public RoomNode To;
    public Vector2Int Direction;

    public RoomConnection(RoomNode from, RoomNode to, Vector2Int direction)
    {
        From = from;
        To = to;
        Direction = direction;
    }
}

public class RoomNode
{
    public int Id;
    public RoomDefinition Definition;
    public RoomType Type;
    public Vector2 Position;
    public Vector2Int GridPos;
    public List<RoomNode> Children = new List<RoomNode>();
    public List<RoomConnection> Connections = new List<RoomConnection>();
    public RoomNode Parent;
    public bool IsCleared;

    public Vector2 Size => Definition != null ? Definition.Size : RoomDataUtility.GetFallbackSize(Type);
    public bool StartsCombat => Definition != null ? Definition.StartsCombat : RoomDataUtility.StartsCombat(Type);
    public bool LocksDoors => Definition != null ? Definition.LocksDoors : RoomDataUtility.LocksDoors(Type);
    public RewardType RewardType => Definition != null ? Definition.RewardType : RoomDataUtility.GetRewardType(Type);
    public GameObject Prefab => Definition != null ? Definition.Prefab : null;

    public Vector2 GetDoorLocalPosition(Vector2Int direction)
    {
        return Definition != null
            ? Definition.GetDoorLocalPosition(direction)
            : RoomDataUtility.GetFallbackDoorLocalPosition(Type, direction);
    }

    public Vector2 GetDoorWorldPosition(Vector2Int direction)
    {
        return Position + GetDoorLocalPosition(direction);
    }

    public Rect Bounds
    {
        get
        {
            var size = Size;
            return new Rect(Position - size * 0.5f, size);
        }
    }
}

public static class RoomDataUtility
{
    public static Vector2 GetFallbackSize(RoomType type)
    {
        switch (type)
        {
            case RoomType.Corridor:
                return new Vector2(14f, 6f);
            case RoomType.Passage:
                return new Vector2(6f, 4f);
            case RoomType.Lab:
                return new Vector2(12f, 10f);
            case RoomType.Archive:
                return new Vector2(10f, 8f);
            case RoomType.Rest:
                return new Vector2(10f, 8f);
            case RoomType.Containment:
                return new Vector2(12f, 10f);
            case RoomType.Boss:
                return new Vector2(18f, 12f);
            case RoomType.Exit:
                return new Vector2(8f, 6f);
            default:
                return new Vector2(10f, 10f);
        }
    }

    public static bool StartsCombat(RoomType type)
    {
        return type == RoomType.Corridor ||
               type == RoomType.Lab ||
               type == RoomType.Archive ||
               type == RoomType.Containment ||
               type == RoomType.Boss;
    }

    public static bool LocksDoors(RoomType type)
    {
        return StartsCombat(type);
    }

    public static RewardType GetRewardType(RoomType type)
    {
        switch (type)
        {
            case RoomType.Lab:
                return RewardType.LabReward;
            case RoomType.Archive:
                return RewardType.ArchiveReward;
            case RoomType.Rest:
                return RewardType.RestReward;
            case RoomType.Containment:
                return RewardType.ContainmentReward;
            case RoomType.Boss:
                return RewardType.BossClear;
            default:
                return RewardType.None;
        }
    }

    public static Vector2 GetFallbackDoorLocalPosition(RoomType type, Vector2Int direction)
    {
        var normalized = NormalizeDirection(direction);
        var size = GetFallbackSize(type);
        return new Vector2(normalized.x * size.x * 0.5f, normalized.y * size.y * 0.5f);
    }

    public static Vector2Int NormalizeDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
            return Vector2Int.right;

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            return new Vector2Int(direction.x >= 0 ? 1 : -1, 0);

        return new Vector2Int(0, direction.y >= 0 ? 1 : -1);
    }

    public static Vector2Int GetOppositeDirection(Vector2Int direction)
    {
        var normalized = NormalizeDirection(direction);
        return new Vector2Int(-normalized.x, -normalized.y);
    }
}
