using System;
using DreamOfMilitary.Routine;
using DreamOfMilitary.Audio;
using TMPro;
using UnityEngine;

/// <summary>
/// ������ ���� ������ ���� ��, ������ �޹� Ÿ�ֿ̹� ����
/// ���콺�� ���� �޹��� ���ߴ� �̵� ���� �̴ϰ����̴�.
///
/// ���콺 ���� = �޹�
/// ���콺 ��   = ������
///
/// ������ �޹ߺ��� ���� �����ų� �ʰ� �Է��ص�
/// acceptWindow ���̶�� �������� �����Ѵ�.
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

    [Header("Ÿ�̹� ���")]
    [SerializeField, Min(0f)] private float countdownDuration = 3f;
    [SerializeField, Min(0f)] private float prePlayPauseDuration = 0.5f;

    [Tooltip("������ �޹� ���� ��/�ڷ� ����� �ð�")]
    [SerializeField, Min(0.01f)] private float acceptWindow = 0.15f;

    [Header("����")]
    [SerializeField] private WalkFrontController frontController;

    [Header("ī��Ʈ�ٿ� ǥ��")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("���� ���� ǥ��")]
    [SerializeField] private GameObject judgementObject;

    [SerializeField, Min(0.1f)] private float judgementDisplayDuration = 0.5f;

    [Header("�÷��̾� �� ǥ��")]
    [SerializeField] private GameObject playerLeftFoot;
    [SerializeField] private GameObject playerRightFoot;

    private Action<MinigameJudgement> onCompleted;

    private Phase phase;

    private float countdownRemaining;
    private float prePlayPauseRemaining;

    // ���� �ֱ� ������ �޹� ���� �ð�
    private float lastFrontLeftTime;

    // ������ �޹��� �� ���̶� ���� ���� �ִ���
    private bool hasFrontLeftStep;

    // ���� ���� �޹��� �̹� ���� ó���ߴ���
    private bool currentStepJudged;

    // ���� �޹��� �̸� �Է����� �� ����
    private float pendingEarlyPlayerStepTime;
    private bool hasPendingEarlyPlayerStep;

    private bool didFail;

    // Good! ǥ�� �ð�
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
         * ���̵� ����
         */
        (acceptWindow, frontController.stepInterval) =
            context.DifficultyTier switch
            {
                1 => (0.3f, 0.7f),
                2 => (0.25f, 0.5f),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(context.DifficultyTier),
                    "���̵��� 1, 2 �� �ϳ����� �մϴ�."
                )
            };

        SetPlayerFoot(false);

        HideJudgement();

        UpdateCountdownVisual();

        /*
         * ī��Ʈ�ٿ� ���� ���찡 �����鼭
         * �÷��̾�� ���� ������ �����ش�.
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
         * ī��Ʈ�ٿ� ����
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
         * ���� �÷��� ����
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
    // �÷��̾� �Է�
    // =========================================================

    private void UpdatePlayerFootVisual()
    {
        if (MouseInputManager.Instance == null)
        {
            return;
        }

        /*
         * ���콺�� ������ �޹�
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
         * ���콺�� ���� ������
         */
        if (MouseInputManager.Instance.IsClickUp())
        {
            SetPlayerFoot(false);
        }
    }


    // =========================================================
    // ���� �޹�
    // =========================================================

    /// <summary>
    /// WalkFrontController���� ������ �޹��� ���۵� �� ȣ��ȴ�.
    /// </summary>
    public void OnFrontLeftStep()
    {
        if (didFail || phase != Phase.Playing)
        {
            return;
        }

        /*
         * ���ο� ���� �޹� ����
         */
        hasFrontLeftStep = true;

        lastFrontLeftTime = Time.time;

        currentStepJudged = false;


        /*
         * �÷��̾ ���� �޹��� �̸� �����ٸ�
         * ���� ���� ���� �޹߰� ���Ѵ�.
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
             * �̸� ���� �ð��� ��� ���� ���̸� ����
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
    // �÷��̾� �޹�
    // =========================================================

    private void OnPlayerLeftStep()
    {
        /*
         * ���� ������ ù �޹��� ������ �ʾҴٸ�
         * �ʹ� ������ ���� ��.
         *
         * ù �޹� �������� �̸� �Է��� ������� �ʴ´�.
         */
        if (!hasFrontLeftStep)
        {
            Complete(MinigameJudgement.Failure);
            return;
        }


        /*
         * ���� ���� �޹��� ���� ������Ű�� �ʾҴٸ�
         * ���� �޹߰� ���Ѵ�.
         */
        if (!currentStepJudged)
        {
            float timingDifference =
                Mathf.Abs(
                    Time.time -
                    lastFrontLeftTime
                );

            /*
             * ���� �޹ߺ��� �ʹ� �ʰ� �����ٸ� ����
             */
            if (timingDifference > acceptWindow)
            {
                Complete(MinigameJudgement.Failure);
                return;
            }

            /*
             * ���� �޹� Ÿ�ֿ̹� ����
             */
            currentStepJudged = true;

            ShowGood();

            return;
        }


        /*
         * ���� �޹��� �̹� �����ߴ�.
         *
         * �̹� �Է��� ���� �޹��� ����
         * �̸� �Է����� ����Ѵ�.
         */

        if (frontController == null)
        {
            Complete(MinigameJudgement.Failure);
            return;
        }


        /*
         * ���� �޹���
         *
         * �޹� �� ������ �� �޹�
         *
         * �̹Ƿ� stepInterval * 2 ��ŭ �ڿ� �ִ�.
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
         * ���� �޹� Ÿ�ְ̹� �ʹ� ���̰� ũ��
         * �߸��� �Է�
         */
        if (differenceToNext > acceptWindow)
        {
            Complete(MinigameJudgement.Failure);
            return;
        }


        /*
         * ���� �޹��� �̸� �Է��� ������ ����
         */
        pendingEarlyPlayerStepTime = Time.time;

        hasPendingEarlyPlayerStep = true;
    }


    // =========================================================
    // ��ģ �޹� �˻�
    // =========================================================

    private void CheckMissedStep()
    {
        if (!hasFrontLeftStep)
        {
            return;
        }

        /*
         * �̹� �����ߴٸ� �˻��� �ʿ� ����
         */
        if (currentStepJudged)
        {
            return;
        }

        /*
         * ���� �޹� ���� acceptWindow �ȿ�
         * �Է����� ������ ����
         */
        if (Time.time - lastFrontLeftTime > acceptWindow)
        {
            Complete(MinigameJudgement.Failure);
        }
    }


    // =========================================================
    // Good! ǥ��
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
    // �÷��̾� �� ǥ��
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

        if (leftFootDown)
            GameAudioController.Instance?.PlayplayerLeft();
        else
            GameAudioController.Instance?.PlayplayerRight();
    }


    // =========================================================
    // ���ѽð� ����
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