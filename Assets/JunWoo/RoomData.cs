using System.Collections.Generic;
using UnityEngine;

public enum RoomType{
    Corridor,   // 복도
    Passage,    // 통로
    Lab,        // 실험실
    Archive,    // 기록소
    Boss,       // 보스방
    Exit        // 다음 층 올라가는 방
}


public class RoomNode
{
    public int Id;
    public RoomType Type;
    public Vector2Int GridPos;  
    public List<RoomNode> Children;
    public RoomNode Parent;
    public bool IsCleared;
}
