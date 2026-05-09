using System.Collections.Generic;
using Animancer;
using KinematicCharacterController;
using UnityEditor;
using UnityEngine;
using UPlayGround;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;

namespace Game.Editor.P09Builder
{
    /// <summary>
    /// Player(PlayerActor) 프리팹 빌드 템플릿.
    /// KCC + MotionWarp + PlayerMovementController + PlayerActor + Combat/Equipment/SkillGauge 부착.
    /// </summary>
    internal sealed class PlayerActorTemplate : IActorTemplate
    {
        public BuilderActorKind Kind => BuilderActorKind.Player;

        public void AttachComponents(GameObject root, CharacterBuildConfig config)
        {
            if (root == null)
            {
                Debug.LogWarning("[P09Builder] PlayerActorTemplate.AttachComponents: root is null");
                return;
            }

            LayerAssignmentUtil.ApplyActorLayer(root, "Player");

            // 물리 / 이동
            GetOrAdd<KinematicCharacterMotor>(root);
            GetOrAdd<PlayerMovementController>(root);

            // Player 핵심 컴포넌트
            var actor = GetOrAdd<PlayerActor>(root);
            GetOrAdd<PlayerCombat>(root);
            GetOrAdd<PlayerEquipment>(root);
            GetOrAdd<PlayerSkillGauge>(root);

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

            // ActorType 설정 (Flags enum)
            ReflectionUtil.SetField(actor, "_actorType", (int)(ActorType.Player | ActorType.Combat));

            // CharacterActorType 설정
            if (config != null
                && config.ActorKind == BuilderActorKind.Player
                && config.PlayerCharacterType != CharacterActorType.None)
            {
                ReflectionUtil.SetField(actor, "_characterActorType", (int)config.PlayerCharacterType);
            }
        }

        public IEnumerable<IDescDef> GetDescDefs(CharacterBuildConfig config)
        {
            // Player 스탯은 기존 SO에서 참조하므로 새로 생성하지 않음.
            yield break;
        }

        public void WireDescAssets(GameObject root, List<ScriptableObject> generatedDescs, CharacterBuildConfig config)
        {
            // 현재는 wiring 없음. Player 스탯은 기존 SO에서 참조.
        }

        // ---------- helpers ----------
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(go);
            return c;
        }
    }
}
