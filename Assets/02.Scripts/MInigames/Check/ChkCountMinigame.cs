using System;
using DreamOfMilitary.Routine;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 총원에서 보드에 표시된 모든 제외 인원을 빼 점호 인원을 계산하는 미니게임이다.
/// 제한 시간 만료는 RoutineRunner의 기본 실패 처리에 맡긴다.
/// </summary>
public sealed class ChkCountMinigame : MonoBehaviour, IMinigame
{
    [Header("점호 현황 텍스트")]
    [SerializeField] private Text totalCountText;
    [Tooltip("휴가, 외박, 근무 등 총원에서 제외할 항목의 숫자 텍스트를 모두 등록합니다.")]
    [SerializeField] private Text[] absentCountTexts;
    [SerializeField] private Text inputText;

    [Header("숫자 키패드")]
    [SerializeField] private ChkCountKeypadButton[] keypadButtons;
    [SerializeField, Range(1, 4)] private int maximumInputDigits = 3;

    [Header("무작위 인원 범위")]
    [SerializeField, Min(1)] private int minimumTotalCount = 40;
    [SerializeField, Min(1)] private int maximumTotalCount = 80;
    [SerializeField, Min(0)] private int minimumAbsentCount = 1;
    [SerializeField, Min(0)] private int maximumAbsentCount = 9;

    private Action<MinigameJudgement> onCompleted;
    private int correctCount;
    private string input = string.Empty;
    private bool isRunning;

    public void Begin(MinigameContext context, Action<MinigameJudgement> completed)
    {
        if (completed == null)
        {
            throw new ArgumentNullException(nameof(completed));
        }

        if (!HasValidSetup())
        {
            Debug.LogError("[ChkCount] 총원, 제외 인원 텍스트와 숫자 키패드 버튼을 Inspector에 등록하세요.", this);
            completed(MinigameJudgement.Failure);
            return;
        }

        onCompleted = completed;
        isRunning = true;
        input = string.Empty;
        GenerateCounts();
        RefreshInputText();
    }

    private void Update()
    {
        if (!isRunning || MouseInputManager.Instance == null || !MouseInputManager.Instance.IsClickDown())
        {
            return;
        }

        var clickedObject = MouseInputManager.Instance.GetClickedObject();
        if (clickedObject == null)
        {
            return;
        }

        var key = clickedObject.GetComponentInParent<ChkCountKeypadButton>();
        if (key == null)
        {
            return;
        }

        HandleKey(key);
    }

    public void Abort()
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        onCompleted = null;
    }

    private void HandleKey(ChkCountKeypadButton key)
    {
        switch (key.Type)
        {
            case ChkCountKeypadButton.KeyType.Digit:
                if (input.Length < maximumInputDigits)
                {
                    input += key.Digit.ToString();
                    RefreshInputText();
                }
                break;

            case ChkCountKeypadButton.KeyType.Backspace:
                if (input.Length > 0)
                {
                    input = input.Substring(0, input.Length - 1);
                    RefreshInputText();
                }
                break;

            case ChkCountKeypadButton.KeyType.Submit:
                SubmitAnswer();
                break;
        }
    }

    private void GenerateCounts()
    {
        var minimumTotal = Mathf.Min(minimumTotalCount, maximumTotalCount);
        var maximumTotal = Mathf.Max(minimumTotalCount, maximumTotalCount);
        var totalCount = UnityEngine.Random.Range(minimumTotal, maximumTotal + 1);
        var remainingCount = totalCount;

        for (var index = 0; index < absentCountTexts.Length; index++)
        {
            // 뒤에 남은 항목도 최소값을 배정할 수 있게 최대값을 제한한다.
            var remainingEntries = absentCountTexts.Length - index - 1;
            var maximumForThisEntry = Mathf.Max(0, Mathf.Min(
                maximumAbsentCount,
                remainingCount - minimumAbsentCount * remainingEntries));
            var minimumForThisEntry = Mathf.Clamp(minimumAbsentCount, 0, maximumForThisEntry);
            var absentCount = UnityEngine.Random.Range(minimumForThisEntry, maximumForThisEntry + 1);

            absentCountTexts[index].text = $"{absentCount}";
            remainingCount -= absentCount;
        }

        totalCountText.text = $"{totalCount}";
        correctCount = remainingCount;
    }

    private void RefreshInputText()
    {
        if (inputText != null)
        {
            inputText.text = string.IsNullOrEmpty(input) ? "0" : $"{input}";
        }
    }

    private void SubmitAnswer()
    {
        if (!int.TryParse(input, out var answer) || answer != correctCount)
        {
            Complete(MinigameJudgement.Failure);
            return;
        }

        Complete(MinigameJudgement.Success);
    }

    private bool HasValidSetup()
    {
        if (totalCountText == null || inputText == null || absentCountTexts == null || absentCountTexts.Length == 0 ||
            keypadButtons == null || keypadButtons.Length == 0)
        {
            return false;
        }

        for (var index = 0; index < absentCountTexts.Length; index++)
        {
            if (absentCountTexts[index] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void Complete(MinigameJudgement judgement)
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        var completed = onCompleted;
        onCompleted = null;
        completed?.Invoke(judgement);
    }

    private void OnDisable()
    {
        Abort();
    }
}
