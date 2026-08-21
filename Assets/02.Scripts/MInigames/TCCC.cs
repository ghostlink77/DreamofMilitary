using DreamOfMilitary.Routine;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // New Input System
using UnityEngine.UI;

public class TCCC : MonoBehaviour, IMinigame
{
    [Header("출혈 부위 (마우스 감지 및 알파값 조절 대상)")]
    [SerializeField] private GameObject[] bleedingAreas;

    [Header("지혈대 부착 위치 (클릭 영역)")]
    [SerializeField] private GameObject[] clickAreas;

    [Header("지혈대 배열 (1번 자식이 막대 UI여야 함)")]
    [SerializeField] private GameObject[] tourniquets;

    [Header("성공 시 활성화할 단일 이미지 (고통스러운 표정 등)")]
    [SerializeField] private GameObject successImage;

    [Header("디버그용: 피벗 위치 시각화 점")]
    [SerializeField] private RectTransform debugPivotMarker;

    private bool _isPlaying;
    private int _activeIndex = -1;
    private bool _isBleedingRevealed;

    // 회전 로직 관련 변수
    private Vector2 _fixedScreenPivot;   // 화면(Screen) 상의 막대 중심점
    private float _previousMouseAngle;
    private float _currentStickAngle;
    private float _netRotationAccumulated; // 순 수치 (양수/음수)
    private float _successRotation;
    private bool _isStickInitialized;

    private Action<MinigameOutcome> _onCompleted;

    public void Begin(MinigameContext context, Action<MinigameOutcome> onCompleted)
    {
        _onCompleted = onCompleted;

        _netRotationAccumulated = 0f;
        _currentStickAngle = 0f;
        _isStickInitialized = false;
        _isBleedingRevealed = false;

        _successRotation = context.DifficultyTier switch
        {
            1 => 360f * 4,
            2 => 360f * 8,
            _ => 360f * 6
        };

        _isPlaying = true;
        _activeIndex = -1;

        ResetObjects();

        if (bleedingAreas != null && bleedingAreas.Length > 0)
        {
            _activeIndex = UnityEngine.Random.Range(0, bleedingAreas.Length);
        }
    }

    private void Update()
    {
        if (!_isPlaying) return;
        if (Mouse.current == null) return;

        // 1. New Input System 기반 마우스 감지
        Vector2 mousePos = Mouse.current.position.ReadValue();
        bool isClickDown = Mouse.current.leftButton.wasPressedThisFrame;

        if (_activeIndex >= 0)
        {
            // 출혈 부위 감지 (마우스 오버 시 알파값 1)
            if (bleedingAreas != null && _activeIndex < bleedingAreas.Length && bleedingAreas[_activeIndex] != null)
            {
                if (IsMouseOverObject(bleedingAreas[_activeIndex], mousePos))
                {
                    SetAlpha(bleedingAreas[_activeIndex], 1f);
                    _isBleedingRevealed = true;
                }
            }

            // 출혈 부위 노출 후 지혈대 부착 위치 클릭 시 활성화
            if (_isBleedingRevealed && isClickDown)
            {
                if (clickAreas != null && _activeIndex < clickAreas.Length && clickAreas[_activeIndex] != null)
                {
                    if (IsMouseOverObject(clickAreas[_activeIndex], mousePos))
                    {
                        if (tourniquets != null && _activeIndex < tourniquets.Length && tourniquets[_activeIndex] != null)
                        {
                            tourniquets[_activeIndex].SetActive(true);
                            _isStickInitialized = false;
                        }
                    }
                }
            }
        }

        // 지혈대 막대 회전 처리
        if (_activeIndex >= 0 && tourniquets != null && _activeIndex < tourniquets.Length)
        {
            GameObject currentTourniquet = tourniquets[_activeIndex];
            if (currentTourniquet != null && currentTourniquet.activeSelf)
            {
                if (currentTourniquet.transform.childCount > 0)
                {
                    Transform stickTransform = currentTourniquet.transform.GetChild(0);
                    ProcessStickRotation(stickTransform);
                }
            }
        }
    }

