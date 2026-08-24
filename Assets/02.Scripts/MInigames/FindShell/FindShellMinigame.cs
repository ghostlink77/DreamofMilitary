using System;
using System.Collections.Generic;
using DreamOfMilitary.Routine;
using UnityEngine;

/// <summary>
/// 화면에 흩어진 탄피의 수를 세고,
/// 난이도에 맞는 답을 선택하는 미니게임이다.
/// 
/// 난이도 1 : 탄피 4~7개 / 버튼 4,5,6,7
/// 난이도 2 : 탄피 7~10개 / 버튼 7,8,9,10
/// </summary>
public sealed class FindShellMinigame : MonoBehaviour, IMinigame
{
    [Header("배치")]
    [SerializeField] private RectTransform shellSpawnArea;
    [SerializeField] private RectTransform shellPrefab;
    [SerializeField, Min(0f)] private float shellSpacing = 8f;
    [SerializeField, Min(1)] private int placementAttemptsPerShell = 100;

    [Header("답안 버튼")]
    [SerializeField] private FindShellAnswerButton[] answerButtons;

    private readonly List<RectTransform> spawnedShells = new List<RectTransform>();
    private readonly List<Vector2> occupiedPositions = new List<Vector2>();

    private Action<MinigameJudgement> onCompleted;
    private bool isRunning;
    private float shellRadius;

    public void Begin(MinigameContext context, Action<MinigameJudgement> completed)
    {
        if (completed == null)
        {
            throw new ArgumentNullException(nameof(completed));
        }

        if (shellSpawnArea == null || shellPrefab == null)
        {
            Debug.LogError(
                "[FindShell] Spawn Area 또는 Shell Prefab이 지정되지 않았습니다.",
                this);

            completed(MinigameJudgement.Failure);
            return;
        }

        if (answerButtons == null || answerButtons.Length != 4)
        {
            Debug.LogError(
                "[FindShell] 답안 버튼은 정확히 4개가 필요합니다.",
                this);

            completed(MinigameJudgement.Failure);
            return;
        }

        ClearSpawnedShells();

        onCompleted = completed;
        isRunning = true;

        // 난이도에 따른 탄피 개수 범위 설정
        GetShellCountRange(
            context.DifficultyTier,
            out var minimumShellCount,
            out var maximumShellCount);

        // 난이도에 맞게 버튼 숫자 설정
        SetAnswerButtons(context.DifficultyTier);

        // 탄피 개수 랜덤 결정
        var shellCount = UnityEngine.Random.Range(
            minimumShellCount,
            maximumShellCount + 1);

        shellRadius = GetShellBoundingRadius();

        if (!TrySpawnShells(shellCount))
        {
            Debug.LogError(
                "[FindShell] 탄피를 겹치지 않게 배치할 공간이 부족합니다. " +
                "Shell Spawn Area를 키우거나 Shell Prefab/Spacing을 줄이세요.",
                this);

            Complete(MinigameJudgement.Failure);
        }
    }

    private void Update()
    {
        if (!isRunning || MouseInputManager.Instance == null)
        {
            return;
        }

        if (!MouseInputManager.Instance.IsClickDown())
        {
            return;
        }

        var clickedObject = MouseInputManager.Instance.GetClickedObject();

        if (clickedObject == null)
        {
            return;
        }

        var answerButton =
            clickedObject.GetComponentInParent<FindShellAnswerButton>();

        if (answerButton == null || !answerButton.IsAnswerable)
        {
            return;
        }

        var judgement =
            answerButton.AnswerCount == spawnedShells.Count
                ? MinigameJudgement.Success
                : MinigameJudgement.Failure;

        Complete(judgement);
    }

    public void Abort()
    {
        isRunning = false;
        onCompleted = null;
    }

