using UnityEngine;

public class WalkPlayerFootController : MonoBehaviour
{
    [Header("플레이어 발")]
    [SerializeField] private GameObject playerLeft;
    [SerializeField] private GameObject playerRight;

    private void Start()
    {
        SetRight();
    }

    public void SetLeft()
    {
        if (playerLeft != null)
            playerLeft.SetActive(true);

        if (playerRight != null)
            playerRight.SetActive(false);
    }

    public void SetRight()
    {
        if (playerLeft != null)
            playerLeft.SetActive(false);

        if (playerRight != null)
            playerRight.SetActive(true);
    }
}