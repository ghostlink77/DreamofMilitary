using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Advances a UI cut sequence when the configured UI hierarchy is clicked.
/// Configure only the fields needed by each scene: an optional start button and
/// panel for an opening sequence, cuts, and the scene to load after the final cut.
/// </summary>
public sealed class ClickSequenceController : MonoBehaviour, IPointerClickHandler
{
    private const string ViewedKeyPrefix = "ClickSequenceController.Viewed.";

    [System.Serializable]
    private struct Cut
    {
        [SerializeField] private Graphic graphic;
        [Tooltip("When this cut is shown, hide every cut that was shown before it.")]
        [SerializeField] private bool hidePreviousCuts;

        public Graphic Graphic => graphic;
        public bool HidePreviousCuts => hidePreviousCuts;
    }

    [Header("Start")]
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject sequencePanel;
    [SerializeField] private Graphic openingGraphic;

    [Header("Cuts")]
    [SerializeField] private Cut[] cuts;
    [SerializeField, Min(0f)] private float openingFadeDuration = 0.5f;
    [SerializeField, Min(0f)] private float cutFadeDuration;

    [Header("Completion")]
    [SerializeField] private string nextSceneName;

    [Header("Skip")]
    [Tooltip("When this sequence has already been completed once, the start input immediately completes it without showing the panel or opening graphic.")]
    [SerializeField] private bool isSkipped;
    [Tooltip("Optional persistent key for this sequence. Leave empty to use the scene and GameObject names.")]
    [SerializeField] private string viewingStateKey;

    private int nextCutIndex;
    private bool isRunning;
    private bool isInputLocked;
    private bool isComplete;
    private Coroutine fadeRoutine;

    //기본 설정
    private void Awake()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartSequence);
        }

        foreach (Cut cut in cuts)
        {
            Graphic graphic = cut.Graphic;
            if (graphic == null)
            {
                continue;
            }

            graphic.raycastTarget = false;
            graphic.gameObject.SetActive(false);
        }

        if (openingGraphic != null)
        {
            openingGraphic.raycastTarget = true;
            SetAlpha(openingGraphic, 0f);
        }

        isRunning = startButton == null && sequencePanel == null;
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartSequence);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Advance();
    }

    //Sequence 시작
    public void StartSequence()
    {
        if (isRunning || isComplete)
        {
            return;
        }

        if (isSkipped && HasBeenViewed())
        {
            CompleteSequence();
            return;
        }

        nextCutIndex = 0;
        isRunning = true;
        isInputLocked = false;

        if (startButton != null)
        {
            startButton.interactable = false;
        }

        if (sequencePanel != null)
        {
            sequencePanel.SetActive(true);
        }

        if (openingGraphic != null)
        {
            StartFade(openingGraphic, openingFadeDuration);
        }
    }

    //Sequence 진행
    public void Advance()
    {
        if (!isRunning || isInputLocked || isComplete)
        {
            return;
        }

        if (isSkipped && HasBeenViewed())
        {
            CompleteSequence();
            return;
        }

        if (nextCutIndex < cuts.Length)
        {
            Cut nextCut = cuts[nextCutIndex++];
            if (nextCut.HidePreviousCuts)
            {
                HideShownCuts(nextCutIndex - 1);
            }

            Graphic graphic = nextCut.Graphic;
            if (graphic != null)
            {
                graphic.gameObject.SetActive(true);
                StartFade(graphic, cutFadeDuration);
            }

            return;
        }

        CompleteSequence();
    }

    //Sequence 완료
    private void CompleteSequence()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            isComplete = true;
            MarkAsViewed();
            HideShownCuts(cuts.Length);

            if (sequencePanel != null)
            {
                sequencePanel.SetActive(false);
            }

            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogWarning($"The configured next scene '{nextSceneName}' is not in Build Settings.", this);
            return;
        }

        isComplete = true;
        MarkAsViewed();
        SceneManager.LoadScene(nextSceneName);
    }

    private bool HasBeenViewed()
    {
        return PlayerPrefs.GetInt(GetViewedKey(), 0) == 1;
    }

    private void MarkAsViewed()
    {
        string viewedKey = GetViewedKey();
        if (PlayerPrefs.GetInt(viewedKey, 0) == 1)
        {
            return;
        }

        PlayerPrefs.SetInt(viewedKey, 1);
        PlayerPrefs.Save();
    }

    private string GetViewedKey()
    {
        if (!string.IsNullOrWhiteSpace(viewingStateKey))
        {
            return ViewedKeyPrefix + viewingStateKey;
        }

        return ViewedKeyPrefix + SceneManager.GetActiveScene().name + "." + gameObject.name;
    }

    private void HideShownCuts(int endExclusive)
    {
        for (int index = 0; index < endExclusive; index++)
        {
            Graphic graphic = cuts[index].Graphic;
            if (graphic != null)
            {
                graphic.gameObject.SetActive(false);
            }
        }
    }

    //Fade In/Out 
    private void StartFade(Graphic graphic, float duration)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        SetAlpha(graphic, 0f);
        if (duration <= 0f)
        {
            SetAlpha(graphic, 1f);
            return;
        }

        fadeRoutine = StartCoroutine(FadeIn(graphic, duration));
    }

    private IEnumerator FadeIn(Graphic graphic, float duration)
    {
        isInputLocked = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(graphic, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        SetAlpha(graphic, 1f);
        isInputLocked = false;
        fadeRoutine = null;
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
}
