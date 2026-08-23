using UnityEngine;

public class RepairScrew : MonoBehaviour
{
    [SerializeField] private RepairGameController repairGameController;

    public bool IsTarget { get; private set; }
    public bool IsRemoved { get; private set; }

    public void SetTarget(bool isTarget)
    {
        IsTarget = isTarget;
        IsRemoved = false;

        // 선택되지 않은 나사는 보이지 않고 상호작용할 수 없음
        gameObject.SetActive(isTarget);
    }

    public void TryRemove()
    {
        repairGameController.TryRemoveScrew(this);
    }

    public void Remove()
    {
        IsRemoved = true;
        gameObject.SetActive(false);
    }
}