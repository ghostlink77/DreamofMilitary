using System;
using System.Collections.Generic;
using DreamOfMilitary.Routine;
using DreamOfMilitary.Audio;
using TMPro;
using UnityEngine;

/// <summary>
/// 등록된 인물 중 필요한 인원을 중복 없이 무작위로 뽑아,
/// 병사에게는 좌클릭 인사, 간부에게는 우클릭 경례를 하는 미니게임이다.
/// </summary>
public sealed class SayHiMinigame : MonoBehaviour, IMinigame
{
    private int RequiredCharacterCount;

    [Header("등장 후보 인물")]
    [SerializeField] private SayHiCharacter[] characters;

    [Header("성공 말풍선")]
    [SerializeField] private GameObject successTextBubble;
    [SerializeField] private TMP_Text successText;

    [Header("간부 성공 문구 (3개)")]
    [SerializeField] private string[] officerSuccessTexts = new string[3];

    [Header("병사 성공 문구 (3개)")]
    [SerializeField] private string[] soldierSuccessTexts = new string[3];

    [Header("성공 모션 표시 시간")]
    [SerializeField, Min(0f)] private float successDisplayDuration = 0.7f;

    [Header("다음 인물 등장 전환 시간")]
    [SerializeField, Min(0f)] private float nextCharacterDelay = 0.2f;

    private Action<MinigameJudgement> onCompleted;

    private readonly List<SayHiCharacter> selectedCharacters =
        new List<SayHiCharacter>();

    private int currentCharacterIndex;
    private float nextCharacterAt;

    private bool isRunning;
    private bool isShowingSuccess;

    public void Begin(
        MinigameContext context,
        Action<MinigameJudgement> completed)
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
            Debug.LogError(
                $"[SayHi] Characters에 중복되지 않는 인물 {RequiredCharacterCount}명 이상을 등록해야 합니다.",
                this);

