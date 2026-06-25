using UnityEngine;

/// <summary>
/// 현재 선택 인덱스와 좌/우/시작 버튼 동작을 담당한다.
/// 화면 표시는 CharacterSelectUI 에 위임한다.
/// </summary>
[DisallowMultipleComponent]
public class CharacterSelectManager : MonoBehaviour
{
    [SerializeField] private CharacterSelectUI ui;
    [SerializeField] private CharacterData[] characters;

    [Tooltip("화살표로 양 끝에서 반대편으로 순환할지 여부.")]
    [SerializeField] private bool wrapAround = true;
    [SerializeField] private int startIndex;

    private int currentIndex;

    private int CharacterCount => characters != null ? characters.Length : 0;

    public CharacterData CurrentCharacter =>
        currentIndex >= 0 && currentIndex < CharacterCount ? characters[currentIndex] : null;

    private void Start()
    {
        currentIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, CharacterCount - 1));

        if (ui != null)
            ui.BindButtons(SelectPrevious, SelectNext, ConfirmSelection);

        Refresh();
    }

    /// <summary>오른쪽 화살표 — 다음 캐릭터.</summary>
    public void SelectNext()
    {
        Move(1);
    }

    /// <summary>왼쪽 화살표 — 이전 캐릭터.</summary>
    public void SelectPrevious()
    {
        Move(-1);
    }

    /// <summary>게임 시작 — 잠기지 않은 캐릭터만 저장한다.</summary>
    public void ConfirmSelection()
    {
        CharacterData character = CurrentCharacter;

        if (character == null || character.isLocked)
        {
            Debug.LogWarning("[CharacterSelectManager] 잠겼거나 비어 있는 캐릭터는 선택할 수 없습니다.");
            return;
        }

        SelectedCharacterStore.Set(character);
        Debug.Log($"[CharacterSelectManager] '{character.characterName}' 선택 완료 (임시 저장).");
        // TODO: 이후 실제 게임 씬 로드 연결 (지금은 선택/저장까지만).
    }

    private void Move(int direction)
    {
        if (CharacterCount == 0)
            return;

        int next = currentIndex + direction;

        if (wrapAround)
            next = (next % CharacterCount + CharacterCount) % CharacterCount;
        else
            next = Mathf.Clamp(next, 0, CharacterCount - 1);

        if (next == currentIndex)
            return;

        currentIndex = next;
        Refresh();
    }

    private void Refresh()
    {
        if (ui != null)
            ui.Render(CurrentCharacter, currentIndex, CharacterCount);
    }
}
