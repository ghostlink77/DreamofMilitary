using DreamOfMilitary.Routine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoutineHUDView : MonoBehaviour
{
    [SerializeField] private RoutineRunner routineRunner;
    [SerializeField] private TextMeshProUGUI commandText;
    [SerializeField] private Slider timeSlider;

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
        OnStateChanged(routineRunner.State);
    }

    private void OnDisable()
    {
        routineRunner.StateChanged -= OnStateChanged;
        routineRunner.CommandShown -= OnCommandShown;
        routineRunner.TimeNormalizedChanged -= OnTimeNormalizedChanged;
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
        var commandVisible = state == RoutineRunState.ShowingCommand || state == RoutineRunState.Playing;
        var timeSliderVisible = state == RoutineRunState.Playing || state == RoutineRunState.ShowingProgress;

        commandText.gameObject.SetActive(commandVisible);
        timeSlider.gameObject.SetActive(timeSliderVisible);
    }
}
