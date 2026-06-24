using TMPro;
using UnityEngine;

public class MutationDescriptionUI : MonoBehaviour
{
    public static MutationDescriptionUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text gradeText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[MutationDescriptionUI] Duplicate instance found.", this);
            return;
        }

        Instance = this;
        BindReferences();
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Show(MutationData data)
    {
        BindReferences();

        if (data == null)
        {
            Hide();
            return;
        }

        if (panel != null)
        {
            panel.SetActive(true);
        }

        SetText(gradeText, data.grade.ToString());
        SetText(nameText, data.mutationName);
        SetText(descriptionText, data.descriptionText);
    }

    public void Hide()
    {
        BindReferences();

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void SetText(TMP_Text targetText, string value)
    {
        if (targetText != null)
        {
            targetText.text = value ?? string.Empty;
        }
    }

    private void BindReferences()
    {
        if (panel == null)
        {
            Transform panelTransform = FindDescendant(transform, "MutationDescriptionPanel");
            panel = panelTransform == null ? null : panelTransform.gameObject;
        }

        if (gradeText == null)
        {
            gradeText = FindText("GradeText");
        }

        if (nameText == null)
        {
            nameText = FindText("NameText");
        }

        if (descriptionText == null)
        {
            descriptionText = FindText("DescriptionText");
        }
    }

    private TMP_Text FindText(string childName)
    {
        Transform child = FindDescendant(transform, childName);
        return child == null ? null : child.GetComponent<TMP_Text>();
    }

    private Transform FindDescendant(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == childName)
            {
                return child;
            }

            Transform result = FindDescendant(child, childName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
