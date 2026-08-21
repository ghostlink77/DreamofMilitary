// ========================
// MOPP 방호태세
// ========================

using DreamOfMilitary.Routine;
using System;
using UnityEngine;
using UnityEngine.UI;

public class MOPP : MonoBehaviour, IMinigame
{
    [Header("단계 표시")]
    [SerializeField] private Text stageText;

    [Header("플레이어")]
    [Tooltip("플레이어를 클릭하면 현재 착용 상태에서 한 단계 벗습니다.")]
    [SerializeField] private GameObject player;

    [Header("보호구")]
    [Tooltip("0: 보호의 / 1: 전투화 / 2: 방독면 / 3: 보호장갑")]
    [SerializeField] private GameObject[] equipmentItems;

    [Header("보호구 배치 위치")]
    [Tooltip("보호구가 랜덤하게 배치될 4개의 위치")]
    [SerializeField] private Transform[] equipmentSlots;

    [Header("착용 모습")]
    [Tooltip("0: 기본 / 1: 보호의 / 2: 보호의+전투화 / 3: +방독면 / 4: +보호장갑")]
    [SerializeField] private GameObject[] equippedStates;


    private const int EquipmentCount = 4;

    private bool _isPlaying;

    // 현재 MOPP 단계
    // 1~4 = 일반 MOPP
    // 5 = 알파
    private int _moppStage;

    // 다음에 장착해야 할 보호구
    // 0 = 보호의
    // 1 = 전투화
    // 2 = 방독면
    // 3 = 보호장갑
    private int _nextEquipmentIndex;

    // 알파 단계에서 방독면을 먼저 착용했는지
    private bool _isAlphaMaskFirst;

    // 미니게임 성공을 RoutineRunner에게 알리기 위한 이벤트
    private Action<MinigameOutcome> _onCompleted;


    // ========================
    // 미니게임 시작
    // ========================

    public void Begin(
        MinigameContext context,
        Action<MinigameOutcome> onCompleted)
    {
        _onCompleted = onCompleted;

        _isPlaying = true;

        _isAlphaMaskFirst = false;
        _nextEquipmentIndex = 0;

        // 난이도 2에서는 1~5단계 중 하나
        // 5 = 알파
        if (context.DifficultyTier == 2)
        {
            _moppStage = UnityEngine.Random.Range(2, 6);
        }
        else
        {
            _moppStage = UnityEngine.Random.Range(1, 5);
        }

        SetupStage();
    }


    // ========================
    // 매 프레임
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

