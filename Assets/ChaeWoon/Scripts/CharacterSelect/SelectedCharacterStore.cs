/// <summary>
/// 선택한 캐릭터를 씬 전환 사이에 임시로 들고 있는 정적 저장소.
/// (영구 세이브가 아니라 런타임 메모리 보관용 — 게임 시작 시 이 값을 읽어 쓰면 된다.)
/// </summary>
public static class SelectedCharacterStore
{
    /// <summary>현재 선택된 캐릭터. 선택 전이면 null.</summary>
    public static CharacterData Selected { get; private set; }

    public static bool HasSelection => Selected != null;

    public static void Set(CharacterData character)
    {
        Selected = character;
    }

    public static void Clear()
    {
        Selected = null;
    }
}
