using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DreamOfMilitary.Routine.Minigames.QuickPack
{
    /// <summary>
    /// 신속 군장 싸기 미니게임.
    ///
    /// 입력은 프로젝트 공통 MouseInputManager를 사용한다.
    /// 점수, 계급, 복무 개월 등의 게임 진행은 직접 수정하지 않는다.
    /// </summary>
    public sealed class QuickPackMinigame
        : MonoBehaviour, IMinigame
    {
        [Header("군장 물품")]
        [SerializeField]
        private QuickPackItem[] _items;

        [Header("UI")]
        [SerializeField]
        private TMP_Text _instructionText;

        [SerializeField]
        private TMP_Text _nextItemText;

        [SerializeField]
        private TMP_Text _progressText;

        [SerializeField]
        private TMP_Text _mistakeText;

        [Header("기본 게임 설정")]
        [SerializeField, Min(1)]
        private int _baseItemCount = 4;

        private MinigameContext _context;

        private Action<MinigameJudgement> _onCompleted;

        private readonly List<QuickPackItem> _sequence =
            new List<QuickPackItem>();

        private int _currentIndex;
        private int _mistakeCount;

        private bool _isPlaying;
        private bool _hasFinished;

        public void Begin(
            MinigameContext context,
            Action<MinigameJudgement> onCompleted)
        {
            if (_isPlaying)
            {
                Debug.LogWarning(
                    "QuickPackMinigame은 이미 실행 중입니다.");

                return;
            }

            if (onCompleted == null)
            {
                Debug.LogError(
                    "QuickPackMinigame: 완료 콜백이 null입니다.");

                return;
            }

            if (_items == null || _items.Length == 0)
            {
                Debug.LogError(
                    "QuickPackMinigame: 군장 물품이 등록되지 않았습니다.");

                return;
            }

            _context = context;
            _onCompleted = onCompleted;

            _currentIndex = 0;
            _mistakeCount = 0;
            _hasFinished = false;
            _isPlaying = true;

            PrepareGame();
        }

        /// <summary>
        /// RoutineRunner에서 강제 종료할 때 호출한다.
        /// Abort 이후에는 완료 콜백을 호출하지 않는다.
        /// </summary>
        public void Abort()
        {
            if (!_isPlaying)
            {
                return;
            }

            _isPlaying = false;
            _hasFinished = true;

            _onCompleted = null;

            HideAllItems();

            if (_nextItemText != null)
            {
                _nextItemText.text = string.Empty;
            }
        }

        private void Update()
        {
            if (!_isPlaying)
            {
                return;
            }

            HandleMouseInput();
        }

        /// <summary>
        /// 프로젝트 공통 MouseInputManager를 이용해서
        /// 마우스 클릭을 처리한다.
        /// </summary>
        private void HandleMouseInput()
        {
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

            QuickPackItem clickedItem =
                clickedObject.GetComponent<QuickPackItem>();

            if (clickedItem == null)
            {
                clickedItem =
                    clickedObject.GetComponentInParent<QuickPackItem>();
            }

            if (clickedItem == null)
            {
                return;
            }

            HandleItemClicked(clickedItem);
        }

        private void PrepareGame()
        {
            ResetAllItems();

            int itemCount =
                CalculateItemCount(
                    _context.DifficultyTier);

            CreateSequence(
                itemCount,
                _context.RandomSeed);

            ConfigureVisibleItems();

            UpdateUI();
        }

        private int CalculateItemCount(
            int difficultyTier)
        {
            int count =
                _baseItemCount + difficultyTier;

            return Mathf.Clamp(
                count,
                1,
                _items.Length);
        }

        /// <summary>
        /// RandomSeed를 이용해서 이번 게임의
        /// 군장 물품 순서를 결정한다.
        /// </summary>
        private void CreateSequence(
            int itemCount,
            int randomSeed)
        {
            _sequence.Clear();

            List<QuickPackItem> candidates =
                new List<QuickPackItem>(_items);

            System.Random random =
                new System.Random(randomSeed);

            for (int i = 0; i < itemCount; i++)
            {
                int randomIndex =
                    random.Next(candidates.Count);

                QuickPackItem selected =
                    candidates[randomIndex];

                candidates.RemoveAt(randomIndex);

                _sequence.Add(selected);
            }
        }

        private void ConfigureVisibleItems()
        {
            for (int i = 0; i < _items.Length; i++)
            {
                QuickPackItem item = _items[i];

                if (item == null)
                {
                    continue;
                }

                item.Hide();
            }

            for (int i = 0; i < _sequence.Count; i++)
            {
                _sequence[i].Show();
            }
        }

        private void ResetAllItems()
        {
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i] == null)
                {
                    continue;
                }

                _items[i].ResetItem();
            }
        }

        private void HandleItemClicked(
            QuickPackItem clickedItem)
        {
            if (!_isPlaying)
            {
                return;
            }

            if (_hasFinished)
            {
                return;
            }

            if (!clickedItem.IsInteractable)
            {
                return;
            }

            if (_currentIndex >= _sequence.Count)
            {
                return;
            }

            QuickPackItem expectedItem =
                _sequence[_currentIndex];

            if (clickedItem != expectedItem)
            {
                HandleWrongItem();
                return;
            }

            HandleCorrectItem(clickedItem);
        }

        private void HandleCorrectItem(
            QuickPackItem item)
        {
            item.Hide();

            _currentIndex++;

            UpdateUI();

            if (_currentIndex >= _sequence.Count)
            {
                CompleteGame();
            }
        }

        private void HandleWrongItem()
        {
            _mistakeCount++;

            UpdateUI();

            Debug.Log(
                $"[QuickPack] 잘못된 물품! " +
                $"실수 횟수 = {_mistakeCount}");
        }

        private void CompleteGame()
        {
            if (_hasFinished)
            {
                return;
            }

            _hasFinished = true;
            _isPlaying = false;

            Action<MinigameJudgement> callback =
                _onCompleted;

            _onCompleted = null;

            callback?.Invoke(MinigameJudgement.Success);
        }

        private void UpdateUI()
        {
            if (_instructionText != null)
            {
                _instructionText.text =
                    "신속하게 군장을 싸라!";
            }

            if (_nextItemText != null)
            {
                if (_currentIndex >= _sequence.Count)
                {
                    _nextItemText.text = "완료!";
                }
                else
                {
                    QuickPackItem nextItem =
                        _sequence[_currentIndex];

                    _nextItemText.text =
                        $"다음 : {nextItem.ItemId}";
                }
            }

            if (_progressText != null)
            {
                _progressText.text =
                    $"{_currentIndex} / {_sequence.Count}";
            }

            if (_mistakeText != null)
            {
                _mistakeText.text =
                    $"실수 : {_mistakeCount}";
            }
        }

        private void HideAllItems()
        {
            for (int i = 0; i < _items.Length; i++)
            {
                if (_items[i] == null)
                {
                    continue;
                }

                _items[i].Hide();
            }
        }
    }
}