            completed(MinigameJudgement.Failure);
            return;
        }

        onCompleted = completed;

        isRunning = true;
        isShowingSuccess = false;

        currentCharacterIndex = 0;
        nextCharacterAt = 0f;

        // 말풍선은 게임 시작 시 꺼져 있어야 함
        HideSuccessBubble();

        // 모든 캐릭터의 Success 상태 초기화
        ResetAllSuccessStates();

        PickRandomCharacters();

        SetOnlyCurrentCharacterActive();
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        // 성공 모션을 보여주는 동안에는 입력을 받지 않는다.
        if (isShowingSuccess)
        {
            if (Time.time >= nextCharacterAt)
            {
                FinishCurrentSuccess();
            }

            return;
        }

        if (Time.time < nextCharacterAt)
        {
            return;
        }

        if (MouseInputManager.Instance != null &&
            MouseInputManager.Instance.IsClickDown())
        {
            // 좌클릭 = 병사에게 인사
            JudgeInput(SayHiCharacter.Rank.Soldier);
            return;
        }

        if (MouseInputManager.Instance != null &&
            MouseInputManager.Instance.IsRightClickDown())
        {
            // 우클릭 = 간부에게 경례
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
        isShowingSuccess = false;

        onCompleted = null;

        selectedCharacters.Clear();

        HideSuccessBubble();
        ResetAllSuccessStates();
        HideAllCharacters();
    }

    private void JudgeInput(SayHiCharacter.Rank inputRank)
    {
        SayHiCharacter currentCharacter =
            selectedCharacters[currentCharacterIndex];

        // 잘못된 입력
        if (inputRank != currentCharacter.CharacterRank)
        {
            Debug.Log("실패");

            Complete(MinigameJudgement.Failure);
            return;
        }

        // 정답!
        Debug.Log(
            $"성공 - {currentCharacter.CharacterRank}");

        ShowSuccess(currentCharacter);

        // 성공 모션을 보여주는 동안 입력을 막는다.
        isShowingSuccess = true;

        nextCharacterAt =
            Time.time + successDisplayDuration;
    }

    /// <summary>
    /// 정답을 맞췄을 때 Success 오브젝트와 말풍선을 보여준다.
    /// </summary>
    private void ShowSuccess(SayHiCharacter character)
    {
        // 현재 캐릭터의 Success 모션 켜기
        character.SetSuccessVisible(true);
       
        GameAudioController.Instance?.Playsuccess();
        // 말풍선 켜기
        if (successTextBubble != null)
        {
            successTextBubble.SetActive(true);
        }

        // 계급에 맞는 성공 문구 배열 선택
        string[] texts = character.CharacterRank switch
        {
            SayHiCharacter.Rank.Officer => officerSuccessTexts,
            SayHiCharacter.Rank.Soldier => soldierSuccessTexts,
            _ => null
        };

        // 문구가 등록되어 있다면 랜덤으로 하나 선택
        if (successText != null &&
            texts != null &&
            texts.Length > 0)
        {
            List<string> validTexts = new List<string>();

            foreach (string text in texts)
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    validTexts.Add(text);
                }
            }

            if (validTexts.Count > 0)
            {
                int randomIndex =
                    UnityEngine.Random.Range(0, validTexts.Count);

                successText.text = validTexts[randomIndex];
            }
        }
    }

    /// <summary>
    /// 성공 모션을 보여준 후 다음 캐릭터로 넘어간다.
    /// </summary>
    private void FinishCurrentSuccess()
    {
        isShowingSuccess = false;

        // 현재 캐릭터의 성공 모션 끄기
        selectedCharacters[currentCharacterIndex]
            .SetSuccessVisible(false);

        // 말풍선 끄기
        HideSuccessBubble();

        // 마지막 캐릭터인지 확인
        currentCharacterIndex++;

        if (currentCharacterIndex >= RequiredCharacterCount)
        {
            Debug.Log("미니게임 성공");

            Complete(MinigameJudgement.Success);
            return;
        }

        // 다음 캐릭터 등장
        nextCharacterAt =
            Time.time + nextCharacterDelay;

        SetOnlyCurrentCharacterActive();
    }

    private bool HasValidCharacterSetup()
    {
        if (characters == null ||
            characters.Length < RequiredCharacterCount)
        {
            return false;
        }

        var uniqueCharacters =
            new HashSet<SayHiCharacter>();

        for (int index = 0;
             index < characters.Length;
             index++)
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

        var candidates =
            new List<SayHiCharacter>();

        var uniqueCharacters =
            new HashSet<SayHiCharacter>();

        for (int index = 0;
             index < characters.Length;
             index++)
        {
            SayHiCharacter character =
                characters[index];

            if (character != null &&
                uniqueCharacters.Add(character))
            {
                candidates.Add(character);
            }
        }

        // Fisher-Yates Shuffle
        for (int index = candidates.Count - 1;
             index > 0;
             index--)
        {
            int randomIndex =
                UnityEngine.Random.Range(0, index + 1);

            SayHiCharacter temporary =
                candidates[index];

            candidates[index] =
                candidates[randomIndex];

            candidates[randomIndex] =
                temporary;
        }

        for (int index = 0;
             index < RequiredCharacterCount;
             index++)
        {
            selectedCharacters.Add(
                candidates[index]);
        }
    }

    /// <summary>
    /// 현재 캐릭터만 화면에 보이게 한다.
    /// </summary>
    private void SetOnlyCurrentCharacterActive()
    {
        HideAllCharacters();

        if (selectedCharacters.Count == 0)
        {
            return;
        }

        SayHiCharacter currentCharacter =
            selectedCharacters[currentCharacterIndex];

        currentCharacter.gameObject.SetActive(true);

        // 기본 상태에서는 Success 모션이 꺼져 있어야 한다.
        currentCharacter.SetSuccessVisible(false);
    }

    private void HideAllCharacters()
    {
        if (characters == null)
        {
            return;
        }

        for (int index = 0;
             index < characters.Length;
             index++)
        {
            if (characters[index] != null)
            {
                characters[index].SetSuccessVisible(false);
                characters[index].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 모든 캐릭터의 Success 오브젝트를 끈다.
    /// </summary>
    private void ResetAllSuccessStates()
    {
        if (characters == null)
        {
            return;
        }

        for (int index = 0;
             index < characters.Length;
             index++)
        {
            if (characters[index] != null)
            {
                characters[index].SetSuccessVisible(false);
            }
        }
    }

    private void HideSuccessBubble()
    {
        if (successTextBubble != null)
        {
            successTextBubble.SetActive(false);
        }
    }

    private void Complete(MinigameJudgement judgement)
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        isShowingSuccess = false;

        HideSuccessBubble();
        ResetAllSuccessStates();
        HideAllCharacters();

        Action<MinigameJudgement> completed =
            onCompleted;

        onCompleted = null;

        selectedCharacters.Clear();

        completed?.Invoke(judgement);
    }

    private void OnDisable()
    {
        Abort();
    }
}