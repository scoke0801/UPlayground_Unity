#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Animation;
// UnityEngine.Motion(레거시 애니메이션 타입)과 이름이 겹친다.
using Motion = UPlayGround.Animation.Motion;
using UPlayGround.Tool.Editor.AI;

namespace UPlayGround.Editor
{
    /// <summary>
    /// Humanoid 일반 몬스터(Enemy_Random_*)의 GAS 데이터를 일괄 저작하는 batchmode 진입점 모음.
    /// 설계 근거는 Assets/docs/TODO/HUMANOID_MONSTER_GAS_BT_DESIGN.md.
    ///
    /// 안전 규칙 (CLAUDE.md "Editor 데이터 도구 안전 규칙"):
    ///  - 모든 변경은 단일 Undo 그룹으로 묶고, 예외 시 RevertAllDownToGroup으로 전체 롤백한다.
    ///  - 에셋을 생성·삭제하지 않는다. 기존 에셋의 필드만 수정한다.
    ///  - DRY_RUN(Preview) 모드에서 변경 예정 내역만 출력하고 아무것도 쓰지 않는다.
    ///
    /// batchmode 사용:
    ///   Unity.exe -batchmode -quit -nographics -projectPath &lt;proj&gt;
    ///     -executeMethod UPlayGround.Editor.HumanoidAuthoringBatch.&lt;Step&gt;
    ///     -logFile &lt;log&gt; [-uplayground-apply]
    /// 기본은 Preview다. 실제 적용은 -uplayground-apply 인자를 붙인다.
    /// </summary>
    public static class HumanoidAuthoringBatch
    {
        private const string ApplyArgument = "-uplayground-apply";
        private const string AbilityRoot = "Assets/10.Datas/Ability/Actor";
        private const string MotionRoot = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid";

        /// <summary>이번 작업이 다루는 5개 아키타입. 키는 AbilitySet 폴더 접미사다.</summary>
        private static readonly string[] Archetypes =
        {
            "GreatSword", "DoubleAxe", "DualBlade", "SwordShield", "Bow"
        };

        // ────────────────────────────────────────────────────────────────
        // 실행 컨텍스트
        // ────────────────────────────────────────────────────────────────

        private sealed class BatchContext : IDisposable
        {
            private readonly string _name;
            private readonly int _undoGroup;
            private readonly StringBuilder _log = new();
            private int _changeCount;
            private bool _failed;

            public bool Apply { get; }

            public BatchContext(string name)
            {
                _name = name;
                Apply = Environment.GetCommandLineArgs().Contains(ApplyArgument);
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName($"HumanoidAuthoringBatch.{name}");
                _undoGroup = Undo.GetCurrentGroup();
                Line($"===== {name} ({(Apply ? "APPLY" : "PREVIEW")}) =====");
            }

            public void Line(string message) => _log.AppendLine(message);

            /// <summary>단일 필드 변경을 기록한다. Apply가 아니면 호출자가 쓰기를 건너뛴다.</summary>
            public void Change(UnityEngine.Object target, string field, object before, object after)
            {
                _changeCount++;
                Line($"  [{target.name}] {field}: {Fmt(before)} -> {Fmt(after)}");
            }

            public void MarkFailed() => _failed = true;

            /// <summary>수정 직전 호출. Apply일 때만 Undo에 등록한다.</summary>
            public void Record(UnityEngine.Object target)
            {
                if (Apply)
                    Undo.RegisterCompleteObjectUndo(target, _name);
            }

            public void Dirty(UnityEngine.Object target)
            {
                if (Apply)
                    EditorUtility.SetDirty(target);
            }

            public void Dispose()
            {
                if (_failed)
                {
                    Undo.RevertAllDownToGroup(_undoGroup);
                    Line($"!! 실패 — Undo 그룹 전체 롤백함. 변경 없음.");
                }
                else if (Apply)
                {
                    Undo.CollapseUndoOperations(_undoGroup);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                Line($"----- {_name}: {_changeCount}건 {(_failed ? "롤백" : Apply ? "적용" : "미적용(Preview)")} -----");
                Debug.Log(_log.ToString());
            }

            private static string Fmt(object value) =>
                value switch
                {
                    null => "null",
                    float f => f.ToString("0.###", CultureInfo.InvariantCulture),
                    _ => value.ToString()
                };
        }

