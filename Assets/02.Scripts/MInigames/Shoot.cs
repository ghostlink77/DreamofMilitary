// ========================
// 훈련 사격
// ========================

using System;
using DreamOfMilitary.Routine;
using DreamOfMilitary.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class Shoot : MonoBehaviour, IMinigame
{
    private enum TargetMotionState
    {
        Hidden,
        Rising,
        Visible,
        Lowering
    }

    [Header("거리별 표적 (50m / 100m / 200m)")]
    [SerializeField] private GameObject[] targetObjects;

    [Header("조준점")]
    [SerializeField] private RectTransform cursor;

    [Header("탄약")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField, Min(1)] private int easyAmmoCount = 10;
    [SerializeField, Min(1)] private int hardAmmoCount = 8;

    [Header("표적 기록")]
    [SerializeField] private TMP_Text targetText;
    [SerializeField, Min(1)] private int easyTargetCount = 10;
    [SerializeField, Min(1)] private int hardTargetCount = 10;

    [Header("난이도별 표적 노출시간")]
    [SerializeField, Min(0.05f)] private float easyTargetExposureSeconds = 1.5f;
    [SerializeField, Min(0.05f)] private float hardTargetExposureSeconds = 1f;

    [Header("표적 이동 연출")]
    [SerializeField, Min(0.01f)] private float targetMoveSeconds = 0.25f;
    [SerializeField] private Color hitTargetColor = Color.red;

    private Action<MinigameJudgement> _onCompleted;
    private System.Random _random;
    private bool _isPlaying;
    private bool _currentTargetHit;
    private int _currentTargetIndex = -1;
    private int _remainingAmmo;
    private int _maxAmmo;
    private int _hitTargetCount;
    private int _appearedTargetCount;
    private int _maxTargetCount;
    private float _targetExposureSeconds;
    private float _targetExpiresAt;
    private float _gameEndsAt;
    private RectTransform[] _targetRects;
    private Vector2[] _targetRaisedPositions;
    private Graphic[] _targetGraphics;
    private Color[] _targetOriginalColors;
    private TargetMotionState _targetMotionState;
    private float _motionStartedAt;
    private Vector2 _motionFrom;
    private Vector2 _motionTo;
    private bool _failAfterLowering;

    public void Begin(MinigameContext context, Action<MinigameJudgement> onCompleted)
    {
        _onCompleted = onCompleted;
        _random = new System.Random(context.RandomSeed);
        _targetExposureSeconds = context.DifficultyTier >= 2
            ? hardTargetExposureSeconds
            : easyTargetExposureSeconds;
        _maxAmmo = context.DifficultyTier >= 2
            ? hardAmmoCount
            : easyAmmoCount;
        _maxAmmo = Mathf.Max(1, _maxAmmo);
        _remainingAmmo = _maxAmmo;
        _maxTargetCount = context.DifficultyTier >= 2
            ? hardTargetCount
            : easyTargetCount;
        _maxTargetCount = Mathf.Max(1, _maxTargetCount);
        _hitTargetCount = 0;
        _appearedTargetCount = 0;
        _targetExposureSeconds = Mathf.Max(0.05f, _targetExposureSeconds);
        _gameEndsAt = Time.time + context.TimeLimitSeconds;
        _currentTargetIndex = -1;
        _currentTargetHit = false;
        _targetMotionState = TargetMotionState.Hidden;
        _failAfterLowering = false;
        _isPlaying = true;
        Cursor.visible = false;

        CacheTargetLayout();
        HideAllTargets();
        UpdateAmmoText();
        UpdateTargetText();

        if (targetObjects == null || targetObjects.Length == 0)
        {
            Debug.LogError("Shoot: 표적이 연결되지 않았습니다.", this);
            Complete(MinigameJudgement.Failure);
            return;
        }

        ShowNextTarget();
    }

    private void Update()
    {
        if (!_isPlaying)
        {
            return;
        }

        UpdateCursorPosition();

        // 최종 제한시간 판정은 SurviveUntilLimit 규칙을 사용하는 RoutineRunner가 담당한다.
        if (Time.time >= _gameEndsAt)
        {
            return;
        }

        UpdateTargetMotion();

        if (!_isPlaying)
        {
            return;
        }

        if (MouseInputManager.Instance != null
            && MouseInputManager.Instance.IsClickDown())
        {
            Fire(MouseInputManager.Instance.MouseScreenPosition);
        }
    }

    private void Fire(Vector2 pointerScreenPosition)
    {
        if (_remainingAmmo <= 0
            || (_targetMotionState != TargetMotionState.Rising
                && _targetMotionState != TargetMotionState.Visible
                && _targetMotionState != TargetMotionState.Lowering))
        {
            return;
        }

        _remainingAmmo--;
        GameAudioController.Instance?.PlayGunshot();
        UpdateAmmoText();
        TryHitCurrentTarget(pointerScreenPosition);
    }

    private void UpdateCursorPosition()
    {
        if (cursor == null || MouseInputManager.Instance == null)
        {
            return;
        }

        if (cursor.parent is not RectTransform parentRect)
        {
            return;
        }

        var uiCamera = GetUiCamera(cursor);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                MouseInputManager.Instance.MouseScreenPosition,
                uiCamera,
                out var localPoint))
        {
            cursor.anchoredPosition = localPoint;
        }
    }

    private void TryHitCurrentTarget(Vector2 pointerScreenPosition)
    {
        if (_currentTargetHit
            || _currentTargetIndex < 0
            || _currentTargetIndex >= targetObjects.Length)
        {
            return;
        }

        var activeTarget = targetObjects[_currentTargetIndex];
        if (activeTarget == null
            || !activeTarget.activeInHierarchy
            || !activeTarget.TryGetComponent<RectTransform>(out var targetRect))
        {
            return;
        }

        if (RectTransformUtility.RectangleContainsScreenPoint(
                targetRect,
                pointerScreenPosition,
                GetUiCamera(targetRect)))
        {
            _currentTargetHit = true;
            GameAudioController.Instance?.PlayTargetHit();
            _hitTargetCount++;
            UpdateTargetText();
            SetTargetHitColor(_currentTargetIndex);

            if (_targetMotionState == TargetMotionState.Lowering)
            {
                _failAfterLowering = false;
            }
            else
            {
                BeginCurrentTargetLowering(false);
            }
        }
    }

    private void ShowNextTarget()
    {
        HideAllTargets();

        if (_appearedTargetCount >= _maxTargetCount)
        {
            _currentTargetIndex = -1;
            return;
        }

        var nextIndex = GetNextTargetIndex();
        if (nextIndex < 0)
        {
            Complete(MinigameJudgement.Failure);
            return;
        }

        _currentTargetIndex = nextIndex;
        _currentTargetHit = false;
        _failAfterLowering = false;

        var targetRect = _targetRects[_currentTargetIndex];
        if (targetRect == null)
        {
            Complete(MinigameJudgement.Failure);
            return;
        }

        var raisedPosition = _targetRaisedPositions[_currentTargetIndex];
        var loweredPosition = raisedPosition + Vector2.down * targetRect.rect.height;
        targetRect.anchoredPosition = loweredPosition;
        targetObjects[_currentTargetIndex].SetActive(true);
        _appearedTargetCount++;
        UpdateTargetText();
        BeginTargetMotion(TargetMotionState.Rising, loweredPosition, raisedPosition);
    }

    private void UpdateTargetMotion()
    {
        if (_currentTargetIndex < 0
            || _currentTargetIndex >= targetObjects.Length
            || _targetRects == null
            || _currentTargetIndex >= _targetRects.Length)
        {
            return;
        }

        var targetRect = _targetRects[_currentTargetIndex];
        if (targetRect == null)
        {
            return;
        }

        switch (_targetMotionState)
        {
            case TargetMotionState.Rising:
                if (UpdateMotionPosition(targetRect))
                {
                    _targetMotionState = TargetMotionState.Visible;
                    _targetExpiresAt = Time.time + _targetExposureSeconds;
                }
                break;

            case TargetMotionState.Visible:
                if (Time.time >= _targetExpiresAt)
                {
                    BeginCurrentTargetLowering(!_currentTargetHit);
                }
                break;

            case TargetMotionState.Lowering:
                if (UpdateMotionPosition(targetRect))
                {
                    targetObjects[_currentTargetIndex].SetActive(false);
                    _targetMotionState = TargetMotionState.Hidden;

                    if (_hitTargetCount >= _maxTargetCount)
                    {
                        Complete(MinigameJudgement.Success);
                    }
                    else if (_failAfterLowering)
                    {
                        Complete(MinigameJudgement.Failure);
                    }
                    else
                    {
                        ShowNextTarget();
                    }
                }
                break;
        }
    }

    private void BeginCurrentTargetLowering(bool failAfterLowering)
    {
        if (_currentTargetIndex < 0
            || _targetRects == null
            || _currentTargetIndex >= _targetRects.Length)
        {
            return;
        }

        var targetRect = _targetRects[_currentTargetIndex];
        if (targetRect == null)
        {
            return;
        }

        _failAfterLowering = failAfterLowering;
        var loweredPosition = _targetRaisedPositions[_currentTargetIndex]
                              + Vector2.down * targetRect.rect.height;
        BeginTargetMotion(
            TargetMotionState.Lowering,
            targetRect.anchoredPosition,
            loweredPosition);
    }

    private void BeginTargetMotion(TargetMotionState state, Vector2 from, Vector2 to)
    {
        _targetMotionState = state;
        _motionStartedAt = Time.time;
        _motionFrom = from;
        _motionTo = to;
    }

    private bool UpdateMotionPosition(RectTransform targetRect)
    {
        var duration = Mathf.Max(0.01f, targetMoveSeconds);
        var progress = Mathf.Clamp01((Time.time - _motionStartedAt) / duration);
        targetRect.anchoredPosition = Vector2.Lerp(_motionFrom, _motionTo, progress);

        if (progress < 1f)
        {
            return false;
        }

        targetRect.anchoredPosition = _motionTo;
        return true;
    }

    private void CacheTargetLayout()
    {
        if (targetObjects == null)
        {
            _targetRects = Array.Empty<RectTransform>();
            _targetRaisedPositions = Array.Empty<Vector2>();
            _targetGraphics = Array.Empty<Graphic>();
            _targetOriginalColors = Array.Empty<Color>();
            return;
        }

        _targetRects = new RectTransform[targetObjects.Length];
        _targetRaisedPositions = new Vector2[targetObjects.Length];
        _targetGraphics = new Graphic[targetObjects.Length];
        _targetOriginalColors = new Color[targetObjects.Length];

        for (var index = 0; index < targetObjects.Length; index++)
        {
            if (targetObjects[index] == null
                || !targetObjects[index].TryGetComponent<RectTransform>(out var targetRect))
            {
                continue;
            }

            _targetRects[index] = targetRect;
            _targetRaisedPositions[index] = targetRect.anchoredPosition;

            if (targetObjects[index].TryGetComponent<Graphic>(out var targetGraphic))
            {
                _targetGraphics[index] = targetGraphic;
                _targetOriginalColors[index] = targetGraphic.color;
            }
        }
    }

    private int GetNextTargetIndex()
    {
        if (targetObjects == null || targetObjects.Length == 0)
        {
            return -1;
        }

        if (targetObjects.Length == 1)
        {
            return targetObjects[0] != null ? 0 : -1;
        }

        var availableCount = 0;
        for (var index = 0; index < targetObjects.Length; index++)
        {
            if (index != _currentTargetIndex && targetObjects[index] != null)
            {
                availableCount++;
            }
        }

        if (availableCount == 0)
        {
            return -1;
        }

        var selected = _random.Next(availableCount);
        for (var index = 0; index < targetObjects.Length; index++)
        {
            if (index == _currentTargetIndex || targetObjects[index] == null)
            {
                continue;
            }

            if (selected == 0)
            {
                return index;
            }

            selected--;
        }

        return -1;
    }

    private void HideAllTargets()
    {
        if (targetObjects == null)
        {
            return;
        }

        for (var index = 0; index < targetObjects.Length; index++)
        {
            if (targetObjects[index] != null)
            {
                if (_targetRects != null
                    && _targetRaisedPositions != null
                    && index < _targetRects.Length
                    && index < _targetRaisedPositions.Length
                    && _targetRects[index] != null)
                {
                    _targetRects[index].anchoredPosition = _targetRaisedPositions[index];
                }

                RestoreTargetColor(index);

                targetObjects[index].SetActive(false);
            }
        }
    }

    private void SetTargetHitColor(int targetIndex)
    {
        if (_targetGraphics != null
            && targetIndex >= 0
            && targetIndex < _targetGraphics.Length
            && _targetGraphics[targetIndex] != null)
        {
            _targetGraphics[targetIndex].color = hitTargetColor;
        }
    }

    private void RestoreTargetColor(int targetIndex)
    {
        if (_targetGraphics != null
            && _targetOriginalColors != null
            && targetIndex >= 0
            && targetIndex < _targetGraphics.Length
            && targetIndex < _targetOriginalColors.Length
            && _targetGraphics[targetIndex] != null)
        {
            _targetGraphics[targetIndex].color = _targetOriginalColors[targetIndex];
        }
    }

    private void UpdateAmmoText()
    {
        if (ammoText != null)
        {
            ammoText.text = $"{_remainingAmmo} / {_maxAmmo}";
        }
    }

    private void UpdateTargetText()
    {
        if (targetText != null)
        {
            targetText.text = $"{_hitTargetCount} / {_maxTargetCount}";
        }
    }

    private static Camera GetUiCamera(Component component)
    {
        var canvas = component.GetComponentInParent<Canvas>();
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
    }

    private void Complete(MinigameJudgement judgement)
    {
        if (!_isPlaying)
        {
            return;
        }

        _isPlaying = false;
        Cursor.visible = true;
        var callback = _onCompleted;
        _onCompleted = null;
        callback?.Invoke(judgement);
    }

    public void Abort()
    {
        _isPlaying = false;
        _onCompleted = null;
        _currentTargetIndex = -1;
        _currentTargetHit = false;
        _targetMotionState = TargetMotionState.Hidden;
        Cursor.visible = true;
        HideAllTargets();
    }

    private void OnDisable()
    {
        Abort();
    }
}
