using UnityEngine;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    [Header("Mini Games")]
    [SerializeField] private MiniGame[] miniGames;

    private MiniGame currentMiniGame;
    private int currentIndex = -1;

    public MiniGame CurrentMiniGame => currentMiniGame;
    public int CurrentIndex => currentIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartFirstMiniGame();
    }

    /// 지정한 미니게임 시작
    public void StartMiniGame(int index)
    {
        if (miniGames == null || miniGames.Length == 0)
        {
            Debug.LogWarning("등록된 미니게임이 없습니다.");
            return;
        }

        if (index < 0 || index >= miniGames.Length)
        {
            Debug.LogError($"잘못된 미니게임 인덱스입니다. Index: {index}");
            return;
        }

        // 기존 미니게임 종료
        if (currentMiniGame != null)
        {
            currentMiniGame.EndGame();
            currentMiniGame.gameObject.SetActive(false);
        }

        currentIndex = index;
        currentMiniGame = miniGames[index];

        currentMiniGame.gameObject.SetActive(true);
        currentMiniGame.StartGame();
    }

    /// 첫 번째 미니게임 시작
    public void StartFirstMiniGame()
    {
        StartMiniGame(0);
    }

    /// 랜덤 미니게임 시작
    public void StartRandomMiniGame()
    {
        if (miniGames == null || miniGames.Length == 0)
            return;

        int randomIndex = Random.Range(0, miniGames.Length);

        StartMiniGame(randomIndex);
    }

    /// 현재 미니게임 성공
    public void GameSuccess(MiniGame miniGame)
    {
        if (miniGame != currentMiniGame)
            return;

        Debug.Log($"미니게임 성공: {miniGame.name}");

        miniGame.EndGame();

        StartNextMiniGame();
    }

    /// 현재 미니게임 실패
    public void GameFail(MiniGame miniGame)
    {
        if (miniGame != currentMiniGame)
            return;

        Debug.Log($"미니게임 실패: {miniGame.name}");

        miniGame.EndGame();

        // 일단 테스트용으로 다음 게임 실행
        StartNextMiniGame();
    }

    /// 다음 미니게임
    public void StartNextMiniGame()
    {
        if (miniGames == null || miniGames.Length == 0)
            return;

        int nextIndex = currentIndex + 1;

        if (nextIndex >= miniGames.Length)
        {
            nextIndex = 0;
        }

        StartMiniGame(nextIndex);
    }

    /// 현재 미니게임 종료
    public void StopCurrentMiniGame()
    {
        if (currentMiniGame == null)
            return;

        currentMiniGame.EndGame();
        currentMiniGame.gameObject.SetActive(false);

        currentMiniGame = null;
        currentIndex = -1;
    }
}