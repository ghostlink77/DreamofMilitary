using DreamOfMilitary.Progression;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EndingResultView : MonoBehaviour
{
    private const float AGradeThreshold = 0.9f;
    private const float BGradeThreshold = 0.75f;
    private const float CGradeThreshold = 0.6f;

    private static readonly Color SGradeColor = new Color32(255, 213, 79, 255);
    private static readonly Color AGradeColor = new Color32(229, 57, 53, 255);
    private static readonly Color BGradeColor = new Color32(30, 136, 229, 255);
    private static readonly Color CGradeColor = new Color32(67, 160, 71, 255);
    private static readonly Color DGradeColor = new Color32(251, 140, 0, 255);

    [Header("Discharge Certificate")]
    [SerializeField] private TMP_Text serviceMonthsText;
    [SerializeField] private TMP_Text successCountText;
    [SerializeField] private TMP_Text failureCountText;
    [SerializeField] private TMP_Text gradeText;
    [SerializeField] private TMP_Text summaryText;

    private enum EndingGrade
    {
        S,
        A,
        B,
        C,
        D
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (GameState.Instance == null)
        {
            Debug.LogError("전역 결과를 표시할 GameState가 존재하지 않습니다.", this);
            return;
        }

        var successCount = GameState.Instance.TotalMinigameSuccessCount;
        var failureCount = GameState.Instance.TotalMinigameFailureCount;
        var totalCount = successCount + failureCount;
        var grade = CalculateGrade(successCount, totalCount);

        serviceMonthsText.text = $"{GameState.Instance.ServiceMonths}개월";
        successCountText.text = successCount.ToString();
        failureCountText.text = failureCount.ToString();

        gradeText.text = grade.ToString();
        gradeText.color = GetGradeColor(grade);
        gradeText.outlineColor = Color.black;
        gradeText.outlineWidth = 0.2f;

        summaryText.text = GetGradeSummary(grade);
    }

    private static EndingGrade CalculateGrade(int successCount, int totalCount)
    {
        if (totalCount <= 0)
        {
            return EndingGrade.D;
        }

        if (successCount == totalCount)
        {
            return EndingGrade.S;
        }

        var successRate = (float)successCount / totalCount;

        if (successRate >= AGradeThreshold)
        {
            return EndingGrade.A;
        }

        if (successRate >= BGradeThreshold)
        {
            return EndingGrade.B;
        }

        if (successRate >= CGradeThreshold)
        {
            return EndingGrade.C;
        }

        return EndingGrade.D;
    }

    private static Color GetGradeColor(EndingGrade grade)
    {
        switch (grade)
        {
            case EndingGrade.S:
                return SGradeColor;
            case EndingGrade.A:
                return AGradeColor;
            case EndingGrade.B:
                return BGradeColor;
            case EndingGrade.C:
                return CGradeColor;
            default:
                return DGradeColor;
        }
    }

    private static string GetGradeSummary(EndingGrade grade)
    {
        switch (grade)
        {
            case EndingGrade.S:
                return "당신은 모두가 인정하는 전설적인 에이스였습니다.";
            case EndingGrade.A:
                return "당신은 어떤 임무도 믿고 맡길 수 있는 최정예 병사였습니다.";
            case EndingGrade.B:
                return "당신은 맡은 임무를 충실히 수행한 모범적인 병사였습니다.";
            case EndingGrade.C:
                return "우여곡절은 있었지만 끝까지 자신의 임무를 완수했습니다.";
            default:
                return "당신은 전우들의 기억에 강렬하게 남은 폐급이었습니다.";
        }
    }
}
