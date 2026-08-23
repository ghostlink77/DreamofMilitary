using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamOfMilitary.Routine
{
    public readonly struct RoutineResultData
    {
        public int SuccessCount { get; }
        public int FailureCount { get; }
        public int BasePoints { get; }
        public int PerfectBonusPoints { get; }
        public int PreviousTotalPoints { get; }
        public int CurrentTotalPoints { get; }
        public int RequiredPoints { get; }

        public RoutineResultData(int successCount, int failureCount, int basePoints, int perfectBonusPoints,
            int previousTotalPoints, int currentTotalPoints, int requiredPoints)
        {
            SuccessCount = successCount;
            FailureCount = failureCount;
            BasePoints = basePoints;
            PerfectBonusPoints = perfectBonusPoints;
            PreviousTotalPoints = previousTotalPoints;
            CurrentTotalPoints = currentTotalPoints;
            RequiredPoints = requiredPoints;
        }
    }

    [DisallowMultipleComponent]
    public sealed class RoutineResultView : MonoBehaviour
    {
        [Header("결과 화면")]
        [SerializeField] private GameObject resultRoot;

        [Header("성공 및 실패")]
        [SerializeField] private GameObject successFailureGroup;
        [SerializeField] private TextMeshProUGUI successCountText;
        [SerializeField] private TextMeshProUGUI failureCountText;

        [Header("획득 상점")]
        [SerializeField] private GameObject basePointsGroup;
        [SerializeField] private TextMeshProUGUI basePointsText;

        [Header("퍼펙트 보너스")]
        [SerializeField] private GameObject perfectBonusGroup;
        [SerializeField] private TextMeshProUGUI perfectBonusText;

        [Header("다음 심사 진행도")]
        [SerializeField] private GameObject promotionProgressGroup;
        [SerializeField] private Slider promotionProgressSlider;
        [SerializeField] private TextMeshProUGUI promotionProgressText;

        [Header("계속")]
        [SerializeField] private Button continueButton;

        [Header("연출 시간")]
        [SerializeField, Min(0f)] private float revealIntervalSeconds = 0.35f;
        [SerializeField, Min(0f)] private float progressAnimationSeconds = 0.5f;

        private Coroutine _revealCoroutine;
        private Action _onContinue;

        private void Awake()
        {
            ValidateReferences();
            continueButton.onClick.AddListener(OnContinueClicked);
            resultRoot.SetActive(false);
        }

        private void OnDisable()
        {
            StopReveal();
            _onContinue = null;

            if (resultRoot != null)
            {
                resultRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }
        }

        public void Show(RoutineResultData data, Action onContinue)
        {
            if (onContinue == null)
            {
                throw new ArgumentNullException(nameof(onContinue));
            }

            StopReveal();
            _onContinue = onContinue;

            successCountText.text = $"성공 : {data.SuccessCount}";
            failureCountText.text = $"실패 : {data.FailureCount}";
            basePointsText.text = $"획득 상점 : {data.BasePoints}";
            perfectBonusText.text = $"일과 퍼펙트 보너스 : {data.PerfectBonusPoints}";

            var requiredPoints = Mathf.Max(1, data.RequiredPoints);

            promotionProgressSlider.minValue = 0f;
            promotionProgressSlider.maxValue = requiredPoints;
            promotionProgressSlider.wholeNumbers = true;
            promotionProgressSlider.interactable = false;

            UpdatePromotionProgress(data.PreviousTotalPoints, requiredPoints);

            successFailureGroup.SetActive(false);
            basePointsGroup.SetActive(false);
            perfectBonusGroup.SetActive(false);
            promotionProgressGroup.SetActive(false);

            continueButton.interactable = false;
            continueButton.gameObject.SetActive(false);

            resultRoot.SetActive(true);
            _revealCoroutine = StartCoroutine(RevealSequence(data));
        }

        private IEnumerator RevealSequence(RoutineResultData data)
        {
            successFailureGroup.SetActive(true);
            yield return WaitForRevealInterval();

            basePointsGroup.SetActive(true);
            yield return WaitForRevealInterval();

            perfectBonusGroup.SetActive(true);
            yield return WaitForRevealInterval();

            promotionProgressGroup.SetActive(true);
            yield return AnimatePromotionProgress(data);

            continueButton.gameObject.SetActive(true);
            continueButton.interactable = true;

            _revealCoroutine = null;
        }

        private IEnumerator WaitForRevealInterval()
        {
            if (revealIntervalSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(revealIntervalSeconds);
            }
        }

        private IEnumerator AnimatePromotionProgress(RoutineResultData data)
        {
            var requiredPoints = Mathf.Max(1, data.RequiredPoints);

            if (progressAnimationSeconds <= 0f || data.PreviousTotalPoints == data.CurrentTotalPoints)
            {
                UpdatePromotionProgress(data.CurrentTotalPoints, requiredPoints);
                yield break;
            }

            var elapsedSeconds = 0f;

            while (elapsedSeconds < progressAnimationSeconds)
            {
                elapsedSeconds += Time.unscaledDeltaTime;
                var normalized = Mathf.Clamp01(elapsedSeconds / progressAnimationSeconds);
                var displayedPoints = Mathf.RoundToInt(Mathf.Lerp(data.PreviousTotalPoints, data.CurrentTotalPoints, normalized));

                UpdatePromotionProgress(displayedPoints, requiredPoints);
                yield return null;
            }

            UpdatePromotionProgress(data.CurrentTotalPoints, requiredPoints);
        }

        private void UpdatePromotionProgress(int displayedPoints, int requiredPoints)
        {
            promotionProgressSlider.value = Mathf.Clamp(displayedPoints, 0, requiredPoints);
            promotionProgressText.text = $"{displayedPoints} / {requiredPoints}";
        }

        private void OnContinueClicked()
        {
            if (!continueButton.interactable)
            {
                return;
            }

            continueButton.interactable = false;

            var callback = _onContinue;
            _onContinue = null;

            callback?.Invoke();
        }

        private void StopReveal()
        {
            if (_revealCoroutine == null)
            {
                return;
            }

            StopCoroutine(_revealCoroutine);
            _revealCoroutine = null;
        }

        private void ValidateReferences()
        {
            if (resultRoot != null && successFailureGroup != null && successCountText != null &&
                failureCountText != null && basePointsGroup != null && basePointsText != null &&
                perfectBonusGroup != null && perfectBonusText != null && promotionProgressGroup != null &&
                promotionProgressSlider != null && promotionProgressText != null && continueButton != null)
            {
                return;
            }

            throw new InvalidOperationException("RoutineResultView의 UI 참조가 모두 연결되어야 합니다.");
        }
    }
}
