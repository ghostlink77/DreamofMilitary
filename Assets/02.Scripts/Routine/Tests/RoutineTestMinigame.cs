// ========================
// RoutineRunner의 실행, 완료 콜백, 타임아웃 처리를 확인하는 더미 미니게임
// 설정된 시간이 지나면 지정 판정을 반환하며 타임아웃 테스트에서는 완료하지 않는다.
// ========================

using System;
using System.Collections;
using UnityEngine;

namespace DreamOfMilitary.Routine.Tests
{
    public sealed class RoutineTestMinigame : MonoBehaviour, IMinigame, ITimeLimitResolver
    {
        [SerializeField] private MinigameJudgement _judgement = MinigameJudgement.Success;
        [SerializeField] private MinigameJudgement _timeLimitJudgement = MinigameJudgement.Failure;
        [SerializeField, Min(0f)] private float _completeAfterSeconds = 0.02f;
        [SerializeField] private bool _neverComplete;

        private Coroutine _completeCoroutine;
        private Action<MinigameJudgement> _onCompleted;
        private bool _aborted;

        public void Begin(MinigameContext context, Action<MinigameJudgement> onCompleted)
        {
            _onCompleted = onCompleted ?? throw new ArgumentNullException(nameof(onCompleted));
            _aborted = false;

            Debug.Log($"[RoutineTest][Minigame] Begin: {name}, Limit={context.TimeLimitSeconds:0.00}");

            if (!_neverComplete)
            {
                _completeCoroutine = StartCoroutine(CompleteAfterDelay());
            }
        }

        public void Abort()
        {
            if (_aborted)
            {
                return;
            }

            _aborted = true;

            if (_completeCoroutine != null)
            {
                StopCoroutine(_completeCoroutine);
                _completeCoroutine = null;
            }

            _onCompleted = null;
            Debug.Log($"[RoutineTest][Minigame] Abort: {name}");
        }

        public MinigameJudgement ResolveAtTimeLimit()
        {
            Debug.Log($"[RoutineTest][Minigame] ResolveAtTimeLimit: {name}, Judgement={_timeLimitJudgement}");
            return _timeLimitJudgement;
        }

        private IEnumerator CompleteAfterDelay()
        {
            if (_completeAfterSeconds > 0f)
            {
                yield return new WaitForSeconds(_completeAfterSeconds);
            }

            if (_aborted)
            {
                yield break;
            }

            var callback = _onCompleted;
            _onCompleted = null;
            _completeCoroutine = null;

            Debug.Log($"[RoutineTest][Minigame] Complete: {name}, Judgement={_judgement}");
            callback?.Invoke(_judgement);
        }
    }
}
