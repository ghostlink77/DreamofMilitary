using UnityEngine;

/// <summary>
/// 전우의 좌우 발을 일정한 템포로 교대 표시한다.
/// 왼발이 시작되는 프레임에 WalkMiniGameManager로 타이밍을 전달한다.
/// </summary>
public sealed class WalkFrontController : MonoBehaviour
{
    [Header("전우 발")]
    [SerializeField] private GameObject frontLeft;
    [SerializeField] private GameObject frontRight;
    [SerializeField] private GameObject front;

    [Header("게임 매니저")]
    [SerializeField] private WalkMiniGameManager manager;

    [Header("걸음 간격")]
    [SerializeField, Min(0.01f)] private float stepInterval = 0.5f;

    private bool isWalking;
    private bool isLeftStep;
    private float stepTimer;

    private void Awake()
    {
        front.SetActive(true);
    }

    private void Update()
    {
        if (!isWalking)
        {
            return;
        }

        stepTimer += Time.deltaTime;

        // 프레임 지연이 있어도 보행 템포가 누적해서 느려지지 않게 처리한다.
        while (stepTimer >= stepInterval)
        {
            stepTimer -= stepInterval;
            ChangeStep();
        }
    }

    public void StartWalking()
    {
        isWalking = true;
        stepTimer = 0f;

        // 카운트다운 시작부터 왼발로 출발한다.
        isLeftStep = true;
        ShowLeftFoot();
    }

    public void StopWalking()
    {
        isWalking = false;
        stepTimer = 0f;
        HideFeet();
    }

    private void ChangeStep()
    {
        isLeftStep = !isLeftStep;

        if (isLeftStep)
        {
            ShowLeftFoot();
        }
        else
        {
            ShowRightFoot();
        }
    }

    private void ShowLeftFoot()
    {
        if (frontLeft != null)
        {
            frontLeft.SetActive(true);
        }

        if (frontRight != null)
        {
            frontRight.SetActive(false);
        }

        // 카운트다운 중 호출은 WalkMiniGameManager가 무시한다.
        if (manager != null)
        {
            manager.OnFrontLeftStep();
        }
    }

    private void ShowRightFoot()
    {
        if (frontLeft != null)
        {
            frontLeft.SetActive(false);
        }

        if (frontRight != null)
        {
            frontRight.SetActive(true);
        }
    }

    private void HideFeet()
    {
        if (frontLeft != null)
        {
            frontLeft.SetActive(false);
        }

        if (frontRight != null)
        {
            frontRight.SetActive(false);
        }
    }
}
