using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DungeonGenerationValidator
{
    private const int MaxFloor = 8;
    private const int IterationsPerFloor = 20;
    private const float BoundsContactTolerance = 0.01f;

    [MenuItem("Tools/Dungeon/Validate Generated Floors")]
    public static void ValidateGeneratedFloors()
    {
        var testObject = new GameObject("Dungeon Generation Validator");
        var generator = testObject.AddComponent<DungeonGenerator>();
        var totalRooms = 0;

        try
        {
            var floorRules = LoadFloorRules();
            if (floorRules.Count > 0)
            {
                for (var i = 0; i < floorRules.Count; i++)
                {
                    var rule = floorRules[i];
                    for (var iteration = 0; iteration < IterationsPerFloor; iteration++)
                    {
                        var rooms = generator.GenerateRooms(rule.Floor, rule);
                        ValidateFloor(rule.Floor, rooms, rule.HasBoss, true);
                        totalRooms += rooms.Count;
                    }
                }

                Debug.Log($"Dungeon generation validation passed. FloorRules: {floorRules.Count}, Iterations: {IterationsPerFloor}, Total rooms checked: {totalRooms}");
                return;
            }

            for (var floor = 1; floor <= MaxFloor; floor++)
            {
                for (var iteration = 0; iteration < IterationsPerFloor; iteration++)
                {
                    var rooms = generator.GenerateRooms(floor);
                    ValidateFloor(floor, rooms, generator.IsFallbackBossFloor(floor), false);
                    totalRooms += rooms.Count;
                }
            }

            Debug.Log($"Dungeon generation validation passed. Floors: {MaxFloor}, Iterations: {IterationsPerFloor}, Total rooms checked: {totalRooms}");
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }
    }

    private static List<FloorRule> LoadFloorRules()
    {
        var result = new List<FloorRule>();
        var guids = AssetDatabase.FindAssets("t:FloorRule");

        for (var i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var rule = AssetDatabase.LoadAssetAtPath<FloorRule>(path);
            if (rule != null)
                result.Add(rule);
        }

        result.Sort((a, b) => a.Floor.CompareTo(b.Floor));
        return result;
    }

    private static void ValidateFloor(int floor, List<RoomNode> rooms, bool shouldHaveBoss, bool requireDefinitions)
    {
        if (rooms == null || rooms.Count == 0)
            throw new System.Exception($"Floor {floor} generated no rooms.");

        var ids = new HashSet<int>();
        var hasExit = false;
        var hasBoss = false;

        for (var i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];

            if (room == null)
                throw new System.Exception($"Floor {floor} has a null room.");

            if (!ids.Add(room.Id))
                throw new System.Exception($"Floor {floor} has duplicated room id: {room.Id}");

            ValidateDefinition(floor, room, requireDefinitions);

            if (room.Type == RoomType.Exit)
                hasExit = true;

            if (room.Type == RoomType.Boss)
                hasBoss = true;

            ValidateConnections(floor, room);
            ValidateNoOverlap(floor, rooms, i);
        }

        if (!hasExit)
            throw new System.Exception($"Floor {floor} has no exit room.");

        if (shouldHaveBoss && !hasBoss)
            throw new System.Exception($"Floor {floor} should have a boss room, but none was generated.");

        if (!shouldHaveBoss && hasBoss)
            throw new System.Exception($"Floor {floor} generated an unexpected boss room.");
    }

    private static void ValidateDefinition(int floor, RoomNode room, bool requireDefinitions)
    {
        if (!requireDefinitions)
            return;

        if (room.Definition == null)
            throw new System.Exception($"Floor {floor}, room {room.Id} ({room.Type}) has no RoomDefinition.");

        if (room.Definition.Type != room.Type)
        {
            throw new System.Exception(
                $"Floor {floor}, room {room.Id} has mismatched type. Node: {room.Type}, Definition: {room.Definition.Type}.");
        }

        if (room.Prefab == null)
            throw new System.Exception($"Floor {floor}, room {room.Id} ({room.Type}) has no prefab.");
    }

    private static void ValidateConnections(int floor, RoomNode room)
    {
        for (var i = 0; i < room.Children.Count; i++)
        {
            var child = room.Children[i];
            if (child == null)
                throw new System.Exception($"Floor {floor}, room {room.Id} has a null child.");

            if (child.Parent != room)
                throw new System.Exception($"Floor {floor}, room {child.Id} has an invalid parent.");
        }
    }

    private static void ValidateNoOverlap(int floor, List<RoomNode> rooms, int currentIndex)
    {
        var current = rooms[currentIndex];
        var currentBounds = ShrinkRect(current.Bounds, BoundsContactTolerance);

        for (var i = currentIndex + 1; i < rooms.Count; i++)
        {
            var other = rooms[i];
            var otherBounds = ShrinkRect(other.Bounds, BoundsContactTolerance);
            if (currentBounds.Overlaps(otherBounds))
            {
                throw new System.Exception(
                    $"Floor {floor} has overlapping rooms. Room {current.Id} ({current.Type}) overlaps room {other.Id} ({other.Type}).");
            }
        }
    }

    private static Rect ShrinkRect(Rect rect, float amount)
    {
        var shrinkX = Mathf.Min(amount, rect.width * 0.5f);
        var shrinkY = Mathf.Min(amount, rect.height * 0.5f);

        return new Rect(
            rect.xMin + shrinkX,
            rect.yMin + shrinkY,
            Mathf.Max(0f, rect.width - shrinkX * 2f),
            Mathf.Max(0f, rect.height - shrinkY * 2f));
    }
}
