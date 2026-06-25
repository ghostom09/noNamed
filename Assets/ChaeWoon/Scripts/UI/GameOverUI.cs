using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// View-only game over overlay. The controller owns death handling and scene actions.
/// </summary>
[DisallowMultipleComponent]
public class GameOverUI : MonoBehaviour
{
    private const string PanelName = "GameOverPanel";
    private const string TitleTextName = "GameOverTitle";
    private const string RestartButtonName = "RestartButton";
    private const string MainMenuButtonName = "MainMenuButton";
    private const string KoreanTmpFontPath = "Assets/ChaeWoon/Fonts/NotoSansKR-Regular SDF.asset";
    private const string KoreanTmpFontName = "NotoSansKR-Regular SDF";
    private const int OverlaySortingOrder = 10000;

    [Header("View")]
    [SerializeField] private GameObject panel;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Settings")]
    [Tooltip("Find missing references by child names.")]
    [SerializeField] private bool autoFindReferences = true;
    [SerializeField] private TMP_FontAsset textFont;
    [SerializeField] private string titleMessage = "\uAC8C\uC784 \uC624\uBC84";

    [Header("Fade")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.35f;
    [SerializeField] private AnimationCurve fadeEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine fadeRoutine;

    public bool IsShown => panel != null && panel.activeSelf;

    private void Awake()
    {
        BindReferences();
        Hide();
    }

    public void BindButtons(UnityAction onRestart, UnityAction onMainMenu)
    {
        BindReferences();
        Bind(restartButton, onRestart);
        Bind(mainMenuButton, onMainMenu);
    }

    public void Show()
    {
        BindReferences();

        if (panel == null)
        {
            return;
        }

        EnsureOverlayLayout();
        ApplyTextFont();

        if (titleText != null)
        {
            titleText.text = titleMessage;
        }

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();

        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        SelectDefaultButton();
        StartFade();
    }

    public void Hide()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void StartFade()
    {
        if (canvasGroup == null)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = fadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = fadeEase.Evaluate(t);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        fadeRoutine = null;
    }

    private void EnsureOverlayLayout()
    {
        if (panel == null)
        {
            return;
        }

        Canvas targetCanvas = panel.GetComponentInParent<Canvas>(true);
        if (targetCanvas == null)
        {
            targetCanvas = GetComponentInParent<Canvas>(true);
        }
        if (targetCanvas == null)
        {
            targetCanvas = FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        }

        RectTransform panelRect = panel.transform as RectTransform;
        if (targetCanvas != null && panelRect != null && panel.transform.parent is not RectTransform)
        {
            panel.transform.SetParent(targetCanvas.transform, false);
        }

        panelRect = panel.transform as RectTransform;
        if (panelRect != null)
        {
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.localRotation = Quaternion.identity;
            panelRect.localScale = Vector3.one;
        }

        if (panelRect != null)
        {
            Canvas overlayCanvas = panel.GetComponent<Canvas>();
            if (overlayCanvas == null)
            {
                overlayCanvas = panel.AddComponent<Canvas>();
            }

            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = OverlaySortingOrder;

            if (panel.GetComponent<GraphicRaycaster>() == null)
            {
                panel.AddComponent<GraphicRaycaster>();
            }
        }
    }

    private void ApplyTextFont()
    {
        TMP_FontAsset resolvedFont = ResolveTextFont();
        if (resolvedFont == null)
        {
            return;
        }

        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            if (text != null && text.font != resolvedFont)
            {
                text.font = resolvedFont;
            }
        }
    }

    private TMP_FontAsset ResolveTextFont()
    {
        if (textFont != null)
        {
            return textFont;
        }

#if UNITY_EDITOR
        textFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanTmpFontPath);
#endif
        if (textFont == null)
        {
            foreach (TMP_FontAsset fontAsset in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (fontAsset != null && fontAsset.name == KoreanTmpFontName)
                {
                    textFont = fontAsset;
                    break;
                }
            }
        }

        return textFont;
    }

    private void SelectDefaultButton()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        GameObject selection = restartButton != null
            ? restartButton.gameObject
            : mainMenuButton != null
                ? mainMenuButton.gameObject
                : null;

        if (selection != null)
        {
            eventSystem.SetSelectedGameObject(selection);
        }
    }

    private static void Bind(Button button, UnityAction action)
    {
        if (button == null || action == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void BindReferences()
    {
        if (panel == null)
        {
            Transform found = transform.Find(PanelName);
            panel = found != null ? found.gameObject : gameObject;
        }

        if (canvasGroup == null && panel != null)
        {
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }
        }

        if (!autoFindReferences)
        {
            return;
        }

        if (titleText == null)
        {
            titleText = FindInChildren<TMP_Text>(TitleTextName);
        }

        if (restartButton == null)
        {
            restartButton = FindButton(RestartButtonName);
        }

        if (mainMenuButton == null)
        {
            mainMenuButton = FindButton(MainMenuButtonName);
        }
    }

    private Button FindButton(string targetName)
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (button.name == targetName)
            {
                return button;
            }
        }

        return null;
    }

    private T FindInChildren<T>(string targetName) where T : Component
    {
        foreach (T component in GetComponentsInChildren<T>(true))
        {
            if (component.name == targetName)
            {
                return component;
            }
        }

        return null;
    }
}
