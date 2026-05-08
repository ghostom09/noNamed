using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DungeonGenerator : MonoBehaviour
{
    [SerializeField] private int corridorCount = 4;
    [SerializeField] private float branchChance = 0.5f;
    [SerializeField] private float childChance = 0.3f;
    // [SerializeField] private int tempFloor;

    private List<RoomNode> _allRooms = new List<RoomNode>();
    private HashSet<Vector2Int> _occupiedPositions = new HashSet<Vector2Int>();
    private int _idCounter = 0;

    // void Update()
    // {
    //     if (Keyboard.current.aKey.wasPressedThisFrame)
    //         GenerateRooms(tempFloor);
    // }

    public List<RoomNode> GenerateRooms(int currentFloor)
    {
        _allRooms.Clear();
        _occupiedPositions.Clear();
        _idCounter = 0;

        var corridor = GenerateCorridor();
        GenerateBranch(corridor);

        bool hasBoss = (currentFloor % 3 == 0);
        if (hasBoss)
            AddBoss(corridor);

        AddExit(corridor);

        return _allRooms;
    }

    private List<RoomNode> GenerateCorridor()
    {
        var corridor = new List<RoomNode>();

        for (var i = 0; i < corridorCount; i++)
        {
            int x = i * 2;

            var room = new RoomNode
            {
                Id = _idCounter++,
                Type = RoomType.Corridor,
                GridPos = new Vector2Int(x, 0),
                Children = new List<RoomNode>()
            };
            corridor.Add(room);
            _allRooms.Add(room);
            _occupiedPositions.Add(room.GridPos);
            
            

            if (i >= corridorCount - 1) continue;
            
            var passage = new RoomNode
            {
                Id = _idCounter++,
                Type = RoomType.Passage,
                GridPos = new Vector2Int(x + 1, 0),
                Children = new List<RoomNode>(),
            };
            _allRooms.Add(passage);
            _occupiedPositions.Add(passage.GridPos);
        }
        for (int i = 0; i < corridor.Count - 1; i++)
        {
            corridor[i].Children.Add(corridor[i + 1]);
            corridor[i + 1].Parent = corridor[i];
        }

        return corridor;
    }

    private void GenerateBranch(List<RoomNode> corridor)
    {
        foreach (var room in corridor)
        {
            if (Random.value < branchChance)
                SpawnBranch(room, 1);   
            if (Random.value < branchChance)
                SpawnBranch(room, -1); 
        }
    }

    private void SpawnBranch(RoomNode parent, int dir)
    {
        var branchPos = parent.GridPos + new Vector2Int(0, dir);

        var branch = new RoomNode
        {
            Id = _idCounter++,
            Type = GetRandomBranchType(),
            GridPos = branchPos,
            Children = new List<RoomNode>(),
            Parent = parent
        };
        parent.Children.Add(branch);
        _allRooms.Add(branch);
        _occupiedPositions.Add(branchPos);
        
        if (Random.value < childChance)
            SpawnChildRoom(branch);
    }

    private void SpawnChildRoom(RoomNode parent)
    {
        Vector2Int[] directions = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        Shuffle(directions);

        foreach (var dir in directions)
        {
            var candidatePos = parent.GridPos + dir; // (candidate = 후보자)

            if (_occupiedPositions.Contains(candidatePos)) continue;

            var child = new RoomNode
            {
                Id = _idCounter++,
                Type = GetRandomBranchType(),
                GridPos = candidatePos,
                Children = new List<RoomNode>(),
                Parent = parent
            };
            parent.Children.Add(child);
            _allRooms.Add(child);
            _occupiedPositions.Add(candidatePos);
            break; // 하나만 생성하게 막아놓는거 없애도 댐
        }
    }

    private void AddBoss(List<RoomNode> corridor)
    {
        var lastCorridor = corridor[^1];
        var boss = new RoomNode
        {
            Id = _idCounter++,
            Type = RoomType.Boss,
            GridPos = lastCorridor.GridPos + new Vector2Int(1, 0),
            Children = new List<RoomNode>(),
            Parent = lastCorridor
        };
        lastCorridor.Children.Add(boss);
        _allRooms.Add(boss);
        _occupiedPositions.Add(boss.GridPos);
    }

    private void AddExit(List<RoomNode> corridor)
    {
        var lastCorridor = corridor[^1];

        var hasBoss = lastCorridor.Children.Count > 0 &&
                      lastCorridor.Children[^1].Type == RoomType.Boss;

        var exitParent = hasBoss ? lastCorridor.Children[^1] : lastCorridor;

        var exit = new RoomNode
        {
            Id = _idCounter++,
            Type = RoomType.Exit,
            GridPos = exitParent.GridPos + new Vector2Int(1, 0),
            Children = new List<RoomNode>(),
            Parent = exitParent
        };
        exitParent.Children.Add(exit);
        _allRooms.Add(exit);
        _occupiedPositions.Add(exit.GridPos);
    }

    private RoomType GetRandomBranchType()
    {
        RoomType[] types = { RoomType.Lab, RoomType.Archive };
        return types[Random.Range(0, types.Length)];
    }

    private void Shuffle(Vector2Int[] array)
    {
        for (var i = array.Length - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    // private void OnDrawGizmos()
    // {
    //     if (_allRooms == null) return;
    //
    //     foreach (var room in _allRooms)
    //     {
    //         Gizmos.color = GetRoomColor(room.Type);
    //         Vector3 pos = new Vector3(room.GridPos.x * 3f, room.GridPos.y * 3f, 0);
    //
    //         // 통로는 작게 표시
    //         float size = room.Type == RoomType.Passage ? 1f : 2f;
    //         Gizmos.DrawCube(pos, Vector3.one * size);
    //
    //         foreach (var child in room.Children)
    //         {
    //             Vector3 childPos = new Vector3(child.GridPos.x * 3f, child.GridPos.y * 3f, 0);
    //             Gizmos.color = Color.white;
    //             Gizmos.DrawLine(pos, childPos);
    //         }
    //     }
    // }
    //
    // private Color GetRoomColor(RoomType type)
    // {
    //     return type switch
    //     {
    //         RoomType.Corridor => Color.gray,
    //         RoomType.Passage  => Color.white,
    //         RoomType.Lab      => Color.cyan,
    //         RoomType.Archive  => Color.yellow,
    //         RoomType.Boss     => Color.red,
    //         RoomType.Exit     => Color.green,
    //         _                 => Color.white
    //     };
    // }
}