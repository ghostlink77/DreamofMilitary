using DreamOfMilitary.Progression;
using UnityEngine;

public class RemoveData : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        GameState.Instance.ResetAfterPlayerPrefsClear();
    }

}
