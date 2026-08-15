using UnityEngine;

public abstract class MiniGame : MonoBehaviour
{
    [Header("Mini Game Settings")]
    [SerializeField] protected float timeLimit = 10f;

    protected float currentTime;
    protected bool isRunning;

    public float TimeLimit => timeLimit;
    public float CurrentTime => currentTime;
    public bool IsRunning => isRunning;

    /// 미니게임이 시작될 때 호출
    public virtual void StartGame()
    {
        currentTime = timeLimit;
        isRunning = true;

        OnGameStart();
    }

    /// 미니게임이 종료될 때 호출
    public virtual void EndGame()
    {
        isRunning = false;

        OnGameEnd();
    }

    /// 미니게임 성공
    protected void Success()
    {
        if (!isRunning)
            return;

        isRunning = false;

        OnSuccess();

        MiniGameManager.Instance.GameSuccess(this);
    }

    /// 미니게임 실패
    protected void Fail()
    {
        if (!isRunning)
            return;

        isRunning = false;

        OnFail();

        MiniGameManager.Instance.GameFail(this);
    }

    private void Update()
    {
        if (!isRunning)
            return;

        currentTime -= Time.deltaTime;

        OnGameUpdate();

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            Fail();
        }
    }

    /// 게임 시작 시 자식 클래스에서 구현
    protected virtual void OnGameStart(){}

    /// 게임이 실행되는 동안 매 프레임 호출
    protected virtual void OnGameUpdate(){}

    /// 게임 종료 시 호출
    protected virtual void OnGameEnd(){}

    /// 성공 시 호출
    protected virtual void OnSuccess(){}

    /// 실패 시 호출
    protected virtual void OnFail() {}
}