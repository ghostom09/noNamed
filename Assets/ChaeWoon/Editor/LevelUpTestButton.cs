using UnityEngine;
using UnityEngine.UI;

public class LevelUpTestButtonEditorStub : UnityEngine.MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        var stat = FindFirstObjectByType<PlayerStatManager>();
        if (stat != null)
            stat.LevelUp();
        else
            Debug.LogWarning("PlayerStatManager를 찾을 수 없습니다.");
    }
}
