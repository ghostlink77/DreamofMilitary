using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class PrologueSequenceController : MonoBehaviour
{
    [SerializeField] private Button titleButton;
    [SerializeField] private GameObject prologuePanel;
    [SerializeField] private Image prologueBackground;
    [SerializeField] private Image[] cuts;

    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName;

    [Header("Durations")]
    [SerializeField, Min(0f)] private float panelFadeDuration = 0.5f;
    [SerializeField, Min(0f)] private float cutFadeDuration = 0.5f;

    private int nextCutIndex;
    private bool panelReady;
    private bool isFading;
    private bool isLoadingScene;

    private void Awake()
    {
        titleButton.onClick.AddListener(OpenPrologue);

        prologueBackground.raycastTarget = true;
        foreach (Image cut in cuts)
        {
            cut.raycastTarget = false;
            SetAlpha(cut, 0f);
        }

        SetAlpha(prologueBackground, 0f);
    }

    private void OnDestroy()
    {
        titleButton.onClick.RemoveListener(OpenPrologue);
    }

    public void OpenPrologue()
    {
        if (prologuePanel.activeSelf)
        {
            return;
        }

        titleButton.interactable = false;
        nextCutIndex = 0;
        panelReady = false;
        isFading = true;

        SetAlpha(prologueBackground, 0f);
        foreach (Image cut in cuts)
        {
            cut.DOKill();
            SetAlpha(cut, 0f);
        }

        prologuePanel.SetActive(true);
        prologueBackground.DOFade(1f, panelFadeDuration)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                panelReady = true;
                isFading = false;
            });
    }

    public void ShowNextCut()
    {
        if (!panelReady || isFading || isLoadingScene)
        {
            return;
        }

        if (nextCutIndex >= cuts.Length)
        {
            Debug.Log("PrologueSequenceController: All cuts have been shown. Loading next scene.");
            LoadNextScene();
            return;
        }

        isFading = true;
        Image nextCut = cuts[nextCutIndex++];
        nextCut.DOFade(1f, cutFadeDuration)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject)
            .OnComplete(() => isFading = false);
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("Set Next Scene Name on PrologueSequenceController before continuing past the final cut.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogWarning($"The configured next scene '{nextSceneName}' is not in Build Settings.", this);
            return;
        }

        isLoadingScene = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
}