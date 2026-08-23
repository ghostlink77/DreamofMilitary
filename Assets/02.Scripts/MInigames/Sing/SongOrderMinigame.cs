using System;
using System.Collections.Generic;
using DreamOfMilitary.Routine;
using System;
using System.Collections.Generic;
using DreamOfMilitary.Routine;
using UnityEngine;

/// <summary>
/// 여러 군가 중 한 곡의 구절을 섞어 표시하고, 첫 구절부터 순서대로 선택하게 한다.
/// 곡마다 구절 수가 달라도 되며, 필요한 만큼 버튼을 표시한다.
/// </summary>
public sealed class SongOrderMinigame : MonoBehaviour, IMinigame
{
    [Serializable]
    public sealed class SongLyrics
    {
        [TextArea(1, 3)]
        public string[] phrases;

        public string[] Phrases => phrases;
    }

    [Header("군가 목록 (곡마다 구절 수를 자유롭게 설정)")]
    [SerializeField]
    private SongLyrics[] songs =
    {
        new SongLyrics
        {
            phrases = new[]
            {
                "백두산 정기 뻗은 삼천리 강산",
                "무궁화 대한은 온누리의 빛",
                "화랑의 핏줄타고 자라난 우리",
                "그 이름 용감하다 대한 육군",
                "앞으로 앞으로 용진 또 용진",
                "우리는 영원한 조국의 방패"
            }
        },
        new SongLyrics
        {
            phrases = new[]
            {
                "겨레의 늠름한 아들로 태어나",
                "조국을 지키는 보람찬 길에서",
                "우리는 젊음을 함께 사르며",
                "깨끗이 피고 질 무궁화 꽃이다"
            }
        },
        new SongLyrics
        {
            phrases = new[]
            {
                "높은 산 깊은 골 적막한 산하",
                "눈 내린 전선을 우리는 간다",
                "젊은 넋 숨져간 그 때 그 자리",
                "상처 입은 노송(老松)은 말을 잊었네",
                "전우여 들리는가 그 성난 목소리",
                "전우여 보이는가 한 맺힌 눈동자"
            }
        },
        new SongLyrics
        {
            phrases = new[]
            {
                "사나이로 태어나서 할 일도 많다만",
                "너와 나 나라지키는 영광에 살았다",
                "전투와 전투 속에 맺어진 전우야",
                "산봉우리에 해 뜨고 해가 질 적에",
                "부모형제 나를 믿고 단잠을 이룬다"
            }
        }
    };

    [Header("처음부터 씬에 배치한 구절 버튼")]
    [SerializeField] private SongPhraseButton[] phraseButtons;

    [Header("버튼이 부족할 때 자동 생성")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private SongPhraseButton buttonPrefab;

    private readonly List<SongPhraseButton> activePhraseButtons = new List<SongPhraseButton>();
    private readonly List<SongPhraseButton> buttonPool = new List<SongPhraseButton>();
    private readonly List<SongPhraseButton> spawnedButtons = new List<SongPhraseButton>();
    private Action<MinigameJudgement> onCompleted;
    private int nextRequiredOrder;
    private bool isRunning;

    public void Begin(MinigameContext context, Action<MinigameJudgement> completed)
    {
        if (completed == null)
        {
            throw new ArgumentNullException(nameof(completed));
        }

        if (!TryGetRandomSong(out var song))
        {
            Debug.LogError("[SongOrder] 구절이 있는 군가를 Songs에 하나 이상 등록해야 합니다.", this);
            completed(MinigameJudgement.Failure);
            return;
        }

        if (!PrepareButtons(song.Phrases.Length))
        {
            Debug.LogError("[SongOrder] 버튼이 부족합니다. Button Prefab과 Button Container를 지정하세요.", this);
            completed(MinigameJudgement.Failure);
            return;
        }

        onCompleted = completed;
        isRunning = true;
        nextRequiredOrder = 0;
        ShowSong(song.Phrases);
    }

    private void Update()
    {
        if (!isRunning || MouseInputManager.Instance == null || !MouseInputManager.Instance.IsClickDown())
        {
            return;
        }

        var clickedObject = MouseInputManager.Instance.GetClickedObject();
        if (clickedObject == null)
        {
            return;
        }

        var phraseButton = clickedObject.GetComponentInParent<SongPhraseButton>();
        if (phraseButton == null || !phraseButton.IsUsable || !activePhraseButtons.Contains(phraseButton))
        {
            return;
        }

        if (phraseButton.PhraseOrder != nextRequiredOrder)
        {
            Complete(MinigameJudgement.Failure);
            return;
        }

        phraseButton.SetUsed();
        nextRequiredOrder++;

        if (nextRequiredOrder >= activePhraseButtons.Count)
        {
            Complete(MinigameJudgement.Success);
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
    }

    private bool TryGetRandomSong(out SongLyrics song)
    {
        var validSongs = new List<SongLyrics>();
        if (songs != null)
        {
            for (var index = 0; index < songs.Length; index++)
            {
                if (songs[index] != null && songs[index].Phrases != null && songs[index].Phrases.Length > 0)
                {
                    validSongs.Add(songs[index]);
                }
            }
        }

        if (validSongs.Count == 0)
        {
            song = null;
            return false;
        }

        song = validSongs[UnityEngine.Random.Range(0, validSongs.Count)];
        return true;
    }

    private bool PrepareButtons(int requiredButtonCount)
    {
        buttonPool.Clear();
        var uniqueButtons = new HashSet<SongPhraseButton>();

        if (phraseButtons != null)
        {
            for (var index = 0; index < phraseButtons.Length; index++)
            {
                var button = phraseButtons[index];
                if (button != null && uniqueButtons.Add(button))
                {
                    buttonPool.Add(button);
                }
            }
        }

        for (var index = 0; index < spawnedButtons.Count; index++)
        {
            var button = spawnedButtons[index];
            if (button != null && uniqueButtons.Add(button))
            {
                buttonPool.Add(button);
            }
        }

        while (buttonPool.Count < requiredButtonCount)
        {
            if (buttonPrefab == null || buttonContainer == null)
            {
                return false;
            }

            var button = Instantiate(buttonPrefab, buttonContainer);
            button.gameObject.SetActive(true);
            buttonPool.Add(button);
            spawnedButtons.Add(button);
        }

        activePhraseButtons.Clear();
        for (var index = 0; index < buttonPool.Count; index++)
        {
            var button = buttonPool[index];
            var isNeeded = index < requiredButtonCount;
            button.gameObject.SetActive(isNeeded);
            if (isNeeded)
            {
                activePhraseButtons.Add(button);
            }
        }

        return true;
    }

    private void ShowSong(string[] phrases)
    {
        var phraseOrders = new List<int>();
        for (var index = 0; index < phrases.Length; index++)
        {
            phraseOrders.Add(index);
        }

        for (var index = phraseOrders.Count - 1; index > 0; index--)
        {
            var randomIndex = UnityEngine.Random.Range(0, index + 1);
            var temporary = phraseOrders[index];
            phraseOrders[index] = phraseOrders[randomIndex];
            phraseOrders[randomIndex] = temporary;
        }

        for (var index = 0; index < activePhraseButtons.Count; index++)
        {
            var phraseOrder = phraseOrders[index];
            activePhraseButtons[index].SetPhrase(phrases[phraseOrder], phraseOrder);
        }
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

    private void OnDisable()
    {
        Abort();
    }
}

