using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Party;
using UPlayGround.Data.Projectile;
using UPlayGround.Data.Stat;
using UPlayGround.Gameplay.Ability;
using UPlayGround.Gameplay.Tag;
using UPlayGround.MovementController;
using Motion = UPlayGround.Animation.Motion;

namespace UPlayGround.EditorTools
{
    /// <summary>원본 궁극기를 보존하며 시그니처 데이터를 준비하고 검증 뒤 한 번에 연결한다.</summary>
    public static class WeaponSignatureSkillAuthoring
    {
        private const string AbilityRoot = "Assets/10.Datas/Ability/Player/Signature/";
        private const string MotionRoot = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Signature/";
        private const string PlayerSetRoot = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/";
        private const string HumanoidRoot = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid/";
        private const string ProjectileRoot = "Assets/10.Datas/Projectile/Signature/";
        private const string CameraRoot = "Assets/10.Datas/Camera/CameraEffect/Signature/";
        private const string ShieldAbsentTag = "State.Combat.SignatureShieldAbsent";

        private sealed class Recipe
        {
            public string Weapon;
            public int Frames, DodgeFrame, MoveFrame;
            public int[] Repeats;
            public float[] Shares;
            public AttackReactionType[] Reactions;
            public Recipe(string weapon, int frames, int dodge, int move, int[] repeats, float[] shares,
                params AttackReactionType[] reactions)
            { Weapon = weapon; Frames = frames; DodgeFrame = dodge; MoveFrame = move; Repeats = repeats; Shares = shares; Reactions = reactions; }
        }

        private static readonly Recipe[] Recipes =
        {
            new("Katana", 51, 36, 42, new[] { 1, 1 }, new[] { .75f, .25f }, AttackReactionType.Heavy, AttackReactionType.Heavy),
            new("Bow", 48, 30, 38, new[] { 6 }, new[] { 1f }, AttackReactionType.Light),
            new("Spear", 60, 52, 56, new[] { 1 }, new[] { 1f }, AttackReactionType.Knockdown),
            new("DoubleAxe", 117, 88, 108, new[] { 5, 1 }, new[] { .6f, .4f }, AttackReactionType.Light, AttackReactionType.KnockBack),
            new("DualBlade", 45, 36, 40, new[] { 2, 12 }, new[] { .3f, .7f }, AttackReactionType.Light, AttackReactionType.Light),
            new("Staff", 90, 78, 84, new[] { 7, 1 }, new[] { .8f, .2f }, AttackReactionType.Light, AttackReactionType.KnockBack),
            new("GreatSword", 60, 48, 52, new[] { 1, 1 }, new[] { .8f, .2f }, AttackReactionType.Knockdown, AttackReactionType.KnockBack),
            new("Whip", 72, 57, 62, new[] { 1, 1, 1 }, new[] { .3f, .3f, .4f }, AttackReactionType.Light, AttackReactionType.KnockBack, AttackReactionType.KnockBack),
            new("SwordShield", 60, 57, 57, new[] { 1 }, new[] { 1f }, AttackReactionType.Knockdown)
        };

        private static readonly List<string> Created = new();
        private static readonly List<UnityEngine.Object> Touched = new();

        /// <summary>라이브 연결 없이 9종의 독립 실행 데이터를 준비한다.</summary>
        public static string Build() => BuildWeapons(Recipes.Select(r => r.Weapon).ToArray());

        /// <summary>제작이 끝난 무기만 준비한다. Apply는 항상 9종 전체를 요구한다.</summary>
        public static string BuildWeapons(params string[] weapons)
        {
            CheckEditor();
            Recipe[] selected = weapons.Distinct().Select(w => Recipes.FirstOrDefault(r => r.Weapon == w)
                ?? throw new ArgumentException("알 수 없는 무기: " + w)).ToArray();
            foreach (Recipe recipe in selected) CheckInputs(recipe);
            return Transaction("시그니처 데이터 준비", () =>
            {
                var report = new StringBuilder();
                foreach (Recipe recipe in selected)
                {
                    BuildWeapon(recipe);
                    ValidateWeapon(recipe);
                    report.AppendLine(recipe.Weapon + ": 준비 완료, " + recipe.Frames + "f/30fps");
                }
                return report.ToString();
            });
        }

        /// <summary>준비한 데이터의 클립·타격·수치·연출 참조를 검사한다.</summary>
        public static string Validate()
        {
            CheckEditor();
            foreach (Recipe recipe in Recipes) ValidateWeapon(recipe);
            Require<MotionSetAsset>(MotionRoot + "Katana_Raon_Signature.asset");
            Require<MotionSetAsset>(MotionRoot + "Whip_Weapon_Signature.asset");
            return "시그니처 9종: 준비 데이터 검사 통과. 실제 포즈·미술·플레이 품질은 별도 확인 필요.";
        }

        /// <summary>검증된 9종을 기존 궁극기 슬롯과 실제 무기 세트에 함께 연결한다.</summary>
        public static string Apply()
        {
            Validate();
            foreach (Recipe recipe in Recipes) Require<ActorAnimationMotionSet>(PlayerSetRoot + "Humanoid_Player_" + recipe.Weapon + "AnimationSet.asset");
            Require<ActorAnimationMotionSet>("Assets/10.Datas/Actor/Footsteps/Motions/Set_Katana.asset");
            Require<ActorAnimationMotionSet>("Assets/10.Datas/Actor/Animation/WeaponMotion/Whip/WhipMotionSet.asset");
            return Transaction("시그니처 연결", () =>
            {
                foreach (Recipe recipe in Recipes) ApplyWeapon(recipe);
                ApplyShieldRules();
                ApplyImpactEffects();
                return "9종 궁극기 연결 완료. 원본 MotionSet·Sequence·공유 Default 세트는 보존됨.";
            });
        }

