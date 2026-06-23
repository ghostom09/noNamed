using UnityEngine;
using UnityEngine.UI;

public class LevelUpTestButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        var stat = FindAnyObjectByType<PlayerStatManager>();
        if (stat != null)
            stat.LevelUp();
        else
            Debug.LogWarning("PlayerStatManager를 찾을 수 없습니다.");
    }
}
