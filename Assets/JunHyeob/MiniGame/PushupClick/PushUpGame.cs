using UnityEngine;
using UnityEngine.UI;

public class PushUpGame : MonoBehaviour
{
    [Header("Push Up Image")]
    [SerializeField] private GameObject pushUpDown;
    [SerializeField] private GameObject pushUpUp;

    [Header("Click Area")]
    [SerializeField] private RectTransform pushUpClickArea;

    [Header("Count")]
    [SerializeField] private Text countText;
    [SerializeField] private Slider countSlider;

    [SerializeField] private int targetCount = 30;

    [Header("Time")]
    [SerializeField] private Slider timeSlider;
    [SerializeField] private Text timerText;

    [SerializeField] private float timeLimit = 10f;

    private int currentCount;
    private float remainingTime;

    private bool isPlaying;
    private bool isPressing;
    private bool isFinished;

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        if (!isPlaying || isFinished)
            return;

        UpdateTimer();
        UpdateInput();
    }

    // =========================================================
    // 게임 시작
    // =========================================================

    public void StartGame()
    {
        currentCount = 0;
        remainingTime = timeLimit;

        isPlaying = true;
        isPressing = false;
        isFinished = false;

        // 처음에는 DOWN 자세
        pushUpDown.SetActive(true);
        pushUpUp.SetActive(false);

        // 시간 슬라이더
        timeSlider.minValue = 0f;
        timeSlider.maxValue = 1f;
        timeSlider.value = 1f;

        // 횟수 슬라이더
        if (countSlider != null)
        {
            countSlider.minValue = 0;
            countSlider.maxValue = targetCount;
            countSlider.value = 0;
        }

        UpdateCountUI();
        //UpdateTimerUI();

        Debug.Log("[PushUpGame] 게임 시작");
    }

    // =========================================================
    // 마우스 입력
    // =========================================================

    private void UpdateInput()
    {
        // 마우스를 누른 순간
        if (MouseInputManager.Instance.IsClickDown())
        {
            TryPressPushUp();
        }

        // 마우스를 뗀 순간
        if (MouseInputManager.Instance.IsClickUp())
        {
            ReleasePushUp();
        }
    }

    // =========================================================
    // 팔굽혀펴기 누르기
    // =========================================================

    private void TryPressPushUp()
    {
        if (isPressing)
            return;

        // 마우스가 PushUp_Down 영역 위에 있는지 확인
        Vector2 mousePosition = MouseInputManager.Instance.MouseScreenPosition;

        bool isInside = RectTransformUtility.RectangleContainsScreenPoint(
            pushUpClickArea,
            mousePosition,
            null
        );

        if (!isInside)
            return;

        isPressing = true;

        // DOWN → UP
        pushUpDown.SetActive(false);
        pushUpUp.SetActive(true);

        Debug.Log("[PushUpGame] UP");
    }

    // =========================================================
    // 팔굽혀펴기 떼기
    // =========================================================

    private void ReleasePushUp()
    {
        if (!isPressing)
            return;

        isPressing = false;

        // UP → DOWN
        pushUpUp.SetActive(false);
        pushUpDown.SetActive(true);

        // 팔굽혀펴기 1회
        currentCount++;

        UpdateCountUI();

        Debug.Log($"[PushUpGame] 팔굽혀펴기 {currentCount}회");

        // 목표 달성
        if (currentCount >= targetCount)
        {
            SuccessGame();
        }
    }

    // =========================================================
    // 시간 처리
    // =========================================================

    private void UpdateTimer()
    {
        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;

            //UpdateTimerUI();

            FailGame();
            return;
        }

       // UpdateTimerUI();

        // 1 → 0으로 감소
        timeSlider.value = remainingTime / timeLimit;
    }

    // =========================================================
    // UI
    // =========================================================

    private void UpdateCountUI()
    {
        if (countText != null)
        {
            countText.text = $"{currentCount} / {targetCount}";
        }

        if (countSlider != null)
        {
            countSlider.value = currentCount;
        }
    }

    //private void UpdateTimerUI()
    //{
    //    if (timerText != null)
    //    {
    //        timerText.text = remainingTime.ToString("F1");
    //    }
    //}

    // =========================================================
    // 성공
    // =========================================================

    private void SuccessGame()
    {
        if (isFinished)
            return;

        isFinished = true;
        isPlaying = false;

        // UP 상태였다면 DOWN으로 복귀
        pushUpUp.SetActive(false);
        pushUpDown.SetActive(true);

        Debug.Log("================================");
        Debug.Log("[PushUpGame] 성공!");
        Debug.Log($"목표 횟수 : {targetCount}");
        Debug.Log($"현재 횟수 : {currentCount}");
        Debug.Log("================================");

        NotifyRoutineComplete(true);
    }

    // =========================================================
    // 실패
    // =========================================================

    private void FailGame()
    {
        if (isFinished)
            return;

        isFinished = true;
        isPlaying = false;

        pushUpUp.SetActive(false);
        pushUpDown.SetActive(true);

        Debug.Log("================================");
        Debug.Log("[PushUpGame] 실패 - 시간 초과!");
        Debug.Log($"목표 횟수 : {targetCount}");
        Debug.Log($"현재 횟수 : {currentCount}");
        Debug.Log("================================");

        NotifyRoutineComplete(false);
    }

    // =========================================================
    // RoutineRunner 연결
    // =========================================================

    private void NotifyRoutineComplete(bool success)
    {
        if (success)
        {
            Debug.Log("[PushUpGame] Routine 성공");
        }
        else
        {
            Debug.Log("[PushUpGame] Routine 실패");
        }

        // TODO
        // 여기에서 나중에 RoutineRunner에 결과 전달
    }
}