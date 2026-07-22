using System.Collections.Generic;
using UPlayGround.Data.EnumType;
using UnityEngine;
using UPlayGround.Data.Item;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Actor
{
    [CreateAssetMenu(fileName = "InteractableActorSO", menuName = "UPlayGround/액터/Interactable")]
    public class InteractableActorSO : ScriptableObject
    {
        public string actorName;
        public string description;

        public InteractionObjectType interactionObjectType;

        public int hp;

        [Header("상호작용")]
        [Tooltip("상호작용 입력 후 완료 처리까지 유지해야 하는 시간(초). 0이면 기존처럼 즉시 완료됩니다.")]
        [Min(0f)] public float interactionCompleteDuration = 0f;

        [Tooltip("상호작용 시 플레이어가 재생할 모션 슬롯. 비어 있으면 모션 없이 진행합니다. (현재 DROP_ITEM에서 사용)\n" +
                 "interactionCompleteDuration이 있으면 모션 내 Loop 이벤트 구간에서 대기하고, 없으면 Loop 이벤트를 건너뛰고 끝까지 재생합니다.")]
        public GameplayTag interactionMotionSlot;

        public List<ItemDropList> dropItems = new List<ItemDropList>();

        public bool showInfoUI = true;
        public bool showShakeEffect = true;

        [Header("낚시 (FISHING_ZONE 전용)")]
        [Tooltip("낚시 성공 N회 후 소진 상태로 만든다. 0 이하면 무제한.")]
        [Min(0)] public int fishingDepleteCatchCount = 0;

        [Header("회복 (REST_POINT 전용)")]
        public bool reviveDowned = true;     // HP 0 멤버도 부활
        // 풀 회복 고정이라 ratio 필드는 생략. 추후 부분회복 원하면 [Range(0,1)] healRatio 추가.
    }
}
