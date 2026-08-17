using System.Collections.Generic;
using UnityEngine;

namespace DreamOfMilitary.Routine
{
    [CreateAssetMenu(
        fileName = "MinigameCatalog",
        menuName = "Dream Of Military/Minigame Catalog")]
    public sealed class MinigameCatalog : ScriptableObject
    {
        [SerializeField]
        private List<MinigameDef> _definitions = new List<MinigameDef>();

        public IReadOnlyList<MinigameDef> Definitions => _definitions;
    }
}