        private static void BuildWeapon(Recipe recipe)
        {
            string weapon = recipe.Weapon;
            GameplayAbilitySO sourceAbility = SourceAbility(weapon);
            var source = Load<UPlayGroundMotionAbilityPayloadSO>(AbilityRoot + "SourcePayload_" + weapon + ".asset");
            if (source == null)
            {
                var original = sourceAbility.variants[0].executionPayload as UPlayGroundMotionAbilityPayloadSO;
                if (original == null) throw new InvalidOperationException(weapon + " 원본 Motion Payload 없음");
                source = UnityEngine.Object.Instantiate(original);
                source.name = "SourcePayload_" + weapon;
                CreateAsset(source, AbilityRoot + source.name + ".asset");
            }

            var payload = GetOrCreate<UPlayGroundMotionAbilityPayloadSO>(AbilityRoot + "Payload_" + weapon + "_Signature.asset");
            payload.attackInfo = JsonUtility.FromJson<AbilityAttackInfo>(JsonUtility.ToJson(source.attackInfo));
            payload.attackInfo.motionKey = new MotionKey(weapon + ".Ultimate");
            payload.attackInfo.isMotionContentPending = false;
            payload.attackInfo.aiSelectable = false;
            payload.attackInfo.interruptActions = PlayerInterruptAction.Dodge | PlayerInterruptAction.Move;
            if (weapon == "SwordShield") payload.attackInfo.interruptActions |= PlayerInterruptAction.Guard;
            payload.attackInfo.moveCancelDelayAfterLastHit = 0f;
            payload.attackInfo.useTelegraph = false;
            payload.attackInfo.useDangerRing = false;
            payload.attackInfo.baseInfo = new AttackInfoBase { attackType = source.attackInfo.baseInfo.attackType };
            var balanceSource = source;
            if (weapon == "Spear")
            {
                // 기존 Default 궁극기는 피해 페이즈가 없다. 단일 착탄의 초기 수치만 대검 기준으로 둔다.
                balanceSource = Load<UPlayGroundMotionAbilityPayloadSO>(AbilityRoot + "SourcePayload_GreatSword.asset");
                if (balanceSource == null)
                {
                    balanceSource = UnityEngine.Object.Instantiate(SourceAbility("GreatSword").variants[0].executionPayload as UPlayGroundMotionAbilityPayloadSO);
                    balanceSource.name = "SourcePayload_GreatSword";
                    CreateAsset(balanceSource, AbilityRoot + "SourcePayload_GreatSword.asset");
                }
            }
            var baseline = Totals(balanceSource.attackInfo.baseInfo.hitPhases);
            float imbuedHeatWeight = source.attackInfo.baseInfo.hitPhases.Sum(p => p.receivesImbuedHeat ? p.imbuedHeatWeight : 0f);
            for (int i = 0; i < recipe.Repeats.Length; i++)
            {
                HitPhaseData phase = weapon == "Spear" ? new HitPhaseData()
                    : JsonUtility.FromJson<HitPhaseData>(JsonUtility.ToJson(source.attackInfo.baseInfo.hitPhases[Mathf.Min(i, source.attackInfo.baseInfo.hitPhases.Count - 1)]));
                float weight = recipe.Shares[i] / recipe.Repeats[i];
                phase.damage = baseline.x * weight;
                phase.poiseDamage = baseline.y * weight;
                phase.breakDamage = baseline.z * weight;
                phase.receivesImbuedHeat = imbuedHeatWeight > 0f;
                phase.imbuedHeatWeight = imbuedHeatWeight * weight;
                phase.reactionType = recipe.Reactions[i];
                phase.forceReaction = false;
                phase.projectileDefinition = null;
                phase.reactionProfile = new AttackReactionProfile
                {
                    useAutoReaction = true, useManualOverride = true,
                    manualOverride = new ManualReactionOverride
                    {
                        overrideHitStop = true, hitStopDuration = recipe.Reactions[i] == AttackReactionType.Light ? .01f : 0f,
                        hitStopScale = .3f, overrideCamera = true, cameraShakeAmplitude = 0f,
                        overrideFov = true, fovKickAmount = 0f
                    }
                };
                if (weapon == "Katana") phase.reactionProfile.manualOverride.hitStopDuration = 0f;
                if (weapon == "DualBlade" && i == 1) phase.reactionProfile.manualOverride.hitStopDuration = 0f;
                payload.attackInfo.baseInfo.hitPhases.Add(phase);
            }
            // 기본 생성자의 기본 페이즈를 실제 분배 페이즈로 교체한다.
            payload.attackInfo.baseInfo.hitPhases.RemoveAt(0);
            if (weapon == "Bow" || weapon == "Spear" || weapon == "SwordShield")
                payload.attackInfo.baseInfo.hitPhases[0].projectileDefinition = Projectile(weapon);
            if (weapon == "DualBlade") payload.attackInfo.baseInfo.hitPhases[1].projectileDefinition = Projectile(weapon);

            var motion = GetOrCreate<MotionSetAsset>(MotionRoot + weapon + "_Signature.asset");
            motion.motionSet = MotionSet.CreateAuthored(weapon + "_Signature");
            motion.motionSet.internalBlendDuration = .04f;
            BuildClips(recipe, motion.motionSet);
            BuildEvents(recipe, motion.motionSet);
            BuildCamera(recipe, motion.motionSet);
            BuildFace(recipe, motion.motionSet);
            Dirty(payload); Dirty(motion);
            if (weapon == "Katana")
            {
                var raon = GetOrCreate<MotionSetAsset>(MotionRoot + "Katana_Raon_Signature.asset");
                EditorUtility.CopySerialized(motion, raon);
                raon.name = "Katana_Raon_Signature";
                raon.motionSet.motionSetName = raon.name;
                raon.motionSet.motions.Clear();
                AddClip(raon.motionSet, SignatureClip("Katana_Raon"), 0f, 1f, recipe.Frames / 30f);
                Dirty(raon);
            }
            if (weapon == "Whip")
            {
                var whip = GetOrCreate<MotionSetAsset>(MotionRoot + "Whip_Weapon_Signature.asset");
                whip.motionSet = MotionSet.CreateAuthored("Whip_Weapon_Signature");
                AddClip(whip.motionSet, Require<MotionSetAsset>("Assets/10.Datas/Actor/Animation/WeaponMotion/Whip/Whip_Skill_1.asset").motionSet.motions[0].motionClip, 0f, 1f, 72f / 30f);
                Dirty(whip);
            }
            if (weapon == "Spear") BuildSpearAbility(sourceAbility, payload);
            if (weapon == "SwordShield") BuildShieldEffect();
        }

