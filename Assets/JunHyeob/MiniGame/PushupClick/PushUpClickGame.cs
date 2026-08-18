using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PushUpClickGame : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] private float gameTime = 10f;

    [Header("UI")]
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button pushUpButton;

    private int pushUpCount;
    private float remainingTime;

    private bool isPlaying;
    private bool isFinished;

    private void Awake()
    {
        pushUpButton.onClick.AddListener(OnPushUpClicked);
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            FinishGame();
        }

        UpdateTimerUI();
    }

    /// <summary>
    /// RoutineRunner에서 미니게임을 시작할 때 호출
    /// </summary>
    public void StartGame()
    {
        pushUpCount = 0;
        remainingTime = gameTime;

        isPlaying = true;
        isFinished = false;

        countText.text = "0";
        timerText.text = gameTime.ToString("F1");
        resultText.text = "";

        pushUpButton.interactable = true;

        gameObject.SetActive(true);
    }

    private void OnPushUpClicked()
    {
        if (!isPlaying)
            return;

        pushUpCount++;

        countText.text = pushUpCount.ToString();
    }

    private void UpdateTimerUI()
    {
        timerText.text = remainingTime.ToString("F1");
    }

    private void FinishGame()
    {
        if (isFinished)
            return;

        isFinished = true;
        isPlaying = false;

        pushUpButton.interactable = false;

        resultText.text = GetResultText();

        Debug.Log($"[PushUpGame] 팔굽혀펴기 횟수: {pushUpCount}");

        // 여기에서 RoutineRunner에게 완료를 알려준다.
        NotifyRoutineComplete();
    }

    private string GetResultText()
    {
        if (pushUpCount >= 30)
            return "최우수!\n30회 이상";

        if (pushUpCount >= 20)
            return "우수!\n20회 이상";

        if (pushUpCount >= 10)
            return "보통!\n10회 이상";

        return "부족!\n10회 미만";
    }

    /// <summary>
    /// RoutineRunner와 연결되는 부분
    /// </summary>
    private void NotifyRoutineComplete()
    {
        Debug.Log("[PushUpGame] 미니게임 종료");

        // TODO:
        // 네가 만든 RoutineContracts / RoutineRunner의
        // 실제 완료 메서드에 연결한다.
    }

    public int GetPushUpCount()
    {
        return pushUpCount;
    }

    public bool IsPlaying()
    {
        return isPlaying;
    }

    public bool IsFinished()
    {
        return isFinished;
    }
}

