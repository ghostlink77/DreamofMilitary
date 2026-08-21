using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WalkMiniGameManager : MonoBehaviour
{
    public static WalkMiniGameManager Instance { get; private set; }

    public enum GameState
    {
        Countdown,
        Playing,
        Success,
        Failed
    }

    [Header("게임 설정")]
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private float playTime = 30f;

    [Header("판정 설정")]
    [SerializeField] private float acceptWindow = 0.15f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI toDoText;
    [SerializeField] private Slider timeSlider;

    [Header("전우")]
    [SerializeField] private WalkFrontController frontController;

    private GameState state;

    private float countdownTimer;
    private float playTimer;

    // 가장 최근 전우의 왼발 시작 시간
    private float lastFrontLeftTime = -1f;

    // 현재 전우 왼발에 대한 판정을 이미 했는가?
    private bool currentStepJudged;

    public GameState State => state;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        switch (state)
        {
            case GameState.Countdown:
                UpdateCountdown();
                break;

            case GameState.Playing:
                UpdatePlaying();
                break;
        }
    }

    // =========================================================
    // 게임 시작
    // =========================================================

    private void StartGame()
    {
        state = GameState.Countdown;

        countdownTimer = countdownDuration;
        playTimer = playTime;

        lastFrontLeftTime = -1f;
        currentStepJudged = false;

        if (timeSlider != null)
        {
            timeSlider.minValue = 0f;
            timeSlider.maxValue = playTime;
            timeSlider.value = playTime;
        }

        UpdateCountdownText();
    }

    // =========================================================
    // 카운트다운
    // =========================================================

    private void UpdateCountdown()
    {
        countdownTimer -= Time.deltaTime;

        UpdateCountdownText();

        if (countdownTimer <= 0f)
        {
            StartPlaying();
        }
    }

    private void UpdateCountdownText()
    {
        if (toDoText == null)
            return;

        int count = Mathf.CeilToInt(countdownTimer);

        if (count > 0)
        {
            toDoText.text = count.ToString();
        }
        else
        {
            toDoText.text = "왼발을 맞춰라!";
        }
    }

    // =========================================================
    // 실제 게임 시작
    // =========================================================

    private void StartPlaying()
    {
        state = GameState.Playing;

        playTimer = playTime;

        if (toDoText != null)
        {
            toDoText.text = "왼발을 맞춰라!";
        }

        // 전우 걷기 시작
        if (frontController != null)
        {
            frontController.StartWalking();
        }
    }

    // =========================================================
    // 플레이 중
    // =========================================================

    private void UpdatePlaying()
    {
        playTimer -= Time.deltaTime;

        if (timeSlider != null)
        {
            timeSlider.value = playTimer;
        }

        // 제한 시간 버티면 성공
        if (playTimer <= 0f)
        {
            Success();
        }

        // 전우 왼발을 놓쳤는지 검사
        CheckMissedStep();
    }

    // =========================================================
    // 전우 왼발 시작
    // WalkFrontController에서 호출
    // =========================================================

    public void OnFrontLeftStep()
    {
        if (state != GameState.Playing)
            return;

        lastFrontLeftTime = Time.time;

        currentStepJudged = false;

        Debug.Log("전우 왼발 시작");
    }

    // =========================================================
    // 플레이어 왼발
    // MouseInputManager에서 클릭 감지 후 호출
    // =========================================================

    public void OnPlayerLeftStep()
    {
        if (state != GameState.Playing)
            return;

        // 아직 전우의 왼발이 시작되지 않았다면 실패
        if (lastFrontLeftTime < 0f)
        {
            Failed();
            return;
        }

        // 이미 이번 왼발을 판정했다면 무시
        if (currentStepJudged)
            return;

        float playerTime = Time.time;

        float difference = Mathf.Abs(
            playerTime - lastFrontLeftTime
        );

        Debug.Log(
            $"전우 왼발과 플레이어 왼발 차이 : {difference:F3}초"
        );

        if (difference <= acceptWindow)
        {
            // 성공
            currentStepJudged = true;

            Debug.Log("왼발 판정 성공!");
        }
        else
        {
            // 허용 오차를 벗어나면 실패
            Failed();
        }
    }

    // =========================================================
    // 플레이어 오른발
    // =========================================================

    public void OnPlayerRightStep()
    {
        if (state != GameState.Playing)
            return;

        Debug.Log("플레이어 오른발");
    }

    // =========================================================
    // 전우 왼발을 놓쳤는지 검사
    // =========================================================

    private void CheckMissedStep()
    {
        if (lastFrontLeftTime < 0f)
            return;

        if (currentStepJudged)
            return;

        float elapsed = Time.time - lastFrontLeftTime;

        // 허용 시간이 지나도록 왼발을 누르지 않았다면 실패
        if (elapsed > acceptWindow)
        {
            Failed();
        }
    }

    // =========================================================
    // 성공
    // =========================================================

    private void Success()
    {
        if (state != GameState.Playing)
            return;

        state = GameState.Success;

        if (frontController != null)
        {
            frontController.StopWalking();
        }

        if (toDoText != null)
        {
            toDoText.text = "성공!";
        }

        Debug.Log("이동 제식 성공!");
    }

    // =========================================================
    // 실패
    // =========================================================

    private void Failed()
    {
        if (state != GameState.Playing)
            return;

        state = GameState.Failed;

        if (frontController != null)
        {
            frontController.StopWalking();
        }

        if (toDoText != null)
        {
            toDoText.text = "실패!";
        }

        Debug.Log("이동 제식 실패!");
    }
}
