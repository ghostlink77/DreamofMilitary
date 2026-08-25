using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DreamOfMilitary.Routine
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ContinueButtonHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField, Min(1f)] private float hoverScaleMultiplier = 1.08f;
        [SerializeField, Min(0f)] private float animationSeconds = 0.15f;
        [SerializeField] private Ease animationEase = Ease.OutCubic;

        private Button _button;
        private Vector3 _defaultScale;
        private bool _initialized;

        public static void Ensure(Button button)
        {
            if (button != null && button.GetComponent<ContinueButtonHoverAnimation>() == null)
            {
                button.gameObject.AddComponent<ContinueButtonHoverAnimation>();
            }
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            CaptureDefaultScale();
        }

        private void OnEnable()
        {
            CaptureDefaultScale();
            RestoreDefaultScale();
        }

        private void OnDisable()
        {
            RestoreDefaultScale();
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button == null || !_button.interactable)
            {
                return;
            }

            var hoverScale = Vector3.Scale(_defaultScale, new Vector3(hoverScaleMultiplier, hoverScaleMultiplier, 1f));
            AnimateScale(hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            AnimateScale(_defaultScale);
        }

        private void CaptureDefaultScale()
        {
            if (_initialized)
            {
                return;
            }

            _defaultScale = transform.localScale;
            _initialized = true;
        }

        private void AnimateScale(Vector3 targetScale)
        {
            transform.DOKill();

            if (animationSeconds <= 0f)
            {
                transform.localScale = targetScale;
                return;
            }

            transform.DOScale(targetScale, animationSeconds).SetEase(animationEase).SetUpdate(true);
        }

        private void RestoreDefaultScale()
        {
            transform.DOKill();

            if (_initialized)
            {
                transform.localScale = _defaultScale;
            }
        }
    }
}
