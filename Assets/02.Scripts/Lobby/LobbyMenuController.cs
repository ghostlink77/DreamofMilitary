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

    private void Awake()
    {
        settingBlackBack.SetActive(false);
        stageButton.onClick.AddListener(BeginRoutine);
        settingButton.onClick.AddListener(OpenSettings);
        exitButton.onClick.AddListener(CloseSettings);
    }

    private void Start()
    {
        var routineFlow = DreamOfMilitary.Routine.RoutineFlowController.Instance;
        var canStartPromotionExam = routineFlow.RefreshLobbyPointUI(pointSlider, pointText);

        if (canStartPromotionExam)
        {
            stageButton.image.sprite = promotionExamButtonSprite;
            stageButtonText.text = "진급 심사";
        }
    }

    private void OnDestroy()
    {
        stageButton.onClick.RemoveListener(BeginRoutine);
        settingButton.onClick.RemoveListener(OpenSettings);
        exitButton.onClick.RemoveListener(CloseSettings);
    }

    private void BeginRoutine()
    {
        DreamOfMilitary.Routine.RoutineFlowController.Instance.BeginRoutine(miniGameSceneName);
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