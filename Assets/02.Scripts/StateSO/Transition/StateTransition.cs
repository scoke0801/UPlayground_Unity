// StateTransition.cs (CharacterBrain.cs 파일 내부에 정의해도 무방함)
using System;

namespace Game.FSM
{
    /// <summary>
    /// 상태 전환을 위한 조건과 목표 상태를 묶는 데이터 구조
    /// </summary>
    [Serializable]
    public class StateTransition
    {
        // 이 조건이 참일 때만 전환이 일어납니다.
        public TransitionConditionSO Condition; 
        
        // 전환이 성공하면 이 상태로 이동합니다.
        public StateSO TargetState;             
        
        // 여러 조건이 동시에 참일 때 우선순위를 결정합니다. (높을수록 먼저 검사)
        // public int Priority = 0; 
        
        // 전환 성공 시 입력 데이터 초기화 여부
        public bool ConsumeInputOnTransition = true; 
    }
}