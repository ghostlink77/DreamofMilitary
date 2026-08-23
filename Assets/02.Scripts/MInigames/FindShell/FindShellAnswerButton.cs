using UnityEngine;

/// <summary>
/// FindShellMinigame의 답안 버튼 루트에 붙인다.
/// Button.onClick은 연결하지 않는다. 클릭은 MouseInputManager가 일괄 처리한다.
/// </summary>
public sealed class FindShellAnswerButton : MonoBehaviour
{
    [SerializeField, Range(4, 10)] private int answerCount = 4;
    [SerializeField] private bool isAnswerable = true;

    public int AnswerCount => answerCount;
    public bool IsAnswerable => isAnswerable && gameObject.activeInHierarchy;
}

