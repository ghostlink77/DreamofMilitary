// ========================
// 마우스 입력 매니저
// ========================

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MouseInputManager : MonoBehaviour
{
    public static MouseInputManager Instance { get; private set; }

    private GameInputActions inputActions;

    // ========================
    // 마우스 위치
    // ========================

    // 현재 마우스의 화면 좌표
    public Vector2 MouseScreenPosition
    {
        get
        {
            if (Mouse.current == null)
            {
                return Vector2.zero;
            }

            return Mouse.current.position.ReadValue();
        }
    }


    // 현재 마우스의 월드 좌표
    public Vector2 MouseWorldPosition
    {
        get
        {
            if (Camera.main == null)
            {
                return Vector2.zero;
            }

            return Camera.main.ScreenToWorldPoint(
                MouseScreenPosition);
        }
    }


    // ========================
    // 초기화
    // ========================

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


    // ========================
    // 마우스 입력
    // ========================

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

    /// 마우스 오른쪽 버튼을 누른 순간
    public bool IsRightClickDown()
    {
        return inputActions.Gameplay.ClickRight.WasPressedThisFrame();
    }

    /// 마우스 오른쪽 버튼을 누르고 있는 동안
    public bool IsRightClickHeld()
    {
        return inputActions.Gameplay.ClickRight.IsPressed();
    
    }

    /// 마우스 오른쪽 버튼을 뗀 순간
    public bool IsRightClickUp()
    {
        return inputActions.Gameplay.ClickRight.WasReleasedThisFrame();
    }

    // ========================
    // 클릭 오브젝트
    // ========================

    /// 현재 마우스 위치에서 클릭된 오브젝트 반환
    /// UI Image와 2D Collider 모두 지원
    public GameObject GetClickedObject()
    {
        // 1. UI 먼저 검사
        GameObject uiObject = GetClickedUIObject();

        if (uiObject != null)
        {
            return uiObject;
        }

        // 2. 월드 2D 오브젝트 검사
        return GetClicked2DObject();
    }


    // ========================
    // UI 클릭
    // ========================

    private GameObject GetClickedUIObject()
    {
        if (EventSystem.current == null)
        {
            return null;
        }

        PointerEventData pointerData =
            new PointerEventData(EventSystem.current);

        pointerData.position = MouseScreenPosition;

        var results = new System.Collections.Generic.List<RaycastResult>();

        EventSystem.current.RaycastAll(
            pointerData,
            results);

        if (results.Count == 0)
        {
            return null;
        }

        // 가장 위에 있는 UI 반환
        for (int i = 0; i < results.Count; i++)
        {
            GameObject hitObject = results[i].gameObject;

            if (hitObject == null)
            {
                continue;
            }

            // Image, RawImage 등 Graphic 기반 UI
            if (hitObject.GetComponent<Graphic>() != null)
            {
                return hitObject;
            }
        }

        return null;
    }


    // ========================
    // 2D 오브젝트 클릭
    // ========================

    private GameObject GetClicked2DObject()
    {
        Vector2 worldPosition = MouseWorldPosition;

        Collider2D hit =
            Physics2D.OverlapPoint(worldPosition);

        if (hit != null)
        {
            return hit.gameObject;
        }

        return null;
    }


    // ========================
    // Collider 반환
    // ========================

    /// 현재 마우스 위치에서 클릭된 2D Collider 반환
    public Collider2D GetClickedCollider()
    {
        Vector2 worldPosition = MouseWorldPosition;

        return Physics2D.OverlapPoint(worldPosition);
    }


    /// 현재 마우스 위치에서 클릭된 UI Graphic 반환
    public Graphic GetClickedUIGraphic()
    {
        if (EventSystem.current == null)
        {
            return null;
        }

        PointerEventData pointerData =
            new PointerEventData(EventSystem.current);

        pointerData.position = MouseScreenPosition;

        var results =
            new System.Collections.Generic.List<RaycastResult>();

        EventSystem.current.RaycastAll(
            pointerData,
            results);

        for (int i = 0; i < results.Count; i++)
        {
            Graphic graphic =
                results[i].gameObject.GetComponent<Graphic>();

            if (graphic != null)
            {
                return graphic;
            }
        }

        return null;
    }
}