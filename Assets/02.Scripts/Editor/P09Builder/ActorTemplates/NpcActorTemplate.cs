using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using KinematicCharacterController;
using UnityEditor;
using UnityEngine;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.Data.Actor;
using UPlayGround.Components;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>
    /// Npc(NpcActor) 프리팹 빌드 템플릿.
    /// KCC + NpcMovementController + NpcActor + NpcBrain + Animator + Collider 부착.
    /// 새 데이터 모드에서는 입력값으로 NpcActorSO를 생성하고, 기존 데이터 모드에서는 선택 자산을 연결한다.
    /// </summary>
    internal sealed class NpcActorTemplate : IActorTemplate
    {
        public BuilderActorKind Kind => BuilderActorKind.Npc;

        public void AttachComponents(GameObject root, CharacterBuildConfig config)
        {
            if (root == null)
            {
                Debug.LogWarning("[P09Builder] NpcActorTemplate.AttachComponents: root is null");
                return;
            }

            LayerAssignmentUtil.ApplyActorLayer(root, "Npc");

            // 물리 / 이동
            GetOrAdd<KinematicCharacterMotor>(root);
            GetOrAdd<NpcMovementController>(root);

            var actor = GetOrAdd<NpcActor>(root);
            var brain = GetOrAdd<NpcBrain>(root);

            if (root.GetComponent<Animator>() == null)
                Undo.AddComponent<Animator>(root);

            if (root.GetComponent<AnimancerComponent>() == null)
                Undo.AddComponent<AnimancerComponent>(root);

            if (root.GetComponent<CapsuleCollider>() == null)
            {
                var col = Undo.AddComponent<CapsuleCollider>(root);
                col.radius = 0.35f;
                col.height = 1.8f;
                col.center = Vector3.up * 0.9f;
            }

            // ActorType 설정 (NPC + Talkable Flags)
            ReflectionUtil.SetField(actor, "_actorType", (int)(ActorType.NPC | ActorType.Talkable));
            ReflectionUtil.SetField(actor, "_actorId", root.name);

            if (config?.Stats != null)
            {
                ReflectionUtil.SetField(brain, "_enableWander", config.Stats.npcEnableWander);
                ReflectionUtil.SetField(brain, "_patrolRadius", Mathf.Max(0f, config.Stats.wanderRadius));
                ReflectionUtil.SetField(brain, "_patrolWaitTime", Mathf.Max(0f, config.Stats.npcWanderWaitTime));
            }
        }

        public IEnumerable<IDescDef> GetDescDefs(CharacterBuildConfig config)
        {
            if (config?.Stats?.createNewNpcData != false)
                yield return new NpcDataDescDef();
        }

        public void WireDescAssets(GameObject root, List<ScriptableObject> generatedDescs, CharacterBuildConfig config)
        {
            if (root == null) return;

            var actor = root.GetComponent<NpcActor>();
            if (actor == null) return;

            var dataSo = (generatedDescs?.OfType<NpcActorSO>().FirstOrDefault())
                         ?? config?.Stats?.existingNpcData;

            if (dataSo != null)
                ReflectionUtil.SetField(actor, "_data", dataSo);
        }

        // ---------- helpers ----------
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(go);
            return c;
        }

        // ---------- DescDefs ----------
        private sealed class NpcDataDescDef : IDescDef
        {
            public Type DescType => typeof(NpcActorSO);
            public string Suffix => "_NpcData";

            public void ApplyDefaults(ScriptableObject so, CharacterBuildConfig config)
            {
                if (so is not NpcActorSO npcData || config?.Stats == null)
                    return;

                var stats = config.Stats;
                npcData.actorName = stats.npcDisplayName?.Trim() ?? string.Empty;
                npcData.description = stats.npcDescription ?? string.Empty;
                npcData.hp = Mathf.Max(0, stats.npcHp);
                npcData.storyEntries = stats.npcStoryEntries ?? Array.Empty<UPlayGround.Story.StoryEntrySO>();
                npcData.dialogueGraph = stats.npcDialogueGraph;
                npcData.interactionObjectType = InteractionObjectType.NPC;
                npcData.interactionCompleteDuration = Mathf.Max(0f, stats.npcInteractionCompleteDuration);
                npcData.interactionMotionSlot = stats.npcInteractionMotionSlot;
                npcData.showInfoUI = false;
                npcData.showShakeEffect = false;
                EditorUtility.SetDirty(so);
            }
        }
    }
}