    /// <summary>
    /// 난이도에 따라 탄피 개수 범위를 결정한다.
    /// </summary>
    private void GetShellCountRange(
        int difficultyTier,
        out int minimum,
        out int maximum)
    {
        switch (difficultyTier)
        {
            case 1:
                minimum = 4;
                maximum = 7;
                break;

            case 2:
                minimum = 7;
                maximum = 10;
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(difficultyTier),
                    difficultyTier,
                    "난이도는 1, 2 중 하나여야 합니다.");
        }
    }

    /// <summary>
    /// 난이도에 따라 UI 버튼의 답을 설정한다.
    /// 
    /// 난이도 1 → 4, 5, 6, 7
    /// 난이도 2 → 7, 8, 9, 10
    /// </summary>
    private void SetAnswerButtons(int difficultyTier)
    {
        int startNumber;

        switch (difficultyTier)
        {
            case 1:
                startNumber = 4;
                break;

            case 2:
                startNumber = 7;
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(difficultyTier),
                    difficultyTier,
                    "난이도는 1, 2 중 하나여야 합니다.");
        }

        for (int i = 0; i < answerButtons.Length; i++)
        {
            answerButtons[i].SetAnswerCount(startNumber + i);
        }
    }

    private bool TrySpawnShells(int count)
    {
        occupiedPositions.Clear();

        for (var shellIndex = 0; shellIndex < count; shellIndex++)
        {
            if (!TryGetAvailablePosition(out var position))
            {
                return false;
            }

            var shell = Instantiate(shellPrefab, shellSpawnArea);

            shell.anchorMin = new Vector2(0.5f, 0.5f);
            shell.anchorMax = new Vector2(0.5f, 0.5f);
            shell.pivot = new Vector2(0.5f, 0.5f);

            shell.anchoredPosition = position;

            shell.localRotation = Quaternion.Euler(
                0f,
                0f,
                UnityEngine.Random.Range(0f, 360f));

            shell.gameObject.SetActive(true);

            spawnedShells.Add(shell);
            occupiedPositions.Add(position);
        }

        return true;
    }

    private bool TryGetAvailablePosition(out Vector2 position)
    {
        var area = shellSpawnArea.rect;

        var minimumX = area.xMin + shellRadius;
        var maximumX = area.xMax - shellRadius;

        var minimumY = area.yMin + shellRadius;
        var maximumY = area.yMax - shellRadius;

        if (minimumX > maximumX || minimumY > maximumY)
        {
            position = default;
            return false;
        }

        var minimumDistance =
            shellRadius * 2f + shellSpacing;

        var minimumDistanceSqr =
            minimumDistance * minimumDistance;

        for (var attempt = 0;
             attempt < placementAttemptsPerShell;
             attempt++)
        {
            var candidate = new Vector2(
                NextFloat(minimumX, maximumX),
                NextFloat(minimumY, maximumY));

            var overlapsExistingShell = false;

            for (var index = 0;
                 index < occupiedPositions.Count;
                 index++)
            {
                if ((occupiedPositions[index] - candidate).sqrMagnitude
                    < minimumDistanceSqr)
                {
                    overlapsExistingShell = true;
                    break;
                }
            }

            if (!overlapsExistingShell)
            {
                position = candidate;
                return true;
            }
        }

        position = default;
        return false;
    }

    private float GetShellBoundingRadius()
    {
        var size = shellPrefab.rect.size;
        var scale = shellPrefab.localScale;

        size = new Vector2(
            Mathf.Abs(size.x * scale.x),
            Mathf.Abs(size.y * scale.y));

        return Mathf.Sqrt(
            size.x * size.x +
            size.y * size.y) * 0.5f;
    }

    private float NextFloat(float minimum, float maximum)
    {
        return UnityEngine.Random.Range(minimum, maximum);
    }

    private void Complete(MinigameJudgement judgement)
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;

        var completed = onCompleted;
        onCompleted = null;

        completed?.Invoke(judgement);
    }

    private void ClearSpawnedShells()
    {
        for (var index = 0;
             index < spawnedShells.Count;
             index++)
        {
            if (spawnedShells[index] != null)
            {
                Destroy(spawnedShells[index].gameObject);
            }
        }

        spawnedShells.Clear();
        occupiedPositions.Clear();
    }

    private void OnDisable()
    {
        Abort();
    }
}