        private static void BuildClips(Recipe recipe, MotionSet motion)
        {
            string weapon = recipe.Weapon;
            float duration = recipe.Frames / 30f;
            if (weapon == "Bow" || weapon == "Spear" || weapon == "SwordShield" || weapon == "Staff" || weapon == "Katana")
                AddClip(motion, SignatureClip(weapon), 0f, 1f, duration);
            else if (weapon == "DoubleAxe")
            {
                AnimationClip clip = WhirlwindClip(false);
                AddClip(motion, clip, 0f, .25f, .4f);
                for (int i = 0; i < 5; i++) AddClip(motion, clip, .25f, .75f, .5f);
                AddClip(motion, clip, .75f, 1f, 1f);
            }
            else if (weapon == "DualBlade") AddClip(motion, WhirlwindClip(true), 0f, 1f, duration);
            else
            {
                MotionSetAsset source = Require<MotionSetAsset>(weapon == "Whip"
                    ? PlayerSetRoot + "Whip/WhipCharacter_Skill_1.asset"
                    : HumanoidRoot + "GreatSword/Humanoid_GreatSwordAnimationSet_HeavyAttack_3.asset");
                AnimationClip clip = source.motionSet.motions[0].motionClip;
                AddClip(motion, clip, 0f, 1f, duration);
            }
            if (weapon == "Staff")
            {
                MotionSetAsset old = Require<MotionSetAsset>("Assets/10.Datas/Ability/Player/Staff/Motions/Staff_Ultimate.asset");
                motion.layers = JsonUtility.FromJson<LayerCopy>(JsonUtility.ToJson(new LayerCopy { layers = old.motionSet.layers })).layers;
            }
        }

        [Serializable] private sealed class LayerCopy { public List<MotionLayer> layers; }

        private static void AddClip(MotionSet motion, AnimationClip clip, float start, float end, float duration)
        {
            if (clip == null || clip.length <= 0f) throw new InvalidOperationException("유효한 입력 클립이 없습니다.");
            motion.motions.Add(new Motion
            {
                id = Guid.NewGuid().ToString("N"), motionName = clip.name, motionClip = clip,
                clipStartTime = clip.length * start, clipEndTime = clip.length * end,
                playbackSpeed = clip.length * (end - start) / duration
            });
        }

        private static void BuildEvents(Recipe recipe, MotionSet motion)
        {
            string weapon = recipe.Weapon;
            float end = recipe.Frames / 30f;
            Add(motion, new CancelWindowEvent { maskOverride = PlayerInterruptAction.Dodge }, recipe.DodgeFrame / 30f, end);
            Add(motion, new CancelWindowEvent { maskOverride = PlayerInterruptAction.Move }, recipe.MoveFrame / 30f, end);
            if (weapon == "SwordShield") Add(motion, new CancelWindowEvent { maskOverride = PlayerInterruptAction.Guard }, 57f / 30f, end);
            switch (weapon)
            {
                case "Katana":
                    Hit(motion, 15, 21, 0, Box(new Vector3(2.5f, 2f, 2f), new Vector3(0, 1, .5f), true));
                    Hit(motion, 21, 24, 1, Box(new Vector3(2.5f, 2f, 2f), new Vector3(0, 1, 2), true));
                    Add(motion, new InvincibilityEvent(), .4f, .7f);
                    Add(motion, new AfterimageEvent { spawnCount = 4, spawnInterval = .05f, holdDuration = .08f, fadeOutDuration = .15f }, .5f, .7f);
                    Add(motion, new MotionEvent_MotionWarp { overrideDistance = true, minDistance = 0f, maxDistance = 8f, maxSpeed = 40f,
                        arrivalMode = WarpArrivalMode.ContactShell, desiredStandOff = 1f, playbackRateWarpPolicy = PlaybackRateWarpPolicy.Disabled,
                        yPolicy = WarpYPolicy.IgnoreY, correctionTiming = MotionWarpCorrectionTiming.AuthoredCurve,
                        correctionTimingCurve = AnimationCurve.Linear(0, 0, 1, 1) }, .5f, .7f);
                    break;
                case "Bow": Spawn(motion, weapon, 22, 0, 12f); break;
                case "Spear":
                    Add(motion, new AttackGravityEvent { gravityMultiplier = .3f }, .3f, end);
                    Add(motion, new AddForceEvent { direction = Vector3.up, force = 9f }, .3f, .31f);
                    Add(motion, new WeaponSlotVisibilityEvent { slot = WeaponVisibilitySlot.Main }, 1f, 50f / 30f);
                    Spawn(motion, weapon, 30, 0, 8f);
                    break;
                case "DoubleAxe":
                    for (int i = 0; i < 5; i++) Hit(motion, 12 + i * 15, 15 + i * 15, 0, Sphere(2.8f, Vector3.zero, true));
                    Hit(motion, 87, 88, 1, Sphere(3.5f, Vector3.zero, false));
                    Add(motion, new AttackInputMovementEvent { speedMultiplier = .55f, rotationSpeed = 360f }, .4f, 2.9f);
                    break;
                case "DualBlade":
                    Hit(motion, 12, 15, 0, Sphere(2.2f, Vector3.zero, true));
                    Hit(motion, 21, 24, 0, Sphere(2.2f, Vector3.zero, true));
                    Spawn(motion, weapon, 27, 1, 0f);
                    break;
                case "Staff":
                    for (int i = 0; i < 8; i++) Hit(motion, 27 + i * 6, 28.5f + i * 6, i == 7 ? 1 : 0,
                        Box(new Vector3(1.6f, 1.6f, 18f), new Vector3(0, 1.2f, 9.5f), true));
                    break;
                case "GreatSword":
                    Hit(motion, 33, 34, 0, Box(new Vector3(3, 3, 9), new Vector3(0, 1.5f, 5.5f), false));
                    Hit(motion, 36, 37, 1, Sphere(4f, new Vector3(0, 0, 7), false));
                    break;
                case "Whip":
                    Hit(motion, 12, 21, 0, Sphere(5.5f, Vector3.zero, true));
                    Hit(motion, 27, 36, 1, Sphere(5.5f, Vector3.zero, true));
                    var capsule = Sphere(.8f, new Vector3(0, 1f, 3.5f), false);
                    capsule.shapeType = CollisionShapeType.Capsule; capsule.capsuleHeight = 7f; capsule.localEulerAngles = new Vector3(90, 0, 0);
                    Hit(motion, 42, 45, 2, capsule);
                    break;
                case "SwordShield":
                    Add(motion, new WeaponSlotVisibilityEvent { slot = WeaponVisibilitySlot.Sub }, 16f / 30f, 55f / 30f);
                    Spawn(motion, weapon, 16, 0, 6f);
                    break;
            }
            if (weapon == "GreatSword")
            {
                Add(motion, new TimedPropEvent
                {
                    prefab = Require<GameObject>("Assets/03.Prefabs/Effects/Signature/GreatSword_Signature.prefab"),
                    startOffset = new Vector3(0, 12, 7), endOffset = new Vector3(0, 0, 7),
                    positionProgress = new AnimationCurve(new Keyframe(0, 0), new Keyframe(12f / 33f, 0), new Keyframe(18f / 33f, 1), new Keyframe(1, 1))
                }, .5f, 1.6f);
                Add(motion, new BeginParticleEvent
                {
                    particlePrefab = Require<GameObject>("Assets/03.Prefabs/Effects/Signature/GreatSword_Impact_Signature.prefab"),
                    offset = new Vector3(0, 0, 7), attachToTarget = false, projectToGround = true,
                    groundProbeDistance = 4, groundLayers = (1 << 0) | (1 << 3), destroyOnFinish = true
                }, 1.1f, 1.6f);
            }
            else if (weapon is "Staff" or "Katana" or "DoubleAxe" or "Whip")
            {
                float fxStart = weapon == "Staff" ? .9f : weapon == "Katana" ? .5f : .4f;
                Add(motion, new BeginParticleEvent
                {
                    particlePrefab = Require<GameObject>("Assets/03.Prefabs/Effects/Signature/" + weapon + "_Signature.prefab"),
                    attachToTarget = true, destroyOnFinish = true
                }, fxStart, end);
            }
        }

