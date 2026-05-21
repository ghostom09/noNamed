using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    public RoomNode RoomData { get; private set; }

    [SerializeField] private GameObject blockPrefab;
    private List<GameObject> _blocks = new List<GameObject>();

    public void Init(RoomNode roomData)
    {
        RoomData = roomData;
    }

    public void OnEnter()
    {
        if (RoomData.Type != RoomType.Exit && RoomData.Type != RoomType.Passage)
        {
            StartCombat();    
        }
    }

    private void StartCombat()
    {
        BlockExits();
    }
    public void OnCleared()
    {
        RoomData.IsCleared = true;
        UnblockExits();
        SpawnReward();
    }

    private void BlockExits()
    {
        // 출입구 위치에 block 오브젝트 활성화
    }

    private void UnblockExits()
    {
        // block 오브젝트 비활성화
    }

    private void SpawnReward()
    {
        switch (RoomData.Type)
        {
            case RoomType.Lab:
                // 스킬 선택
                break;
            case RoomType.Archive:
                // 실험일지
                break;
        }
    }
}