using System;
using System.Collections.Generic;
using DreamOfMilitary.Routine;
using UnityEngine;

/// <summary>
/// 등록된 인물 중 다섯 명을 매번 중복 없이 무작위로 뽑아,
/// 병사에게는 좌클릭 인사, 간부에게는 우클릭 경례를 하는 미니게임이다.
/// 시간 초과는 RoutineRunner의 기본 실패 처리에 맡긴다.
/// </summary>
public sealed class SayHiMinigame : MonoBehaviour, IMinigame
{
    //private const int RequiredCharacterCount = 5;
    private int RequiredCharacterCount;

    [Header("등장 후보 인물 (11명 모두 등록)")]
    [SerializeField] private SayHiCharacter[] characters;

    [Header("선택 사항: 등장 전환 효과")]
    [SerializeField, Min(0f)] private float nextCharacterDelay;

    private Action<MinigameJudgement> onCompleted;
    //private readonly List<SayHiCharacter> selectedCharacters = new List<SayHiCharacter>(RequiredCharacterCount);
    private readonly List<SayHiCharacter> selectedCharacters = new List<SayHiCharacter>();
    private int currentCharacterIndex;
    private float nextCharacterAt;
    private bool isRunning;

    public void Begin(MinigameContext context, Action<MinigameJudgement> completed)
    {
        if (completed == null)
        {
            throw new ArgumentNullException(nameof(completed));
        }

        RequiredCharacterCount = context.DifficultyTier switch
        {
            1 => 5,
            2 => 7,
            _ => throw new ArgumentOutOfRangeException(
                nameof(context.DifficultyTier),
                context.DifficultyTier,
                "난이도는 1, 2 중 하나여야 합니다.")
        };

        if (!HasValidCharacterSetup())
        {
            Debug.LogError("[SayHi] Characters에 중복되지 않는 인물 {requiredCharacterCount}명 이상을 등록해야 합니다.", this);
           // Debug.LogError("[SayHi] Characters에 중복되지 않는 인물 5명 이상을 등록해야 합니다.", this);
            completed(MinigameJudgement.Failure);
            return;
        }

        onCompleted = completed;
        isRunning = true;
        currentCharacterIndex = 0;
        nextCharacterAt = 0f;
        PickRandomCharacters();
        SetOnlyCurrentCharacterActive();
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        if (Time.time < nextCharacterAt)
        {
            return;
        }

        // MouseInputManager가 좌클릭의 UI/Raycast 처리를 통일한다.
        if (MouseInputManager.Instance != null && MouseInputManager.Instance.IsClickDown())
        {
            JudgeInput(SayHiCharacter.Rank.Soldier);
            return;
        }


        if (MouseInputManager.Instance != null && MouseInputManager.Instance.IsRightClickDown())
        {
            JudgeInput(SayHiCharacter.Rank.Officer);
        }
    }

    public void Abort()
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        onCompleted = null;
        selectedCharacters.Clear();
        HideAllCharacters();
    }

    private void JudgeInput(SayHiCharacter.Rank inputRank)
    {
        if (inputRank != selectedCharacters[currentCharacterIndex].CharacterRank)
        {
            Debug.Log("실패");
            Complete(MinigameJudgement.Failure);
            return;
        }

        currentCharacterIndex++;
        if (currentCharacterIndex >= RequiredCharacterCount)
        {
            Debug.Log("성공");
            Complete(MinigameJudgement.Success);
            return;
        }

        nextCharacterAt = Time.time + nextCharacterDelay;
        SetOnlyCurrentCharacterActive();
    }

    private bool HasValidCharacterSetup()
    {
        if (characters == null || characters.Length < RequiredCharacterCount)
        {
            return false;
        }

        var uniqueCharacters = new HashSet<SayHiCharacter>();
        for (var index = 0; index < characters.Length; index++)
        {
            if (characters[index] != null)
            {
                uniqueCharacters.Add(characters[index]);
            }
        }

        return uniqueCharacters.Count >= RequiredCharacterCount;
    }

    private void PickRandomCharacters()
    {
        selectedCharacters.Clear();
        var candidates = new List<SayHiCharacter>();
        var uniqueCharacters = new HashSet<SayHiCharacter>();

        for (var index = 0; index < characters.Length; index++)
        {
            var character = characters[index];
            if (character != null && uniqueCharacters.Add(character))
            {
                candidates.Add(character);
            }
        }

        // Fisher-Yates 방식으로 섞은 뒤 앞의 다섯 명을 사용한다.
        for (var index = candidates.Count - 1; index > 0; index--)
        {
            var randomIndex = UnityEngine.Random.Range(0, index + 1);
            var temporary = candidates[index];
            candidates[index] = candidates[randomIndex];
            candidates[randomIndex] = temporary;
        }

        for (var index = 0; index < RequiredCharacterCount; index++)
        {
            selectedCharacters.Add(candidates[index]);
        }
    }

    private void SetOnlyCurrentCharacterActive()
    {
        for (var index = 0; index < characters.Length; index++)
        {
            if (characters[index] != null)
            {
                characters[index].gameObject.SetActive(false);
            }
        }

        selectedCharacters[currentCharacterIndex].gameObject.SetActive(true);
    }

    private void HideAllCharacters()
    {
        if (characters == null)
        {
            return;
        }

        for (var index = 0; index < characters.Length; index++)
        {
            if (characters[index] != null)
            {
                characters[index].gameObject.SetActive(false);
            }
        }
    }

    private void Complete(MinigameJudgement judgement)
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        HideAllCharacters();

        var completed = onCompleted;
        onCompleted = null;
        selectedCharacters.Clear();
        completed?.Invoke(judgement);
    }

    private void OnDisable()
    {
        Abort();
    }
}

