using UnityEngine;

/// <summary>
/// 경례하기 미니게임에 등장하는 인물의 계급을 정의한다.
/// 각 인물 오브젝트(또는 그 부모)에 붙여 SaluteMinigame의 characters 배열에 등록한다.
/// </summary>
public sealed class SayHiCharacter : MonoBehaviour
{
    public enum Rank
    {
        Soldier,
        Officer
    }

    [SerializeField] private Rank rank;

    public Rank CharacterRank => rank;
}

