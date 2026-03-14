using UnityEngine;

namespace UPlayGround.Dialogue
{
    // 노드 진입 시 실행되는 게임 이벤트의 추상 기반
    // 아이템 지급, 퀘스트 업데이트 등을 별도 SO로 구현
    public abstract class DialogueActionSO : ScriptableObject
    {
        public abstract void Execute();
    }
}
