using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyMenuController : MonoBehaviour
{
    [SerializeField] private Button stageButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject settingBlackBack;
    [SerializeField] private Slider pointSlider;
    [SerializeField] private Text pointText;
    [SerializeField] private Text stageButtonText;
    [SerializeField] private Sprite promotionExamButtonSprite;
    [SerializeField] private string miniGameSceneName = "SampleMiniGameScene";

    private bool _canTakeExam;

    private void Awake()
    {
        settingBlackBack.SetActive(false);
        stageButton.onClick.AddListener(OnStageButtonClicked);
        settingButton.onClick.AddListener(OpenSettings);
        exitButton.onClick.AddListener(CloseSettings);
    }

    private void Start()
    {
        var routineFlow = DreamOfMilitary.Routine.RoutineFlowController.Instance;
        _canTakeExam = routineFlow.RefreshLobbyPointUI(pointSlider, pointText);

        if (_canTakeExam)
        {
            stageButton.image.sprite = promotionExamButtonSprite;
            stageButtonText.text = routineFlow.IsDischargeExam() ? "전역 심사" : "진급 심사";
        }
    }

    private void OnDestroy()
    {
        stageButton.onClick.RemoveListener(OnStageButtonClicked);
        settingButton.onClick.RemoveListener(OpenSettings);
        exitButton.onClick.RemoveListener(CloseSettings);
    }

    private void OnStageButtonClicked()
    {
        var routineFlow = DreamOfMilitary.Routine.RoutineFlowController.Instance;

        if (_canTakeExam)
        {
            routineFlow.BeginExam(miniGameSceneName);
        }
        else
        {
            routineFlow.BeginRoutine(miniGameSceneName);
        }
    }

    private void OpenSettings()
    {
        settingBlackBack.SetActive(true);
    }

    private void CloseSettings()
    {
        settingBlackBack.SetActive(false);
    }
}