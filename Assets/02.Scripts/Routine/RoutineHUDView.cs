using DreamOfMilitary.Routine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoutineHUDView : MonoBehaviour
{
    [SerializeField] private RoutineRunner routineRunner;
    [SerializeField] private TextMeshProUGUI commandText;
    [SerializeField] private Slider timeSlider;

    [Header("임시 테스트")]
    [SerializeField] private Button routineStartButton;
    [SerializeField] private MinigameDef testMinigame;
    [SerializeField] private int testSeed = 12345;

    private void Awake()
    {
        timeSlider.minValue = 0;
        timeSlider.maxValue = 1;
        timeSlider.wholeNumbers = false;
        timeSlider.interactable = false;
        timeSlider.value = 0;
    }

    private void OnEnable()
    {
        routineRunner.StateChanged += OnStateChanged;
        routineRunner.CommandShown += OnCommandShown;
        routineRunner.TimeNormalizedChanged += OnTimeNormalizedChanged;

        if (routineStartButton != null)
        {
            routineStartButton.onClick.AddListener(OnRoutineStartClicked);
        }

        OnStateChanged(routineRunner.State);
    }

    private void OnDisable()
    {
        routineRunner.StateChanged -= OnStateChanged;
        routineRunner.CommandShown -= OnCommandShown;
        routineRunner.TimeNormalizedChanged -= OnTimeNormalizedChanged;

        if (routineStartButton != null)
        {
            routineStartButton.onClick.RemoveListener(OnRoutineStartClicked);
        }
    }

    private void OnRoutineStartClicked()
    {
        if (routineRunner.IsRunning || testMinigame == null)
        {
            return;
        }
        routineRunner.StartRoutine(new[] { testMinigame }, testSeed);
    }

    private void OnCommandShown(string command, int current, int total)
    {
        commandText.text = command;
    }

    private void OnTimeNormalizedChanged(float normalizedTime)
    {
        timeSlider.value = Mathf.Clamp01(normalizedTime);
    }

    private void OnStateChanged(RoutineRunState state)
    {
        bool visible =
            state != RoutineRunState.Idle &&
            state != RoutineRunState.Completed;

        commandText.gameObject.SetActive(visible);
        timeSlider.gameObject.SetActive(state == RoutineRunState.Playing);

        if (routineStartButton != null)
        {
            routineStartButton.interactable =
                state == RoutineRunState.Idle ||
                state == RoutineRunState.Completed;
        }
    }
}
