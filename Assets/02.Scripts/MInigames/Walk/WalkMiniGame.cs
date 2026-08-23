using System;
using DreamOfMilitary.Routine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카운트다운으로 전우의 보행 템포를 익힌 뒤, 전우의 왼발 타이밍에 맞춰
/// 마우스를 눌러(왼발) 제한시간까지 버티는 이동 제식 미니게임이다.
/// </summary>
public sealed class WalkMiniGame : MonoBehaviour, IMinigame, ITimeLimitResolver
{
    private enum Phase
    {
        Idle,
        Countdown,
        Playing,
        Finished
    }

    [Header("타이밍 잡기")]
    [SerializeField, Min(0f)] private float countdownDuration = 3f;
    [SerializeField, Min(0.01f)] private float acceptWindow = 0.15f;

    [Header("전우")]
    [SerializeField] private WalkFrontController frontController;

    [Header("카운트다운 표시 (전용 UI)")]
    [SerializeField] private GameObject countdown3;
    [SerializeField] private GameObject countdown2;
    [SerializeField] private GameObject countdown1;
   // [SerializeField] private TextMeshProUGUI countdown;

    [Header("플레이어 발 표시 (선택)")]
    [SerializeField] private GameObject playerLeftFoot;
    [SerializeField] private GameObject playerRightFoot;

    private Action<MinigameJudgement> onCompleted;
    private Phase phase;
    private float countdownRemaining;
    private float lastFrontLeftTime;
    private bool hasFrontLeftStep;
    private bool currentStepJudged;

    public void Begin(MinigameContext context, Action<MinigameJudgement> completed)
    {
        if (completed == null)
        {
            throw new ArgumentNullException(nameof(completed));
        }

        onCompleted = completed;
        phase = Phase.Countdown;
        countdownRemaining = countdownDuration;
        hasFrontLeftStep = false;
        currentStepJudged = false;
        lastFrontLeftTime = -1f;
        SetPlayerFoot(false);
        UpdateCountdownVisual();

        // 카운트다운부터 전우가 같은 템포로 걷기 시작한다.
        if (frontController != null)
        {
            frontController.StartWalking();
        }
    }

    private void Update()
    {
        if (phase == Phase.Idle || phase == Phase.Finished)
        {
            return;
        }

        UpdatePlayerFootVisual();

        if (phase == Phase.Countdown)
        {
            UpdateCountdown();
            return;
        }

        CheckMissedStep();
    }

    private void UpdateCountdown()
    {
        countdownRemaining -= Time.deltaTime;
        UpdateCountdownVisual();

        if (countdownRemaining > 0f)
        {
            return;
        }

        phase = Phase.Playing;
        hasFrontLeftStep = false;
        currentStepJudged = false;
        lastFrontLeftTime = -1f;

        HideCountdownVisuals();
    }

    private void UpdateCountdownVisual()
    {
        var count = Mathf.CeilToInt(countdownRemaining);

        if (countdown3 != null)
        {
            countdown3.SetActive(count == 3);
        }

        if (countdown2 != null)
        {
            countdown2.SetActive(count == 2);
        }

        if (countdown1 != null)
        {
            countdown1.SetActive(count == 1);
        }


        //if (countdown != null)
        //{
        //    //countdown.text = $"{count}}";
        //    countdown.SetActive(true);
        //}
    }

    private void HideCountdownVisuals()
    {
        if (countdown3 != null)
        {
            countdown3.SetActive(false);
        }

        if (countdown2 != null)
        {
            countdown2.SetActive(false);
        }

        if (countdown1 != null)
        {
            countdown1.SetActive(false);
        }

        //if (countdown != null)
        //{
        //    countdown.SetActive(false);
        //}
    }

    private void UpdatePlayerFootVisual()
    {
        if (MouseInputManager.Instance == null)
        {
            return;
        }

        // 누르는 순간만 왼발 타이밍 판정 대상이다.
        if (MouseInputManager.Instance.IsClickDown())
        {
            SetPlayerFoot(true);

            if (phase == Phase.Playing)
            {
                OnPlayerLeftStep();
            }
        }

        // 버튼을 떼면 오른발로 전환한다. 오른발은 현재 판정 대상이 아니다.
        if (MouseInputManager.Instance.IsClickUp())
        {
            SetPlayerFoot(false);
        }
    }

    /// <summary>
    /// WalkFrontController의 전우 왼발 애니메이션 시작 이벤트에서 호출한다.
    /// 카운트다운 중의 전우 발걸음은 판정하지 않는다.
    /// </summary>
    public void OnFrontLeftStep()
    {
        if (phase != Phase.Playing)
        {
            return;
        }

        hasFrontLeftStep = true;
        lastFrontLeftTime = Time.time;
        currentStepJudged = false;
    }

    private void OnPlayerLeftStep()
    {

        if (!hasFrontLeftStep)
        {
            Complete(MinigameJudgement.Failure);
            return;
        }

        // 같은 전우 왼발에 대해 한 번 정확히 눌렀다면, 다음 왼발까지 추가 입력은 무시한다.
        if (currentStepJudged)
        {
            return;
        }

        if (Mathf.Abs(Time.time - lastFrontLeftTime) > acceptWindow)
        {
            Complete(MinigameJudgement.Failure);
            return;
        }

        currentStepJudged = true;
    }

    private void CheckMissedStep()
    {
        if (!hasFrontLeftStep || currentStepJudged)
        {
            return;
        }

        // 클릭을 계속 누르고 있으면 다음 왼발에 IsClickDown이 발생하지 않으므로 여기서 실패한다.
        if (Time.time - lastFrontLeftTime > acceptWindow)
        {
            Debug.Log("실패");
            Complete(MinigameJudgement.Failure);
        }
    }

    private void SetPlayerFoot(bool leftFootDown)
    {
        if (playerLeftFoot != null)
        {
            playerLeftFoot.SetActive(leftFootDown);
        }

        if (playerRightFoot != null)
        {
            playerRightFoot.SetActive(!leftFootDown);
        }
    }

    /// <summary>
    /// MinigameDef의 Time Limit Rule을 SurviveUntilLimit으로 설정했을 때,
    /// RoutineRunner가 제한시간에 호출하여 성공 여부를 결정한다.
    /// </summary>
    public MinigameJudgement ResolveAtTimeLimit()
    {
        if (phase != Phase.Playing)
        {
            return MinigameJudgement.Failure;
        }
    
        phase = Phase.Finished;
        HideCountdownVisuals();
        StopWalking();
        onCompleted = null;
        return MinigameJudgement.Success;
    }

    public void Abort()
    {
        if (phase == Phase.Finished || phase == Phase.Idle)
        {
            return;
        }

        phase = Phase.Finished;
        onCompleted = null;
        HideCountdownVisuals();
        StopWalking();
    }

    private void Complete(MinigameJudgement judgement)
    {
        if (phase != Phase.Playing)
        {
            return;
        }

        phase = Phase.Finished;
        HideCountdownVisuals();
        StopWalking();

        var completed = onCompleted;
        onCompleted = null;
        completed?.Invoke(judgement);
    }

    private void StopWalking()
    {
        if (frontController != null)
        {
            frontController.StopWalking();
        }
    }

    private void OnDisable()
    {
        Abort();
    }
}