        private static void Spawn(MotionSet motion, string weapon, float frame, int phase, float fallback)
            => Add(motion, new SpawnProjectileEvent
            {
                projectileDefinition = Projectile(weapon), hitPhaseIndex = phase, suppressWeaponAim = true,
                targetMode = weapon == "DualBlade" ? ProjectileTargetMode.Forward : ProjectileTargetMode.TargetPosition,
                projectTargetToGround = weapon != "DualBlade", playerTargetFallbackDistance = fallback,
                spawnOffset = weapon == "Spear" ? new Vector3(0, 1.4f, .4f) : new Vector3(0, 1f, .5f),
                groundLayerMask = (1 << 0) | (1 << 3), groundProbeHeight = 12, groundProbeDistance = 30
            }, frame / 30f, frame / 30f + .01f);

        private static ExplicitCollisionShapeData Sphere(float radius, Vector3 offset, bool follow)
            => new() { radius = radius, localOffset = offset, anchor = CollisionAnchorType.ActorRoot,
                anchorSampling = follow ? CollisionAnchorSampling.FollowDuringWindow : CollisionAnchorSampling.SnapshotOnBegin,
                evaluation = follow ? CollisionEvaluationType.Window : CollisionEvaluationType.OnceOnBegin,
                direction = CollisionDirectionType.ShapeCenterToTarget };

        private static ExplicitCollisionShapeData Box(Vector3 size, Vector3 offset, bool follow)
        { var shape = Sphere(1, offset, follow); shape.shapeType = CollisionShapeType.Box; shape.boxSize = size; shape.direction = CollisionDirectionType.ActorForward; return shape; }

        private static void Hit(MotionSet motion, float startFrame, float endFrame, int phase, ExplicitCollisionShapeData shape)
            => Add(motion, new BeginCollisionEvent { collisionSource = CollisionSourceType.ExplicitShape,
                explicitShape = shape, hitPhaseIndex = phase, suppressWeaponAim = true }, startFrame / 30f, endFrame / 30f);

        private static void Add(MotionSet motion, MotionEventBase evt, float start, float end)
        { evt.startTime = start; evt.endTime = end; motion.globalEvents.Add(evt); }

        private static void BuildFace(Recipe recipe, MotionSet motion)
        {
            bool shortPanel = recipe.Weapon == "Katana" || recipe.Weapon == "Staff" || recipe.Weapon == "DualBlade";
            float hold = recipe.Weapon switch { "Katana" => .25f, "Staff" or "DualBlade" => .35f, "DoubleAxe" => .8f,
                "Whip" or "SwordShield" => .6f, _ => .5f };
            Add(motion, new UltimateFaceStripMotionEvent
            { inDuration = shortPanel ? .12f : .18f, hold = hold, outDuration = shortPanel ? .15f : .2f }, 0, recipe.Frames / 30f);
        }

