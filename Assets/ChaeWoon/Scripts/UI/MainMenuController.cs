using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// StartScene 메인 메뉴의 버튼 동작을 코드로 연결한다.
/// 인스펙터의 정적 API(OnClick) 바인딩 대신 런타임에 리스너를 등록한다.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("Settings")]
    [Tooltip("게임 시작 버튼이 로드할 씬 이름")]
    [SerializeField] private string gameSceneName = "IntegratedTest";

    [Tooltip("버튼 참조가 비어 있으면 이름으로 자동 탐색한다")]
    [SerializeField] private bool autoFindButtons = true;

    private const string StartButtonName = "StartButton";
    private const string QuitButtonName = "QuitButton";

    private void Awake()
    {
        if (autoFindButtons)
        {
            BindMissingReferences();
        }
    }

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitClicked);
        }
    }

    private void OnDisable()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuitClicked);
        }
    }

    public void OnStartClicked()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("[MainMenuController] gameSceneName이 비어 있습니다.", this);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BindMissingReferences()
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (startButton == null && button.name == StartButtonName)
            {
                startButton = button;
            }
            else if (quitButton == null && button.name == QuitButtonName)
            {
                quitButton = button;
            }
        }
    }
}
