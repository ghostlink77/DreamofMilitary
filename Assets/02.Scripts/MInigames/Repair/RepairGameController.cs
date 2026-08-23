using DreamOfMilitary.Routine;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RepairGameController : MonoBehaviour, IMinigame
{
    [Header("장비")]
    [SerializeField] private GameObject[] repairItems;

    [Header("전체 나사 - 총 18개")]
    [SerializeField] private RepairScrew[] allScrews;

    [Header("정비 진행도 UI")]
    [SerializeField] private Slider repairCountSlider;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("장비 전환 버튼")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [Header("드라이버 커서")]
    [SerializeField] private GameObject repairCursor;

    private Action<MinigameJudgement> _onCompleted;

    private int _targetScrewCount;
    private int _removedScrewCount;
    private int _currentItemIndex;

    private bool _isPlaying;
    private bool _isCompleted;

    public void Begin(MinigameContext context, Action<MinigameJudgement> onCompleted)
    {
        _onCompleted = onCompleted;

        _isPlaying = true;
        _isCompleted = false;
        _removedScrewCount = 0;
        _currentItemIndex = 0;

        // 난이도: Easy 1개 / Normal 4개 / Hard 8개
        _targetScrewCount = context.DifficultyTier switch
        {
            0 => 2,
            1 => 4,
            2 => 8,
            _ => throw new ArgumentOutOfRangeException(
                nameof(context.DifficultyTier),
                "난이도는 0, 1, 2 중 하나여야 합니다."
            )
        };

        if (allScrews == null || allScrews.Length < _targetScrewCount)
        {
            Debug.LogError("RepairGameController: allScrews에 나사 18개를 등록하세요.");
            Complete(MinigameJudgement.Failure);
            return;
        }

        SetupScrews(context.RandomSeed);
        SetupUI();
        ShowItem(0);

        if (repairCursor != null)
        {
            repairCursor.SetActive(true);
        }
    }

    private void Update()
    {
        if (!_isPlaying || _isCompleted)
        {
            return;
        }

        if (MouseInputManager.Instance == null)
        {
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

        // 나사 Image의 자식이 클릭되어도 부모의 RepairScrew를 찾음
        RepairScrew screw =
            clickedObject.GetComponentInParent<RepairScrew>();

        if (screw != null)
        {
            TryRemoveScrew(screw);
        }
    }

    public void Abort()
    {
        _isPlaying = false;
        _isCompleted = true;
        _onCompleted = null;

        if (repairCursor != null)
        {
            repairCursor.SetActive(false);
        }
    }

    private void SetupScrews(int seed)
    {
        foreach (RepairScrew screw in allScrews)
        {
            if (screw != null)
            {
                screw.SetTarget(false);
            }
        }

        List<RepairScrew> shuffledScrews = new List<RepairScrew>(allScrews);
        System.Random random = new System.Random(seed);

        // 시드 기반 셔플: 같은 routine seed면 같은 나사가 선택됨
        for (int i = shuffledScrews.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(i + 1);

            RepairScrew temp = shuffledScrews[i];
            shuffledScrews[i] = shuffledScrews[randomIndex];
            shuffledScrews[randomIndex] = temp;
        }

        for (int i = 0; i < _targetScrewCount; i++)
        {
            shuffledScrews[i].SetTarget(true);
        }
    }

    private void SetupUI()
    {
        repairCountSlider.minValue = 0;
        repairCountSlider.maxValue = _targetScrewCount;
        repairCountSlider.value = 0;
        repairCountSlider.interactable = false;

        UpdateProgressUI();
    }

    public void TryRemoveScrew(RepairScrew screw)
    {
        if (!_isPlaying || _isCompleted || screw == null)
        {
            return;
        }

        if (!screw.IsTarget || screw.IsRemoved)
        {
            return;
        }

        screw.Remove();
        _removedScrewCount++;

        UpdateProgressUI();

        if (_removedScrewCount >= _targetScrewCount)
        {
            Complete(MinigameJudgement.Success);
        }
    }

    public void ShowPreviousItem()
    {
        if (!_isPlaying)
        {
            return;
        }

        ShowItem(_currentItemIndex - 1);
    }

    public void ShowNextItem()
    {
        if (!_isPlaying)
        {
            return;
        }

        ShowItem(_currentItemIndex + 1);
    }

    private void ShowItem(int index)
    {
        _currentItemIndex = Mathf.Clamp(index, 0, repairItems.Length - 1);

        for (int i = 0; i < repairItems.Length; i++)
        {
            repairItems[i].SetActive(i == _currentItemIndex);
        }

        // Item 1: 오른쪽 버튼만 / Item 2: 왼쪽 버튼만
        leftButton.gameObject.SetActive(_currentItemIndex > 0);
        rightButton.gameObject.SetActive(_currentItemIndex < repairItems.Length - 1);
    }

    private void UpdateProgressUI()
    {
        repairCountSlider.value = _removedScrewCount;
        countText.text = $"{_removedScrewCount} / {_targetScrewCount}";
    }

    private void Complete(MinigameJudgement judgement)
    {
        if (_isCompleted)
        {
            return;
        }

        _isCompleted = true;
        _isPlaying = false;

        if (repairCursor != null)
        {
            repairCursor.SetActive(false);
        }

        Action<MinigameJudgement> callback = _onCompleted;
        _onCompleted = null;

        callback?.Invoke(judgement);
    }

    private void OnDisable()
    {
        Abort();
    }
}