using UnityEngine;

public class TestClick : MonoBehaviour
{
    private void Update()
    {
        if (MouseInputManager.Instance.IsClickDown())
        {
            Debug.Log("마우스를 클릭했습니다!");
        }
    }
}
