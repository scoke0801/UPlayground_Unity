// TransitionConditionSO.cs
using UnityEngine;

namespace Game.FSM
{
    /// <summary>
    /// 상태 전환 조건을 정의하는 추상 클래스
    /// </summary>
    public abstract class TransitionConditionSO : ScriptableObject
    {
        // CharacterBrain의 데이터를 기반으로 조건 충족 여부를 체크합니다.
        public abstract bool CheckCondition(CharacterBrain brain);
    }
}