using System;
using DreamOfMilitary.Progression;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TitleNewGameController : MonoBehaviour
{
    [SerializeField] private Button startButton;

    private void Awake()
    {
        if (startButton == null)
        {
            throw new InvalidOperationException("새 게임을 시작할 버튼이 연결되지 않았습니다.");
        }

        startButton.onClick.AddListener(ResetGameState);
    }

    private void OnDestroy()
    {
        startButton.onClick.RemoveListener(ResetGameState);
    }

    private static void ResetGameState()
    {
        var gameState = GameState.Instance;

        if (gameState != null && !gameState.HasSavedProgress)
        {
            gameState.ResetForNewGame();
        }
    }
}
