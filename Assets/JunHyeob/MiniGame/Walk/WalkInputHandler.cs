using UnityEngine;

public class WalkPlayerInputHandler : MonoBehaviour
{
    [Header("게임 매니저")]
    [SerializeField] private WalkMiniGameManager manager;

    [Header("플레이어 발")]
    [SerializeField] private WalkPlayerFootController footController;

    private void Update()
    {
        if (MouseInputManager.Instance == null)
            return;

        if (manager == null)
            return;

        // =====================================================
        // 마우스 왼쪽 버튼을 누른 순간
        // =====================================================

        if (MouseInputManager.Instance.IsClickDown())
        {
            // 플레이어 왼발 표시
            if (footController != null)
            {
                footController.SetLeft();
            }

            // 왼발 판정
         //   manager.OnPlayerLeftStep();
        }

        // =====================================================
        // 마우스 왼쪽 버튼을 뗀 순간
        // =====================================================

        if (MouseInputManager.Instance.IsClickUp())
        {
            // 플레이어 오른발 표시
            if (footController != null)
            {
                footController.SetRight();
            }

            // 오른발 처리
          //  manager.OnPlayerRightStep();
        }
    }
}