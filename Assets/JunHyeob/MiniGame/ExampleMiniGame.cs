using UnityEngine;

public class ExampleMiniGame : MiniGame
{
    [SerializeField] private GameObject target;

    protected override void OnGameStart()
    {
        Debug.Log("클릭 타겟 게임 시작!");

        target.SetActive(true);
    }

    protected override void OnGameUpdate()
    {
        if (MouseInputManager.Instance.IsClickDown())
        {
            GameObject clickedObject =
                MouseInputManager.Instance.GetClickedObject();

            if (clickedObject == target)
            {
                Success();
            }
        }
    }

    protected override void OnSuccess()
    {
        Debug.Log("타겟 클릭 성공!");

        target.SetActive(false);
    }

    protected override void OnFail()
    {
        Debug.Log("시간 초과!");

        target.SetActive(false);
    }

    protected override void OnGameEnd()
    {
        Debug.Log("클릭 타겟 게임 종료");
    }
}