        private static void Run(string name, Action<BatchContext> body)
        {
            using var ctx = new BatchContext(name);
            try
            {
                body(ctx);
            }
            catch (Exception ex)
            {
                ctx.MarkFailed();
                ctx.Line($"예외: {ex}");
                // 롤백은 Dispose에서 수행한다. batchmode 종료 코드를 실패로 만든다.
                EditorApplication.Exit(1);
            }
        }

        // ────────────────────────────────────────────────────────────────
        // 조회 헬퍼
        // ────────────────────────────────────────────────────────────────

        private static string AbilityFolder(string archetype) =>
            $"{AbilityRoot}/Humanoid_{FolderToken(archetype)}AttackData";

        /// <summary>폴더명이 enum/무기명과 어긋나는 예외를 흡수한다. Spear 폴더는 오타 그대로 "Speat"다.</summary>
        private static string FolderToken(string archetype) =>
            archetype == "Spear" ? "Speat" : archetype;

        private static IEnumerable<GameplayAbilitySO> LoadAbilities(string archetype)
        {
            string folder = AbilityFolder(archetype);
            foreach (string guid in AssetDatabase.FindAssets("t:GameplayAbilitySO", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ability = AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(path);
                if (ability != null)
                    yield return ability;
            }
        }

        /// <summary>Ability의 Default Variant가 물고 있는 Motion Payload를 꺼낸다.</summary>
        private static UPlayGroundMotionAbilityPayloadSO PayloadOf(GameplayAbilitySO ability)
        {
            if (ability?.variants == null)
                return null;
            foreach (AbilityVariantDefinition variant in ability.variants)
            {
                if (variant?.executionPayload is UPlayGroundMotionAbilityPayloadSO payload)
                    return payload;
            }
            return null;
        }

        /// <summary>MotionSet 안의 모든 MotionEvent를 계층 구분 없이 훑는다.</summary>
        private static IEnumerable<MotionEventBase> AllEvents(MotionSetAsset asset)
        {
            MotionSet set = asset?.motionSet;
            if (set == null)
                yield break;

            foreach (MotionEventBase e in Safe(set.globalEvents))
                yield return e;
            foreach (Motion motion in Safe(set.motions))
                foreach (MotionEventBase e in Safe(motion?.events))
                    yield return e;
            foreach (MotionLayer layer in Safe(set.layers))
            {
                foreach (MotionEventBase e in Safe(layer?.globalEvents))
                    yield return e;
                foreach (Motion motion in Safe(layer?.motions))
                    foreach (MotionEventBase e in Safe(motion?.events))
                        yield return e;
            }
        }

        private static IEnumerable<T> Safe<T>(List<T> list) =>
            list ?? Enumerable.Empty<T>();

        /// <summary>Ability의 MotionKey를 해당 아키타입 AnimationSet에서 MotionSetAsset으로 해석한다.</summary>
        private static MotionSetAsset ResolveMotion(
            ActorAnimationMotionSet animationSet,
            GameplayAbilitySO ability)
        {
            UPlayGroundMotionAbilityPayloadSO payload = PayloadOf(ability);
            MotionKey key = payload?.attackInfo?.motionKey ?? default;
            // fallbackMotionSet 체인까지 따라가는 정식 접근자를 쓴다.
            return animationSet != null ? animationSet.GetAbilityMotionAsset(key) : null;
        }

        private static ActorAnimationMotionSet LoadAnimationSet(string archetype)
        {
            // Spear 자산 파일명만 폴더 토큰과 다르다.
            string file = archetype == "Spear" ? "Spear" : archetype;
            string path = $"{MotionRoot}/Humanoid_{file}AnimationSet.asset";
            return AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(path);
        }

        // ────────────────────────────────────────────────────────────────
        // Step 1 — 현황 덤프 (읽기 전용, 안전 확인용)
        // ────────────────────────────────────────────────────────────────

        public static void Step_Report()
        {
            Run(nameof(Step_Report), ctx =>
            {
                foreach (string archetype in Archetypes)
                {
                    ActorAnimationMotionSet animationSet = LoadAnimationSet(archetype);
                    ctx.Line($"[{archetype}] animationSet={(animationSet != null ? animationSet.name : "NULL")}");
                    foreach (GameplayAbilitySO ability in LoadAbilities(archetype).OrderBy(a => a.name))
                    {
                        UPlayGroundMotionAbilityPayloadSO payload = PayloadOf(ability);
                        AbilityAttackInfo info = payload?.attackInfo;
                        MotionSetAsset motion = ResolveMotion(animationSet, ability);
                        int hitEvents = motion == null
                            ? -1
                            : AllEvents(motion).Count(e => e is BeginCollisionEvent or SpawnProjectileEvent);
                        int phases = info?.baseInfo?.hitPhases?.Count ?? -1;
                        ctx.Line(
                            $"  {ability.name,-52} cat={info?.attackCategory} roles={info?.aiRoles} " +
                            $"w={info?.selectionWeight} cd={ability.cooldown?.durationSeconds} " +
                            $"at={info?.baseInfo?.attackType} phases={phases} hitEvents={hitEvents} " +
                            $"motion={(motion != null ? motion.name : "UNRESOLVED")}");
                    }
                }
            });
        }

        // ────────────────────────────────────────────────────────────────
        // Step 2 — Bow 수정 (설계서 §2.2 / §6.5 / §10.2)
        //   (1) attackType Melee -> Ranged  : 근접 사거리 클램프 탈출. 궁수 동작의 단일 결정 결함.
        //   (2) projectilePrefab 교체        : Nenmir_Default_Arrow(플레이어용) -> DefaultArrow(몬스터 공용)
        //   (3) hitPhase 개수 정합 + 결선    : SpawnProjectile 수만큼 페이즈를 만들고 hitPhaseIndex를 0..N-1로
        // ────────────────────────────────────────────────────────────────

        private const string MonsterArrowPath = "Assets/03.Prefabs/Projectile/DefaultArrow.prefab";

        /// <summary>Counter 계열은 근접 판정이므로 Ranged 전환 대상이 아니다.</summary>
        private static bool IsBowRangedAbility(GameplayAbilitySO ability) =>
            !ability.name.Contains("Counter", StringComparison.OrdinalIgnoreCase);

        public static void Step_BowFix()
        {
            Run(nameof(Step_BowFix), ctx =>
            {
                var arrow = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterArrowPath);
                BaseProjectile arrowProjectile = arrow != null ? arrow.GetComponent<BaseProjectile>() : null;
                if (arrowProjectile == null)
                    throw new InvalidOperationException($"몬스터 공용 화살 프리팹을 찾지 못했습니다: {MonsterArrowPath}");

                ActorAnimationMotionSet animationSet = LoadAnimationSet("Bow");
                if (animationSet == null)
                    throw new InvalidOperationException("Humanoid_BowAnimationSet을 찾지 못했습니다.");

                foreach (GameplayAbilitySO ability in LoadAbilities("Bow").OrderBy(a => a.name))
                {
                    UPlayGroundMotionAbilityPayloadSO payload = PayloadOf(ability);
                    AttackInfoBase baseInfo = payload?.attackInfo?.baseInfo;
                    if (baseInfo == null)
                        throw new InvalidOperationException($"{ability.name}: baseInfo가 없습니다.");

                    MotionSetAsset motion = ResolveMotion(animationSet, ability);
                    if (motion == null)
                        throw new InvalidOperationException($"{ability.name}: MotionKey가 해석되지 않습니다.");

                    // 플레이어 Bow MotionSet을 잘못 잡지 않았는지 경로로 확인한다.
                    string motionPath = AssetDatabase.GetAssetPath(motion);
                    if (!motionPath.StartsWith(MotionRoot, StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            $"{ability.name}: Humanoid 폴더 밖 MotionSet을 참조합니다 — {motionPath}");

                    // hitPhaseIndex는 발사 시간 순서와 일치해야 한다. 레이어를 가로질러 수집하면
                    // 저장 순서가 시간 순이 아니므로 반드시 startTime으로 정렬한다.
                    var shots = AllEvents(motion)
                        .OfType<SpawnProjectileEvent>()
                        .OrderBy(e => e.startTime)
                        .ToList();

                    // (1) attackType
                    if (IsBowRangedAbility(ability) && baseInfo.attackType != AttackType.Ranged)
                    {
                        ctx.Change(payload, "baseInfo.attackType", baseInfo.attackType, AttackType.Ranged);
                        if (ctx.Apply)
                        {
                            ctx.Record(payload);
                            baseInfo.attackType = AttackType.Ranged;
                            ctx.Dirty(payload);
                        }
                    }

                    if (shots.Count == 0)
                        continue;

                    // (3) hitPhase 개수 정합 — 총량 보존으로 분할한다.
                    if (baseInfo.hitPhases == null || baseInfo.hitPhases.Count == 0)
                        throw new InvalidOperationException($"{ability.name}: hitPhases가 비어 있습니다.");

                    if (baseInfo.hitPhases.Count != shots.Count)
                    {
                        HitPhaseData source = baseInfo.hitPhases[0];
                        ctx.Change(payload, "baseInfo.hitPhases.Count",
                            baseInfo.hitPhases.Count, shots.Count);
                        ctx.Line($"      총량 보존 분할: dmg {source.damage} -> {source.damage / shots.Count:0.##} x{shots.Count}");
                        if (ctx.Apply)
                        {
                            ctx.Record(payload);
                            var split = new List<HitPhaseData>(shots.Count);
                            for (var i = 0; i < shots.Count; i++)
                            {
                                HitPhaseData clone = ClonePhase(source);
                                clone.damage = source.damage / shots.Count;
                                clone.poiseDamage = source.poiseDamage / shots.Count;
                                clone.breakDamage = source.breakDamage / shots.Count;
                                split.Add(clone);
                            }
                            baseInfo.hitPhases = split;
                            ctx.Dirty(payload);
                        }
                    }

                    // (2)(3) MotionEvent — 프리팹 교체와 hitPhaseIndex 결선
                    for (var i = 0; i < shots.Count; i++)
                    {
                        SpawnProjectileEvent shot = shots[i];
                        bool motionDirty = false;

                        // 프리팹 교체는 "소유권 정리"지 "거동 변경"이 아니다.
                        // Arcing(곡사) 같은 다른 구체 타입을 직사 화살로 바꾸면 공격 성격이 달라지므로 건너뛴다.
                        bool sameKind = shot.projectilePrefab != null
                                        && shot.projectilePrefab.GetType() == arrowProjectile.GetType();
                        if (shot.projectilePrefab != null && !sameKind)
                        {
                            ctx.Line($"  [{motion.name}] shot[{i}].projectilePrefab 유지 — "
                                     + $"{shot.projectilePrefab.name}({shot.projectilePrefab.GetType().Name})은 "
                                     + $"{arrowProjectile.GetType().Name}와 거동이 다름");
                        }
                        else if (shot.projectilePrefab != arrowProjectile)
                        {
                            ctx.Change(motion, $"shot[{i}].projectilePrefab",
                                shot.projectilePrefab != null ? shot.projectilePrefab.name : "null",
                                arrowProjectile.name);
                            if (ctx.Apply)
                            {
                                ctx.Record(motion);
                                shot.projectilePrefab = arrowProjectile;
                                motionDirty = true;
                            }
                        }

                        if (shot.hitPhaseIndex != i)
                        {
                            ctx.Change(motion, $"shot[{i}].hitPhaseIndex", shot.hitPhaseIndex, i);
                            if (ctx.Apply)
                            {
                                ctx.Record(motion);
                                shot.hitPhaseIndex = i;
                                motionDirty = true;
                            }
                        }

                        if (motionDirty)
                            ctx.Dirty(motion);
                    }
                }
            });
        }

        /// <summary>
        /// HitPhaseData는 클래스이고 중첩 참조(reactionProfile)를 갖는다. 필드를 손으로 옮기면
        /// 스키마가 늘 때 조용히 누락되므로 직렬화 왕복으로 깊은 복사한다.
        /// </summary>
        private static HitPhaseData ClonePhase(HitPhaseData source) =>
            JsonUtility.FromJson<HitPhaseData>(JsonUtility.ToJson(source));

        // ────────────────────────────────────────────────────────────────
        // Step 3 — 개별 결함 수정 (설계서 §2.3 / §6.7)
        // ────────────────────────────────────────────────────────────────

        public static void Step_FixDefects()
        {
            Run(nameof(Step_FixDefects), ctx =>
            {
                // (a) Enemy_Random_M_GreatSword_002 의 AbilitySet 오연결
                const string defPath =
                    "Assets/10.Datas/Actor/DataBase/Enemy_Random_M_GreatSword_002_ActorDef.asset";
                const string setPath =
                    "Assets/10.Datas/Ability/Actor/Humanoid_GreatSwordAttackData/AbilitySet_Humanoid_GreatSwordAttackData.asset";
                var actorDef = AssetDatabase.LoadAssetAtPath<Data.Actor.ActorDefinitionSO>(defPath);
                var greatSwordSet = AssetDatabase.LoadAssetAtPath<AbilitySetSO>(setPath);
                if (actorDef == null || greatSwordSet == null)
                    throw new InvalidOperationException("GreatSword_002 ActorDef 또는 GreatSword AbilitySet을 찾지 못했습니다.");

                if (actorDef.abilitySet != greatSwordSet)
                {
                    ctx.Change(actorDef, "abilitySet",
                        actorDef.abilitySet != null ? actorDef.abilitySet.name : "null",
                        greatSwordSet.name);
                    if (ctx.Apply)
                    {
                        ctx.Record(actorDef);
                        actorDef.abilitySet = greatSwordSet;
                        ctx.Dirty(actorDef);
                    }
                }

                // (b) hitPhases 수 > 실제 히트 이벤트 수 인 Ability — 남는 페이즈는 영원히 미발화한다.
                foreach (string archetype in Archetypes)
                {
                    ActorAnimationMotionSet animationSet = LoadAnimationSet(archetype);
                    foreach (GameplayAbilitySO ability in LoadAbilities(archetype).OrderBy(a => a.name))
                    {
                        UPlayGroundMotionAbilityPayloadSO payload = PayloadOf(ability);
                        AttackInfoBase baseInfo = payload?.attackInfo?.baseInfo;
                        MotionSetAsset motion = ResolveMotion(animationSet, ability);
                        if (baseInfo?.hitPhases == null || motion == null)
                            continue;

                        int hitEvents = AllEvents(motion)
                            .Count(e => e is BeginCollisionEvent or SpawnProjectileEvent);
                        if (hitEvents <= 0 || baseInfo.hitPhases.Count <= hitEvents)
                            continue;

                        ctx.Change(payload, "baseInfo.hitPhases.Count(초과분 제거)",
                            baseInfo.hitPhases.Count, hitEvents);
                        if (ctx.Apply)
                        {
                            ctx.Record(payload);
                            baseInfo.hitPhases = baseInfo.hitPhases.Take(hitEvents).ToList();
                            ctx.Dirty(payload);
                        }
                    }
                }
            });
        }

        // ────────────────────────────────────────────────────────────────
        // Step 4 — 역할·가중치·쿨다운·예고 (설계서 §6.2 / §6.4, 결정 D1)
        //
        // D1(b)안: aiSelectable은 건드리지 않는다. 후보 풀은 넓게 두고,
        //          역할 지명(aiRoles)으로 결정론 축만 좁힌다.
        //
        // 역할 배정은 모션 리듬에서 파생한다:
        //   Opener     — startup 최단 2개 (Basic)
        //   Punish     — Heavy 중 startup 짧고 데미지 높은 2개
        //   Counter    — Counter 계열 중 모션이 고유한 1개
        //   Finisher   — 데미지 최대 2개 (Basic/Heavy 무관)
        //   GapCloser  — 베이크된 maxDistance 최대 1개 (P5 이후여야 유효)
        //   Signature  — Skill 중 startup 최장 1~2개
        // ────────────────────────────────────────────────────────────────

        private sealed class RoleCandidate
        {
            public GameplayAbilitySO Ability;
            public UPlayGroundMotionAbilityPayloadSO Payload;
            public AbilityAttackInfo Info;
            public float Startup;
            public float Damage;
            public float MaxDistance;
            public int HitEvents;
            public bool IsCounterMotion;
            public AbilityAIRole Assigned;
        }

        /// <summary>역할별 (가중치, 쿨다운초, 쿨다운그룹 접미사, DangerRing) 규약. 보스 Siuha 실측 규약 승계.</summary>
        private static readonly Dictionary<AbilityAIRole, (float Weight, float Cooldown, string Group, bool Ring)> RoleTuning =
            new()
            {
                [AbilityAIRole.Opener] = (12f, 0.6f, null, false),
                [AbilityAIRole.Punish] = (9f, 6.0f, "Heavy", true),
                [AbilityAIRole.Counter] = (9f, 5.0f, "Heavy", true),
                [AbilityAIRole.Finisher] = (7f, 4.0f, null, true),
                [AbilityAIRole.GapCloser] = (5f, 8.0f, "Skill", true),
                [AbilityAIRole.Signature] = (4f, 12.0f, "Skill", true),
            };

        public static void Step_AssignRoles()
        {
            Run(nameof(Step_AssignRoles), ctx =>
            {
                foreach (string archetype in Archetypes)
                {
                    ActorAnimationMotionSet animationSet = LoadAnimationSet(archetype);
                    var candidates = new List<RoleCandidate>();

                    foreach (GameplayAbilitySO ability in LoadAbilities(archetype).OrderBy(a => a.name))
                    {
                        UPlayGroundMotionAbilityPayloadSO payload = PayloadOf(ability);
                        AbilityAttackInfo info = payload?.attackInfo;
                        MotionSetAsset motion = ResolveMotion(animationSet, ability);
                        if (info?.baseInfo?.hitPhases == null || motion == null)
                            continue;

                        var hits = AllEvents(motion)
                            .Where(e => e is BeginCollisionEvent or SpawnProjectileEvent)
                            .ToList();
                        if (hits.Count == 0)
                            continue;

                        candidates.Add(new RoleCandidate
                        {
                            Ability = ability,
                            Payload = payload,
                            Info = info,
                            Startup = hits.Min(e => e.startTime),
                            Damage = info.baseInfo.hitPhases.Sum(p => p.damage),
                            MaxDistance = ability.activation.maxDistance,
                            HitEvents = hits.Count,
                            IsCounterMotion = ability.name.Contains("Counter", StringComparison.OrdinalIgnoreCase),
                        });
                    }

                    if (candidates.Count == 0)
                    {
                        ctx.Line($"[{archetype}] 후보 없음 — 건너뜀");
                        continue;
                    }

                    AssignRolesFor(ctx, archetype, candidates);
                    ApplyRoleTuning(ctx, archetype, candidates);
                }
            });
        }

        private static void AssignRolesFor(BatchContext ctx, string archetype, List<RoleCandidate> all)
        {
            var taken = new HashSet<GameplayAbilitySO>();

            void Take(AbilityAIRole role, IEnumerable<RoleCandidate> ordered, int count)
            {
                foreach (RoleCandidate c in ordered.Where(c => !taken.Contains(c.Ability)).Take(count))
                {
                    // 여러 역할을 겸하지 않는다 — 한 Ability가 여러 역할에 잡히면
                    // "역할별로 다른 공격이 나온다"는 설계 전제가 깨진다.
                    c.Assigned = role;
                    taken.Add(c.Ability);
                }
            }

            bool ranged = archetype == "Bow";
            var basics = all.Where(c => c.Info.attackCategory == AbilityAttackCategory.Basic && !c.IsCounterMotion);
            var heavies = all.Where(c => c.Info.attackCategory == AbilityAttackCategory.Heavy && !c.IsCounterMotion);
            var skills = all.Where(c => c.Info.attackCategory == AbilityAttackCategory.Skill && !c.IsCounterMotion);
            var counters = all.Where(c => c.IsCounterMotion);

            // Bow는 Heavy 카테고리가 없다. Skill을 Punish/Signature로 나눠 쓴다.
            IEnumerable<RoleCandidate> punishPool = ranged ? skills : heavies;

            Take(AbilityAIRole.Opener, basics.OrderBy(c => c.Startup), 2);
            Take(AbilityAIRole.Punish, punishPool.OrderBy(c => c.Startup).ThenByDescending(c => c.Damage), 2);
            Take(AbilityAIRole.Counter, counters.OrderBy(c => c.Ability.name), 1);
            Take(AbilityAIRole.GapCloser, all.OrderByDescending(c => c.MaxDistance).ThenByDescending(c => c.Damage), 1);
            Take(AbilityAIRole.Signature, skills.OrderByDescending(c => c.Startup), ranged ? 1 : 2);
            Take(AbilityAIRole.Finisher, all.Where(c => !c.IsCounterMotion).OrderByDescending(c => c.Damage), 2);

            ctx.Line($"[{archetype}] 역할 배정 {taken.Count}/{all.Count}");
            foreach (RoleCandidate c in all.Where(c => c.Assigned != AbilityAIRole.None)
                         .OrderBy(c => c.Assigned.ToString()))
            {
                ctx.Line($"    {c.Assigned,-10} {c.Ability.name,-52} "
                         + $"startup={c.Startup:0.##} dmg={c.Damage:0.#} maxD={c.MaxDistance:0.##} hits={c.HitEvents}");
            }

            // 역할 커버리지 — 하나라도 비면 BT의 해당 RequestAction이 영구 실패한다.
            foreach (AbilityAIRole role in RoleTuning.Keys)
            {
                if (all.All(c => c.Assigned != role))
                    throw new InvalidOperationException(
                        $"[{archetype}] 역할 {role}에 배정된 Ability가 없습니다. BT가 이 역할을 요청하면 빈 스윙이 됩니다.");
            }
        }

        private static void ApplyRoleTuning(BatchContext ctx, string archetype, List<RoleCandidate> all)
        {
            foreach (RoleCandidate c in all)
            {
                AbilityAIRole role = c.Assigned;

                // 역할 미배정 Ability는 aiRoles를 None으로 되돌려 역할 지명에 걸리지 않게 한다.
                // (D1(b)안이므로 aiSelectable은 그대로 두어 역할 미지정 경로에서는 계속 후보다.)
                if (role == AbilityAIRole.None)
                {
                    if (c.Info.aiRoles != AbilityAIRole.None)
                    {
                        ctx.Change(c.Payload, "aiRoles", c.Info.aiRoles, AbilityAIRole.None);
                        if (ctx.Apply)
                        {
                            ctx.Record(c.Payload);
                            c.Info.aiRoles = AbilityAIRole.None;
                            ctx.Dirty(c.Payload);
                        }
                    }
                    continue;
                }

                (float weight, float cooldown, string group, bool ring) = RoleTuning[role];
                string groupId = group == null ? string.Empty : $"Humanoid.{archetype}.{group}";

                if (c.Info.aiRoles != role)
                {
                    ctx.Change(c.Payload, "aiRoles", c.Info.aiRoles, role);
                    if (ctx.Apply) { ctx.Record(c.Payload); c.Info.aiRoles = role; ctx.Dirty(c.Payload); }
                }

                if (!Mathf.Approximately(c.Info.selectionWeight, weight))
                {
                    ctx.Change(c.Payload, "selectionWeight", c.Info.selectionWeight, weight);
                    if (ctx.Apply) { ctx.Record(c.Payload); c.Info.selectionWeight = weight; ctx.Dirty(c.Payload); }
                }

                if (c.Info.useDangerRing != ring)
                {
                    ctx.Change(c.Payload, "useDangerRing", c.Info.useDangerRing, ring);
                    if (ctx.Apply) { ctx.Record(c.Payload); c.Info.useDangerRing = ring; ctx.Dirty(c.Payload); }
                }

                if (!Mathf.Approximately(c.Ability.cooldown.durationSeconds, cooldown))
                {
                    ctx.Change(c.Ability, "cooldown.durationSeconds",
                        c.Ability.cooldown.durationSeconds, cooldown);
                    if (ctx.Apply) { ctx.Record(c.Ability); c.Ability.cooldown.durationSeconds = cooldown; ctx.Dirty(c.Ability); }
                }

                if (c.Ability.cooldown.cooldownGroupId != groupId)
                {
                    ctx.Change(c.Ability, "cooldown.cooldownGroupId",
                        string.IsNullOrEmpty(c.Ability.cooldown.cooldownGroupId) ? "(없음)" : c.Ability.cooldown.cooldownGroupId,
                        string.IsNullOrEmpty(groupId) ? "(없음)" : groupId);
                    if (ctx.Apply) { ctx.Record(c.Ability); c.Ability.cooldown.cooldownGroupId = groupId; ctx.Dirty(c.Ability); }
                }
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Step 5 — 근접 유효 사거리 베이크 (설계서 §6.3)
        // 실제 공격 포즈를 샘플링해 activation.maxDistance를 산출한다.
        // 반드시 Step_AssignRoles 앞에 실행한다 — GapCloser 판별이 이 값을 쓴다.
        // ────────────────────────────────────────────────────────────────

        public static void Step_BakeMeleeRange()
        {
            // 이 도구는 자체 Undo/저장을 수행하므로 BatchContext로 감싸지 않는다.
            Debug.Log("===== Step_BakeMeleeRange — MonsterMeleeRangeBakeTool.BakeAll() =====");
            MonsterMeleeRangeBakeTool.BakeAll();
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