        private static void BuildCamera(Recipe recipe, MotionSet motion)
        {
            string w = recipe.Weapon;
            switch (w)
            {
                case "Katana":
                    Camera(motion, w, "Focus", 0, .4f, -4, -.8f);
                    Camera(motion, w, "Slash", .5f, .8f, 8, 1.2f);
                    Impact(motion, w, .7f, 1.5f, 1f, .15f, .3f, .12f);
                    Lock(motion, 0, .8f); break;
                case "Bow":
                    Camera(motion, w, "Sky", 0, 22f / 30f, 5, 0, -12);
                    Camera(motion, w, "Field", 22f / 30f, 1.6f, 0, 2);
                    Impact(motion, w, 40f / 30f, .5f, .3f, .12f); break;
                case "Spear":
                    Camera(motion, w, "Jump", .3f, .9f, 0, 1.5f, -6);
                    Camera(motion, w, "Throw", 1, 1.1f, -6, 0);
                    Camera(motion, w, "Impact", 1.3f, 1.5f, 8, -1, 0, true);
                    Impact(motion, w, 1.3f, 1.8f, 1.2f, .18f, .25f, .08f); break;
                case "DoubleAxe":
                    Camera(motion, w, "Spin", .4f, 2.9f, 4, 0);
                    Shake(motion, w, "Spin", .4f, 2.9f, .2f, .3f, 0, true);
                    Impact(motion, w, 2.9f, .9f, .6f, .15f); break;
                case "DualBlade":
                    Camera(motion, w, "Spin", .3f, 1, 6, 0);
                    Shake(motion, w, "Spin", .3f, 1, 0, .4f, .6f, true);
                    Camera(motion, w, "Tornado", .9f, 1.5f, 0, 1.5f, -4); break;
                case "Staff":
                    Camera(motion, w, "Charge", 0, .8f, -6, -1.2f, 0, false, new Vector3(.4f, 0, 0));
                    Camera(motion, w, "Release", .8f, 1f, 10, 0, 0, true);
                    Impact(motion, w, .8f, .6f, .4f, .15f);
                    Camera(motion, w, "Beam", .9f, 2.5f, 0, 1.5f);
                    Shake(motion, w, "Beam", .9f, 2.5f, .3f, 0, 0, true);
                    Lock(motion, 0, .9f); break;
                case "GreatSword":
                    Camera(motion, w, "Lift", 0, .9f, 0, -.5f, -10);
                    Camera(motion, w, "Slam", 1.1f, 1.4f, 8, 0, 0, true);
                    Impact(motion, w, 1.1f, 2f, 1.2f, .18f, .25f, .1f);
                    Lock(motion, 0, 1.1f); break;
                case "Whip":
                    Camera(motion, w, "Wide", 0, 1.5f, 0, 2.5f, 8);
                    Impact(motion, w, 1.4f, .9f, .6f, .15f); break;
                case "SwordShield":
                    Camera(motion, w, "Rise", .4f, 1f, 0, 0, -15);
                    Camera(motion, w, "Fall", 1f, 1.5f, 0, 2f);
                    Camera(motion, w, "Break", 1.5f, 1.8f, 6, 0, 0, true);
                    Impact(motion, w, 1.5f, 1.8f, 1.2f, .18f, .2f, .1f);
                    Lock(motion, .4f, 1.5f); break;
            }
        }

        private static void Camera(MotionSet motion, string weapon, string key, float start, float end,
            float fov, float zoom, float pitch = 0, bool unscaled = false, Vector3 offset = default)
        {
            var evt = new CameraEffectEvent();
            float duration = end - start;
            if (fov != 0) { var data = Effect<FOVCameraEffectData>(weapon, key + "Fov", duration, unscaled); data.fovDelta = fov; data.fovCurve = AnimationCurve.Constant(0, 1, 1); evt.effectDataList.Add(data); }
            if (zoom != 0) { var data = Effect<ZoomCameraEffectData>(weapon, key + "Zoom", duration, unscaled); data.distanceDelta = zoom; data.offsetDelta = offset; data.zoomCurve = AnimationCurve.Constant(0, 1, 1); evt.effectDataList.Add(data); }
            if (pitch != 0) { var data = Effect<RotationCameraEffectData>(weapon, key + "Pitch", duration, unscaled); data.pitchDelta = pitch; data.rotationCurve = AnimationCurve.Constant(0, 1, 1); evt.effectDataList.Add(data); }
            Add(motion, evt, start, end);
        }

        private static T Effect<T>(string weapon, string key, float duration, bool unscaled) where T : CameraEffectData
        {
            T data = GetOrCreate<T>(CameraRoot + weapon + "/" + weapon + "_" + key + ".asset");
            data.effectKey = "Signature." + weapon + "." + key;
            data.duration = duration; data.priority = 20; data.useUnscaledTime = unscaled;
            data.blendInDuration = Mathf.Min(.05f, duration * .2f); data.blendOutDuration = Mathf.Min(.3f, duration * .4f);
            Dirty(data); return data;
        }

        private static void Lock(MotionSet motion, float start, float end)
        {
            var effect = Effect<ZoomCameraEffectData>(motion.motionSetName.Replace("_Signature", ""), "InputLock", end - start, false);
            effect.distanceDelta = 0f; effect.offsetDelta = Vector3.zero;
            Add(motion, new CameraEffectEvent { lockCameraInput = true, effectDataList = new List<CameraEffectData> { effect } }, start, end);
        }

        private static void Impact(MotionSet motion, string weapon, float start, float pitch, float yaw, float duration,
            float timeScale = 1f, float slowDuration = 0f)
        {
            Shake(motion, weapon, "Impact", start, start + duration, pitch, yaw, 0, false);
            if (timeScale >= 1f) return;
            var data = Effect<TimeScaleCameraEffectData>(weapon, "Slow", slowDuration, true);
            data.targetTimeScale = timeScale; data.useHitStopManager = true;
            Add(motion, new CameraEffectEvent { effectDataList = new List<CameraEffectData> { data } }, start, start + duration);
        }

        private static void Shake(MotionSet motion, string weapon, string key, float start, float end,
            float pitch, float yaw, float roll, bool continuous)
        {
            var shake = GetOrCreate<CameraShakeData>("Assets/10.Datas/Camera/CameraShake/Signature_" + weapon + "_" + key + ".asset");
            shake.key = "Signature_" + weapon + "_" + key; shake.Duration = end - start;
            shake.PitchAmplitude = pitch; shake.YawAmplitude = yaw; shake.RollAmplitude = roll;
            shake.DirectionalBias = weapon == "Katana" ? .7f : .5f;
            shake.Frequency = continuous ? 5 : 22;
            shake.Dampening = continuous ? CameraShakeData.DampeningType.Constant : CameraShakeData.DampeningType.EaseOut;
            Dirty(shake);
            var effect = Effect<ShakeCameraEffectData>(weapon, key + "Shake", end - start, true);
            effect.shakeData = shake;
            Add(motion, new CameraEffectEvent { effectDataList = new List<CameraEffectData> { effect } }, start, end);
        }

