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
    [SerializeField] private GameObject examProgressRoot;
    [SerializeField] private TextMeshProUGUI examProgressText;

    private RoutineRunState _previousState = RoutineRunState.Idle;
    private int _examSuccessCount;

    private void Awake()
    {
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
        slot.gameObject.SetActive(true);
        slot.color = judgement == MinigameJudgement.Success ? successColor : failureColor;
    }

    private void ShowExamProgress(int current, int total)
    {
        SetActive(examProgressRoot, true);

        if (examProgressText == null)
        {
            return;
        }

        var requiredSuccessCount = progressionConfig.GetExamRequiredSuccessCount(GameState.Instance.CaptureSnapshot().Rank);
        var remaining = total - current;

        examProgressText.text = $"승급까지 {_examSuccessCount}/{requiredSuccessCount}\n남은 종목 : {remaining}";
    }

    private void ResetSlots()
    {
        if (progressSlots == null)
        {
            return;
        }

        for (var index = 0; index < progressSlots.Length; index++)
        {
            if (progressSlots[index] != null)
            {
                progressSlots[index].gameObject.SetActive(false);
            }
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
