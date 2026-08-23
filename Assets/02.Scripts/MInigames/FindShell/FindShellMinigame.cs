using System;
using System.Collections.Generic;
using DreamOfMilitary.Routine;
using UnityEngine;

/// <summary>
/// 화면에 흩어진 탄피의 수를 세고, 6~12 중 알맞은 답을 선택하는 미니게임이다.
/// Shell Spawn Area는 다른 UI와 겹치지 않는 전용 RectTransform으로 지정한다.
/// </summary>
public sealed class FindShellMinigame : MonoBehaviour, IMinigame
{
    private const int MinimumShellCount = 4;
    private const int MaximumShellCount = 10;

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
            Debug.LogError("[FindShell] Spawn Area 또는 Shell Prefab이 지정되지 않았습니다.", this);
            completed(MinigameJudgement.Failure);
            return;
        }

        ClearSpawnedShells();

        onCompleted = completed;
        isRunning = true;

        var shellCount = UnityEngine.Random.Range(MinimumShellCount, MaximumShellCount + 1);
        shellRadius = GetShellBoundingRadius();
        //여기서 개수조절
        if (!TrySpawnShells(shellCount))
        {
            Debug.LogError(
                "[FindShell] 탄피를 겹치지 않게 배치할 공간이 부족합니다. " +
                "Shell Spawn Area를 키우거나 Shell Prefab/Spacing을 줄이세요.", this);
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

        var answerButton = clickedObject.GetComponentInParent<FindShellAnswerButton>();
        if (answerButton == null || !answerButton.IsAnswerable)
        {
            return;
        }

        var judgement = answerButton.AnswerCount == spawnedShells.Count
            ? MinigameJudgement.Success
            : MinigameJudgement.Failure;

        Complete(judgement);
    }

    public void Abort()
    {
        // RoutineRunner의 시간 초과 처리 후에는 어떤 클릭도 완료 콜백으로 이어지지 않는다.
        isRunning = false;
        onCompleted = null;
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

        var minimumDistance = shellRadius * 2f + shellSpacing;
        var minimumDistanceSqr = minimumDistance * minimumDistance;

        for (var attempt = 0; attempt < placementAttemptsPerShell; attempt++)
        {
            var candidate = new Vector2(
                NextFloat(minimumX, maximumX),
                NextFloat(minimumY, maximumY));

            var overlapsExistingShell = false;
            for (var index = 0; index < occupiedPositions.Count; index++)
            {
                if ((occupiedPositions[index] - candidate).sqrMagnitude < minimumDistanceSqr)
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
        size = new Vector2(Mathf.Abs(size.x * scale.x), Mathf.Abs(size.y * scale.y));

        // 회전한 직사각형도 항상 포함하는 원의 반지름이다.
        return Mathf.Sqrt(size.x * size.x + size.y * size.y) * 0.5f;
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
        for (var index = 0; index < spawnedShells.Count; index++)
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