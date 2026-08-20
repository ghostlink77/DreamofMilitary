using UnityEngine;

public class WalkFrontController : MonoBehaviour
{
    [Header("전우 발")]
    [SerializeField] private GameObject frontLeft;
    [SerializeField] private GameObject frontRight;

    [Header("게임 매니저")]
    [SerializeField] private WalkMiniGameManager manager;

    [Header("걸음 간격")]
    [SerializeField] private float stepInterval = 0.5f;

    private bool isWalking;
    private bool isLeftStep;

    private float stepTimer;

    private void Start()
    {
        // 게임 시작 전에는 발을 숨김
        HideFeet();
    }

    private void Update()
    {
        if (!isWalking)
            return;

        stepTimer += Time.deltaTime;

        if (stepTimer >= stepInterval)
        {
            stepTimer -= stepInterval;

            ChangeStep();
        }
    }

    // =========================================================
    // 걷기 시작
    // =========================================================

    public void StartWalking()
    {
        isWalking = true;

        stepTimer = 0f;

        // 첫 발은 왼발
        isLeftStep = true;

        ShowLeftFoot();
    }

    // =========================================================
    // 걷기 종료
    // =========================================================

    public void StopWalking()
    {
        isWalking = false;

        HideFeet();
    }

    // =========================================================
    // 발 변경
    // =========================================================

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

    // =========================================================
    // 왼발
    // =========================================================

    private void ShowLeftFoot()
    {
        if (frontLeft != null)
            frontLeft.SetActive(true);

        if (frontRight != null)
            frontRight.SetActive(false);

        // 전우의 왼발 시작 시간을 기록
        if (manager != null)
        {
            manager.OnFrontLeftStep();
        }
    }

    // =========================================================
    // 오른발
    // =========================================================

    private void ShowRightFoot()
    {
        if (frontLeft != null)
            frontLeft.SetActive(false);

        if (frontRight != null)
            frontRight.SetActive(true);
    }

    // =========================================================
    // 발 숨기기
    // =========================================================

    private void HideFeet()
    {
        if (frontLeft != null)
            frontLeft.SetActive(false);

        if (frontRight != null)
            frontRight.SetActive(false);
    }
}