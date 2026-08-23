using UnityEngine;

/// <summary>
/// 점호 인원 세기 미니게임의 숫자 키패드 버튼에 붙인다.
/// Button.onClick은 연결하지 않으며 ChkCountMinigame이 클릭을 처리한다.
/// </summary>
public sealed class ChkCountKeypadButton : MonoBehaviour
{
    public enum KeyType
    {
        Digit,
        Backspace,
        Submit
    }

    [SerializeField] private KeyType keyType;
    [SerializeField, Range(0, 9)] private int digit;

    public KeyType Type => keyType;
    public int Digit => digit;
}