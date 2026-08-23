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
using UPlayGround.Components;

namespace UPlayGround.Editor
{
    /// <summary>
    /// Humanoid 일반 몬스터(Enemy_Random_*)의 GAS 데이터를 일괄 저작하는 batchmode 진입점 모음.
    /// 설계 근거는 Assets/docs/TODO/HUMANOID_MONSTER_GAS_BT_DESIGN.md.
    ///
    /// 안전 규칙 (CLAUDE.md "Editor 데이터 도구 안전 규칙"):
    ///  - 변경은 단일 Undo 그룹으로 묶고, 예외 시 신규 에셋/폴더까지 전체 롤백한다.
    ///  - Step_WireBehaviorData만 누락된 Strategy 에셋을 생성할 수 있다.
    ///  - BT import는 전체 입력을 사전 검증하고 기존 Generated 파일을 백업해 실패 시 복원한다.
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
            private readonly List<string> _createdAssetPaths = new();
            private readonly List<string> _createdFolderPaths = new();

            public bool Apply { get; }

            public BatchContext(string name)
            {
                _name = name;
                Apply = Environment.GetCommandLineArgs().Contains(ApplyArgument);
                if (Apply)
                {
                    Undo.IncrementCurrentGroup();
                    Undo.SetCurrentGroupName($"HumanoidAuthoringBatch.{name}");
                    _undoGroup = Undo.GetCurrentGroup();
                }
                else
                {
                    _undoGroup = -1;
                }
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

            public void EnsureFolder(string parentFolder, string folderName)
            {
                string path = $"{parentFolder}/{folderName}";
                if (!Apply || AssetDatabase.IsValidFolder(path)) return;
                string guid = AssetDatabase.CreateFolder(parentFolder, folderName);
                if (string.IsNullOrEmpty(guid))
                    throw new InvalidOperationException($"폴더 생성 실패: {path}");
                _createdFolderPaths.Add(path);
            }

            public void RegisterCreatedAsset(UnityEngine.Object target, string path)
            {
                if (!Apply || target == null) return;
                Undo.RegisterCreatedObjectUndo(target, _name);
                _createdAssetPaths.Add(path);
            }

            public void Dispose()
            {
                if (_failed && Apply)
                {
                    Undo.RevertAllDownToGroup(_undoGroup);
                    for (int i = _createdAssetPaths.Count - 1; i >= 0; i--)
                        if (AssetDatabase.LoadMainAssetAtPath(_createdAssetPaths[i]) != null)
                            AssetDatabase.DeleteAsset(_createdAssetPaths[i]);
                    for (int i = _createdFolderPaths.Count - 1; i >= 0; i--)
                        if (AssetDatabase.IsValidFolder(_createdFolderPaths[i])
                            && AssetDatabase.FindAssets(
                                string.Empty,
                                new[] { _createdFolderPaths[i] }).Length == 0)
                        {
                            AssetDatabase.DeleteAsset(_createdFolderPaths[i]);
                        }
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Line($"!! 실패 — Undo 그룹 전체 롤백함. 변경 없음.");
                }
                else if (_failed)
                {
                    Line("!! Preview 검증 실패 — 프로젝트 변경 없음.");
                }
                else if (Apply)
                {
                    Undo.CollapseUndoOperations(_undoGroup);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                string result = _failed
                    ? Apply ? "롤백" : "검증 실패(Preview)"
                    : Apply ? "적용" : "미적용(Preview)";
                Line($"----- {_name}: {_changeCount}건 {result} -----");
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
            Exception failure = null;
            using (var ctx = new BatchContext(name))
            {
                try
                {
                    body(ctx);
                }
                catch (Exception ex)
                {
                    ctx.MarkFailed();
                    ctx.Line($"예외: {ex}");
                    failure = ex;
                }
            }

            if (failure == null) return;
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
            else
                throw new InvalidOperationException($"{name} 실행 실패", failure);
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
        //   (2) projectilePrefab 교체        : SeolA_Default_Arrow(플레이어용) -> DefaultArrow(몬스터 공용)
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

        /// <summary>역할별 (가중치, 쿨다운초, 쿨다운그룹 접미사, DangerRing) 규약. 보스 MyoRyeong 실측 규약 승계.</summary>
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

            // ⚠ 역할↔카테고리 계약. BT는 (attackCategory, abilityRole) 쌍으로 요청하므로
            //    배정도 반드시 같은 카테고리 안에서 골라야 한다. 어긋나면 그 요청은
            //    후보 0이 되어 영원히 실패한다 (MatchesCategory & MatchesRole 이 AND 조건).
            //    이 계약은 gen_bt.py의 ROLE_CATEGORY와 한 글자도 다르면 안 된다.
            bool ranged = archetype == "Bow";
            var basics = all.Where(c => c.Info.attackCategory == AbilityAttackCategory.Basic && !c.IsCounterMotion).ToList();
            var heavies = all.Where(c => c.Info.attackCategory == AbilityAttackCategory.Heavy && !c.IsCounterMotion).ToList();
            var skills = all.Where(c => c.Info.attackCategory == AbilityAttackCategory.Skill && !c.IsCounterMotion).ToList();
            var counters = all.Where(c => c.IsCounterMotion).ToList();

            // Bow는 Heavy 카테고리가 없어 Punish도 Skill에서 뽑는다.
            List<RoleCandidate> punishPool = ranged ? skills : heavies;

            Take(AbilityAIRole.Opener, basics.OrderBy(c => c.Startup), 2);                    // Basic
            Take(AbilityAIRole.Punish, punishPool.OrderBy(c => c.Startup), 2);                // Heavy (Bow: Skill)
            // Counter는 개수를 제한하지 않는다. 프로젝트 불변식상 Counter 계열 AI 공격은
            // 전부 Counter 역할을 가져야 한다(MonsterAbilitySetIntegrationTests가 강제).
            // 역할 후보가 여럿이어도 가중 랜덤이 하나를 고르므로 결정론이 깨지지 않는다.
            Take(AbilityAIRole.Counter, counters.OrderBy(c => c.Ability.name), counters.Count);  // Skill
            Take(AbilityAIRole.GapCloser, skills.OrderByDescending(c => c.MaxDistance), 1);   // Skill
            Take(AbilityAIRole.Signature, skills.OrderByDescending(c => c.Startup), 1);       // Skill
            Take(AbilityAIRole.Finisher, basics.OrderByDescending(c => c.Damage), 2);         // Basic

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

        [Serializable]
        private sealed class BakeReportEntry
        {
            public string ability;
            public string status;
            public float currentMinDistance;
            public float currentMaxDistance;
            public float recommendedMinDistance;
            public float recommendedMaxDistance;
        }

        [Serializable]
        private sealed class BakeReportRoot
        {
            public List<BakeReportEntry> entries = new();
        }

        private const string BakeReportPath = "Library/MonsterMeleeRangeBakeReport.json";

        /// <summary>
        /// ⚠ MonsterMeleeRangeBakeTool.BakeAll()은 프로젝트의 모든 ActorDefinition을 대상으로
        /// activation 사거리와 BehaviorSO 거리까지 덮어쓴다 — 보스·식물·거미 전부 포함이다.
        /// 이번 작업 범위는 Humanoid 5종이므로 읽기 전용 AnalyzeAll()로 측정만 하고,
        /// 그 리포트에서 Humanoid Ability에만 사거리를 반영한다.
        /// </summary>
        public static void Step_BakeMeleeRangeHumanoidOnly()
        {
            MonsterMeleeRangeBakeTool.AnalyzeAll();

            Run(nameof(Step_BakeMeleeRangeHumanoidOnly), ctx =>
            {
                string reportFull = Path.GetFullPath(BakeReportPath);
                if (!File.Exists(reportFull))
                    throw new FileNotFoundException($"베이크 리포트를 찾지 못했습니다: {reportFull}");

                BakeReportRoot report =
                    JsonUtility.FromJson<BakeReportRoot>(File.ReadAllText(reportFull));

                // Ability 이름 -> 권장 사거리. 같은 Ability가 여러 ActorDefinition에서 측정되면
                // 원 도구와 같은 규칙으로 가장 보수적인(작은) max를 취한다.
                var recommended = new Dictionary<string, (float Min, float Max)>();
                foreach (BakeReportEntry e in report.entries)
                {
                    if (e.status != "Measured" || e.recommendedMaxDistance <= 0f)
                        continue;
                    if (recommended.TryGetValue(e.ability, out (float Min, float Max) prev))
                        recommended[e.ability] = (Mathf.Max(prev.Min, e.recommendedMinDistance),
                                                  Mathf.Min(prev.Max, e.recommendedMaxDistance));
                    else
                        recommended[e.ability] = (e.recommendedMinDistance, e.recommendedMaxDistance);
                }
                ctx.Line($"  리포트 측정 항목 {recommended.Count}건");

                foreach (string archetype in Archetypes)
                {
                    // Bow는 Ranged라 근접 접근 사거리 클램프를 타지 않는다. 20m 저작값을 지킨다.
                    if (archetype == "Bow")
                    {
                        ctx.Line("[Bow] Ranged — 근접 사거리 베이크 대상 아님, 저작값 유지");
                        continue;
                    }

                    foreach (GameplayAbilitySO ability in LoadAbilities(archetype).OrderBy(a => a.name))
                    {
                        if (!recommended.TryGetValue(ability.name, out (float Min, float Max) r))
                            continue;
                        if (Mathf.Approximately(ability.activation.minDistance, r.Min)
                            && Mathf.Approximately(ability.activation.maxDistance, r.Max))
                            continue;

                        ctx.Change(ability, "activation.min/maxDistance",
                            $"{ability.activation.minDistance:0.##}~{ability.activation.maxDistance:0.##}",
                            $"{r.Min:0.##}~{r.Max:0.##}");
                        if (ctx.Apply)
                        {
                            ctx.Record(ability);
                            ability.activation.minDistance = r.Min;
                            ability.activation.maxDistance = r.Max;
                            ctx.Dirty(ability);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Step 5b — 실측 도달 거리를 넘어서는 activation을 실측 기준으로 되돌린다.
        ///
        /// <see cref="EnemyAttackRangePolicy.ResolveEffectiveMaxDistance"/>는 근접에서
        /// 베이크된 activation 최대 거리를 권위값으로 그대로 쓴다. 따라서 activation이
        /// 실제 히트박스 도달보다 크면 그 차이만큼 헛스윙 구간이 된다.
        ///
        /// 베이크 리포트 대조 결과 역할 배정 근접 40건 중 36건은 activation이 실측보다
        /// 정확히 0.15m 작다(도구가 안전 마진을 뺀다). 아래 2건만 실측이 1.35·1.00인데도
        /// 권장값이 2.5로 나왔다 — 베이크 도구 쪽 결함이다. 여러 액터가 공유하므로
        /// 가장 짧은 실측에서 다시 마진을 빼 보수적으로 잡는다.
        ///
        /// 근본 해소는 MonsterMeleeRangeBakeTool의 권장값 산출을 고치는 것이다.
        /// 그 전까지 이 스텝이 회귀를 막는다(멱등).
        /// </summary>
        private static readonly (string Path, float Max, string Reason)[] WhiffRangeFixes =
        {
            ("Assets/10.Datas/Ability/Actor/Humanoid_DualBladeAttackData/"
             + "GA_Humanoid_DualBladeAttackData_08_Attack_8.asset",
             1.20f, "실측 1.35(DualSword_002) - 마진 0.15"),
            ("Assets/10.Datas/Ability/Actor/Humanoid_SwordShieldAttackData/"
             + "GA_Humanoid_SwordShieldAttackData_09_Attack_9.asset",
             0.85f, "실측 1.00(GreatSword_002) - 마진 0.15"),
        };

        public static void Step_FixWhiffRanges()
        {
            Run(nameof(Step_FixWhiffRanges), ctx =>
            {
                foreach ((string path, float max, string reason) in WhiffRangeFixes)
                {
                    var ability = AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(path);
                    if (ability?.activation == null)
                    {
                        ctx.Line($"  [건너뜀] 에셋 없음 {path}");
                        continue;
                    }
                    if (Mathf.Approximately(ability.activation.maxDistance, max))
                        continue;

                    ctx.Line($"  {ability.name}: {reason}");
                    ctx.Change(ability, "activation.maxDistance",
                        $"{ability.activation.maxDistance:0.##}", $"{max:0.##}");
                    if (ctx.Apply)
                    {
                        ctx.Record(ability);
                        ability.activation.maxDistance = max;
                        ctx.Dirty(ability);
                    }
                }
            });
        }

        // ────────────────────────────────────────────────────────────────
        // Step 6 — 전략 SO 생성 + BehaviorData 배선 (설계서 §6.6 / §7.4 / §7.5)
        // ────────────────────────────────────────────────────────────────

        private sealed class ArchetypeProfile
        {
            public string IntentWeights;
            public float RepeatMultiplier;
            public int MaxConsecutiveSame;
            public float MinCommitment;
            public float GroupPressure;
            public float Optimal, Min, PersonalSpace, ChaseStop;
            public float Guard, Retreat, ContinueAttack;
            public EnemyAIRole Role;
        }

        private static readonly Dictionary<string, ArchetypeProfile> Profiles = new()
        {
            ["GreatSword"] = new() { IntentWeights = "IW_AggressiveMelee", RepeatMultiplier = 0.30f, MaxConsecutiveSame = 1, MinCommitment = 0.35f, GroupPressure = 0.8f, Optimal = 2.8f, Min = 1.4f, PersonalSpace = 1.0f, ChaseStop = 2.6f, Guard = 0.20f, Retreat = 0.12f, ContinueAttack = 0.30f, Role = EnemyAIRole.Melee },
            ["DoubleAxe"] = new() { IntentWeights = "IW_AggressiveMelee", RepeatMultiplier = 0.55f, MaxConsecutiveSame = 3, MinCommitment = 0.12f, GroupPressure = 1.2f, Optimal = 2.3f, Min = 1.0f, PersonalSpace = 0.8f, ChaseStop = 2.1f, Guard = 0.05f, Retreat = 0.05f, ContinueAttack = 0.55f, Role = EnemyAIRole.Melee },
            ["DualBlade"] = new() { IntentWeights = "IW_Default_Melee", RepeatMultiplier = 0.50f, MaxConsecutiveSame = 2, MinCommitment = 0.10f, GroupPressure = 1.1f, Optimal = 2.0f, Min = 0.9f, PersonalSpace = 0.75f, ChaseStop = 1.8f, Guard = 0.15f, Retreat = 0.30f, ContinueAttack = 0.40f, Role = EnemyAIRole.Melee },
            ["SwordShield"] = new() { IntentWeights = "IW_DefensiveShield", RepeatMultiplier = 0.40f, MaxConsecutiveSame = 2, MinCommitment = 0.20f, GroupPressure = 0.9f, Optimal = 2.4f, Min = 1.1f, PersonalSpace = 0.85f, ChaseStop = 2.2f, Guard = 0.60f, Retreat = 0.15f, ContinueAttack = 0.28f, Role = EnemyAIRole.Melee },
            ["Bow"] = new() { IntentWeights = "IW_RangedCaster", RepeatMultiplier = 0.45f, MaxConsecutiveSame = 2, MinCommitment = 0.15f, GroupPressure = 0.7f, Optimal = 8.0f, Min = 4.0f, PersonalSpace = 2.0f, ChaseStop = 7.0f, Guard = 0.05f, Retreat = 0.55f, ContinueAttack = 0.30f, Role = EnemyAIRole.RangedMain },
        };

        /// <summary>ActorDef/BehaviorData 이름 -> 아키타입. GreatSword_002는 이름대로 GreatSword다(§2.3 교정 후).</summary>
        private static string ArchetypeOf(string assetName)
        {
            if (assetName.Contains("Bow", StringComparison.OrdinalIgnoreCase)) return "Bow";
            if (assetName.Contains("DualAxe", StringComparison.OrdinalIgnoreCase)) return "DoubleAxe";
            if (assetName.Contains("DualSword", StringComparison.OrdinalIgnoreCase)) return "DualBlade";
            if (assetName.Contains("GreatSword", StringComparison.OrdinalIgnoreCase)) return "GreatSword";
            if (assetName.Contains("SwordShield", StringComparison.OrdinalIgnoreCase)) return "SwordShield";
            return null;
        }

        private const string StrategyFolder = "Assets/10.Datas/Actor/Enemy/BehaviorData/Strategy";
        private const string IntentWeightsFolder = "Assets/10.Datas/AI/IntentWeights";
        private const string GeneratedBtFolder = "Assets/10.Datas/AI/BehaviorTree/Generated";
        private const string BehaviorDataFolder = "Assets/10.Datas/Actor/Enemy/BehaviorData";

        /// <summary>
        /// 아키타입의 역할별 유효 도달 거리를 실제 Ability에서 계산한다.
        ///
        /// 근접 Ability의 유효 최대 거리는 authored maxDistance가 아니라
        /// <see cref="EnemyAttackRangePolicy.ResolveEffectiveMaxDistance"/>가 결정한다.
        /// 베이크로 사거리가 좁아졌는데 교전 거리를 손으로 크게 잡으면, 몬스터가
        /// 접근을 멈춘 지점에서 어떤 역할도 발동하지 못하고 굳는다.
        /// </summary>
        private static Dictionary<AbilityAIRole, float> RoleReach(string archetype, float personalSpace)
        {
            var reach = new Dictionary<AbilityAIRole, float>();
            foreach (GameplayAbilitySO ability in LoadAbilities(archetype))
            {
                AbilityAttackInfo info = PayloadOf(ability)?.attackInfo;
                if (info == null || !EnemyAbilitySelectionPolicy.IsAISelectableAttack(info))
                    continue;
                if (info.aiRoles == AbilityAIRole.None)
                    continue;

                float effective = info.baseInfo.attackType == AttackType.Melee
                    ? EnemyAttackRangePolicy.ResolveEffectiveMaxDistance(ability, info, personalSpace)
                    : ability.activation.maxDistance;

                foreach (AbilityAIRole role in RoleTuning.Keys)
                {
                    if ((info.aiRoles & role) == 0)
                        continue;
                    reach[role] = reach.TryGetValue(role, out float prev)
                        ? Mathf.Max(prev, effective)
                        : effective;
                }
            }
            return reach;
        }

        /// <summary>
        /// 교전 거리를 역할 도달 거리에서 도출한다. GapCloser는 정의상 교전 거리 밖에서
        /// 쓰므로 제외하고, 나머지 역할이 **전부** 닿는 거리를 잡는다.
        /// </summary>
        public static void Step_ReportReach()
        {
            Run(nameof(Step_ReportReach), ctx =>
            {
                foreach ((string archetype, ArchetypeProfile p) in Profiles)
                {
                    Dictionary<AbilityAIRole, float> reach = RoleReach(archetype, p.PersonalSpace);
                    float limiting = reach
                        .Where(kv => kv.Key != AbilityAIRole.GapCloser)
                        .Select(kv => kv.Value)
                        .DefaultIfEmpty(0f)
                        .Min();
                    ctx.Line($"[{archetype}] personalSpace={p.PersonalSpace:0.##} "
                             + $"제한 역할 도달={limiting:0.##} (현재 optimal={p.Optimal:0.##})");
                    foreach ((AbilityAIRole role, float value) in reach.OrderBy(kv => kv.Key.ToString()))
                        ctx.Line($"      {role,-10} {value:0.##}");
                }
            });
        }

        public static void Step_WireBehaviorData()
        {
            Run(nameof(Step_WireBehaviorData), ctx =>
            {
                // 쓰기 전에 모든 외부 의존성을 확인해 중간 실패 가능성을 제거한다.
                var weightsByArchetype = new Dictionary<string, Data.Enemy.EnemyIntentWeightsSO>();
                var treesByArchetype = new Dictionary<string, ScriptableObject>();
                foreach ((string archetype, ArchetypeProfile p) in Profiles)
                {
                    var weights = AssetDatabase.LoadAssetAtPath<Data.Enemy.EnemyIntentWeightsSO>(
                        $"{IntentWeightsFolder}/{p.IntentWeights}.asset");
                    if (weights == null)
                        throw new InvalidOperationException($"IntentWeights를 찾지 못했습니다: {p.IntentWeights}");
                    weightsByArchetype[archetype] = weights;

                    var tree = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                        $"{GeneratedBtFolder}/BT_EnemyBehavior_Humanoid_{archetype}.asset");
                    if (tree == null)
                        throw new InvalidOperationException(
                            $"{archetype} BT 에셋이 없습니다. Step_ImportHumanoidBt를 먼저 실행하세요.");
                    treesByArchetype[archetype] = tree;
                }

                ctx.EnsureFolder(BehaviorDataFolder, "Strategy");

                // (1) 아키타입별 전략 SO — 없으면 만들고 있으면 값만 갱신한다.
                var strategies = new Dictionary<string, Data.Enemy.EnemyCombatStrategySO>();
                foreach ((string archetype, ArchetypeProfile p) in Profiles)
                {
                    string path = $"{StrategyFolder}/CombatStrategy_Humanoid_{archetype}.asset";
                    var strategy = AssetDatabase.LoadAssetAtPath<Data.Enemy.EnemyCombatStrategySO>(path);
                    bool created = false;
                    if (strategy == null)
                    {
                        ctx.Line($"  전략 SO 생성 예정: {path}");
                        if (ctx.Apply)
                        {
                            strategy = ScriptableObject.CreateInstance<Data.Enemy.EnemyCombatStrategySO>();
                            AssetDatabase.CreateAsset(strategy, path);
                            ctx.RegisterCreatedAsset(strategy, path);
                            created = true;
                        }
                    }
                    if (strategy == null)
                        continue;

                    Data.Enemy.EnemyIntentWeightsSO weights = weightsByArchetype[archetype];

                    if (!created)
                        ctx.Record(strategy);
                    ctx.Change(strategy, "intentWeights/repeat/maxSame/commit/groupPressure",
                        created ? "(신규)" : "기존",
                        $"{p.IntentWeights}/{p.RepeatMultiplier}/{p.MaxConsecutiveSame}/{p.MinCommitment}/{p.GroupPressure}");
                    if (ctx.Apply)
                    {
                        strategy.intentWeights = weights;
                        strategy.repeatedAbilityScoreMultiplier = p.RepeatMultiplier;
                        strategy.maxConsecutiveSameAbility = p.MaxConsecutiveSame;
                        strategy.minimumCommitmentSeconds = p.MinCommitment;
                        strategy.groupPressureMultiplier = p.GroupPressure;
                        ctx.Dirty(strategy);
                    }
                    strategies[archetype] = strategy;
                }

                // (2) BehaviorData 10개 — BT/전략/IntentWeights/거리·확률 재배선
                foreach (string guid in AssetDatabase.FindAssets(
                             "t:EnemyBehaviorSO", new[] { BehaviorDataFolder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var behavior = AssetDatabase.LoadAssetAtPath<Data.Enemy.EnemyBehaviorSO>(path);
                    if (behavior == null || !behavior.name.StartsWith("Enemy_Random_", StringComparison.Ordinal))
                        continue;

                    string archetype = ArchetypeOf(behavior.name);
                    if (archetype == null)
                        throw new InvalidOperationException($"아키타입을 판별하지 못했습니다: {behavior.name}");
                    ArchetypeProfile p = Profiles[archetype];

                    ScriptableObject tree = treesByArchetype[archetype];

                    ctx.Change(behavior, "behaviorTree",
                        behavior.behaviorTree != null ? behavior.behaviorTree.name : "null", tree.name);
                    if (ctx.Apply)
                    {
                        ctx.Record(behavior);
                        behavior.behaviorTree = tree;
                        behavior.combatStrategy = strategies[archetype];
                        behavior.intentWeights = strategies[archetype].intentWeights;
                        behavior.aiRole = p.Role;
                        behavior.optimalCombatDistance = p.Optimal;
                        behavior.minCombatDistance = p.Min;
                        behavior.personalSpaceDistance = p.PersonalSpace;
                        behavior.chaseStopDistance = p.ChaseStop;
                        behavior.guardChance = p.Guard;
                        behavior.retreatChance = p.Retreat;
                        behavior.continueAttackChance = p.ContinueAttack;
                        ctx.Dirty(behavior);
                    }
                }
            });
        }

        // ────────────────────────────────────────────────────────────────
        // Step 7 — Humanoid BT JSON import (설계서 §7.1)
        // 임포터의 MenuItem은 Selection에 의존하므로 headless 진입점을 따로 둔다.
        // ────────────────────────────────────────────────────────────────

        public static void Step_ImportHumanoidBt()
        {
            Run(nameof(Step_ImportHumanoidBt), ctx =>
            {
                const string folder = "Assets/10.Datas/AI/BehaviorTree/SourceJson/Humanoid";
                string absolute = Path.GetFullPath(folder);
                if (!Directory.Exists(absolute))
                    throw new DirectoryNotFoundException(absolute);
                string[] jsonPaths = Directory.GetFiles(
                    absolute,
                    "*.json",
                    SearchOption.AllDirectories);
                if (jsonPaths.Length != Archetypes.Length)
                    throw new InvalidOperationException(
                        $"BT JSON {Archetypes.Length}개를 기대했으나 {jsonPaths.Length}개입니다.");

                IReadOnlyList<string> validatedPaths =
                    AI.BehaviorTree.Editor.MonsterBehaviorTreeJsonImporter
                        .PreflightJsonInputs(jsonPaths);
                List<string> generatedPaths = validatedPaths
                    .Select(AI.BehaviorTree.Editor.MonsterBehaviorTreeJsonImporter
                        .ResolveGeneratedAssetPath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                ctx.Line($"  입력 폴더: {absolute}");
                foreach (string jsonPath in validatedPaths)
                    ctx.Line($"  {(ctx.Apply ? "가져오기" : "가져오기 예정")}: {Path.GetFileName(jsonPath)}");
                if (!ctx.Apply) return;

                var backups = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
                foreach (string path in generatedPaths)
                {
                    UnityEngine.Object[] existingAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                    if (existingAssets.Any(asset => asset != null && EditorUtility.IsDirty(asset)))
                        throw new InvalidOperationException(
                            $"저장되지 않은 Generated BT 변경이 있어 import를 중단합니다: {path}");
                    if (File.Exists(path)) backups[path] = File.ReadAllBytes(path);
                }

                try
                {
                    IReadOnlyList<AI.BehaviorTree.BehaviorTreeAsset> imported =
                        AI.BehaviorTree.Editor.MonsterBehaviorTreeJsonImporter.ImportJsonFolder(absolute);
                    ctx.Line($"  import 결과: {imported.Count}개 — "
                             + string.Join(", ", imported.Select(a => a != null ? a.name : "null")));
                    if (imported.Count != validatedPaths.Count)
                        throw new InvalidOperationException(
                            $"BT {validatedPaths.Count}개를 기대했으나 {imported.Count}개가 생성되었습니다.");
                }
                catch
                {
                    foreach (string path in generatedPaths)
                    {
                        if (backups.TryGetValue(path, out byte[] bytes))
                        {
                            File.WriteAllBytes(path, bytes);
                            AssetDatabase.ImportAsset(
                                path,
                                ImportAssetOptions.ForceSynchronousImport
                                | ImportAssetOptions.ForceUpdate);
                        }
                        else if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                        {
                            AssetDatabase.DeleteAsset(path);
                        }
                    }
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    throw;
                }
            });
        }

        // ────────────────────────────────────────────────────────────────
        // Step 8 — 최종 검증 (설계서 §9)
        // BT가 요청하는 (attackCategory, abilityRole) 쌍마다 실제 후보가 있는지 확인한다.
        // 이게 0이면 그 규칙은 런타임에 영원히 실패한다.
        // ────────────────────────────────────────────────────────────────

        /// <summary>gen_bt.py의 role_category 계약과 동일. 어긋나면 후보 0이 된다.</summary>
        private static AbilityAttackCategory ContractCategory(string archetype, AbilityAIRole role) =>
            role switch
            {
                AbilityAIRole.Opener => AbilityAttackCategory.Basic,
                AbilityAIRole.Finisher => AbilityAttackCategory.Basic,
                AbilityAIRole.Punish => archetype == "Bow"
                    ? AbilityAttackCategory.Skill
                    : AbilityAttackCategory.Heavy,
                _ => AbilityAttackCategory.Skill,
            };

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/캐릭터/AI/Humanoid 저작 검증")]
        public static void Step_Validate()
        {
            Run(nameof(Step_Validate), ctx =>
            {
                var failures = new List<string>();

                foreach (string archetype in Archetypes)
                {
                    ActorAnimationMotionSet animationSet = LoadAnimationSet(archetype);
                    var infos = new List<(GameplayAbilitySO Ability, AbilityAttackInfo Info)>();
                    foreach (GameplayAbilitySO ability in LoadAbilities(archetype))
                    {
                        AbilityAttackInfo info = PayloadOf(ability)?.attackInfo;
                        if (info == null)
                            continue;
                        infos.Add((ability, info));

                        // Motion 해석과 hitPhase 정합
                        MotionSetAsset motion = ResolveMotion(animationSet, ability);
                        if (motion == null)
                        {
                            failures.Add($"[{archetype}] {ability.name}: MotionKey 미해석");
                            continue;
                        }
                        int hitEvents = AllEvents(motion)
                            .Count(e => e is BeginCollisionEvent or SpawnProjectileEvent);
                        int phases = info.baseInfo?.hitPhases?.Count ?? 0;
                        if (hitEvents > 0 && phases > hitEvents)
                            failures.Add($"[{archetype}] {ability.name}: hitPhases {phases} > 히트 이벤트 {hitEvents} (미발화 페이즈)");
                    }

                    // 역할 커버리지 — BT가 쓰는 계약 그대로 후보를 센다.
                    foreach (AbilityAIRole role in RoleTuning.Keys)
                    {
                        AbilityAttackCategory category = ContractCategory(archetype, role);
                        int count = infos.Count(x =>
                            EnemyAbilitySelectionPolicy.IsAISelectableAttack(x.Info)
                            && EnemyAbilitySelectionPolicy.MatchesCategory(x.Info, category)
                            && EnemyAbilitySelectionPolicy.MatchesRole(x.Info, role));
                        ctx.Line($"  [{archetype}] {category}+{role} 후보 {count}개");
                        if (count == 0)
                            failures.Add($"[{archetype}] {category}+{role} 후보가 0 — BT 규칙이 영구 실패한다");
                    }

                    // Bow는 전부 Ranged여야 한다 (Counter 제외).
                    if (archetype == "Bow")
                    {
                        foreach ((GameplayAbilitySO ability, AbilityAttackInfo info) in infos)
                        {
                            bool shouldBeRanged = IsBowRangedAbility(ability);
                            var actual = info.baseInfo?.attackType ?? AttackType.Melee;
                            if (shouldBeRanged && actual != AttackType.Ranged)
                                failures.Add($"[Bow] {ability.name}: attackType이 {actual} — 근접 클램프에 걸린다");
                        }
                    }
                }

                if (failures.Count > 0)
                {
                    foreach (string f in failures)
                        ctx.Line($"  FAIL {f}");
                    throw new InvalidOperationException($"검증 실패 {failures.Count}건");
                }
                ctx.Line("  검증 통과 — 실패 0건");
            });
        }

        // ────────────────────────────────────────────────────────────────
        // Step 9 — 몬스터 투사체 hitPhaseIndex 레거시 정리 (설계서 §10.1 보류분)
        //
        // hitPhaseIndex: -1 은 ProjectileManager.ResolveAttackData 에서 legacyDamage(고정 10)를
        // 쓰게 만든다. 0..N-1 로 결선하면 Ability 의 hitPhase 수치(데미지·포이즈·브레이크·리액션)가
        // 적용된다. 즉 저작된 의도대로 동작하게 되는 것이지만, 실측 데미지가 2.8~3배 오른다.
        //
        // ⚠ 플레이어(Bow/Katana/Staff 22건)는 대상이 아니다. 저작값이 35~155라
        //    결선하면 6~15배가 되어 플레이어 전투 밸런스 전체가 바뀐다. 별도 밸런스 과제다.
        // ────────────────────────────────────────────────────────────────

        /// <summary>Humanoid 밖 몬스터 MotionSet 루트. 플레이어(Player/) 폴더는 의도적으로 제외한다.</summary>
        private static readonly string[] MonsterProjectileMotionRoots =
        {
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Skeleton",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Lich",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Plant",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/SpiderQueen",
        };

        public static void Step_WireMonsterProjectileHitPhases()
        {
            Run(nameof(Step_WireMonsterProjectileHitPhases), ctx =>
            {
                foreach (string guid in AssetDatabase.FindAssets(
                             "t:MotionSetAsset", MonsterProjectileMotionRoots))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var motion = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
                    if (motion == null)
                        continue;

                    // 발사 시간 순서가 곧 hitPhase 인덱스다. 저장 순서를 믿지 않는다.
                    var shots = AllEvents(motion)
                        .OfType<SpawnProjectileEvent>()
                        .OrderBy(e => e.startTime)
                        .ToList();
                    if (shots.Count == 0)
                        continue;

                    bool dirty = false;
                    for (var i = 0; i < shots.Count; i++)
                    {
                        if (shots[i].hitPhaseIndex == i)
                            continue;
                        ctx.Change(motion, $"shot[{i}].hitPhaseIndex", shots[i].hitPhaseIndex, i);
                        if (ctx.Apply)
                        {
                            ctx.Record(motion);
                            shots[i].hitPhaseIndex = i;
                            dirty = true;
                        }
                    }
                    if (dirty)
                        ctx.Dirty(motion);
                }
            });
        }

        // ────────────────────────────────────────────────────────────────
        // Step 11 — 플레이어 투사체 레거시 결선 (설계서 §11.2)
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// 저작 데미지는 "스킬 전체 총합"이다(사용자 확정). 따라서 다발 사격은
        /// 총합을 발사 수로 나눠야 하고, 단발은 저작값을 그대로 쓰면 된다.
        ///
        /// 결선 가능 여부는 그 motionKey가 **장착 무기와 무관하게 같은 모션으로 풀리는지**가
        /// 가른다. 플레이어 AnimationSet은 무기별 조회 표라, 같은 키가 무기에 따라 다른
        /// 모션으로 간다. Ability의 hitPhases는 하나뿐인데 모션마다 히트 수가 다르면
        /// 한 phase 목록으로 모든 무기의 총합을 동시에 만족시킬 수 없다.
        /// </summary>
        private static readonly string[] PlayerSingleShotMotions =
        {
            // 발사 1개 — 인덱스 0이면 총합이 곧 그 한 발이다.
            // 다른 무기 장착 시 이 키들은 근접 모션(콜리전 1개, index 0)으로 풀리므로
            // 인덱스 0은 어느 무기에서도 일관된다.
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bow/Humanoid_Bow_Attack_1.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bow/Humanoid_Bow_Attack_2.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bow/Humanoid_Bow_Attack_3.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bow/Humanoid_Bow_Attack_4.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bow/Humanoid_Bow_Attack_5.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bow/Humanoid_Bow_Skill_1.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bow/Humanoid_Bow_Skill_3.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Skill_Ability.asset",
            "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Staff/Humanoid_Staff_HeavyAttack_1.asset",
        };

        /// <summary>
        /// 다발 사격 중 **무기 불변**이라 총합 분할이 안전한 것만. 이 키들은 어떤 무기를
        /// 장착해도 같은 활 모션으로 풀리므로, phase를 발사 수만큼 쪼개도 다른 모션의
        /// 인덱스 매핑을 깨뜨리지 않는다.
        /// </summary>
        private static readonly (string Motion, string Ability, string MotionKey)[] PlayerSplitShots =
        {
            ("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bow/Humanoid_Bow_Skill_4.asset",
             "Assets/10.Datas/Ability/Migrated/PlayerBowAttackData/GA_PlayerBowAttackData_Ability.asset",
             "Bow.Ability.Skill.4"),
            ("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bow/Humanoid_Bow_Skill_5.asset",
             "Assets/10.Datas/Ability/Migrated/PlayerBowAttackData/GA_PlayerBowAttackData_Ability.asset",
             "Bow.Ability.Skill.5"),
        };

        public static void Step_WirePlayerProjectileHitPhases()
        {
            Run(nameof(Step_WirePlayerProjectileHitPhases), ctx =>
            {
                foreach (string path in PlayerSingleShotMotions)
                    WireShots(ctx, path, expected: 1);

                foreach ((string motionPath, string abilityPath, string key) in PlayerSplitShots)
                {
                    var motion = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(motionPath);
                    var ability = AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(abilityPath);
                    if (motion == null || ability == null)
                    {
                        ctx.Line($"  [건너뜀] 에셋 없음 {motionPath}");
                        continue;
                    }

                    int shotCount = AllEvents(motion).OfType<SpawnProjectileEvent>().Count();
                    UPlayGroundMotionAbilityPayloadSO payload = PayloadByMotionKey(ability, key);
                    if (payload?.attackInfo?.baseInfo?.hitPhases == null || shotCount == 0)
                    {
                        ctx.Line($"  [건너뜀] Payload/발사 없음 {key}");
                        continue;
                    }

                    List<HitPhaseData> phases = payload.attackInfo.baseInfo.hitPhases;
                    if (phases.Count == 0)
                    {
                        ctx.Line($"  [건너뜀] HitPhase 없음 {key}");
                        continue;
                    }

                    float totalDamage = phases.Sum(p => p.damage);
                    float totalPoiseDamage = phases.Sum(p => p.poiseDamage);
                    float totalBreakDamage = phases.Sum(p => p.breakDamage);
                    if (phases.Count != shotCount)
                    {
                        float eachDamage = totalDamage / shotCount;
                        float eachPoiseDamage = totalPoiseDamage / shotCount;
                        float eachBreakDamage = totalBreakDamage / shotCount;
                        ctx.Line($"  [{key}] Damage/Poise/Break 총합 "
                                 + $"{totalDamage:0.##}/{totalPoiseDamage:0.##}/{totalBreakDamage:0.##}을 "
                                 + $"{shotCount}발로 분할 → 발당 "
                                 + $"{eachDamage:0.##}/{eachPoiseDamage:0.##}/{eachBreakDamage:0.##} "
                                 + $"(phase {phases.Count} -> {shotCount})");
                        ctx.Change(payload, "hitPhases.Count", phases.Count, shotCount);
                        if (ctx.Apply)
                        {
                            ctx.Record(payload);
                            HitPhaseData template = phases[0];
                            var rebuilt = new List<HitPhaseData>(shotCount);
                            for (var i = 0; i < shotCount; i++)
                            {
                                HitPhaseData p = i < phases.Count ? phases[i] : ClonePhase(template);
                                p.damage = eachDamage;
                                p.poiseDamage = eachPoiseDamage;
                                p.breakDamage = eachBreakDamage;
                                rebuilt.Add(p);
                            }
                            payload.attackInfo.baseInfo.hitPhases = rebuilt;
                            ctx.Dirty(payload);
                        }
                    }

                    WireShots(ctx, motionPath, expected: shotCount);
                }
            });
        }

        /// <summary>Variant의 motionKey로 Payload를 특정한다. 같은 Ability 안에 변형이 여럿이다.</summary>
        private static UPlayGroundMotionAbilityPayloadSO PayloadByMotionKey(
            GameplayAbilitySO ability, string motionKey)
        {
            if (ability?.variants == null)
                return null;
            foreach (AbilityVariantDefinition variant in ability.variants)
            {
                if (variant?.executionPayload is UPlayGroundMotionAbilityPayloadSO p
                    && p.attackInfo != null
                    && p.attackInfo.motionKey.value == motionKey)
                    return p;
            }
            return null;
        }

        /// <summary>발사 시간 순으로 hitPhaseIndex를 0..N-1 로 채운다.</summary>
        private static void WireShots(BatchContext ctx, string motionPath, int expected)
        {
            var motion = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(motionPath);
            if (motion == null)
            {
                ctx.Line($"  [건너뜀] MotionSet 없음 {motionPath}");
                return;
            }

            var shots = AllEvents(motion)
                .OfType<SpawnProjectileEvent>()
                .OrderBy(e => e.startTime)
                .ToList();
            if (shots.Count != expected)
            {
                ctx.Line($"  [건너뜀] 발사 수 불일치 {System.IO.Path.GetFileName(motionPath)} "
                         + $"기대 {expected} 실제 {shots.Count}");
                return;
            }

            bool dirty = false;
            for (var i = 0; i < shots.Count; i++)
            {
                if (shots[i].hitPhaseIndex == i)
                    continue;
                ctx.Change(motion, $"shot[{i}].hitPhaseIndex", shots[i].hitPhaseIndex, i);
                if (ctx.Apply)
                {
                    ctx.Record(motion);
                    shots[i].hitPhaseIndex = i;
                    dirty = true;
                }
            }
            if (dirty)
                ctx.Dirty(motion);
        }
    }
}
#endif
