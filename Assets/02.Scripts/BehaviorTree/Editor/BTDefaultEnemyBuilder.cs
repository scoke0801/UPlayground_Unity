using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.BehaviorTree
{
    using UPlayGround.BehaviorTree;

    /// <summary>
    /// DefaultEnemy용 BehaviorTreeSO 에셋을 코드로 생성한다.
    /// Window > BehaviorTree > Build DefaultEnemy Asset
    /// </summary>
    public static class BTDefaultEnemyBuilder
    {
        private const string SaveFolder  = "Assets/10.Datas/BT";
        private const string MainAsset   = "Assets/10.Datas/BT/BT_DefaultEnemy.asset";
        private const string PostAsset   = "Assets/10.Datas/BT/BT_DefaultEnemy_PostAttack.asset";

        // 생성된 sub-SO들을 수집해서 에셋에 추가할 때 사용
        private static BehaviorTreeSO _tree;

        // ── 진입점 ────────────────────────────────────────────────────────

        [MenuItem("Window/BehaviorTree/Build DefaultEnemy Asset")]
        public static void BuildDefaultEnemy()
        {
            if (!System.IO.Directory.Exists(SaveFolder))
                System.IO.Directory.CreateDirectory(SaveFolder);

            BuildMain();
            BuildPostAttack();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BTDefaultEnemyBuilder] BT_DefaultEnemy.asset / BT_DefaultEnemy_PostAttack.asset 생성 완료");
        }

        // ── 메인 트리 ─────────────────────────────────────────────────────

        private static void BuildMain()
        {
            _tree = CreateTree(MainAsset, "BT_DefaultEnemy");

            // ── Root Selector ─────────────────────────────────────────────
            var root = Sel("Root");

            // 1. 차단 상태 가드 (Attack/Death/Hit/Counter/Airborne/Grabbed 상태면 아무것도 안 함)
            root.children.Add(BuildBlockedStateGuard());

            // 2. 전투 행동 (타겟 있을 때)
            root.children.Add(BuildCombatBehavior());

            // 3. 유휴 행동 (타겟 없을 때)
            root.children.Add(BuildIdleBehavior());

            _tree.rootNode = root;
            EditorUtility.SetDirty(_tree);
        }

        // ── 1. 차단 가드 ──────────────────────────────────────────────────
        private static BTSelectorSO BuildBlockedStateGuard()
        {
            // 이 Selector가 Success를 반환하면 = 현재 차단 상태이므로 루트가 멈춤
            var sel = Sel("BlockedStates");
            sel.children.AddRange(new BTNodeSO[]
            {
                CurState("IsAttacking",  "Attack"),
                CurState("IsDead",       "Death"),
                CurState("IsHit",        "Hit"),
                CurState("IsCounter",    "Counter"),
                CurState("IsAirborne",   "Airborne"),
                CurState("IsGrabbed",    "Grabbed"),
            });
            return sel;
        }

        // ── 2. 전투 행동 ─────────────────────────────────────────────────
        private static BTSequenceSO BuildCombatBehavior()
        {
            var seq = Seq("CombatBehavior");
            seq.children.Add(Node<BTCond_HasTargetSO>("HasTarget"));
            seq.children.Add(BuildCombatDecision());
            return seq;
        }

        private static BTSelectorSO BuildCombatDecision()
        {
            var sel = Sel("CombatDecision");
            sel.children.Add(BuildPersonalSpaceGuard());
            sel.children.Add(BuildOverAttackGuard());
            sel.children.Add(BuildReactToPlayer());
            sel.children.Add(BuildInterruptIdleState());
            sel.children.Add(BuildAttackIfReady());
            sel.children.Add(BuildChaseIfFar());
            sel.children.Add(BuildDistanceBehavior());
            return sel;
        }

        // 2a. 개인 공간 침범 → 후퇴
        private static BTSequenceSO BuildPersonalSpaceGuard()
        {
            var seq = Seq("PersonalSpaceGuard");
            seq.children.Add(Node<BTCond_PersonalSpaceSO>("InPersonalSpace"));
            seq.children.Add(Node<BTAction_RetreatSO>("Retreat_Personal"));
            return seq;
        }

        // 2b. 연속 공격 초과 → 후퇴
        private static BTSequenceSO BuildOverAttackGuard()
        {
            var seq = Seq("OverAttackGuard");
            seq.children.Add(Node<BTCond_OverAttackingSO>("IsOverAttacking"));
            seq.children.Add(Node<BTAction_RetreatSO>("Retreat_OverAttack"));
            return seq;
        }

        // 2c. 플레이어 상태 반응
        private static BTSelectorSO BuildReactToPlayer()
        {
            var sel = Sel("ReactToPlayer");
            sel.children.Add(BuildVsPlayerAttacking());
            sel.children.Add(BuildVsPlayerGuarding());
            sel.children.Add(BuildVsPlayerStaggered());
            sel.children.Add(BuildVsPlayerRecovering());
            return sel;
        }

        private static BTSequenceSO BuildVsPlayerAttacking()
        {
            var seq = Seq("VsPlayerAttacking");
            seq.children.Add(PlayerState("PlayerAttacking", PlayerStateQuery.IsAttacking));

            var rnd = Rnd("ReactVsAttacking");

            // DoGuard (weight=0.5)
            var doGuard = Seq("DoGuard");
            doGuard.children.Add(BBBool("HasGuardMotion", BBKey.HasGuardMotion));
            doGuard.children.Add(DistBB("DistOptimal_Guard", DistanceCheck.LessThan, BBKey.OptimalCombatDistance));
            doGuard.children.Add(GuardAction("Guard_VsAttack"));
            rnd.children.Add(doGuard);
            rnd.weights.Add(0.5f);

            // DoFlank (weight=0.4)
            var doFlank = Seq("DoFlank");
            doFlank.children.Add(BBBool("AllowFlank_VsAttack", BBKey.PhaseAllowFlank));
            doFlank.children.Add(DistBBBetween("DistMidRange_Flank",
                BBKey.MinCombatDistance, BBKey.OptimalCombatDistance, 1.5f));
            doFlank.children.Add(Node<BTAction_FlankSO>("Flank_VsAttack"));
            rnd.children.Add(doFlank);
            rnd.weights.Add(0.4f);

            seq.children.Add(rnd);
            return seq;
        }

        private static BTSequenceSO BuildVsPlayerGuarding()
        {
            var seq = Seq("VsPlayerGuarding");
            seq.children.Add(PlayerState("PlayerGuarding", PlayerStateQuery.IsGuarding));

            var decision = Sel("VsGuarding_Decision");

            var attackBreak = Seq("AttackBreakGuard");
            attackBreak.children.Add(DistBB("InMaxRange_AttackBreak", DistanceCheck.LessThan, BBKey.MaxAttackRange));
            attackBreak.children.Add(Node<BTCond_CanAttackSO>("CanAttack_BreakGuard"));
            attackBreak.children.Add(Node<BTAction_AttackSO>("Attack_BreakGuard"));
            decision.children.Add(attackBreak);

            var chargeBreak = Seq("ChargeBreakGuard");
            chargeBreak.children.Add(BBBool("AllowCharge_BreakGuard", BBKey.PhaseAllowCharge));
            chargeBreak.children.Add(DistBB("DistFar_BreakGuard", DistanceCheck.GreaterThan, BBKey.OptimalCombatDistance));
            chargeBreak.children.Add(Node<BTAction_ChargeSO>("Charge_BreakGuard"));
            decision.children.Add(chargeBreak);

            seq.children.Add(decision);
            return seq;
        }

        private static BTSequenceSO BuildVsPlayerStaggered()
        {
            var seq = Seq("VsPlayerStaggered");
            seq.children.Add(PlayerState("PlayerStaggered", PlayerStateQuery.IsStaggered));

            var decision = Sel("FollowUp_Decision");

            var followUp = Seq("FollowUpAttack");
            followUp.children.Add(DistBB("InRange_Stagger", DistanceCheck.LessThan, BBKey.MaxAttackRange, 1.3f));
            followUp.children.Add(Node<BTCond_CanAttackSO>("CanAttack_Stagger"));
            followUp.children.Add(Node<BTAction_AttackSO>("Attack_FollowUp"));
            decision.children.Add(followUp);

            decision.children.Add(Node<BTAction_ChaseSO>("Chase_Stagger"));
            seq.children.Add(decision);
            return seq;
        }

        private static BTSequenceSO BuildVsPlayerRecovering()
        {
            var seq = Seq("VsPlayerRecovering");
            seq.children.Add(PlayerState("PlayerRecovering", PlayerStateQuery.IsRecovering));
            seq.children.Add(DistBB("DistFar_Recovering", DistanceCheck.GreaterThan, BBKey.OptimalCombatDistance));

            var rnd = Rnd("RecoverReact");

            var chargeSeq = Seq("Charge_Recovery_Seq");
            chargeSeq.children.Add(BBBool("AllowCharge_Recovery", BBKey.PhaseAllowCharge));
            chargeSeq.children.Add(Node<BTAction_ChargeSO>("Charge_Recovery"));
            rnd.children.Add(chargeSeq);
            rnd.weights.Add(0.3f);

            rnd.children.Add(Node<BTAction_ChaseSO>("Chase_Recovery"));
            rnd.weights.Add(0.7f);

            seq.children.Add(rnd);
            return seq;
        }

        // 2d. 서클/가드 중 갑작스러운 공격 인터럽트
        private static BTSelectorSO BuildInterruptIdleState()
        {
            var sel = Sel("InterruptIdleState");

            // 서클 중 인터럽트
            var circleInt = Seq("CircleInterrupt");
            circleInt.children.Add(CurState("IsCircling", "Circle"));
            circleInt.children.Add(DistBB("InRange_CircleInt", DistanceCheck.LessThan, BBKey.MaxAttackRange, 1.3f));
            circleInt.children.Add(RandomChance("RandomCircleInt", 0.02f));
            circleInt.children.Add(Node<BTCond_HasAvailableSkillSO>("HasSkill_CircleInt"));
            circleInt.children.Add(Node<BTAction_AttackSO>("Attack_CircleInt"));
            sel.children.Add(circleInt);

            // 가드 중 인터럽트
            var guardInt = Seq("GuardInterrupt");
            guardInt.children.Add(CurState("IsGuarding_Int", "Guard"));
            guardInt.children.Add(DistBB("InRange_GuardInt", DistanceCheck.LessThan, BBKey.MaxAttackRange));
            guardInt.children.Add(InvertPlayerState("PlayerNotAttacking", PlayerStateQuery.IsAttacking));
            guardInt.children.Add(RandomChance("RandomGuardInt", 0.03f));
            guardInt.children.Add(Node<BTAction_AttackSO>("Attack_GuardInt"));
            sel.children.Add(guardInt);

            return sel;
        }

        // 2e. 메인 공격 판단
        private static BTSequenceSO BuildAttackIfReady()
        {
            var seq = Seq("AttackIfReady");
            seq.children.Add(Node<BTCond_ActionReadySO>("ActionReady"));
            seq.children.Add(Node<BTCond_CanAttackSO>("CanAttack_Main"));
            seq.children.Add(DistBB("InAttackRange", DistanceCheck.LessThan, BBKey.MaxAttackRange));
            seq.children.Add(DistBB("InOptimalRange", DistanceCheck.LessThan, BBKey.OptimalCombatDistance, 1.2f));
            seq.children.Add(Node<BTCond_HasAvailableSkillSO>("HasSkill_Main"));
            seq.children.Add(Node<BTAction_AttackSO>("Attack_Main"));
            return seq;
        }

        // 2f. 너무 멀면 추격
        private static BTSequenceSO BuildChaseIfFar()
        {
            var seq = Seq("ChaseIfFar");
            seq.children.Add(DistBB("DistFar_Chase", DistanceCheck.GreaterThan, BBKey.OptimalCombatDistance));
            seq.children.Add(Node<BTAction_ChaseSO>("Chase_Main"));
            return seq;
        }

        // 2g. 거리 기반 행동
        private static BTSelectorSO BuildDistanceBehavior()
        {
            var sel = Sel("DistanceBehavior");

            // 너무 가까움 → 후퇴 (연속 방어 초과 아닐 때만)
            var tooClose = Seq("TooClose");
            tooClose.children.Add(DistBB("DistMin_TooClose", DistanceCheck.LessThan, BBKey.MinCombatDistance));
            tooClose.children.Add(InvertConsecDefense("NotOverDefense", 2));
            tooClose.children.Add(Node<BTAction_RetreatSO>("Retreat_TooClose"));
            sel.children.Add(tooClose);

            // 너무 멀음 → 차지/플랭/추격 중 랜덤
            var tooFar = Seq("TooFar");
            tooFar.children.Add(DistBB("DistFar_TooFar", DistanceCheck.GreaterThan, BBKey.OptimalCombatDistance));

            var tooFarRnd = Rnd("TooFar_Action");

            var chargeSeq = Seq("Charge_TooFar_Seq");
            chargeSeq.children.Add(BBBool("AllowCharge_TF", BBKey.PhaseAllowCharge));
            chargeSeq.children.Add(Node<BTAction_ChargeSO>("Charge_TooFar"));
            tooFarRnd.children.Add(chargeSeq);
            tooFarRnd.weights.Add(0.3f);

            var flankSeq = Seq("Flank_TooFar_Seq");
            flankSeq.children.Add(BBBool("AllowFlank_TF", BBKey.PhaseAllowFlank));
            flankSeq.children.Add(Node<BTAction_FlankSO>("Flank_TooFar"));
            tooFarRnd.children.Add(flankSeq);
            tooFarRnd.weights.Add(0.3f);

            tooFarRnd.children.Add(Node<BTAction_ChaseSO>("Chase_TooFar"));
            tooFarRnd.weights.Add(0.4f);

            tooFar.children.Add(tooFarRnd);
            sel.children.Add(tooFar);

            // 적정 거리 → 가드/서클 중 랜덤
            var inRangeRnd = Rnd("InRange_Idle");
            inRangeRnd.children.Add(GuardAction("Guard_InRange"));
            inRangeRnd.weights.Add(0.35f);
            inRangeRnd.children.Add(CircleAction("Circle_InRange"));
            inRangeRnd.weights.Add(0.65f);
            sel.children.Add(inRangeRnd);

            return sel;
        }

        // ── 3. 유휴 행동 ─────────────────────────────────────────────────
        private static BTSelectorSO BuildIdleBehavior()
        {
            var sel = Sel("IdleBehavior");

            var doPatrol = Seq("DoPatrol");
            doPatrol.children.Add(Node<BTAction_PatrolSO>("Patrol"));
            sel.children.Add(doPatrol);

            sel.children.Add(Node<BTAction_IdleSO>("Idle_Default"));
            return sel;
        }

        // ── 포스트 어택 트리 ──────────────────────────────────────────────

        private static void BuildPostAttack()
        {
            _tree = CreateTree(PostAsset, "BT_DefaultEnemy_PostAttack");

            var root = Sel("PostAttack_Root");

            // 경직 중 연속타
            var hitStagger = Seq("AttackHit_Staggered");
            hitStagger.children.Add(PlayerState("PlayerStaggered_PA", PlayerStateQuery.IsStaggered));
            hitStagger.children.Add(DistBB("InRange_Stagger_PA", DistanceCheck.LessThan, BBKey.MaxAttackRange, 1.2f));
            hitStagger.children.Add(Node<BTAction_AttackSO>("Attack_FollowStagger"));
            root.children.Add(hitStagger);

            // 연속 공격 확률
            var hitContinue = Seq("AttackHit_Continue");
            hitContinue.children.Add(RandomChance("ContinueChance", 0.35f));
            hitContinue.children.Add(DistBB("InRange_Continue", DistanceCheck.LessThan, BBKey.MaxAttackRange, 1.2f));
            hitContinue.children.Add(Node<BTAction_AttackSO>("Attack_Continue"));
            root.children.Add(hitContinue);

            // 회피형 플레이어 → 차지/플랭 강화
            var missDodge = Seq("AttackMiss_DodgingPlayer");
            missDodge.children.Add(PlayerState("PlayerDodging_PA", PlayerStateQuery.IsDodgingFrequently));

            var dodgeRnd = Rnd("DodgeReact");
            var chargeSeq = Seq("Charge_Dodge_Seq");
            chargeSeq.children.Add(BBBool("AllowCharge_Dodge", BBKey.PhaseAllowCharge));
            chargeSeq.children.Add(Node<BTAction_ChargeSO>("Charge_Dodge"));
            dodgeRnd.children.Add(chargeSeq);
            dodgeRnd.weights.Add(0.45f);
            var flankSeq = Seq("Flank_Dodge_Seq");
            flankSeq.children.Add(BBBool("AllowFlank_Dodge", BBKey.PhaseAllowFlank));
            flankSeq.children.Add(Node<BTAction_FlankSO>("Flank_Dodge"));
            dodgeRnd.children.Add(flankSeq);
            dodgeRnd.weights.Add(0.45f);
            dodgeRnd.children.Add(Node<BTAction_ChaseSO>("Chase_Dodge"));
            dodgeRnd.weights.Add(0.1f);
            missDodge.children.Add(dodgeRnd);
            root.children.Add(missDodge);

            // 기본 포스트 어택: 가중치 랜덤
            var defaultRnd = Rnd("DefaultPostAttack");

            var chargeDefault = Seq("Charge_Default_Seq");
            chargeDefault.children.Add(BBBool("AllowCharge_Default", BBKey.PhaseAllowCharge));
            chargeDefault.children.Add(Node<BTAction_ChargeSO>("Charge_Default"));
            defaultRnd.children.Add(chargeDefault);
            defaultRnd.weights.Add(0.2f);

            var flankDefault = Seq("Flank_Default_Seq");
            flankDefault.children.Add(BBBool("AllowFlank_Default", BBKey.PhaseAllowFlank));
            flankDefault.children.Add(Node<BTAction_FlankSO>("Flank_Default"));
            defaultRnd.children.Add(flankDefault);
            defaultRnd.weights.Add(0.2f);

            defaultRnd.children.Add(GuardAction("Guard_Default"));
            defaultRnd.weights.Add(0.2f);

            defaultRnd.children.Add(Node<BTAction_RetreatSO>("Retreat_Default"));
            defaultRnd.weights.Add(0.15f);

            defaultRnd.children.Add(CircleAction("Circle_Default"));
            defaultRnd.weights.Add(0.25f);

            root.children.Add(defaultRnd);

            _tree.rootNode = root;
            EditorUtility.SetDirty(_tree);
        }

        // ── SO 생성 헬퍼 ─────────────────────────────────────────────────

        private static BehaviorTreeSO CreateTree(string path, string treeName)
        {
            var tree = ScriptableObject.CreateInstance<BehaviorTreeSO>();
            tree.name = treeName;
            AssetDatabase.CreateAsset(tree, path);
            return tree;
        }

        private static T Node<T>(string name) where T : BTNodeSO
        {
            var so = ScriptableObject.CreateInstance<T>();
            so.name = name;
            so.nodeName = name;
            AssetDatabase.AddObjectToAsset(so, _tree);
            EditorUtility.SetDirty(so);
            return so;
        }

        private static BTSelectorSO Sel(string name)
        {
            var so = Node<BTSelectorSO>(name);
            return so;
        }

        private static BTSequenceSO Seq(string name)
        {
            var so = Node<BTSequenceSO>(name);
            return so;
        }

        private static BTRandomSelectorSO Rnd(string name)
        {
            var so = Node<BTRandomSelectorSO>(name);
            return so;
        }

        private static BTCond_CurrentStateSO CurState(string name, string stateName, bool invert = false)
        {
            var so = Node<BTCond_CurrentStateSO>(name);
            so.stateName = stateName;
            so.invert    = invert;
            return so;
        }

        private static BTCond_PlayerStateSO PlayerState(string name, PlayerStateQuery query)
        {
            var so = Node<BTCond_PlayerStateSO>(name);
            so.query = query;
            return so;
        }

        private static BTCond_BBBoolSO BBBool(string name, string key, bool invert = false)
        {
            var so = Node<BTCond_BBBoolSO>(name);
            so.key    = key;
            so.invert = invert;
            return so;
        }

        private static BTCond_DistanceBBSO DistBB(string name, DistanceCheck check,
            string thresholdKey, float multiplier = 1f)
        {
            var so = Node<BTCond_DistanceBBSO>(name);
            so.check        = check;
            so.thresholdKey = thresholdKey;
            so.multiplier   = multiplier;
            return so;
        }

        private static BTCond_DistanceBBSO DistBBBetween(string name,
            string minKey, string maxKey, float maxMultiplier = 1f)
        {
            var so = Node<BTCond_DistanceBBSO>(name);
            so.check        = DistanceCheck.Between;
            so.minKey       = minKey;
            so.thresholdKey = maxKey;
            so.multiplier   = maxMultiplier;
            return so;
        }

        private static BTCond_RandomChanceSO RandomChance(string name, float probability)
        {
            var so = Node<BTCond_RandomChanceSO>(name);
            so.probability = probability;
            return so;
        }

        private static BTAction_GuardSO GuardAction(string name,
            float minDur = 0.8f, float maxDur = 1.5f)
        {
            var so = Node<BTAction_GuardSO>(name);
            so.minDuration = minDur;
            so.maxDuration = maxDur;
            return so;
        }

        private static BTAction_CircleSO CircleAction(string name,
            float minDur = 1.0f, float maxDur = 2.5f)
        {
            var so = Node<BTAction_CircleSO>(name);
            so.minDuration = minDur;
            so.maxDuration = maxDur;
            return so;
        }

        // Inverter로 감싼 PlayerState 조건
        private static BTInverterSO InvertPlayerState(string name, PlayerStateQuery query)
        {
            var inv   = Node<BTInverterSO>(name);
            var inner = PlayerState(name + "_Inner", query);
            inv.child = inner;
            return inv;
        }

        // Inverter로 감싼 ConsecutiveDefense 조건
        private static BTInverterSO InvertConsecDefense(string name, int maxStreak)
        {
            var inv   = Node<BTInverterSO>(name);
            var inner = Node<BTCond_ConsecutiveDefenseSO>(name + "_Inner");
            inner.maxStreak = maxStreak;
            inv.child       = inner;
            return inv;
        }
    }
}
