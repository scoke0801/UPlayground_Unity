using System;
using System.Collections.Generic;
using System.Linq;
using KinematicCharacterController;
using UnityEditor;
using UnityEngine;
using UPlayGround;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace Game.Editor.P09Builder
{
    /// <summary>
    /// Npc(NpcActor) 프리팹 빌드 템플릿.
    /// KCC + MotionWarp + NpcMovementController + NpcActor + Animator + Collider 부착.
    /// NpcActorSO 가 지정되지 않은 경우 빈 NpcActorSO 를 생성해 _data 필드에 연결한다.
    /// 주의: NpcActorSO는 글로벌 네임스페이스에 있어 using 불필요.
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

            // 물리 / 이동
            GetOrAdd<KinematicCharacterMotor>(root);
            GetOrAdd<MotionWarpController>(root);
            GetOrAdd<NpcMovementController>(root);

            var actor = GetOrAdd<NpcActor>(root);

            if (root.GetComponent<Animator>() == null)
                Undo.AddComponent<Animator>(root);

            if (root.GetComponent<CapsuleCollider>() == null)
            {
                var col = Undo.AddComponent<CapsuleCollider>(root);
                col.radius = 0.35f;
                col.height = 1.8f;
                col.center = Vector3.up * 0.9f;
            }

            // ActorType 설정 (NPC + Talkable Flags)
            ReflectionUtil.SetField(actor, "_actorType", (int)(ActorType.NPC | ActorType.Talkable));
        }

        public IEnumerable<IDescDef> GetDescDefs(CharacterBuildConfig config)
        {
            // dialogueSo가 NpcActorSO 타입이 아니거나 비어있으면 새로 생성
            var existing = config?.Stats?.dialogueSo as NpcActorSO;
            if (existing == null)
                yield return new NpcDataDescDef();
        }

        public void WireDescAssets(GameObject root, List<ScriptableObject> generatedDescs, CharacterBuildConfig config)
        {
            if (root == null) return;

            var actor = root.GetComponent<NpcActor>();
            if (actor == null) return;

            var dataSo = (generatedDescs?.OfType<NpcActorSO>().FirstOrDefault())
                         ?? (config?.Stats?.dialogueSo as NpcActorSO);

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
                // 기본값을 그대로 사용한다.
                EditorUtility.SetDirty(so);
            }
        }
    }
}
