using System;
using DreamOfMilitary.Progression;
using UnityEngine;

[CreateAssetMenu(fileName = "LobbyUIData", menuName = "Scriptable Objects/LobbyUIData")]
public sealed class LobbyUIData : ScriptableObject
{
    [Serializable]
    public sealed class RankUI
    {
        [SerializeField] private MilitaryRank rank;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite characterSprite;
        [SerializeField] private Sprite portraitSprite;
        [SerializeField] private string rankText;

        public MilitaryRank Rank => rank;
        public Sprite BackgroundSprite => backgroundSprite;
        public Sprite CharacterSprite => characterSprite;
        public Sprite PortraitSprite => portraitSprite;
        public string RankText => rankText;
    }

    [SerializeField] private RankUI[] rankUIList = Array.Empty<RankUI>();

    public RankUI GetRankUI(MilitaryRank rank)
    {
        for (var i = 0; i < rankUIList.Length; i++)
        {
            var rankUI = rankUIList[i];

            if (rankUI != null && rankUI.Rank == rank)
            {
                return rankUI;
            }
        }

        throw new InvalidOperationException($"{rank} 계급의 로비 UI 데이터가 없습니다.");
    }
}