        private static void BuildSpearAbility(GameplayAbilitySO source, UPlayGroundMotionAbilityPayloadSO payload)
        {
            var ability = GetOrCreate<GameplayAbilitySO>(AbilityRoot + "GA_PlayerSpear_Ultimate.asset");
            EditorUtility.CopySerialized(source, ability); ability.name = "GA_PlayerSpear_Ultimate";
            ability.abilityId = "Player.Spear.Ultimate"; ability.variants[0].executionPayload = payload;
            Dirty(ability);
            var set = GetOrCreate<AbilitySetSO>(AbilityRoot + "AbilitySet_PlayerSpear.asset");
            set.baseSet = Require<AbilitySetSO>("Assets/10.Datas/Ability/Migrated/PlayerDefaultAttackData/AbilitySet_PlayerDefaultAttackData.asset");
            set.playerSlots.Clear(); set.additionalAbilities.Clear(); set.combatBindings.Clear(); set.abilityOverrides.Clear();
            set.abilityOverrides.Add(new AbilitySetSO.AbilityOverrideEntry { sourceAbility = source,
                replacementAbility = ability, operation = AbilitySetOverrideOperation.Replace });
            set.RebuildRuntimeIndex();
            Dirty(set);
        }

        private static void BuildShieldEffect()
        {
            var effect = GetOrCreate<GameplayEffectSO>(AbilityRoot + "GE_SwordShield_SignatureProtection.asset");
            effect.effectId = "Effect.SwordShield.SignatureProtection";
            effect.durationType = GameplayEffectDurationType.Duration; effect.durationSeconds = 55f / 30f;
            effect.polarity = GameplayEffectPolarity.Beneficial; effect.ignorePassiveDurationModifiers = true;
            effect.modifiers = new List<GameplayEffectModifierDefinition>
            { new() { attributeId = Attributes.Combat.DamageTakenMultiplier.Value, modifierType = ModifierType.Multiply, value = .5f } };
            Dirty(effect);
        }

        private static void ApplyWeapon(Recipe recipe)
        {
            string weapon = recipe.Weapon;
            var staged = Require<UPlayGroundMotionAbilityPayloadSO>(AbilityRoot + "Payload_" + weapon + "_Signature.asset");
            GameplayAbilitySO ability = weapon == "Spear" ? Require<GameplayAbilitySO>(AbilityRoot + "GA_PlayerSpear_Ultimate.asset") : SourceAbility(weapon);
            Touch(ability);
            var original = ability.variants[0].executionPayload as UPlayGroundMotionAbilityPayloadSO;
            if (weapon == "Katana" || weapon == "DoubleAxe" || weapon == "Spear") ability.variants[0].executionPayload = staged;
            else
            {
                if (original == null || original.GetType() != typeof(UPlayGroundMotionAbilityPayloadSO)) throw new InvalidOperationException(weapon + " 일반 Payload 타입 불일치");
                Touch(original); string name = original.name; EditorUtility.CopySerialized(staged, original); original.name = name; Dirty(original);
            }
            ActorAnimationMotionSet set = Require<ActorAnimationMotionSet>(PlayerSetRoot + "Humanoid_Player_" + weapon + "AnimationSet.asset");
            Map(set, weapon + ".Ultimate", Require<MotionSetAsset>(MotionRoot + weapon + "_Signature.asset"));
            if (weapon == "Katana") Map(Require<ActorAnimationMotionSet>("Assets/10.Datas/Actor/Footsteps/Motions/Set_Katana.asset"), "Katana.Ultimate", Require<MotionSetAsset>(MotionRoot + "Katana_Raon_Signature.asset"));
            if (weapon == "Whip") Map(Require<ActorAnimationMotionSet>("Assets/10.Datas/Actor/Animation/WeaponMotion/Whip/WhipMotionSet.asset"), "Whip.Ultimate", Require<MotionSetAsset>(MotionRoot + "Whip_Weapon_Signature.asset"));
            if (weapon == "Spear")
            {
                var spearSet = Require<AbilitySetSO>(AbilityRoot + "AbilitySet_PlayerSpear.asset");
                var definition = Require<PlayerCharacterDefinitionSO>("Assets/10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_Reine.asset");
                Touch(definition); definition.abilitySet = spearSet; Dirty(definition);
                Touch(set); set.attackAbilitySet = spearSet; Dirty(set);
                string modelPath = "Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Reine.prefab";
                var model = Require<GameObject>(modelPath).GetComponent<CharacterModelData>();
                if (model == null || model.Definition != definition) throw new InvalidOperationException("레이네 모델의 정의 연결 불일치");
            }
            Dirty(ability);
        }

        private static void ApplyShieldRules()
        {
            var registry = GameplayTagRegistry.Registry;
            if (!registry.IsRegistered(ShieldAbsentTag))
            { Touch(registry); registry.tags.Add(new GameplayTagDefinition { tagName = ShieldAbsentTag, description = "방패 투척 궁극기 실행 중 반격 차단" }); registry.RebuildLookup(); Dirty(registry); }
            GameplayTag tag = GameplayTagRegistry.GetRequired(ShieldAbsentTag);
            var ability = SourceAbility("SwordShield"); Touch(ability);
            if (!ability.activation.executionGrantedTagIds.Contains(tag)) ability.activation.executionGrantedTagIds.Add(tag);
            var effect = Require<GameplayEffectSO>(AbilityRoot + "GE_SwordShield_SignatureProtection.asset");
            if (!ability.variants[0].ownerEffects.Contains(effect)) ability.variants[0].ownerEffects.Add(effect);
            Dirty(ability);
            var set = Require<ActorAnimationMotionSet>(PlayerSetRoot + "Humanoid_Player_SwordShieldAnimationSet.asset").attackAbilitySet;
            foreach (PlayerCombatAbilitySlot slot in new[] { PlayerCombatAbilitySlot.CounterAttack, PlayerCombatAbilitySlot.ParryCounterAttack })
            {
                GameplayAbilitySO counter = set.GetCombatAbility(slot);
                if (counter == null) throw new InvalidOperationException("검방패 반격 Ability 없음: " + slot);
                Touch(counter); if (!counter.activation.blockedTagIds.Contains(tag)) counter.activation.blockedTagIds.Add(tag); Dirty(counter);
            }
        }

