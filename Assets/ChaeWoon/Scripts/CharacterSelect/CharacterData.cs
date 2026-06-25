using UnityEngine;

/// <summary>
/// 캐릭터 선택 화면에 표시할 캐릭터 1명의 데이터.
/// 게임 로직과 분리된 순수 데이터 컨테이너(ScriptableObject).
/// 메뉴: Create > Character > Character Data
/// </summary>
[CreateAssetMenu(fileName = "CharacterData", menuName = "Character/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("기본 정보")]
    public string characterName = "이름 없음";

    [TextArea(2, 4)]
    public string description;

    public Sprite portrait;

    [Header("잠금")]
    [Tooltip("체크 시 선택 불가 — 게임 시작 버튼이 비활성화된다.")]
    public bool isLocked;

    [Header("기본 스탯")]
    [Min(0)] public int health = 100;
    [Min(0)] public int attackPower = 10;
    [Min(0f)] public float moveSpeed = 5f;
}
