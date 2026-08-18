using DG.Tweening;
using UnityEngine;

public sealed class TitleEntranceAnimation : MonoBehaviour
{
    [Header("Title Elements")]
    [SerializeField] private RectTransform titleText;
    [SerializeField] private RectTransform titleCharacter;

    [Header("Entrance")]
    [SerializeField, Min(0f)] private float entranceDuration = 0.9f;

    [Header("Floating")]
    [SerializeField, Min(0f)] private float floatingHeight = 16f;
    [SerializeField, Min(0f)] private float floatingDuration = 1.4f;

    private Sequence titleTextSequence;
    private Sequence titleCharacterSequence;
    private Vector2 titleTextPosition;
    private Vector2 titleCharacterPosition;
    private Vector3 titleTextScale;

    private void Start()
    {
        if (titleText == null || titleCharacter == null)
        {
            Debug.LogError("Title entrance animation requires both title RectTransforms.", this);
            enabled = false;
            return;
        }

        titleTextPosition = titleText.anchoredPosition;
        titleCharacterPosition = titleCharacter.anchoredPosition;
        titleTextScale = titleText.localScale;

        PlayTitleTextEntrance();
        PlayCharacterEntrance();
    }

    private void OnDisable()
    {
        titleTextSequence?.Kill();
        titleCharacterSequence?.Kill();

        if (titleText != null)
        {
            titleText.anchoredPosition = titleTextPosition;
            titleText.localScale = titleTextScale;
        }

        if (titleCharacter != null)
        {
            titleCharacter.anchoredPosition = titleCharacterPosition;
        }
    }

    private void PlayTitleTextEntrance()
    {
        float offScreenOffset = titleText.rect.width + ((RectTransform)titleText.parent).rect.width;
        titleText.anchoredPosition = titleTextPosition + Vector2.right * offScreenOffset;

        titleTextSequence = DOTween.Sequence()
            .Append(titleText.DOAnchorPos(titleTextPosition, entranceDuration).SetEase(Ease.OutBack))
            .Append(titleText.DOScaleY(titleTextScale.y * 1.05f, floatingDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo))
            .SetLink(gameObject);
    }

    private void PlayCharacterEntrance()
    {
        float offScreenOffset = titleCharacter.rect.width + ((RectTransform)titleCharacter.parent).rect.width;
        titleCharacter.anchoredPosition = titleCharacterPosition + Vector2.left * offScreenOffset;

        titleCharacterSequence = DOTween.Sequence()
            .Append(titleCharacter.DOAnchorPos(titleCharacterPosition, entranceDuration).SetEase(Ease.OutBack))
            .Append(titleCharacter.DOAnchorPosY(titleCharacterPosition.y + floatingHeight, floatingDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo))
            .SetLink(gameObject);
    }
}
