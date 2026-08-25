using System;
using DreamOfMilitary.Routine;
using TMPro;
using UnityEngine;

/// <summary>
/// 전우의 보행 템포를 익힌 뒤, 전우의 왼발 타이밍에 맞춰
/// 마우스를 눌러 왼발을 맞추는 이동 제식 미니게임이다.
///
/// 마우스 누름 = 왼발
/// 마우스 뗌   = 오른발
///
/// 전우의 왼발보다 조금 빠르거나 늦게 입력해도
/// acceptWindow 안이라면 성공으로 판정한다.
/// </summary>
public sealed class WalkMiniGameManager : MonoBehaviour, IMinigame, ITimeLimitResolver
{
    private enum Phase
    {
        Idle,
        Countdown,
        ReadyPause,
        Playing,
        Finished
    }

    [Header("타이밍 잡기")]
    [SerializeField, Min(0f)] private float countdownDuration = 3f;
    [SerializeField, Min(0f)] private float prePlayPauseDuration = 0.5f;

    [Tooltip("전우의 왼발 기준 앞/뒤로 허용할 시간")]
    [SerializeField, Min(0.01f)] private float acceptWindow = 0.15f;

    [Header("전우")]
    [SerializeField] private WalkFrontController frontController;

    [Header("카운트다운 표시")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("성공 판정 표시")]
    [SerializeField] private GameObject judgementObject;

    [SerializeField, Min(0.1f)] private float judgementDisplayDuration = 0.5f;

    [Header("플레이어 발 표시")]
    [SerializeField] private GameObject playerLeftFoot;
    [SerializeField] private GameObject playerRightFoot;

    private Action<MinigameJudgement> onCompleted;

    private Phase phase;

    private float countdownRemaining;
    private float prePlayPauseRemaining;

    // 가장 최근 전우의 왼발 시작 시간
    private float lastFrontLeftTime;

    // 전우의 왼발이 한 번이라도 나온 적이 있는지
    private bool hasFrontLeftStep;

    // 현재 전우 왼발을 이미 성공 처리했는지
    private bool currentStepJudged;

    // 다음 왼발을 미리 입력했을 때 저장
    private float pendingEarlyPlayerStepTime;
    private bool hasPendingEarlyPlayerStep;

    private bool didFail;

    // Good! 표시 시간
    private float judgementObjectRemaining;


    public void Begin(
        MinigameContext context,
        Action<MinigameJudgement> completed)
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

        hasPendingEarlyPlayerStep = false;

        didFail = false;

        lastFrontLeftTime = -1f;

        judgementObjectRemaining = 0f;

