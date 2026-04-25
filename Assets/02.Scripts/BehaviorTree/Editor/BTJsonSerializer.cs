using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.BehaviorTree
{
    using UPlayGround.BehaviorTree;

    public static class BTJsonSerializer
    {
        // ── 직렬화 데이터 구조 ──────────────────────────────────────

        [Serializable]
        private class JsonTree
        {
            public string         treeName;
            public string         rootGuid;
            public List<JsonNode> nodes = new();
        }

        [Serializable]
        private class JsonNode
        {
            public string       guid;
            public string       typeName;       // 단순 클래스명 (e.g. "BTSelectorSO")
            public string       soJson;         // JsonUtility.ToJson(so) — 기본형 필드 전체
            public List<string> childGuids = new();
        }

        // ── 내보내기 ───────────────────────────────────────────────

        public static string ExportTree(BehaviorTreeSO tree)
        {
            var guidMap  = new Dictionary<BTNodeSO, string>();
            var allNodes = new List<BTNodeSO>();
            CollectNodes(tree.rootNode, guidMap, allNodes);

            var jTree = new JsonTree
            {
                treeName = tree.name,
                rootGuid = tree.rootNode != null ? guidMap[tree.rootNode] : null,
            };

            foreach (var so in allNodes)
            {
                var jNode = new JsonNode
                {
                    guid     = guidMap[so],
                    typeName = so.GetType().Name,
                    soJson   = JsonUtility.ToJson(so),
                };
                foreach (var child in GetSOChildren(so))
                    jNode.childGuids.Add(guidMap[child]);

                jTree.nodes.Add(jNode);
            }

            return JsonUtility.ToJson(jTree, true);
        }

        // ── 불러오기 ───────────────────────────────────────────────

        public static BehaviorTreeSO ImportTree(string json, string savePath)
        {
            var jTree = JsonUtility.FromJson<JsonTree>(json);
            if (jTree == null)
                throw new InvalidOperationException("유효하지 않은 JSON입니다.");

            // 1. BehaviorTreeSO 에셋 생성
            var tree = ScriptableObject.CreateInstance<BehaviorTreeSO>();
            tree.name = jTree.treeName;
            AssetDatabase.CreateAsset(tree, savePath);

            // 2. 노드 SO 인스턴스 생성 및 프로퍼티 복원
            var guidToNode = new Dictionary<string, BTNodeSO>(jTree.nodes.Count);
            foreach (var jNode in jTree.nodes)
            {
                var type = TypeCache.GetTypesDerivedFrom<BTNodeSO>()
                    .FirstOrDefault(t => t.Name == jNode.typeName);
                if (type == null)
                {
                    Debug.LogWarning($"[BTJsonSerializer] 타입 없음: {jNode.typeName}");
                    continue;
                }

                var so = ScriptableObject.CreateInstance(type) as BTNodeSO;
                if (so == null) continue;

                JsonUtility.FromJsonOverwrite(jNode.soJson, so);
                so.name = so.nodeName;
                AssetDatabase.AddObjectToAsset(so, tree);
                guidToNode[jNode.guid] = so;
            }

            // 3. 자식 연결 복원
            foreach (var jNode in jTree.nodes)
            {
                if (!guidToNode.TryGetValue(jNode.guid, out var so)) continue;
                WireChildren(so, jNode.childGuids, guidToNode);
                EditorUtility.SetDirty(so);
            }

            // 4. 루트 설정
            if (!string.IsNullOrEmpty(jTree.rootGuid) &&
                guidToNode.TryGetValue(jTree.rootGuid, out var root))
            {
                tree.rootNode = root;
            }

            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(savePath);
            return tree;
        }

        // ── 내부 유틸 ──────────────────────────────────────────────

        private static void CollectNodes(BTNodeSO so, Dictionary<BTNodeSO, string> map, List<BTNodeSO> list)
        {
            if (so == null || map.ContainsKey(so)) return;
            map[so] = Guid.NewGuid().ToString("N").Substring(0, 8);
            list.Add(so);
            foreach (var child in GetSOChildren(so))
                CollectNodes(child, map, list);
        }

        private static List<BTNodeSO> GetSOChildren(BTNodeSO so)
        {
            var list = new List<BTNodeSO>();
            switch (so)
            {
                case BTSelectorSO       sel: foreach (var c in sel.children) if (c != null) list.Add(c); break;
                case BTSequenceSO       seq: foreach (var c in seq.children) if (c != null) list.Add(c); break;
                case BTRandomSelectorSO rnd: foreach (var c in rnd.children) if (c != null) list.Add(c); break;
                case BTInverterSO       inv: if (inv.child  != null) list.Add(inv.child);  break;
                case BTCooldownSO       cd:  if (cd.child   != null) list.Add(cd.child);   break;
                case BTForceSuccessSO   fs:  if (fs.child   != null) list.Add(fs.child);   break;
                case BTLoopSO           lp:  if (lp.child   != null) list.Add(lp.child);   break;
                case BTGuardSO          g:
                    if (g.condition != null) list.Add(g.condition);
                    if (g.child     != null) list.Add(g.child);
                    break;
            }
            return list;
        }

        private static void WireChildren(BTNodeSO so, List<string> childGuids, Dictionary<string, BTNodeSO> map)
        {
            if (childGuids.Count == 0) return;

            var resolved = childGuids
                .Where(g => map.ContainsKey(g))
                .Select(g => map[g])
                .ToList();

            switch (so)
            {
                case BTSelectorSO sel:
                    sel.children = resolved;
                    break;
                case BTSequenceSO seq:
                    seq.children = resolved;
                    break;
                case BTRandomSelectorSO rnd:
                    rnd.children = resolved;
                    while (rnd.weights.Count < rnd.children.Count) rnd.weights.Add(1f);
                    while (rnd.weights.Count > rnd.children.Count) rnd.weights.RemoveAt(rnd.weights.Count - 1);
                    break;
                case BTInverterSO inv:
                    inv.child = resolved.Count > 0 ? resolved[0] : null;
                    break;
                case BTCooldownSO cd:
                    cd.child = resolved.Count > 0 ? resolved[0] : null;
                    break;
                case BTForceSuccessSO fs:
                    fs.child = resolved.Count > 0 ? resolved[0] : null;
                    break;
                case BTLoopSO lp:
                    lp.child = resolved.Count > 0 ? resolved[0] : null;
                    break;
                case BTGuardSO g:
                    g.condition = resolved.Count > 0 ? resolved[0] : null;
                    g.child     = resolved.Count > 1 ? resolved[1] : null;
                    break;
            }
        }
    }
}
