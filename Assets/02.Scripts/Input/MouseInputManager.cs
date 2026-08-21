using UnityEngine;
using UnityEngine.InputSystem;

public class MouseInputManager : MonoBehaviour
{
    public static MouseInputManager Instance { get; private set; }

    private GameInputActions inputActions;

    // 현재 마우스의 화면 좌표
    public Vector2 MouseScreenPosition
    {
        get
        {
            if (Mouse.current == null)
                return Vector2.zero;

            return Mouse.current.position.ReadValue();
        }
    }

    // 현재 마우스의 월드 좌표
    public Vector2 MouseWorldPosition
    {
        get
        {
            if (Camera.main == null)
                return Vector2.zero;

            return Camera.main.ScreenToWorldPoint(MouseScreenPosition);
        }
    }

    private void Awake()
    {
        // 싱글톤
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 씬이 바뀌어도 유지
        DontDestroyOnLoad(gameObject);

        // Input Actions 생성
        inputActions = new GameInputActions();
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Enable();
    }

    private void OnDisable()
    {
        inputActions.Gameplay.Disable();
    }

    /// 마우스 왼쪽 버튼을 누른 순간
    public bool IsClickDown()
    {
        return inputActions.Gameplay.Click.WasPressedThisFrame();
    }

    /// 마우스 왼쪽 버튼을 누르고 있는 동안
    public bool IsClickHeld()
    {
        return inputActions.Gameplay.Click.IsPressed();
    }

    /// 마우스 왼쪽 버튼을 뗀 순간
    public bool IsClickUp()
    {
        return inputActions.Gameplay.Click.WasReleasedThisFrame();
    }

    /// 현재 마우스 위치에서 클릭된 2D 오브젝트 반환
    public GameObject GetClickedObject()
    {
        Vector2 worldPosition = MouseWorldPosition;

        Collider2D hit = Physics2D.OverlapPoint(worldPosition);

        if (hit != null)
        {
            return hit.gameObject;
        }

        return null;
    }

    /// 현재 마우스 위치에서 클릭된 Collider2D 반환
    public Collider2D GetClickedCollider()
    {
        Vector2 worldPosition = MouseWorldPosition;

        return Physics2D.OverlapPoint(worldPosition);
    }

}
