using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.BehaviorTree
{
    using UPlayGround.BehaviorTree;

    /// <summary>
    /// EnemyFlyingBrain용 BehaviorTreeSO 에셋을 코드로 생성한다.
    /// Window > BehaviorTree > Build FlyingEnemy Asset
    /// </summary>
    public static class BTFlyingEnemyBuilder
    {
        private const string SaveFolder = "Assets/10.Datas/BT";
        private const string MainAsset  = "Assets/10.Datas/BT/BT_FlyingEnemy.asset";
        private const string PostAsset  = "Assets/10.Datas/BT/BT_FlyingEnemy_PostGroundAttack.asset";

        private static BehaviorTreeSO _tree;

        [MenuItem("Window/BehaviorTree/Build FlyingEnemy Asset")]
        public static void BuildFlyingEnemy()
        {
            if (!System.IO.Directory.Exists(SaveFolder))
                System.IO.Directory.CreateDirectory(SaveFolder);

            BuildMain();
            BuildPostGroundAttack();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BTFlyingEnemyBuilder] BT_FlyingEnemy.asset / BT_FlyingEnemy_PostGroundAttack.asset 생성 완료");
        }

        [MenuItem("Window/BehaviorTree/Generate FlyingEnemy JSON")]
        public static void GenerateFlyingEnemyJson()
        {
            if (!System.IO.Directory.Exists(SaveFolder))
                System.IO.Directory.CreateDirectory(SaveFolder);

            if (AssetDatabase.LoadAssetAtPath<BehaviorTreeSO>(MainAsset) == null
                || AssetDatabase.LoadAssetAtPath<BehaviorTreeSO>(PostAsset) == null)
                BuildFlyingEnemy();

            WriteTestJson(MainAsset, SaveFolder + "/BT_FlyingEnemy_Test.json");
            WriteTestJson(PostAsset,  SaveFolder + "/BT_FlyingEnemy_PostGroundAttack_Test.json");

            AssetDatabase.Refresh();
            Debug.Log("[BTFlyingEnemyBuilder] Test JSON 파일 생성 완료 → Assets/10.Datas/BT/");
        }

        private static void WriteTestJson(string assetPath, string jsonPath)
        {
            var tree = AssetDatabase.LoadAssetAtPath<BehaviorTreeSO>(assetPath);
            if (tree == null) { Debug.LogError($"[BTFlyingEnemyBuilder] 에셋 없음: {assetPath}"); return; }
            string json = BTJsonSerializer.ExportTree(tree);
            System.IO.File.WriteAllText(jsonPath, json, System.Text.Encoding.UTF8);
            Debug.Log($"[BTFlyingEnemyBuilder] JSON 저장: {jsonPath}");
        }

        // ── 메인 트리 ─────────────────────────────────────────────────────
        // EnemyFlyingBrain.MakeDecision(stateName) 로직을 BT로 매핑

        private static void BuildMain()
        {
            _tree = CreateTree(MainAsset, "BT_FlyingEnemy");

            var root = Sel("Root");

            // 1. 개입 금지 상태 (Attack/TakeOff/Dive/Land/Hit/Grabbed) → 즉시 Success로 막음
            root.children.Add(BuildBlockedStates());

            // 2. 피격/Airborne 복귀
            root.children.Add(BuildRecovery());

            // 3. 타겟 소실 처리
            root.children.Add(BuildNoTarget());

            // 4. Patrol 중 타겟 발견 → Chase 전환
            root.children.Add(BuildPatrolToChase());

            // 5. 지상 Chase 중 이륙 조건 달성
            root.children.Add(BuildChaseToTakeOff());

            // 6. Circle/Retreat 중 이륙 조건 달성
            root.children.Add(BuildGroundIdleToTakeOff());

            // 7. AirCircle 중 공격 횟수 소진 → 하강
            root.children.Add(BuildAirCircleToDescend());

            _tree.rootNode = root;
            EditorUtility.SetDirty(_tree);
        }

        // 1. 개입 금지 상태
        private static BTSelectorSO BuildBlockedStates()
        {
            var sel = Sel("BlockedStates");
            sel.children.Add(CurState("IsAttacking",  "Flying_GroundAttack"));
            sel.children.Add(CurState("IsTakingOff",  "Flying_TakeOff"));
            sel.children.Add(CurState("IsDiving",     "Flying_Dive"));
            sel.children.Add(CurState("IsLanding",    "Flying_Land"));
            sel.children.Add(CurState("IsHit",        "Hit"));
            sel.children.Add(CurState("IsGrabbed",    "Grabbed"));
            sel.children.Add(CurState("IsDead",       "Death"));
            return sel;
        }

        // 2. Idle/Airborne 상태에서 복귀 (ReenterLoop)
        private static BTSelectorSO BuildRecovery()
        {
            var sel = Sel("Recovery");

            // Idle 복귀
            var idleRecovery = Seq("IdleRecovery");
            idleRecovery.children.Add(CurState("IsIdle", "Idle"));
            idleRecovery.children.Add(BuildReenterDecision());
            sel.children.Add(idleRecovery);

            // Airborne 복귀
            var airborneRecovery = Seq("AirborneRecovery");
            airborneRecovery.children.Add(CurState("IsAirborne", "Airborne"));
            airborneRecovery.children.Add(BuildReenterDecision());
            sel.children.Add(airborneRecovery);

            return sel;
        }

        // ReenterLoop 결정: ShouldTakeOff → TakeOff, else → Chase or Patrol/Idle
        private static BTSelectorSO BuildReenterDecision()
        {
            var sel = Sel("ReenterDecision");

            // ShouldTakeOff → TakeOff
            var takeOffSeq = Seq("Reenter_TakeOff");
            takeOffSeq.children.Add(BBBool("ShouldTakeOff_Reenter", BBKey.ShouldTakeOff));
            takeOffSeq.children.Add(Node<BTAction_TakeOffSO>("TakeOff_Reenter"));
            sel.children.Add(takeOffSeq);

            // HasTarget → FlyingChase
            var chaseSeq = Seq("Reenter_Chase");
            chaseSeq.children.Add(Node<BTCond_HasTargetSO>("HasTarget_Reenter"));
            chaseSeq.children.Add(Node<BTAction_FlyingChaseSO>("Chase_Reenter"));
            sel.children.Add(chaseSeq);

            // 타겟 없음 → Patrol or Idle
            sel.children.Add(Node<BTAction_FlyingPatrolSO>("Patrol_Reenter"));

            return sel;
        }

        // 3. 타겟 소실
        private static BTSelectorSO BuildNoTarget()
        {
            var sel = Sel("NoTarget");

            // 타겟 없음 조건 먼저
            var noTargetCheck = Seq("NoTargetCheck");
            noTargetCheck.children.Add(Inv(Node<BTCond_HasTargetSO>("HasNoTarget")));

            var noTargetBehavior = Sel("NoTargetBehavior");

            // 공중 상태면 하강
            var airDescend = Seq("Air_Descend");
            airDescend.children.Add(BBBool("IsAirState_NoTgt", BBKey.IsAirState));
            airDescend.children.Add(Node<BTAction_DescendSO>("Descend_NoTarget"));
            noTargetBehavior.children.Add(airDescend);

            // 지상 → Patrol 또는 Idle (이미 Patrol이면 유지)
            var groundIdle = Seq("Ground_ToPatrol");
            groundIdle.children.Add(InvCurState("NotPatrol", "Flying_Patrol"));
            groundIdle.children.Add(InvCurState("NotIdle", "Idle"));
            groundIdle.children.Add(Node<BTAction_FlyingPatrolSO>("Patrol_NoTarget"));
            noTargetBehavior.children.Add(groundIdle);

            noTargetCheck.children.Add(noTargetBehavior);
            sel.children.Add(noTargetCheck);
            return sel;
        }

        // 4. Patrol 중 타겟 발견 → Chase
        private static BTSequenceSO BuildPatrolToChase()
        {
            var seq = Seq("PatrolToChase");

            var isPatrol = Sel("IsPatrolling");
            isPatrol.children.Add(CurState("IsFlyingPatrol", "Flying_Patrol"));
            isPatrol.children.Add(CurState("IsGroundPatrol", "Patrol"));
            seq.children.Add(isPatrol);

            seq.children.Add(Node<BTCond_HasTargetSO>("HasTarget_Patrol"));
            seq.children.Add(Node<BTAction_FlyingChaseSO>("Chase_FromPatrol"));
            return seq;
        }

        // 5. Flying_Chase 중 이륙 조건 달성
        private static BTSequenceSO BuildChaseToTakeOff()
        {
            var seq = Seq("ChaseToTakeOff");
            seq.children.Add(CurState("IsFlying_Chase", "Flying_Chase"));
            seq.children.Add(BBBool("ShouldTakeOff_Chase", BBKey.ShouldTakeOff));
            seq.children.Add(Node<BTAction_TakeOffSO>("TakeOff_Chase"));
            return seq;
        }

        // 6. Circle/Retreat 중 이륙 조건 달성
        private static BTSelectorSO BuildGroundIdleToTakeOff()
        {
            var sel = Sel("GroundIdleToTakeOff");

            var circleSeq = Seq("CircleToTakeOff");
            circleSeq.children.Add(CurState("IsCircle", "Flying_Circle"));
            circleSeq.children.Add(BBBool("ShouldTakeOff_Circle", BBKey.ShouldTakeOff));
            circleSeq.children.Add(Node<BTAction_TakeOffSO>("TakeOff_Circle"));
            sel.children.Add(circleSeq);

            var retreatSeq = Seq("RetreatToTakeOff");
            retreatSeq.children.Add(CurState("IsRetreat", "Flying_Retreat"));
            retreatSeq.children.Add(BBBool("ShouldTakeOff_Retreat", BBKey.ShouldTakeOff));
            retreatSeq.children.Add(Node<BTAction_TakeOffSO>("TakeOff_Retreat"));
            sel.children.Add(retreatSeq);

            return sel;
        }

        // 7. AirCircle 공격 횟수 소진 → 하강
        private static BTSequenceSO BuildAirCircleToDescend()
        {
            var seq = Seq("AirCircleToDescend");
            seq.children.Add(CurState("IsAirCircle", "Flying_AirCircle"));
            seq.children.Add(BBBool("ShouldDescend_Air", BBKey.ShouldDescend));
            seq.children.Add(Node<BTAction_DescendSO>("Descend_AirCircle"));
            return seq;
        }

        // ── 포스트 지상 공격 트리 ─────────────────────────────────────────
        // EnemyFlyingBrain.DecidePostGroundAttack() 로직 매핑

        private static void BuildPostGroundAttack()
        {
            _tree = CreateTree(PostAsset, "BT_FlyingEnemy_PostGroundAttack");

            var root = Sel("PostGroundAttack_Root");

            // 타겟 소실 → Idle
            var noTarget = Seq("PostAttack_NoTarget");
            noTarget.children.Add(Inv(Node<BTCond_HasTargetSO>("HasTarget_PA")));
            noTarget.children.Add(Node<BTAction_FlyingIdleSO>("Idle_PostAttack"));
            root.children.Add(noTarget);

            // 너무 가까움 → 후퇴 확률 증가
            var tooClose = Seq("PostAttack_TooClose");
            tooClose.children.Add(DistBB("TooClose_PA", DistanceCheck.LessThan, BBKey.MinCombatDistance));
            tooClose.children.Add(RandomChance("RetreatChance_Close", 0.4f));
            tooClose.children.Add(Node<BTAction_FlyingRetreatSO>("Retreat_TooClose"));
            root.children.Add(tooClose);

            // Circle (가중치 0.3)
            var circleSeq = Seq("PostAttack_Circle");
            circleSeq.children.Add(RandomChance("CircleChance_PA", 0.3f));
            circleSeq.children.Add(Node<BTAction_FlyingCircleSO>("Circle_PostAttack"));
            root.children.Add(circleSeq);

            // Retreat (가중치 0.2)
            var retreatSeq = Seq("PostAttack_Retreat");
            retreatSeq.children.Add(RandomChance("RetreatChance_PA", 0.2f));
            retreatSeq.children.Add(Node<BTAction_FlyingRetreatSO>("Retreat_PostAttack"));
            root.children.Add(retreatSeq);

            // 기본 → Chase
            root.children.Add(Node<BTAction_FlyingChaseSO>("Chase_PostAttack"));

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

        private static BTSelectorSO Sel(string name)   => Node<BTSelectorSO>(name);
        private static BTSequenceSO Seq(string name)   => Node<BTSequenceSO>(name);

        private static BTCond_CurrentStateSO CurState(string name, string state)
        {
            var so = Node<BTCond_CurrentStateSO>(name);
            so.stateName = state;
            so.invert = false;
            return so;
        }

        private static BTCond_CurrentStateSO InvCurState(string name, string state)
        {
            var so = Node<BTCond_CurrentStateSO>(name);
            so.stateName = state;
            so.invert = true;
            return so;
        }

        private static BTCond_BBBoolSO BBBool(string name, string key, bool invert = false)
        {
            var so = Node<BTCond_BBBoolSO>(name);
            so.key = key;
            so.invert = invert;
            return so;
        }

        private static BTCond_DistanceBBSO DistBB(string name, DistanceCheck check, string thresholdKey, float multiplier = 1f)
        {
            var so = Node<BTCond_DistanceBBSO>(name);
            so.check = check;
            so.thresholdKey = thresholdKey;
            so.multiplier = multiplier;
            return so;
        }

        private static BTCond_RandomChanceSO RandomChance(string name, float prob)
        {
            var so = Node<BTCond_RandomChanceSO>(name);
            so.probability = prob;
            return so;
        }

        private static BTInverterSO Inv(BTNodeSO child)
        {
            var so = Node<BTInverterSO>("Inv_" + child.nodeName);
            so.child = child;
            return so;
        }
    }
}
