using UnityEngine;

namespace DreamOfMilitary.Routine.Minigames.QuickPack
{
    /// <summary>
    /// 신속 군장 싸기에서 클릭할 수 있는 군장 물품.
    /// 입력 자체는 MouseInputManager가 담당한다.
    /// </summary>
    public sealed class QuickPackItem : MonoBehaviour
    {
        [Header("물품 정보")]
        [SerializeField]
        private string _itemId;

        private bool _interactable;

        public string ItemId => _itemId;

        public bool IsInteractable => _interactable;

        /// <summary>
        /// 플레이어가 클릭할 수 있는 상태인지 설정한다.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
        }

        /// <summary>
        /// 게임에서 물품을 보여준다.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            _interactable = true;
        }

        /// <summary>
        /// 게임에서 물품을 숨긴다.
        /// </summary>
        public void Hide()
        {
            _interactable = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 미니게임 시작 전 초기화.
        /// </summary>
        public void ResetItem()
        {
            gameObject.SetActive(true);
            _interactable = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _itemId = _itemId?.Trim() ?? string.Empty;
        }
#endif
    }
}