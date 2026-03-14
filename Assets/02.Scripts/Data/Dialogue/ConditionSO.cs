using UnityEngine;

namespace UPlayGround.Dialogue
{
    // 조건 분기의 추상 기반 — Evaluate 하나만 강제
    // 새 조건 타입 추가 시 이 클래스를 상속받아 구현만 하면 됩니다
    public abstract class ConditionSO : ScriptableObject
    {
        public abstract bool Evaluate();
    }
}
