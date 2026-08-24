// ========================
// 관물대 정리
// ========================

using DreamOfMilitary.Routine;
using DreamOfMilitary.Audio;
using System;
using UnityEngine;

public class CleanUp : MonoBehaviour, IMinigame
{
    [Header("관물대")]
    [SerializeField] private GameObject lockerClosed;
    [SerializeField] private GameObject lockerOpened;

    [Header("정리할 옷가지")]
    [SerializeField] private GameObject[] clothes;

    [Header("정리할 물건")]
    [SerializeField] private GameObject laundryBasket;
    [SerializeField] private GameObject slippers;
    [SerializeField] private GameObject slippers_b;

    [Header("침대 밑 정리 위치")]
    [SerializeField] private GameObject laundryBasketUnderBed;
    [SerializeField] private GameObject slippersUnderBed;
    [SerializeField] private GameObject slippersUnderBed_b;


    private bool _isPlaying;
    private bool _isLockerOpened;

    // 미니게임 성공을 RoutineRunner에게 알리기 위한 이벤트
    private Action<MinigameJudgement> _onCompleted;


    // ========================
    // 미니게임 시작
    // ========================

    public void Begin(
        MinigameContext context,
        Action<MinigameJudgement> onCompleted)
    {
        _onCompleted = onCompleted;

        _isPlaying = true;
        _isLockerOpened = true;

        ResetObjects();

        //난이도 설정
        if(context.DifficultyTier == 2)
        {
            // 난이도 2에서는 관물대를 닫아둔다.
            _isLockerOpened = false;
            lockerClosed.SetActive(true);
            lockerOpened.SetActive(false);
        }
        else
        {
            // 난이도 1에서는 관물대를 열어둔다.
            _isLockerOpened = true;
            lockerClosed.SetActive(false);
            lockerOpened.SetActive(true);
        }
    }


    // ========================
    // 입력 처리
    // ========================

    private void Update()
    {
        if (!_isPlaying)
        {
            return;
        }

        if (MouseInputManager.Instance == null)
        {
            Debug.LogWarning("No MouseInputManager");
            return;
        }

        if (!MouseInputManager.Instance.IsClickDown())
        {
            return;
        }

        GameObject clickedObject =
            MouseInputManager.Instance.GetClickedObject();

        if (clickedObject == null)
        {
            return;
        }

        HandleObjectClick(clickedObject);
    }


    // ========================
    // 오브젝트 클릭 처리
    // ========================

    private void HandleObjectClick(GameObject clickedObject)
    {
        // 관물대
        if (clickedObject == lockerClosed ||
            clickedObject == lockerOpened)
        {
            GameAudioController.Instance?.PlayLocker();
            ToggleLocker();
            CheckSuccess();
            return;
        }

        // 관물대 안의 옷
        if (_isLockerOpened && IsClothes(clickedObject))
        {
            GameAudioController.Instance?.PlayCloth();
            RemoveClothes(clickedObject);
            CheckSuccess();
            return;
        }

        // 빨래바구니
        if (clickedObject == laundryBasket)
        {
            GameAudioController.Instance?.PlayCloth();
            MoveLaundryBasket();
            CheckSuccess();
            return;
        }

        // 슬리퍼
        if (clickedObject == slippers)
        {
            GameAudioController.Instance?.PlayCloth();
            MoveSlippers();
            CheckSuccess();
        }

        // 슬리퍼
        if (clickedObject == slippers_b)
        {
            GameAudioController.Instance?.PlayCloth();
            MoveSlippers_b();
            CheckSuccess();
        }
    }


    // ========================
    // 관물대
    // ========================

    private void ToggleLocker()
    {
        _isLockerOpened = !_isLockerOpened;
        if (lockerClosed != null)
        {
            lockerClosed.SetActive(!_isLockerOpened);
        }

        if (lockerOpened != null)
        {
            lockerOpened.SetActive(_isLockerOpened);
        }
    }


    // ========================
    // 옷 정리
    // ========================

    private bool IsClothes(GameObject target)
    {
        if (clothes == null)
        {
            return false;
        }

        for (int i = 0; i < clothes.Length; i++)
        {
            if (clothes[i] == target)
            {
                return true;
            }
        }

        return false;
    }


