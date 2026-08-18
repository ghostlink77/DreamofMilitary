using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class EndingSequenceController : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject[] cuts;
    [SerializeField] private GameObject scoreboard;

    private int nextCutIndex;
    private bool isComplete;

    private void Awake()
    {
        foreach (GameObject cut in cuts)
        {
            cut.SetActive(false);
            cut.GetComponent<Image>().raycastTarget = false;
        }

        scoreboard.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isComplete)
        {
            return;
        }

        if (nextCutIndex < cuts.Length)
        {
            cuts[nextCutIndex++].SetActive(true);
            return;
        }

        foreach (GameObject cut in cuts)
        {
            cut.SetActive(false);
        }

        scoreboard.SetActive(true);
        isComplete = true;
    }
}