        HandleClick(clickedObject);
    }


    // ========================
    // 단계 초기화
    // ========================

    private void SetupStage()
    {
        UpdateStageUI();

        ResetEquipment();
        ResetEquippedState();

        ShuffleEquipment();

        _nextEquipmentIndex = 0;
        _isAlphaMaskFirst = false;
    }


    // ========================
    // 단계 표시
    // ========================

    private void UpdateStageUI()
    {
        if (stageText != null)
        {
            if(_moppStage==5)
                stageText.text = "α";
            else    
                stageText.text = _moppStage.ToString();
        }
    }


    // ========================
    // 보호구 초기화
    // ========================

    private void ResetEquipment()
    {
        if (equipmentItems == null)
        {
            return;
        }

        for (int i = 0; i < equipmentItems.Length; i++)
        {
            if (equipmentItems[i] != null)
            {
                equipmentItems[i].SetActive(true);
            }
        }
    }


    // ========================
    // 착용 모습 초기화
    // ========================

    private void ResetEquippedState()
    {
        if (equippedStates == null)
        {
            return;
        }

        for (int i = 0; i < equippedStates.Length; i++)
        {
            if (equippedStates[i] != null)
            {
                equippedStates[i].SetActive(false);
            }
        }

        // 기본 상태
        if (equippedStates.Length > 0 &&
            equippedStates[0] != null)
        {
            equippedStates[0].SetActive(true);
        }
    }


    // ========================
    // 보호구 랜덤 배치
    // ========================

    private void ShuffleEquipment()
    {
        if (equipmentItems == null ||
            equipmentSlots == null)
        {
            return;
        }

        if (equipmentItems.Length != EquipmentCount ||
            equipmentSlots.Length != EquipmentCount)
        {
            Debug.LogWarning(
                "MOPP: 보호구와 슬롯은 각각 4개가 필요합니다.");

            return;
        }

        GameObject[] shuffledItems =
            new GameObject[EquipmentCount];

        for (int i = 0; i < EquipmentCount; i++)
        {
            shuffledItems[i] = equipmentItems[i];
        }

        // Fisher-Yates Shuffle
        for (int i = EquipmentCount - 1; i > 0; i--)
        {
            int randomIndex =
                UnityEngine.Random.Range(0, i + 1);

            GameObject temp = shuffledItems[i];

            shuffledItems[i] =
                shuffledItems[randomIndex];

            shuffledItems[randomIndex] =
                temp;
        }

        // 슬롯에 배치
        for (int i = 0; i < EquipmentCount; i++)
        {
            if (shuffledItems[i] == null ||
                equipmentSlots[i] == null)
            {
                continue;
            }

            shuffledItems[i].transform.position =
                equipmentSlots[i].position;
        }
    }


    // ========================
    // 클릭 처리
    // ========================

    private void HandleClick(GameObject clickedObject)
    {
        // 플레이어 클릭
        if (IsTarget(clickedObject, player))
        {
            UndressOneStep();
            return;
        }

        // 보호구 클릭
        int clickedEquipmentIndex =
            GetEquipmentIndex(clickedObject);

        if (clickedEquipmentIndex == -1)
        {
            return;
        }

        // 이미 착용한 보호구
        if (!equipmentItems[clickedEquipmentIndex].activeSelf)
        {
            return;
        }

        // 알파에서 방독면을 먼저 착용한 상태
        // 다른 보호구는 착용할 수 없음
        if (_isAlphaMaskFirst)
        {
            return;
        }

        // 알파 단계
        if (_moppStage == 5)
        {
            HandleAlphaEquipment(clickedEquipmentIndex);
            return;
        }
        
        // 일반 MOPP
        HandleNormalEquipment(clickedEquipmentIndex);
    }


    // ========================
    // 일반 MOPP 착용
    // ========================

    private void HandleNormalEquipment(int clickedEquipmentIndex)
    {
        // 정해진 순서가 아니면 무시
        if (clickedEquipmentIndex != _nextEquipmentIndex)
        {
            return;
        }

        EquipEquipment(clickedEquipmentIndex);

    }


    // ========================
    // 알파 단계 착용
    // ========================

    private void HandleAlphaEquipment(int clickedEquipmentIndex)
    {
        Debug.Log(_nextEquipmentIndex.ToString());

        // 방독면인 경우 + 처음 상태인 경우
        if (clickedEquipmentIndex == 2)
        {
            //첫단계로
            if(_nextEquipmentIndex == 0)
            {
                EquipEquipment(clickedEquipmentIndex);
                _isAlphaMaskFirst = true;
            }
            //3단계로
            else if(_nextEquipmentIndex == 2){
                EquipEquipment(clickedEquipmentIndex);
            }
            return;
        }
        // 방독면 외의 경우 + 방독면 미착용 상태
        else if (!_isAlphaMaskFirst)
        {
            // 정해진 순서가 아니면 무시
            if (clickedEquipmentIndex != _nextEquipmentIndex)
            {
                return;
            }

            EquipEquipment(clickedEquipmentIndex);
        }


    }


    // ========================
    // 클릭한 보호구 찾기
    // ========================

    private int GetEquipmentIndex(GameObject clickedObject)
    {
        if (clickedObject == null ||
            equipmentItems == null)
        {
            return -1;
        }

        for (int i = 0; i < equipmentItems.Length; i++)
        {
            if (equipmentItems[i] == null)
            {
                continue;
            }

            // 직접 클릭
            if (clickedObject == equipmentItems[i])
            {
                return i;
            }

            // 자식 Image가 클릭된 경우
            if (clickedObject.transform.IsChildOf(
                    equipmentItems[i].transform))
            {
                return i;
            }
        }

        return -1;
    }


    // ========================
    // 대상 확인
    // ========================

    private bool IsTarget(
        GameObject clickedObject,
        GameObject target)
    {
        if (clickedObject == null ||
            target == null)
        {
            return false;
        }

        if (clickedObject == target)
        {
            return true;
        }

        return clickedObject.transform.IsChildOf(
            target.transform);
    }


    // ========================
    // 보호구 장착
    // ========================

    private void EquipEquipment(int equipmentIndex)
    {
        //상정한 범위를 벗어난 경우 리턴
        if (equipmentIndex < 0 ||
            equipmentIndex >= equipmentItems.Length)
        {
            return;
        }

        // 보호구를 화면에서 제거
        equipmentItems[equipmentIndex].SetActive(false);

        // 다음 단계
        _nextEquipmentIndex = equipmentIndex +1;

        // 착용 모습 변경
        UpdateEquippedState();
    }


    // ========================
    // 착용 모습 변경
    // ========================

    private void UpdateEquippedState()
    {
        if (equippedStates == null ||
            equippedStates.Length == 0)
        {
            return;
        }

        //그 전꺼는 비활성
        for (int i = 0; i < equippedStates.Length; i++)
        {
            if (equippedStates[i] != null)
            {
                equippedStates[i].SetActive(false);
            }
        }

        int stateIndex;

        // 알파에서 방독면을 먼저 착용한 상태
        if (_isAlphaMaskFirst)
        {
            // 알파 방독면 전용 모습
            stateIndex = 3;
        }
        else
        {
            stateIndex = Mathf.Clamp(
                _nextEquipmentIndex,
                0,
                equippedStates.Length - 1);
        }

        if (stateIndex < equippedStates.Length &&
            equippedStates[stateIndex] != null)
        {
            equippedStates[stateIndex].SetActive(true);
        }
    }


    // ========================
    // 한 단계 벗기
    // ========================

    private void UndressOneStep()
    {
        // 아무것도 안 입었으면 종료
        if (_nextEquipmentIndex <= 0 &&
            !_isAlphaMaskFirst)
        {
            return;
        }

        // ========================
        // 알파에서 방독면을 먼저 쓴 경우
        // ========================

        if (_isAlphaMaskFirst)
        {
            // 방독면 다시 등장
            if (equipmentItems != null &&
                equipmentItems.Length > 2 &&
                equipmentItems[2] != null)
            {
                equipmentItems[2].SetActive(true);
            }

            _isAlphaMaskFirst = false;
            _nextEquipmentIndex = 0;

            UpdateEquippedState();

            return;
        }


        // ========================
        // 일반 착용 상태
        // ========================

        int equipmentToRemove =
            _nextEquipmentIndex - 1;

        if (equipmentToRemove < 0 ||
            equipmentToRemove >= equipmentItems.Length)
        {
            return;
        }

        if (equipmentItems[equipmentToRemove] != null)
        {
            equipmentItems[equipmentToRemove].SetActive(true);
        }

        _nextEquipmentIndex--;

        UpdateEquippedState();
    }


    // ========================
    // 성공 확인
    // ========================

    public void ChkSucess()
    {
        // 방독면을 먼저 착용한 경우
        if (_isAlphaMaskFirst)
        {
            // 알파 단계
            if (_moppStage == 5)
            {
                Success();
                return;
            }
            Debug.Log("MOPP 실패!");
            Abort();
            return;

        }

        // ========================
        // 일반 MOPP
        // ========================

        if (_nextEquipmentIndex >= _moppStage)
        {
            Success();
            return;
        }
        //실패
        Debug.Log("MOPP 실패!");
        Abort();

    }


    // ========================
    // 성공
    // ========================

    private void Success()
    {
        if (!_isPlaying)
        {
            return;
        }

        Debug.Log("MOPP 성공!");

        _isPlaying = false;

        var callback = _onCompleted;
        _onCompleted = null;

        callback?.Invoke(
            new MinigameOutcome(
                MinigameJudgement.Success));
    }


    // ========================
    // 중단
    // ========================

    public void Abort()
    {
        _isPlaying = false;

        _onCompleted = null;

        ResetEquipment();
        ResetEquippedState();

        _nextEquipmentIndex = 0;
        _isAlphaMaskFirst = false;
    }


    // ========================
    // 비활성화
    // ========================

    private void OnDisable()
    {
        Abort();
    }
}