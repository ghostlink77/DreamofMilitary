using UnityEngine;

/// <summary>
/// 인사/경례 미니게임에 등장하는 인물
/// </summary>
public sealed class SayHiCharacter : MonoBehaviour
{
    public enum Rank
    {
        Soldier,
        Officer
    }

    [SerializeField] private Rank rank;

    [Header("성공 모션 오브젝트")]
    [SerializeField] private GameObject successObject;

    public Rank CharacterRank => rank;

    /// <summary>
    /// 성공 모션을 켜거나 끈다.
    /// </summary>
    public void SetSuccessVisible(bool visible)
    {
        // 기본 캐릭터
        gameObject.SetActive(!visible);

        // 성공 캐릭터
        if (successObject != null)
        {
            successObject.SetActive(visible);
        }
    }
}