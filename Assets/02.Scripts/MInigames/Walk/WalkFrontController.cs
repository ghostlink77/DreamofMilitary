using DreamOfMilitary.Audio;
using UnityEngine;

/// <summary>
/// 전우의 스프라이트를 카운트다운 보행, 정지 자세, 실플레이 보행으로 제어한다.
/// 왼발이 시작되는 프레임에 WalkMiniGameManager로 타이밍을 전달한다.
/// </summary>
public sealed class WalkFrontController : MonoBehaviour
{
    [Header("전우 스프라이트")]
    [SerializeField] private GameObject frontLeft;
    [SerializeField] private GameObject frontRight;
    [SerializeField] private GameObject frontStanding;

    [Header("게임 매니저")]
    [SerializeField] private WalkMiniGameManager manager;

    [Header("걸음 간격")]
    [SerializeField, Min(0.01f)] public float stepInterval = 0.5f;

    private bool isWalking;
    private bool isLeftStep;
    private float stepTimer;

    private void Awake()
    {
        HideAllPoses();
    }

    private void Update()
    {
        if (!isWalking)
        {
            return;
        }

        stepTimer += Time.deltaTime;

        // 프레임이 잠깐 느려져도 보행 템포가 누적해서 밀리지 않게 한다.
        while (stepTimer >= stepInterval)
        {
            stepTimer -= stepInterval;
            ChangeStep();
        }
    }

    /// <summary>카운트다운 동안 플레이어에게 박자를 보여 주는 보행.</summary>
    public void StartPracticeWalking()
    {
        StartWalkingFromLeftFoot();
    }

    /// <summary>정지 후, 첫 왼발부터 시작하는 실플레이 보행.</summary>
    public void StartGameWalking()
    {
        StartWalkingFromLeftFoot();
    }

    private void StartWalkingFromLeftFoot()
    {
        isWalking = true;
        stepTimer = 0f;
        isLeftStep = true;
        ShowLeftFoot();
    }

    /// <summary>미니게임 종료 시 전우 스프라이트를 모두 숨긴다.</summary>
    public void StopWalking()
    {
        isWalking = false;
        stepTimer = 0f;
        HideAllPoses();
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
        SetActive(frontLeft, true);
        SetActive(frontRight, false);
        GameAudioController.Instance?.PlayfrontLeft();
      
        // 카운트다운 중의 호출은 WalkMiniGameManager가 판정하지 않는다.
        if (manager != null)
        {
            manager.OnFrontLeftStep();
        }
    }

    private void ShowRightFoot()
    {
        SetActive(frontLeft, false);
        SetActive(frontRight, true);
        GameAudioController.Instance?.PlayfrontRight();
    }

    private void HideAllPoses()
    {
        SetActive(frontLeft, false);
        SetActive(frontRight, false);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
