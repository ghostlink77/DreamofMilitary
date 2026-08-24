// ========================
// 당직 - 눈치껏 졸아라
// ========================

using System;
using DreamOfMilitary.Routine;
using DreamOfMilitary.Audio;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DangZik : MonoBehaviour, IMinigame
{
    [Header("당직병 상태")]
    [SerializeField] private GameObject sleepState;
    [SerializeField] private GameObject awakeState;

    [Header("실패 연출")]
    [FormerlySerializedAs("zzzObject")]
    [SerializeField] private GameObject failureState;

    [Header("피로도")]
    [SerializeField] private Slider fatigueGauge;
    [SerializeField, Min(1f)] private float maxFatigue = 100f;
    [SerializeField, Min(0f)] private float fatigueIncreasePerSecond = 20f;
    [SerializeField, Min(0f)] private float fatigueDecreasePerSecond = 20f;
    [SerializeField, Min(0f)] private float forcedSleepDuration = 0.8f;

    [Header("간부 확인")]
    [SerializeField] private GameObject commander;
    [SerializeField] private GameObject commanderLookState;
    [Tooltip("0: 오른쪽 시작 위치, 1: 확인 위치")]
    [SerializeField] private Transform[] commanderPositions;
    [SerializeField] private Vector2 nextAppearanceTime = new Vector2(2f, 5f);
    [SerializeField] private Vector2 moveTime = new Vector2(1f, 2f);
    [SerializeField, Range(0f, 1f)] private float failureCheckStartNormalized = 0.5f;
    [Tooltip("적발 이미지를 보여준 뒤 실패 판정을 전달하기까지의 시간")]
    [SerializeField, Min(0f)] private float caughtDelay = 1f;

    private Action<MinigameJudgement> _onCompleted;
    private System.Random _random;
    private bool _isPlaying;
    private bool _isAwake;
    private bool _isCommanderApproaching;
    private float _fatigue;
    private float _forcedSleepRemaining;
    private float _nextAppearanceAt;
    private float _approachStartedAt;
    private float _watchAt;
    private float _caughtAt = -1f;

    public void Begin(MinigameContext context, Action<MinigameJudgement> onCompleted)
    {
        _onCompleted = onCompleted;
        _random = new System.Random(context.RandomSeed);
        _isPlaying = true;
        _fatigue = 0f;
        _forcedSleepRemaining = 0f;
        _caughtAt = -1f;

        if (fatigueGauge == null)
        {
            fatigueGauge = GetComponentInChildren<Slider>(true);
        }

        ResetPresentation();
        GameAudioController.Instance?.PlaySleep();
        ScheduleCommanderAppearance();
    }

    private void Update()
    {
        if (!_isPlaying)
        {
            return;
        }

        if (_caughtAt >= 0f)
        {
            UpdateCaughtState();
            return;
        }

        UpdateDutySoldier();
        UpdateCommander();

        if (_caughtAt >= 0f)
        {
            UpdateCaughtState();
        }
    }

    private void UpdateCaughtState()
    {
        if (Time.time >= _caughtAt)
        {
            Complete(MinigameJudgement.Failure);
        }
    }

    private void UpdateDutySoldier()
    {
        var isForcedToSleep = _forcedSleepRemaining > 0f;

        if (isForcedToSleep)
        {
            _forcedSleepRemaining = Mathf.Max(0f, _forcedSleepRemaining - Time.deltaTime);
        }

        var wantsToStayAwake = MouseInputManager.Instance != null
            && MouseInputManager.Instance.IsClickHeld();
        SetAwake(!isForcedToSleep && wantsToStayAwake);

        _fatigue += _isAwake
            ? fatigueIncreasePerSecond * Time.deltaTime
            : -fatigueDecreasePerSecond * Time.deltaTime;
        _fatigue = Mathf.Clamp(_fatigue, 0f, Mathf.Max(1f, maxFatigue));

        if (_fatigue >= maxFatigue && !isForcedToSleep)
        {
            _forcedSleepRemaining = forcedSleepDuration;
            SetAwake(false);
        }

        UpdateFatigueGauge();

    }

    private void UpdateCommander()
    {
        if (_caughtAt >= 0f)
        {
            return;
        }

        if (!_isCommanderApproaching && Time.time >= _nextAppearanceAt)
        {
            ShowCommanderApproaching();
            return;
        }

        if (_isCommanderApproaching)
        {
            var moveProgress = UpdateCommanderPosition();

            if (moveProgress >= failureCheckStartNormalized
                && moveProgress < 1f
                && !_isAwake)
            {
                CatchSleepingSoldier();
                return;
            }

            if (moveProgress >= 1f)
            {
                HideCommander();
                ScheduleCommanderAppearance();
            }
        }
    }

    private void ShowCommanderApproaching()
    {
        _isCommanderApproaching = true;
        GameAudioController.Instance?.PlayFootstep();
        SetCommanderStartPosition();
        SetActive(commander, true);
        SetActive(commanderLookState, false);

        _approachStartedAt = Time.time;
        _watchAt = _approachStartedAt + RandomRange(moveTime);
    }

    private void HideCommander()
    {
        _isCommanderApproaching = false;
        SetActive(commander, false);
        SetActive(commanderLookState, false);
    }

    private void ScheduleCommanderAppearance()
    {
        HideCommander();
        _nextAppearanceAt = Time.time + RandomRange(nextAppearanceTime);
        _watchAt = float.PositiveInfinity;
    }

    private float UpdateCommanderPosition()
    {
        var moveDuration = Mathf.Max(0.0001f, _watchAt - _approachStartedAt);
        var progress = Mathf.Clamp01((Time.time - _approachStartedAt) / moveDuration);

        if (commanderPositions == null || commanderPositions.Length < 2
            || commanderPositions[0] == null || commanderPositions[1] == null)
        {
            return progress;
        }

        SetCommanderPosition(Vector3.Lerp(
            commanderPositions[0].position,
            commanderPositions[1].position,
            progress));

        return progress;
    }

    private void SetCommanderStartPosition()
    {
        if (commanderPositions == null || commanderPositions.Length < 2)
        {
            return;
        }

        var position = commanderPositions[0];
        if (position != null)
        {
            SetCommanderPosition(position.position);
        }
    }

    private void SetCommanderPosition(Vector3 position)
    {
        if (commander != null)
        {
            commander.transform.position = position;
        }

        if (commanderLookState != null)
        {
            commanderLookState.transform.position = position;
        }
    }

    private void CatchSleepingSoldier()
    {
        if (_caughtAt >= 0f)
        {
            return;
        }

        _caughtAt = Time.time + caughtDelay;
        GameAudioController.Instance?.PlaySurprise();
        SetActive(sleepState, false);
        SetActive(awakeState, false);
        SetActive(failureState, true);
        SetActive(commander, false);
        SetActive(commanderLookState, true);
    }

    private void SetAwake(bool isAwake)
    {
        _isAwake = isAwake;
        SetActive(sleepState, !_isAwake);
        SetActive(awakeState, _isAwake);
        SetActive(failureState, false);
    }

    private void UpdateFatigueGauge()
    {
        if (fatigueGauge == null)
        {
            return;
        }

        fatigueGauge.minValue = 0f;
        fatigueGauge.maxValue = Mathf.Max(1f, maxFatigue);
        fatigueGauge.value = _fatigue;
    }

    private void ResetPresentation()
    {
        SetActive(failureState, false);
        SetAwake(false);
        HideCommander();
        UpdateFatigueGauge();
    }

    private float RandomRange(Vector2 range)
    {
        var min = Mathf.Min(range.x, range.y);
        var max = Mathf.Max(range.x, range.y);
        return min + (float)_random.NextDouble() * (max - min);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void Complete(MinigameJudgement judgement)
    {
        if (!_isPlaying)
        {
            return;
        }

        _isPlaying = false;
        var callback = _onCompleted;
        _onCompleted = null;
        callback?.Invoke(judgement);
    }

    public void Abort()
    {
        _isPlaying = false;
        _onCompleted = null;
        _caughtAt = -1f;
        _forcedSleepRemaining = 0f;
        _fatigue = 0f;
        ResetPresentation();
    }

    private void OnDisable()
    {
        Abort();
    }
}