    private void ProcessStickRotation(Transform stick)
    {
        if (Mouse.current == null) return;
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        // 막대 중심점 스크린 좌표 계산 및 고정
        if (!_isStickInitialized)
        {
            if (stick is RectTransform rectStick)
            {
                Canvas parentCanvas = rectStick.GetComponentInParent<Canvas>();
                Camera uiCamera = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay) 
                                  ? parentCanvas.worldCamera 
                                  : null;

                _fixedScreenPivot = RectTransformUtility.WorldToScreenPoint(uiCamera, rectStick.position);
            }
            else
            {
                Camera mainCam = Camera.main;
                _fixedScreenPivot = (mainCam != null) ? (Vector2)mainCam.WorldToScreenPoint(stick.position) : (Vector2)stick.position;
            }

            if (debugPivotMarker != null)
            {
                debugPivotMarker.gameObject.SetActive(true);
                debugPivotMarker.position = stick.position;
            }

            Vector2 initialDir = mouseScreenPos - _fixedScreenPivot;
            if (initialDir.sqrMagnitude > 0.0001f)
            {
                _previousMouseAngle = Mathf.Atan2(initialDir.y, initialDir.x) * Mathf.Rad2Deg;
                _currentStickAngle = stick.localEulerAngles.z;
                _isStickInitialized = true;
            }
            return;
        }

        Vector2 currentDir = mouseScreenPos - _fixedScreenPivot;
        if (currentDir.sqrMagnitude < 0.0001f) return;

        float currentMouseAngle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;
        float mouseDelta = Mathf.DeltaAngle(_previousMouseAngle, currentMouseAngle);

        if (Mathf.Abs(mouseDelta) > 0.01f)
        {
            // [방향 차감 로직]
            // 시계 방향 회전 시 mouseDelta가 음수이므로 -mouseDelta를 해주면 양수로 누적됨.
            // 반시계 방향 회전 시 mouseDelta가 양수이므로 -mouseDelta를 해주면 음수로 차감됨.
            _netRotationAccumulated -= mouseDelta;

            // 시각적 회전 반영
            _currentStickAngle -= mouseDelta;
            stick.localRotation = Quaternion.Euler(0f, 0f, _currentStickAngle);

            _previousMouseAngle = currentMouseAngle;
        }

        // 음수든 양수든 회전 크기(절대값)가 요구 회전 횟수를 넘기면 클리어
        if (Mathf.Abs(_netRotationAccumulated) >= _successRotation)
        {
            Success();
        }
    }

    private bool IsMouseOverObject(GameObject target, Vector2 mouseScreenPos)
    {
        if (target == null) return false;

        if (target.TryGetComponent<RectTransform>(out var rectTransform))
        {
            Canvas parentCanvas = rectTransform.GetComponentInParent<Canvas>();
            Camera uiCamera = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay) 
                              ? parentCanvas.worldCamera 
                              : null;

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mouseScreenPos, uiCamera);
        }
        else
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return false;

            Vector2 worldPoint = mainCam.ScreenToWorldPoint(mouseScreenPos);
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);
            return hit.collider != null && hit.collider.gameObject == target;
        }
    }

    private void Success()
    {
        _isPlaying = false;

        if (debugPivotMarker != null) debugPivotMarker.gameObject.SetActive(false);
        if (successImage != null) successImage.SetActive(true);

        var callback = _onCompleted;
        _onCompleted = null;
        callback?.Invoke(new MinigameOutcome(MinigameJudgement.Success));
    }

    private void SetAlpha(GameObject target, float alpha)
    {
        if (target == null) return;

        if (target.TryGetComponent<Image>(out var img))
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
        else if (target.TryGetComponent<SpriteRenderer>(out var sr))
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    private void ResetObjects()
    {
        _isBleedingRevealed = false;
        _isStickInitialized = false;
        _netRotationAccumulated = 0f;

        if (debugPivotMarker != null) debugPivotMarker.gameObject.SetActive(false);

        if (bleedingAreas != null)
        {
            foreach (var obj in bleedingAreas)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    SetAlpha(obj, 0f);
                }
            }
        }

        if (clickAreas != null)
        {
            foreach (var obj in clickAreas)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        if (tourniquets != null)
        {
            foreach (var obj in tourniquets)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        if (successImage != null) successImage.SetActive(false);
    }

    public void Abort()
    {
        _isPlaying = false;
        _onCompleted = null;
        _activeIndex = -1;
        _isStickInitialized = false;
        _isBleedingRevealed = false;
        _netRotationAccumulated = 0f;
        ResetObjects();
    }

    private void OnDisable()
    {
        Abort();
    }
}