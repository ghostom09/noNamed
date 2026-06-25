using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 선택 화면의 "표시"만 담당한다. 선택 로직(인덱스/잠금 판정)은 갖지 않는다.
/// 버튼 클릭은 매니저가 넘겨준 콜백으로 그대로 전달한다.
/// </summary>
[DisallowMultipleComponent]
public class CharacterSelectUI : MonoBehaviour
{
    [Header("캐릭터 정보 표시")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI pageText;      // 예: "2 / 4"

    [Header("잠금 표시")]
    [Tooltip("잠긴 캐릭터일 때 켜질 오버레이(예: 자물쇠 아이콘 + '잠김').")]
    [SerializeField] private GameObject lockedIndicator;

    [Header("버튼 (동작은 매니저가 연결)")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button startButton;

    /// <summary>매니저가 각 버튼의 동작을 연결한다.</summary>
    public void BindButtons(UnityAction onPrevious, UnityAction onNext, UnityAction onStart)
    {
        Bind(previousButton, onPrevious);
        Bind(nextButton, onNext);
        Bind(startButton, onStart);
    }

    /// <summary>현재 캐릭터 1명을 화면에 갱신한다.</summary>
    public void Render(CharacterData character, int index, int total)
    {
        if (character == null)
        {
            RenderEmpty(index, total);
            return;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = character.portrait;
            portraitImage.enabled = character.portrait != null;
        }

        SetText(nameText, character.characterName);
        SetText(descriptionText, character.description);
        SetText(statsText, BuildStatsText(character));
        SetText(pageText, $"{index + 1} / {Mathf.Max(1, total)}");

        if (lockedIndicator != null)
            lockedIndicator.SetActive(character.isLocked);

        // 잠긴 캐릭터만 시작 버튼 비활성화.
        SetStartInteractable(!character.isLocked);
    }

    public void SetStartInteractable(bool interactable)
    {
        if (startButton != null)
            startButton.interactable = interactable;
    }

    private void RenderEmpty(int index, int total)
    {
        if (portraitImage != null) portraitImage.enabled = false;
        SetText(nameText, "캐릭터 없음");
        SetText(descriptionText, string.Empty);
        SetText(statsText, string.Empty);
        SetText(pageText, $"{index + 1} / {Mathf.Max(1, total)}");
        if (lockedIndicator != null) lockedIndicator.SetActive(false);
        SetStartInteractable(false);
    }

    private static string BuildStatsText(CharacterData c)
    {
        return $"체력     {c.health}\n" +
               $"공격력   {c.attackPower}\n" +
               $"이동속도 {c.moveSpeed:0.#}";
    }

    private static void Bind(Button button, UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        if (action != null)
            button.onClick.AddListener(action);
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value ?? string.Empty;
    }
}