        private static void ApplyImpactEffects()
        {
            var database = Require<UPlayGround.Data.Path.FXPrefabDatabase>("Assets/10.Datas/Path/FXPrefabDatabase.asset");
            Touch(database);
            var serialized = new SerializedObject(database);
            var entries = serialized.FindProperty("prefabs");
            foreach (string weapon in new[] { "Spear", "SwordShield" })
            {
                string key = "Signature_" + weapon + "_Impact";
                GameObject prefab = Require<GameObject>("Assets/03.Prefabs/Effects/Signature/" + weapon + "_Impact_Signature.prefab");
                bool exists = false;
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("key").stringValue != key) continue;
                    if (entry.FindPropertyRelative("prefab").objectReferenceValue != prefab)
                        throw new InvalidOperationException("기존 FX 키 충돌: " + key);
                    exists = true;
                }
                if (exists) continue;
                int index = entries.arraySize++;
                var added = entries.GetArrayElementAtIndex(index);
                added.FindPropertyRelative("key").stringValue = key;
                added.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                added.FindPropertyRelative("description").stringValue = "시그니처 지점 착탄";
            }
            serialized.ApplyModifiedProperties();
            Dirty(database);
        }

        private static void Map(ActorAnimationMotionSet set, string key, MotionSetAsset motion)
        { Touch(set); set.SetAbilityMotionAsset(new MotionKey(key), motion); Dirty(set); }

        private static void ValidateWeapon(Recipe recipe)
        {
            var payload = Require<UPlayGroundMotionAbilityPayloadSO>(AbilityRoot + "Payload_" + recipe.Weapon + "_Signature.asset");
            var source = Require<UPlayGroundMotionAbilityPayloadSO>(AbilityRoot + "SourcePayload_" + recipe.Weapon + ".asset");
            var motion = Require<MotionSetAsset>(MotionRoot + recipe.Weapon + "_Signature.asset").motionSet;
            var sourceAbility = SourceAbility(recipe.Weapon);
            if (sourceAbility.variants.Count != 1 || sourceAbility.cost.resourceType != AbilityResourceType.UltimateEnergy
                || Mathf.Abs(sourceAbility.cost.value - 1f) > .001f || Mathf.Abs(sourceAbility.cooldown.durationSeconds - 12f) > .001f)
                throw new InvalidOperationException(recipe.Weapon + " 궁극기 비용·쿨다운·Variant 계약 불일치");
            if (Mathf.Abs(motion.TotalDuration - recipe.Frames / 30f) > .001f || motion.motions.Any(m => m.motionClip == null)) throw new InvalidOperationException(recipe.Weapon + " 클립·시간 불일치");
            if (payload.attackInfo.baseInfo.hitPhases.Count != recipe.Repeats.Length) throw new InvalidOperationException(recipe.Weapon + " 페이즈 수 불일치");
            Vector3 actual = Vector3.zero;
            for (int i = 0; i < recipe.Repeats.Length; i++)
            { HitPhaseData phase = payload.attackInfo.baseInfo.hitPhases[i]; actual += new Vector3(phase.damage, phase.poiseDamage, phase.breakDamage) * recipe.Repeats[i]; }
            var balanceSource = recipe.Weapon == "Spear"
                ? Require<UPlayGroundMotionAbilityPayloadSO>(AbilityRoot + "SourcePayload_GreatSword.asset") : source;
            if (Vector3.Distance(actual, Totals(balanceSource.attackInfo.baseInfo.hitPhases)) > .01f) throw new InvalidOperationException(recipe.Weapon + " 기대 피해·강인도 합 불일치");
            foreach (MotionEventBase evt in motion.globalEvents)
            {
                if (evt == null || evt.startTime < 0 || evt.endTime < evt.startTime || evt.endTime > motion.TotalDuration + .001f) throw new InvalidOperationException(recipe.Weapon + " 이벤트 시간 불일치");
                if (evt is BeginCollisionEvent collision && (collision.hitPhaseIndex >= recipe.Repeats.Length || !collision.explicitShape.Validate(out _))) throw new InvalidOperationException(recipe.Weapon + " 판정 불일치");
                if (evt is SpawnProjectileEvent spawn && (spawn.projectileDefinition == null || spawn.hitPhaseIndex >= recipe.Repeats.Length)) throw new InvalidOperationException(recipe.Weapon + " 투사체 불일치");
                if (evt is BeginParticleEvent particle && particle.particlePrefab == null) throw new InvalidOperationException(recipe.Weapon + " FX 없음");
                if (evt is CameraEffectEvent camera && camera.effectDataList.Any(data => data == null)) throw new InvalidOperationException(recipe.Weapon + " 카메라 SO 없음");
            }
            if (motion.globalEvents.Count(e => e is UltimateFaceStripMotionEvent) != 1) throw new InvalidOperationException(recipe.Weapon + " 얼굴 표시 이벤트 중복·누락");
            float slowTotal = 0f;
            foreach (var camera in motion.globalEvents.OfType<CameraEffectEvent>())
            foreach (CameraEffectData effect in camera.effectDataList)
            {
                if (effect is RotationCameraEffectData rotation && (Mathf.Abs(rotation.pitchDelta) > 15f || Mathf.Abs(rotation.yawDelta) > 15f)) throw new InvalidOperationException(recipe.Weapon + " 카메라 회전 상한 초과");
                if (effect is FOVCameraEffectData fov && fov.fovDelta > 10f) throw new InvalidOperationException(recipe.Weapon + " FOV 상한 초과");
                if (effect is ShakeCameraEffectData shake && (shake.shakeData == null || shake.shakeData.RollAmplitude > .8f)) throw new InvalidOperationException(recipe.Weapon + " 흔들림 상한 초과·누락");
                if (effect is TimeScaleCameraEffectData slow)
                { if (!slow.useHitStopManager || !slow.useUnscaledTime) throw new InvalidOperationException(recipe.Weapon + " 슬로 소유·시간 기준 불일치"); slowTotal += slow.duration; }
            }
            if (slowTotal > .15f) throw new InvalidOperationException(recipe.Weapon + " 누적 슬로 상한 초과");
        }

        private static Vector3 Totals(List<HitPhaseData> phases)
        {
            if (phases == null || phases.Count == 0) throw new InvalidOperationException("기준 궁극기 히트 페이즈가 없습니다.");
            Vector3 total = Vector3.zero;
            foreach (HitPhaseData phase in phases) total += new Vector3(phase.damage, phase.poiseDamage, phase.breakDamage);
            return total;
        }

        private static void CheckInputs(Recipe recipe)
        {
            if (SourceAbility(recipe.Weapon).variants.Count != 1) throw new InvalidOperationException(recipe.Weapon + " 복수 Variant는 자동 교체할 수 없습니다.");
            var staged = Load<MotionSetAsset>(MotionRoot + recipe.Weapon + "_Signature.asset");
            var liveSet = Load<ActorAnimationMotionSet>(PlayerSetRoot + "Humanoid_Player_" + recipe.Weapon + "AnimationSet.asset");
            if (staged != null && liveSet != null && liveSet.GetAbilityMotionAsset(new MotionKey(recipe.Weapon + ".Ultimate")) == staged)
                throw new InvalidOperationException(recipe.Weapon + " 준비 에셋이 이미 라이브에 연결되었습니다. 재준비 전 원본 연결 복구가 필요합니다.");
            Require<GameObject>("Assets/03.Prefabs/Effects/Signature/" + recipe.Weapon + "_Signature.prefab");
            if (recipe.Weapon is "Bow" or "Spear" or "SwordShield" or "Staff" or "Katana") SignatureClip(recipe.Weapon);
            if (recipe.Weapon == "Katana") SignatureClip("Katana_Raon");
            if (recipe.Weapon is "Bow" or "Spear" or "SwordShield" or "DualBlade") Projectile(recipe.Weapon);
        }

        private static GameplayAbilitySO SourceAbility(string weapon)
            => Require<GameplayAbilitySO>(weapon == "Staff" ? "Assets/10.Datas/Ability/Player/Staff/GA_PlayerStaff_Ultimate.asset"
                : "Assets/10.Datas/Ability/Migrated/Player" + (weapon == "Spear" ? "Default" : weapon) + "AttackData/GA_Player" + (weapon == "Spear" ? "Default" : weapon) + "AttackData_Ultimate.asset");

        private static ProjectileDefinitionSO Projectile(string weapon)
            => Require<ProjectileDefinitionSO>(ProjectileRoot + "Projectile_" + weapon + (weapon == "Bow" ? "SignatureRain" : weapon == "DualBlade" ? "SignatureTornado" : "SignatureFall") + ".asset");

        private static AnimationClip SignatureClip(string weapon)
        {
            if (weapon == "Katana_Raon")
                return Require<AnimationClip>("Assets/07.Animations/Player/Signature/Katana/Katana_Raon_Signature.anim");
            string folder = weapon == "Katana_Raon" ? "Katana" : weapon;
            string path = "Assets/07.Animations/Player/Signature/" + folder + "/" + weapon + "_Signature.fbx";
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            if (clips.Length != 1) throw new InvalidOperationException(path + ": 애니메이션 클립이 정확히 1개여야 합니다. 실제 " + clips.Length);
            return clips[0];
        }

        private static AnimationClip WhirlwindClip(bool dual)
        {
            string path = "Assets/ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/" + (dual
                ? "Frank_SlashPack_Dual/FBX Animation/Root_Motion/Frank_RPG_Dual_Skill01_WhirlWind.FBX"
                : "Frank_SlashPack_2Handed/FBX_Animation/Root_Motion/Frank_RPG_2Hand_Skill01_WhirlWind.FBX");
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            if (clips.Length != 1) throw new InvalidOperationException(path + ": 입력 클립 모호성 " + clips.Length);
            return clips[0];
        }

        private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
        private static T Require<T>(string path) where T : UnityEngine.Object => Load<T>(path) ?? throw new InvalidOperationException("필수 에셋 없음: " + path);

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = Load<T>(path);
            if (asset != null) { Touch(asset); return asset; }
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("기존 에셋 타입 불일치: " + path);
            asset = ScriptableObject.CreateInstance<T>(); asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
            CreateAsset(asset, path); return asset;
        }

        private static void CreateAsset(ScriptableObject asset, string path)
        {
            string folder = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(folder); AssetDatabase.CreateAsset(asset, path); Created.Add(path);
            Undo.RegisterCreatedObjectUndo(asset, "시그니처 데이터 생성"); Touched.Add(asset);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        private static void Touch(UnityEngine.Object asset)
        { if (!Touched.Contains(asset)) { Undo.RegisterCompleteObjectUndo(asset, "시그니처 데이터 변경"); Touched.Add(asset); } }
        private static void Dirty(UnityEngine.Object asset) { EditorUtility.SetDirty(asset); if (!Touched.Contains(asset)) Touched.Add(asset); }

        private static string Transaction(string name, Func<string> action)
        {
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName(name);
            Created.Clear(); Touched.Clear();
            try
            {
                string result = action();
                foreach (UnityEngine.Object asset in Touched) if (asset != null) AssetDatabase.SaveAssetIfDirty(asset);
                Undo.CollapseUndoOperations(group); return result;
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (string path in Created) if (System.IO.File.Exists(path)) AssetDatabase.DeleteAsset(path);
                foreach (UnityEngine.Object asset in Touched)
                {
                    if (asset == null) continue;
                    if (asset is GameplayTagRegistrySO registry) registry.RebuildLookup();
                    if (asset is AbilitySetSO set) set.RebuildRuntimeIndex();
                    AssetDatabase.SaveAssetIfDirty(asset);
                }
                throw;
            }
            finally { Created.Clear(); Touched.Clear(); }
        }

        private static void CheckEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일이 끝난 Edit Mode에서 실행해야 합니다.");
        }
    }
}
