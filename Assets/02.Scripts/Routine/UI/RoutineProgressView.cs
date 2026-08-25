using DG.Tweening;
using DreamOfMilitary.Progression;
using DreamOfMilitary.Routine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RoutineProgressView : MonoBehaviour
{
    [SerializeField] private RoutineRunner routineRunner;
    [SerializeField] private ProgressionConfig progressionConfig;
    [SerializeField] private GameObject feedbackRoot;
    [SerializeField] private TextMeshProUGUI successText;
    [SerializeField] private TextMeshProUGUI failureText;
    [SerializeField] private GameObject calendarRoot;
    [SerializeField] private Image[] progressSlots;
    [SerializeField] private Color successColor = new(1f, 0.8772222f, 0f, 0.5686275f);
    [SerializeField] private Color failureColor = new(1f, 0.08246528f, 0f, 0.5686275f);
    [SerializeField, Min(0f)] private float progressFillDuration = 0.4f;
    [SerializeField] private Ease progressFillEase = Ease.OutCubic;
    [SerializeField] private GameObject examProgressRoot;
    [SerializeField] private TextMeshProUGUI examProgressText;

    private RoutineRunState _previousState = RoutineRunState.Idle;
    private Vector3[] _progressSlotDefaultScales;
    private int _examSuccessCount;

    private void Awake()
    {
        InitializeProgressSlots();
        SetActive(feedbackRoot, false);
        SetActive(calendarRoot, false);
        SetActive(examProgressRoot, false);
        ResetSlots();
    }

    private void OnEnable()
    {
        if (routineRunner == null)
        {
            return;
        }

        routineRunner.StateChanged += OnStateChanged;
        routineRunner.FeedbackShown += OnFeedbackShown;
        routineRunner.ProgressShown += OnProgressShown;
        OnStateChanged(routineRunner.State);
    }

    private void OnDisable()
    {
        CompleteProgressSlotTweens();

        if (routineRunner == null)
        {
            return;
        }

        routineRunner.StateChanged -= OnStateChanged;
        routineRunner.FeedbackShown -= OnFeedbackShown;
        routineRunner.ProgressShown -= OnProgressShown;
    }

    private void OnStateChanged(RoutineRunState state)
    {
        if (state == RoutineRunState.ShowingCommand && (_previousState == RoutineRunState.Idle || _previousState == RoutineRunState.Completed))
        {
            ResetSlots();
            _examSuccessCount = 0;
        }

        if (state != RoutineRunState.ShowingFeedback)
        {
            SetActive(feedbackRoot, false);
        }

        if (state != RoutineRunState.ShowingProgress)
        {
            SetActive(calendarRoot, false);
            SetActive(examProgressRoot, false);
        }

        _previousState = state;
    }

    private void OnFeedbackShown(MinigameJudgement judgement, int score)
    {
        SetActive(feedbackRoot, true);

        if (successText != null)
        {
            successText.gameObject.SetActive(judgement == MinigameJudgement.Success);
        }

        if (failureText != null)
        {
            failureText.gameObject.SetActive(judgement == MinigameJudgement.Failure);
        }

        if (routineRunner.CurrentRunMode == RoutineRunMode.Exam && judgement == MinigameJudgement.Success)
        {
            _examSuccessCount++;
        }
    }

    private void OnProgressShown(MinigameJudgement judgement, int current, int total)
    {
        if (routineRunner.CurrentRunMode == RoutineRunMode.Exam)
        {
            ShowExamProgress(current, total);
            return;
        }

        SetActive(calendarRoot, true);

        var slotIndex = current - 1;

        if (slotIndex < 0 || progressSlots == null || slotIndex >= progressSlots.Length || progressSlots[slotIndex] == null)
        {
            return;
        }

        var slot = progressSlots[slotIndex];
        var slotRectTransform = slot.rectTransform;
        var targetScale = GetProgressSlotDefaultScale(slotIndex, slotRectTransform);

        slotRectTransform.DOKill();
        slotRectTransform.localScale = new Vector3(0f, targetScale.y, targetScale.z);
        slot.color = judgement == MinigameJudgement.Success ? successColor : failureColor;
        slot.gameObject.SetActive(true);

        if (progressFillDuration <= 0f)
        {
            slotRectTransform.localScale = targetScale;
            return;
        }

        slotRectTransform.DOScaleX(targetScale.x, progressFillDuration).SetEase(progressFillEase);
    }

    private void ShowExamProgress(int current, int total)
    {
        SetActive(examProgressRoot, true);

        if (examProgressText == null)
        {
            return;
        }

        var snapshot = GameState.Instance.CaptureSnapshot();
        var requiredSuccessCount = progressionConfig.GetExamRequiredSuccessCount(snapshot.Rank);
        var remaining = total - current;
        var goalText = progressionConfig.IsDischargeExam(snapshot) ? "전역까지" : "승급까지";

        examProgressText.text = $"{goalText} {_examSuccessCount}/{requiredSuccessCount}\n남은 종목 : {remaining}";
    }

    private void InitializeProgressSlots()
    {
        if (progressSlots == null)
        {
            return;
        }

        _progressSlotDefaultScales = new Vector3[progressSlots.Length];

        for (var index = 0; index < progressSlots.Length; index++)
        {
            if (progressSlots[index] == null)
            {
                continue;
            }

            var slotRectTransform = progressSlots[index].rectTransform;
            _progressSlotDefaultScales[index] = slotRectTransform.localScale;
            SetHorizontalPivotWithoutMoving(slotRectTransform, 0f);
        }
    }

    private void ResetSlots()
    {
        if (progressSlots == null)
        {
            return;
        }

        for (var index = 0; index < progressSlots.Length; index++)
        {
            var slot = progressSlots[index];

            if (slot == null)
            {
                continue;
            }

            slot.rectTransform.DOKill();
            slot.rectTransform.localScale = GetProgressSlotDefaultScale(index, slot.rectTransform);
            slot.gameObject.SetActive(false);
        }
    }

    private void CompleteProgressSlotTweens()
    {
        if (progressSlots == null)
        {
            return;
        }

        for (var index = 0; index < progressSlots.Length; index++)
        {
            if (progressSlots[index] != null)
            {
                progressSlots[index].rectTransform.DOKill(true);
            }
        }
    }

    private Vector3 GetProgressSlotDefaultScale(int slotIndex, RectTransform slotRectTransform)
    {
        if (_progressSlotDefaultScales == null || slotIndex >= _progressSlotDefaultScales.Length)
        {
            return slotRectTransform.localScale;
        }

        return _progressSlotDefaultScales[slotIndex];
    }

    private static void SetHorizontalPivotWithoutMoving(RectTransform rectTransform, float pivotX)
    {
        if (Mathf.Approximately(rectTransform.pivot.x, pivotX))
        {
            return;
        }

        var pivot = rectTransform.pivot;
        var anchoredPosition = rectTransform.anchoredPosition;
        anchoredPosition.x += (pivotX - pivot.x) * rectTransform.rect.width * rectTransform.localScale.x;

        rectTransform.pivot = new Vector2(pivotX, pivot.y);
        rectTransform.anchoredPosition = anchoredPosition;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
