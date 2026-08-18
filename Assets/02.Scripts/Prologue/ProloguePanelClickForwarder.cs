using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ProloguePanelClickForwarder : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PrologueSequenceController sequenceController;

    public void OnPointerClick(PointerEventData eventData)
    {
        sequenceController.ShowNextCut();
    }
}