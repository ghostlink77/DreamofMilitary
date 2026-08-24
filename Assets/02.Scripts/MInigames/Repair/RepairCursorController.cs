using UnityEngine;

public class RepairCursorController : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Camera uiCamera;

    [Header("드라이버 회전")]
    [SerializeField] private float twistAngle = 70f;
    [SerializeField] private float twistSpeed = 12f;

    private RectTransform cursorRect;
    private float baseRotation;
    private float currentTwist;

    private void Awake()
    {
        cursorRect = GetComponent<RectTransform>();
        baseRotation = cursorRect.localEulerAngles.z;

        Cursor.visible = false;
    }

    private void Update()
    {
        if (MouseInputManager.Instance == null)
        {
            return;
        }

        FollowMouse();
        PlayTwistAnimation();
    }

    private void FollowMouse()
    {
        Vector2 mousePosition =
            MouseInputManager.Instance.MouseScreenPosition;

        RectTransform parentRect =
            cursorRect.parent as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            mousePosition,
            uiCamera,
            out Vector2 localPosition
        );

        cursorRect.anchoredPosition = localPosition;
    }

    private void PlayTwistAnimation()
    {
        bool isClickHeld = MouseInputManager.Instance.IsClickHeld();
        float targetTwist = isClickHeld ? twistAngle : 0f;

        currentTwist = Mathf.Lerp(
            currentTwist,
            targetTwist,
            Time.unscaledDeltaTime * twistSpeed
        );

        cursorRect.localRotation = Quaternion.Euler(
            0f,
            0f,
            baseRotation + currentTwist
        );
    }

    private void OnDestroy()
    {
        Cursor.visible = true;
    }
}