    private void RemoveClothes(GameObject clothesObject)
    {
        if (clothesObject == null)
        {
            return;
        }

        clothesObject.SetActive(false);
    }


    // ========================
    // 빨래바구니 정리
    // ========================

    private void MoveLaundryBasket()
    {
        if (laundryBasket == null ||
            laundryBasketUnderBed == null)
        {
            return;
        }

        laundryBasket.SetActive(false);
        laundryBasketUnderBed.SetActive(true);
    }


    // ========================
    // 슬리퍼 정리
    // ========================

    private void MoveSlippers()
    {
        if (slippers == null ||
            slippersUnderBed == null)
        {
            return;
        }

        slippers.SetActive(false);
        slippersUnderBed.SetActive(true);
    }

    private void MoveSlippers_b()
    {
        if (slippers_b == null ||
            slippersUnderBed_b == null)
        {
            return;
        }

        slippers_b.SetActive(false);
        slippersUnderBed_b.SetActive(true);
    }

    // ========================
    // 성공 조건 확인
    // ========================

    private void CheckSuccess()
    {
        // 옷가지가 남아 있으면 아직 완료되지 않음
        if (HasRemainingClothes())
        {
            return;
        }

        // 빨래바구니가 아직 정리되지 않았으면 완료되지 않음
        if (laundryBasket != null &&
            laundryBasket.activeSelf)
        {
            return;
        }

        // 슬리퍼가 아직 정리되지 않았으면 완료되지 않음
        if (slippers != null &&
            slippers.activeSelf)
        {
            return;
        }
        // 슬리퍼가 아직 정리되지 않았으면 완료되지 않음
        if (slippers_b != null &&
            slippers_b.activeSelf)
        {
            return;
        }

        // 관물대가 열려 있으면 완료되지 않음
        if (_isLockerOpened)
        {
            return;
        }

        Debug.Log("6");
        Success();
    }


    private bool HasRemainingClothes()
    {
        if (clothes == null)
        {
            return false;
        }

        for (int i = 0; i < clothes.Length; i++)
        {
            if (clothes[i] != null &&
                clothes[i].activeSelf)
            {
                return true;
            }
        }

        return false;
    }


    // ========================
    // 성공
    // ========================

    private void Success()
    {
        Debug.Log("CleanUp 미니게임 성공!");
        if (!_isPlaying)
        {
            return;
        }

        _isPlaying = false;
        _isLockerOpened = false;

        // callback을 비우고 호출하기 위해 복사본 생성
        var callback = _onCompleted;
        _onCompleted = null;

        callback?.Invoke(MinigameJudgement.Success);
    }


    // ========================
    // 초기화
    // ========================

    private void ResetObjects()
    {
        _isLockerOpened = false;

        // 관물대 닫기
        if (lockerClosed != null)
        {
            lockerClosed.SetActive(true);
        }

        if (lockerOpened != null)
        {
            lockerOpened.SetActive(false);
        }

        // 옷가지 복구
        if (clothes != null)
        {
            for (int i = 0; i < clothes.Length; i++)
            {
                if (clothes[i] != null)
                {
                    clothes[i].SetActive(true);
                }
            }
        }

        // 빨래바구니 복구
        if (laundryBasket != null)
        {
            laundryBasket.SetActive(true);
        }

        if (laundryBasketUnderBed != null)
        {
            laundryBasketUnderBed.SetActive(false);
        }

        // 슬리퍼 복구
        if (slippers != null)
        {
            slippers.SetActive(true);
        }

        if (slippersUnderBed != null)
        {
            slippersUnderBed.SetActive(false);
        }

        // 슬리퍼b 복구
        if (slippers_b != null)
        {
            slippers_b.SetActive(true);
        }

        if (slippersUnderBed_b != null)
        {
            slippersUnderBed_b.SetActive(false);
        }

    }


    // ========================
    // 중단
    // ========================

    public void Abort()
    {
        _isPlaying = false;
        _isLockerOpened = false;
        _onCompleted = null;

        ResetObjects();
    }


    private void OnDisable()
    {
        Abort();
    }
}
