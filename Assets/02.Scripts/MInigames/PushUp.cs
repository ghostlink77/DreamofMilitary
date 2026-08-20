// ========================
// 팔굽혀펴기
// ========================

using DreamOfMilitary.Routine;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PushUp : MonoBehaviour, IMinigame
{
    [SerializeField] private GameObject upSprite;
    [SerializeField] private GameObject downSprite;
    [SerializeField] private Slider pushUpCountSlider;
    [SerializeField] private TextMeshProUGUI countText;

    private int _pushUpCount;
    private int _successCount;
    private bool _isPlaying;
    private bool _isPushingDown;
    private Coroutine _currentCoroutine;

    // 미니게임 성공을 RoutineRunner에게 알리기 위한 이벤트
    // Begin에서 RoutineRunner에게 참조를 받아온다.
    // 미니게임 성공 시 Success 에서 이벤트 실행
    private Action<MinigameOutcome> _onCompleted;

    // 중지시 호출되는 메서드
    public void Abort()
    {
        _isPlaying = false;
        _isPushingDown = false;
        _onCompleted = null;

        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }

        upSprite.SetActive(true);
        downSprite.SetActive(false);
    }

    public void Begin(MinigameContext context, Action<MinigameOutcome> onCompleted)
    {
        _onCompleted = onCompleted;

        _pushUpCount = 0;
        _successCount = context.DifficultyTier switch
        {
            0 => 20,
            1 => 30,
            2 => 40,
            _ => throw new ArgumentOutOfRangeException(nameof(_successCount))
        };
        pushUpCountSlider.minValue = 0;
        pushUpCountSlider.maxValue = _successCount;

        _isPlaying = true;
        _isPushingDown = false;

        UpdateUI(_isPushingDown);
    }

    private void Update()
    {
        if (MouseInputManager.Instance == null)
        {
            Debug.LogWarning("No MouseInputManager");
            return;
        }
        if (!_isPlaying)
        {
            return;
        }

        if (MouseInputManager.Instance.IsClickDown() && _isPushingDown == false)
        {
            _isPushingDown = true;
            upSprite.SetActive(false);
            downSprite.SetActive(true);
            _currentCoroutine = StartCoroutine(nameof(PushDown));
        }
    }

    private IEnumerator PushDown()
    {
        _isPushingDown = true;
        UpdateUI(_isPushingDown);

        while (_isPlaying && !MouseInputManager.Instance.IsClickUp())
        {
            yield return null;
        }

        if (!_isPlaying)
        {
            yield break;
        }

        _pushUpCount++;
        _isPushingDown = false;
        _currentCoroutine = null;
        UpdateUI(_isPushingDown);

        if (_pushUpCount >= _successCount)
        {
            Success();
        }
    }

    private void Success()
    {
        _isPlaying = false;
        _isPushingDown = false;

        // onCompleted를 비우고 호출하기 위해 복사본을 생성한다.
        var callback = _onCompleted;
        _onCompleted = null;

        callback?.Invoke(new MinigameOutcome(MinigameJudgement.Success));
    }

    private void UpdateUI(bool isPushingDown)
    {
        upSprite.SetActive(!isPushingDown);
        downSprite.SetActive(isPushingDown);

        pushUpCountSlider.value = _pushUpCount;
        countText.text = $"{_pushUpCount}/{_successCount}";
    }

    private void OnDisable()
    {
        Abort();
    }
}
