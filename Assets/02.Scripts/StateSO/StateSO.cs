using UnityEngine;
using Animancer;

namespace Game.FSM
{
    /// <summary>
    /// 모든 상태(State)의 기본 설계도입니다.
    /// ScriptableObject로 만들어져 에셋 형태로 존재합니다.
    /// </summary>
    public abstract class StateSO : ScriptableObject
    {
        // 상태 진입 시 1회 실행
        public virtual void OnEnter(CharacterBrain brain) { }

        // 매 프레임 실행 (로직, 입력 감지)
        public virtual void OnUpdate(CharacterBrain brain) { }

        // 물리 연산 필요 시 (이동 등)
        public virtual void OnFixedUpdate(CharacterBrain brain) { }

        // 상태 종료 시 1회 실행 (이벤트 해제, 초기화)
        public virtual void OnExit(CharacterBrain brain) { }
    }
}