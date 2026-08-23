using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 군가 순서 맞추기용 구절 버튼이다.
/// Button의 Color Block에서 Disabled Color를 설정하면 정답 선택 후 그 색으로 표시된다.
/// </summary>
[RequireComponent(typeof(Button))]
public sealed class SongPhraseButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Text phraseText;

    public int PhraseOrder { get; private set; }
    public bool IsUsable => button != null && button.interactable && gameObject.activeInHierarchy;

    private void Reset()
    {
        button = GetComponent<Button>();
        phraseText = GetComponentInChildren<Text>(true);
    }

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (phraseText == null)
        {
            phraseText = GetComponentInChildren<Text>(true);
        }
    }

    public void SetPhrase(string phrase, int phraseOrder)
    {
        PhraseOrder = phraseOrder;
        if (phraseText != null)
        {
            phraseText.text = phrase;
        }

        gameObject.SetActive(true);
        if (button != null)
        {
            button.interactable = true;
        }
    }

    public void SetUsed()
    {
        if (button != null)
        {
            button.interactable = false;
        }
    }
}
