using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DreamOfMilitary.Routine
{
    [DisallowMultipleComponent]
    public sealed class RoutineExamResultView : MonoBehaviour
    {
        [SerializeField] private GameObject resultRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Button continueButton;
        [SerializeField, Min(0f)] private float continueButtonDelaySeconds = 1f;

        private Coroutine _showCoroutine;
        private Action _onContinue;

        private void Awake()
        {
            ValidateReferences();

            continueButton.onClick.AddListener(OnContinueClicked);
            continueButton.interactable = false;
            continueButton.gameObject.SetActive(false);
            resultRoot.SetActive(false);
        }

        private void OnDisable()
        {
            StopPresentation();
        }

        private void OnDestroy()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueClicked);
            }
        }

        public void Show(int serviceMonths, string resultMessage, Action onContinue)
        {
            if (string.IsNullOrWhiteSpace(resultMessage))
            {
                throw new ArgumentException("심사 결과 문구가 필요합니다.", nameof(resultMessage));
            }

            if (onContinue == null)
            {
                throw new ArgumentNullException(nameof(onContinue));
            }

            StopPresentation();

            titleText.text = $"복무 {serviceMonths}개월 차";
            resultText.text = resultMessage;
            _onContinue = onContinue;

            continueButton.interactable = false;
            continueButton.gameObject.SetActive(false);

            resultRoot.SetActive(true);
            _showCoroutine = StartCoroutine(ShowContinueButtonAfterDelay());
        }

        private IEnumerator ShowContinueButtonAfterDelay()
        {
            if (continueButtonDelaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(continueButtonDelaySeconds);
            }

            continueButton.gameObject.SetActive(true);
            continueButton.interactable = true;
            _showCoroutine = null;
        }

        private void OnContinueClicked()
        {
            if (!continueButton.interactable)
            {
                return;
            }

            continueButton.interactable = false;

            var onContinue = _onContinue;
            _onContinue = null;

            StopRevealCoroutine();
            onContinue?.Invoke();
        }

        private void StopPresentation()
        {
            StopRevealCoroutine();
            _onContinue = null;
        }

        private void StopRevealCoroutine()
        {
            if (_showCoroutine == null)
            {
                return;
            }

            StopCoroutine(_showCoroutine);
            _showCoroutine = null;
        }

        private void ValidateReferences()
        {
            if (resultRoot != null && titleText != null &&
                resultText != null && continueButton != null)
            {
                return;
            }

            throw new InvalidOperationException("RoutineExamResultView의 UI 참조가 모두 연결되어야 합니다.");
        }
    }
}
