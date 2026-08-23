using DG.Tweening;
using DreamOfMilitary.Routine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoutineHUDView : MonoBehaviour
{
    [SerializeField] private RoutineRunner routineRunner;
    [SerializeField] private TextMeshProUGUI commandText;
    [SerializeField] private Slider timeSlider;

    [Header("지시문 연출")]
    [SerializeField, Min(1f)] private float commandIntroScale = 1.5f;
    [SerializeField, Min(0f)] private float commandCenterHoldSeconds = 0.6f;
    [SerializeField, Min(0f)] private float commandReturnSeconds = 0.15f;

    private RectTransform _commandRectTransform;
    private Vector2 _commandDefaultAnchoredPosition;
    private Vector3 _commandDefaultScale;
    private Sequence _commandSequence;

    private void Awake()
    {
        _commandRectTransform = commandText.rectTransform;
        _commandDefaultAnchoredPosition = _commandRectTransform.anchoredPosition;
        _commandDefaultScale = _commandRectTransform.localScale;

        timeSlider.minValue = 0;
        timeSlider.maxValue = 1;
        timeSlider.wholeNumbers = false;
        timeSlider.interactable = false;
        timeSlider.value = 0;
    }

    private void OnEnable()
    {
        routineRunner.StateChanged += OnStateChanged;
        routineRunner.CommandShown += OnCommandShown;
        routineRunner.TimeNormalizedChanged += OnTimeNormalizedChanged;
        OnStateChanged(routineRunner.State);
    }

    private void OnDisable()
    {
        routineRunner.StateChanged -= OnStateChanged;
        routineRunner.CommandShown -= OnCommandShown;
        routineRunner.TimeNormalizedChanged -= OnTimeNormalizedChanged;
        StopCommandAnimation();
    }

    private void OnCommandShown(string command, int current, int total)
    {
        commandText.text = command;
        PlayCommandAnimation();
    }

    private void PlayCommandAnimation()
    {
        StopCommandAnimation();

        _commandRectTransform.localScale = _commandDefaultScale * commandIntroScale;
        _commandRectTransform.position = GetCommandCenterWorldPosition();

        _commandSequence = DOTween.Sequence()
            .AppendInterval(commandCenterHoldSeconds)
            .Append(_commandRectTransform.DOAnchorPos(_commandDefaultAnchoredPosition, commandReturnSeconds).SetEase(Ease.OutCubic))
            .Join(_commandRectTransform.DOScale(_commandDefaultScale, commandReturnSeconds).SetEase(Ease.OutCubic))
            .OnComplete(() => _commandSequence = null);
    }

    private Vector3 GetCommandCenterWorldPosition()
    {
        if (_commandRectTransform.parent is not RectTransform parentRectTransform)
        {
            return _commandRectTransform.position;
        }

        var centerWorldPosition = parentRectTransform.TransformPoint(parentRectTransform.rect.center);
        var centerToPivot = new Vector3(
            (_commandRectTransform.pivot.x - 0.5f) * _commandRectTransform.rect.width,
            (_commandRectTransform.pivot.y - 0.5f) * _commandRectTransform.rect.height,
            0f);

        return centerWorldPosition + _commandRectTransform.TransformVector(centerToPivot);
    }

    private void StopCommandAnimation()
    {
        _commandSequence?.Kill();
        _commandSequence = null;

        if (_commandRectTransform == null)
        {
            return;
        }

        _commandRectTransform.anchoredPosition = _commandDefaultAnchoredPosition;
        _commandRectTransform.localScale = _commandDefaultScale;
    }

    private void OnTimeNormalizedChanged(float normalizedTime)
    {
        timeSlider.value = Mathf.Clamp01(normalizedTime);
    }

    private void OnStateChanged(RoutineRunState state)
    {
        var commandVisible = state == RoutineRunState.ShowingCommand || state == RoutineRunState.Playing;
        var timeSliderVisible = state == RoutineRunState.Playing || state == RoutineRunState.ShowingProgress;

        if (!commandVisible)
        {
            StopCommandAnimation();
        }

        commandText.gameObject.SetActive(commandVisible);
        timeSlider.gameObject.SetActive(timeSliderVisible);
    }
}