        /*
         * 난이도 설정
         */
        (acceptWindow, frontController.stepInterval) =
            context.DifficultyTier switch
            {
                1 => (0.3f, 0.7f),
                2 => (0.18f, 0.5f),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(context.DifficultyTier),
                    "난이도는 1, 2 중 하나여야 합니다."
                )
            };

        SetPlayerFoot(false);

        HideJudgement();

        UpdateCountdownVisual();

        /*
         * 카운트다운 동안 전우가 걸으면서
         * 플레이어에게 보행 템포를 보여준다.
         */
        if (frontController != null)
        {
            frontController.StartPracticeWalking();
        }
    }


    private void Update()
    {
        if (phase == Phase.Idle || phase == Phase.Finished)
        {
            UpdateJudgementText();
            return;
        }

        UpdatePlayerFootVisual();

        UpdateJudgementText();

        if (phase == Phase.Countdown)
        {
            UpdateCountdown();
            return;
        }

        if (phase == Phase.ReadyPause)
        {
            UpdateReadyPause();
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

        /*
         * 카운트다운 종료
         */
        phase = Phase.ReadyPause;

        prePlayPauseRemaining = prePlayPauseDuration;

        HideCountdownVisuals();
    }


    private void UpdateReadyPause()
    {
        prePlayPauseRemaining -= Time.deltaTime;

        if (prePlayPauseRemaining > 0f)
        {
            return;
        }

        /*
         * 실제 플레이 시작
         */
        phase = Phase.Playing;

        hasFrontLeftStep = false;
        currentStepJudged = false;
        hasPendingEarlyPlayerStep = false;

        lastFrontLeftTime = -1f;
    }


    private void UpdateCountdownVisual()
    {
        if (countdownText == null)
        {
            return;
        }

        int count = Mathf.CeilToInt(countdownRemaining);

        countdownText.gameObject.SetActive(count > 0);

        countdownText.text =
            count > 0
                ? count.ToString()
                : string.Empty;
    }


    private void HideCountdownVisuals()
    {
        if (countdownText == null)
        {
            return;
        }

        countdownText.text = string.Empty;
        countdownText.gameObject.SetActive(false);
    }


    // =========================================================
    // 플레이어 입력
    // =========================================================

    private void UpdatePlayerFootVisual()
    {
        if (MouseInputManager.Instance == null)
        {
            return;
        }

        /*
         * 마우스를 누르면 왼발
         */
        if (MouseInputManager.Instance.IsClickDown())
        {
            SetPlayerFoot(true);

            if (phase == Phase.Playing)
            {
                OnPlayerLeftStep();
            }
        }

        /*
         * 마우스를 떼면 오른발
         */
        if (MouseInputManager.Instance.IsClickUp())
        {
            SetPlayerFoot(false);
        }
    }


    // =========================================================
    // 전우 왼발
    // =========================================================

    /// <summary>
    /// WalkFrontController에서 전우의 왼발이 시작될 때 호출된다.
    /// </summary>
    public void OnFrontLeftStep()
    {
        if (didFail || phase != Phase.Playing)
        {
            return;
        }

        /*
         * 새로운 전우 왼발 시작
         */
        hasFrontLeftStep = true;

        lastFrontLeftTime = Time.time;

        currentStepJudged = false;


        /*
         * 플레이어가 다음 왼발을 미리 눌렀다면
         * 지금 들어온 전우 왼발과 비교한다.
         */
        if (hasPendingEarlyPlayerStep)
        {
            float timingDifference =
                Mathf.Abs(
                    lastFrontLeftTime -
                    pendingEarlyPlayerStepTime
                );

            hasPendingEarlyPlayerStep = false;

            /*
             * 미리 누른 시간이 허용 범위 안이면 성공
             */
            if (timingDifference <= acceptWindow)
            {
                currentStepJudged = true;

                ShowGood();
            }
            else
            {
                Complete(MinigameJudgement.Failure);
            }
        }
    }


    // =========================================================
    // 플레이어 왼발
    // =========================================================

    private void OnPlayerLeftStep()
    {
        /*
         * 아직 전우의 첫 왼발이 나오지 않았다면
         * 너무 빠르게 누른 것.
         *
         * 첫 왼발 이전에는 미리 입력을 허용하지 않는다.
         */
        if (!hasFrontLeftStep)
        {
            Complete(MinigameJudgement.Failure);
            return;
        }


        /*
         * 현재 전우 왼발을 아직 성공시키지 않았다면
         * 현재 왼발과 비교한다.
         */
        if (!currentStepJudged)
        {
            float timingDifference =
                Mathf.Abs(
                    Time.time -
                    lastFrontLeftTime
                );

            /*
             * 현재 왼발보다 너무 늦게 눌렀다면 실패
             */
            if (timingDifference > acceptWindow)
            {
                Complete(MinigameJudgement.Failure);
                return;
            }

            /*
             * 현재 왼발 타이밍에 성공
             */
            currentStepJudged = true;

            ShowGood();

            return;
        }


        /*
         * 현재 왼발은 이미 성공했다.
         *
         * 이번 입력은 다음 왼발을 위한
         * 미리 입력으로 취급한다.
         */

        if (frontController == null)
        {
            Complete(MinigameJudgement.Failure);
            return;
        }


        /*
         * 다음 왼발은
         *
         * 왼발 → 오른발 → 왼발
         *
         * 이므로 stepInterval * 2 만큼 뒤에 있다.
         */
        float nextLeftStepTime =
            lastFrontLeftTime +
            frontController.stepInterval * 2f;


        float differenceToNext =
            Mathf.Abs(
                Time.time -
                nextLeftStepTime
            );


        /*
         * 다음 왼발 타이밍과 너무 차이가 크면
         * 잘못된 입력
         */
        if (differenceToNext > acceptWindow)
        {
            Complete(MinigameJudgement.Failure);
            return;
        }


        /*
         * 다음 왼발을 미리 입력한 것으로 저장
         */
        pendingEarlyPlayerStepTime = Time.time;

        hasPendingEarlyPlayerStep = true;
    }


    // =========================================================
    // 놓친 왼발 검사
    // =========================================================

    private void CheckMissedStep()
    {
        if (!hasFrontLeftStep)
        {
            return;
        }

        /*
         * 이미 성공했다면 검사할 필요 없음
         */
        if (currentStepJudged)
        {
            return;
        }

        /*
         * 전우 왼발 이후 acceptWindow 안에
         * 입력하지 않으면 실패
         */
        if (Time.time - lastFrontLeftTime > acceptWindow)
        {
            Complete(MinigameJudgement.Failure);
        }
    }


    // =========================================================
    // Good! 표시
    // =========================================================

    private void ShowGood()
    {
        if (judgementObject == null)
        {
            return;
        }

        //judgementText.text = "Good!";

        judgementObject.SetActive(true);

        judgementObjectRemaining =
            judgementDisplayDuration;
    }


    private void HideJudgement()
    {
        if (judgementObject == null)
        {
            return;
        }

      //  judgementText.text = string.Empty;

        judgementObject.SetActive(false);

        judgementObjectRemaining = 0f;
    }


    private void UpdateJudgementText()
    {
        if (judgementObject == null)
        {
            return;
        }

        if (!judgementObject.activeSelf)
        {
            return;
        }

        judgementObjectRemaining -= Time.deltaTime;

        if (judgementObjectRemaining <= 0f)
        {
            HideJudgement();
        }
    }


    // =========================================================
    // 플레이어 발 표시
    // =========================================================

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


    // =========================================================
    // 제한시간 종료
    // =========================================================

    public MinigameJudgement ResolveAtTimeLimit()
    {
        if (didFail || phase != Phase.Playing)
        {
            return MinigameJudgement.Failure;
        }

        phase = Phase.Finished;

        HideCountdownVisuals();
        HideJudgement();

        StopWalking();

        onCompleted = null;

        return MinigameJudgement.Success;
    }


    public void Abort()
    {
        if (phase == Phase.Finished ||
            phase == Phase.Idle)
        {
            return;
        }

        phase = Phase.Finished;

        onCompleted = null;

        HideCountdownVisuals();
        HideJudgement();

        StopWalking();
    }


    private void Complete(MinigameJudgement judgement)
    {
        if (phase != Phase.Playing)
        {
            return;
        }

        didFail =
            judgement == MinigameJudgement.Failure;

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