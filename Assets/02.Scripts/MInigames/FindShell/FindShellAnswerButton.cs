using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FindShellMinigame의 답안 버튼 루트에 붙인다.
/// Button.onClick은 연결하지 않는다.
/// 클릭은 MouseInputManager가 일괄 처리한다.
/// </summary>
public sealed class FindShellAnswerButton : MonoBehaviour
{
    [SerializeField, Range(4, 10)]
    private int answerCount = 4;

    [SerializeField]
    private bool isAnswerable = true;

    private Text answerText;

    public int AnswerCount => answerCount;

    public bool IsAnswerable =>
        isAnswerable && gameObject.activeInHierarchy;

    /// <summary>
    /// 답안 숫자와 UI Text를 함께 변경한다.
    /// </summary>
    /// 
    private void Awake()
    {
        if (answerText == null)
        {
            answerText = GetComponentInChildren<Text>();
        }
    }

    public void SetAnswerCount(int value)
    {
        answerCount = value;

        if (answerText != null)
        {
            answerText.text = $"{value}개";
        }
    }
}