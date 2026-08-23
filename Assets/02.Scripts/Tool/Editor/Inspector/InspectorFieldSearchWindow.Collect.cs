using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UPlayGround.Tool.Editor
{
    /// <summary>인스펙터 필드 검색기의 대상 수집·검색 매칭 부분.</summary>
    public partial class InspectorFieldSearchWindow
    {
        // 검색어가 필드 이름에 얼마나 정확히 맞는지에 따라 점수를 매겨 관련도 순으로 정렬한다.
        // 이름 매칭이 값 매칭보다 항상 위에 오도록 값 점수를 가장 낮게 둔다.
        private const int ExactMatchScore = 100;
        private const int PrefixMatchScore = 60;
        private const int WordMatchScore = 40;
        private const int ContainsMatchScore = 20;
        private const int ValueMatchScore = 10;

        private enum EntryKind
        {
            Component,
            Material,
            Asset,
        }

        private enum GroupKind
        {
            GameObject,
            Asset,
            MultiSelection,
        }

        /// <summary>검색에 걸린 필드 하나. 관련도 순 정렬에 쓸 점수와 깊이를 함께 들고 있다.</summary>
        private readonly struct MatchedField
        {
            /// <summary>컴포넌트는 SerializedProperty 경로, 머티리얼은 셰이더 프로퍼티 이름.</summary>
            public readonly string Path;

            public readonly int Score;
            public readonly int Depth;

            public MatchedField(string path, int score, int depth)
            {
                Path = path;
                Score = score;
                Depth = depth;
            }
        }

        /// <summary>창에 표시되는 항목 하나. 컴포넌트·에셋이거나 Renderer가 쓰는 머티리얼이다.</summary>
        private sealed class InspectedEntry
        {
            public EntryKind Kind;

            /// <summary>편집 대상. 다중 선택에서는 같은 타입의 컴포넌트·에셋이 여럿 들어간다.</summary>
            public UnityEngine.Object[] Targets;

            public GameObject Owner;
            public int Key;

            /// <summary>머티리얼일 때 Renderer의 몇 번째 슬롯인지. 그 외는 -1.</summary>
            public int MaterialSlot = -1;

            /// <summary>검색 모드에서만 사용한다. 전체 보기에서는 Editor가 자체 SerializedObject를 소유한다.</summary>
            public SerializedObject SerializedObject;

            /// <summary>전체 보기용. 재수집에서도 재사용해 재생성 비용을 없앤다.</summary>
            public UnityEditor.Editor Editor;

            /// <summary>머티리얼 프로퍼티를 그리기 위한 에디터와 그 대상 배열(매 프레임 할당을 피한다).</summary>
            public MaterialEditor MaterialEditor;
            public UnityEngine.Object[] MaterialContext;

            public readonly List<MatchedField> Matches = new();

            /// <summary>이 결과를 만든 질의 서명. 같으면 재스캔을 건너뛴다.</summary>
            public string ScanSignature;

            public bool IsExpanded;
            public bool IsRenderFailed;

            /// <summary>스크롤 컬링용 실측 높이. 0이면 아직 한 번도 그리지 않은 상태다.</summary>
            public float CachedHeight;
            public bool IsCulled;

            public UnityEngine.Object Target => Targets[0];
            public bool IsMultiTarget => Targets.Length > 1;
        }

        /// <summary>한 대상에 속한 항목 묶음. 어느 오브젝트·에셋의 필드인지 바로 보이게 한다.</summary>
        private sealed class TargetGroup
        {
            public GroupKind Kind;

            /// <summary>단일 대상일 때의 소유자. 다중 선택 그룹은 null이다.</summary>
            public UnityEngine.Object Owner;

            /// <summary>다중 선택에서 Tag·Layer·활성 상태를 함께 편집할 오브젝트들.</summary>
            public GameObject[] OwnerGameObjects;

            /// <summary>검색 대상 루트 기준 상대 경로. 루트 자신은 빈 문자열이다.</summary>
            public string RelativePath;

            public string Title;
            public int Key;

            public readonly List<InspectedEntry> Entries = new();
            public bool IsExpanded = true;

            /// <summary>선택 추적 스크롤에 쓰는 콘텐츠 기준 Y 위치.</summary>
            public float ContentY;
        }

        // ── 수집 ──────────────────────────────────────────────────────
        private void RebuildGroups(List<UnityEngine.Object> targets)
        {
            // 이전 항목을 키로 넘겨받아 Editor와 SerializedObject를 재사용한다.
            // 남는 항목만 마지막에 파괴하므로 Hierarchy 변경마다 인스펙터가 통째로 재생성되지 않는다.
            _reusableEntries.Clear();
            foreach (KeyValuePair<int, InspectedEntry> pair in _entryByKey)
                _reusableEntries.Add(pair.Key, pair.Value);
            _entryByKey.Clear();

            _groups.Clear();
            _missingScriptOwners.Clear();
            _visitedMaterialIds.Clear();
            _matchedFieldCount = 0;
            _componentEntryCount = 0;
            _materialEntryCount = 0;
            _scannedPropertyCount = 0;
            _isBudgetExceeded = false;
            _isTargetBudgetExceeded = false;
            _isMultiEditing = targets.Count > 1;
            _targetDescription = "";

            _queryTokens = BuildQueryTokens(_query);
            _scanSignature = BuildScanSignature();

            if (targets.Count == 1)
                BuildSingleTarget(targets[0]);
            else if (targets.Count > 1)
                BuildMultiTarget(targets);

            foreach (KeyValuePair<int, InspectedEntry> pair in _reusableEntries)
                DisposeEntry(pair.Value);

            _reusableEntries.Clear();
            PruneInlineEditors();
        }

        private void BuildSingleTarget(UnityEngine.Object target)
        {
            if (target is GameObject gameObject)
            {
                _targetDescription = _isIncludingChildren ? $"{gameObject.name} (하위 포함)" : gameObject.name;

                foreach (GameObject owner in EnumerateOwners(gameObject))
                {
                    if (_isBudgetExceeded)
                        break;

                    TargetGroup group = BuildGameObjectGroup(owner, gameObject);
                    if (group != null)
                        _groups.Add(group);
                }

                return;
            }

            _targetDescription = $"{target.name} ({target.GetType().Name})";

            TargetGroup assetGroup = BuildAssetGroup(target);
            if (assetGroup != null)
                _groups.Add(assetGroup);
        }

        /// <summary>
        /// 다중 선택. GameObject가 둘 이상이면 공통 컴포넌트를 묶어 한 번에 편집하고,
        /// 에셋이면 같은 타입끼리 묶는다. 서로 다른 성격을 한 화면에서 섞지 않는다.
        /// </summary>
        private void BuildMultiTarget(List<UnityEngine.Object> targets)
        {
            var gameObjects = new List<GameObject>();
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] is GameObject gameObject)
                    gameObjects.Add(gameObject);
            }

            if (gameObjects.Count > 1)
            {
                _targetDescription = $"오브젝트 {gameObjects.Count}개 (공통 컴포넌트)";
                TargetGroup group = BuildMultiGameObjectGroup(gameObjects);
                if (group != null)
                    _groups.Add(group);
                return;
            }

            _targetDescription = $"에셋 {targets.Count}개";
            BuildMultiAssetGroups(targets);
        }

        private IEnumerable<GameObject> EnumerateOwners(GameObject target)
        {
            if (!_isIncludingChildren)
            {
                yield return target;
                yield break;
            }

            Transform[] transforms = target.GetComponentsInChildren<Transform>(true);
            int count = Mathf.Min(transforms.Length, TargetObjectBudget);
            _isTargetBudgetExceeded = transforms.Length > TargetObjectBudget;

            for (int i = 0; i < count; i++)
                yield return transforms[i].gameObject;
        }

        private TargetGroup BuildGameObjectGroup(GameObject owner, GameObject root)
        {
            bool isRoot = owner == root;
            var group = new TargetGroup
            {
                Kind = GroupKind.GameObject,
                Owner = owner,
                RelativePath = BuildRelativePath(owner, root),
                Key = owner.GetInstanceID(),
                IsExpanded = ResolveGroupExpanded(owner.GetInstanceID(), isRoot),
            };

            Component[] components = owner.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (_isBudgetExceeded)
                    break;

                if (component == null)
                {
                    // GetComponents는 스크립트가 유실된 슬롯을 null로 돌려준다.
                    if (!_missingScriptOwners.Contains(owner.name))
                        _missingScriptOwners.Add(owner.name);
                    continue;
                }

                AddEntry(group, BuildObjectEntry(SingleTargetArray(component), owner, EntryKind.Component, isRoot));
            }

            if (_isIncludingMaterials)
                CollectMaterials(owner, group);

            // 검색 모드에서 아무것도 걸리지 않은 오브젝트는 결과를 흐리므로 통째로 숨긴다.
            return group.Entries.Count > 0 ? group : null;
        }

        private TargetGroup BuildAssetGroup(UnityEngine.Object asset)
        {
            var group = new TargetGroup
            {
                Kind = GroupKind.Asset,
                Owner = asset,
                Title = $"{asset.name}   ({asset.GetType().Name})",
                Key = asset.GetInstanceID(),
                IsExpanded = ResolveGroupExpanded(asset.GetInstanceID(), true),
            };

            InspectedEntry entry = asset is Material material
                ? BuildMaterialEntry(material, null, -1)
                : BuildObjectEntry(SingleTargetArray(asset), null, EntryKind.Asset, true);

            AddEntry(group, entry);
            return group.Entries.Count > 0 ? group : null;
        }

        /// <summary>선택한 GameObject 전부가 함께 가진 컴포넌트만 다중 타깃으로 묶는다.</summary>
        private TargetGroup BuildMultiGameObjectGroup(List<GameObject> owners)
        {
            int groupKey = MakeOwnersKey(owners);
            var group = new TargetGroup
            {
                Kind = GroupKind.MultiSelection,
                OwnerGameObjects = owners.ToArray(),
                Title = $"선택한 오브젝트 {owners.Count}개",
                Key = groupKey,
                IsExpanded = ResolveGroupExpanded(groupKey, true),
            };

            var sharedCounts = new Dictionary<System.Type, int>();
            var typeOrder = new List<System.Type>();
            CollectSharedComponentTypes(owners, sharedCounts, typeOrder);

            var buffer = new List<UnityEngine.Object>(owners.Count);
            foreach (System.Type type in typeOrder)
            {
                if (_isBudgetExceeded)
                    break;

                int count = sharedCounts[type];
                for (int index = 0; index < count; index++)
                {
                    buffer.Clear();
                    for (int i = 0; i < owners.Count; i++)
                    {
                        Component[] components = owners[i].GetComponents(type);
                        if (index < components.Length && components[index] != null)
                            buffer.Add(components[index]);
                    }

                    if (buffer.Count != owners.Count)
                        continue;

                    AddEntry(group, BuildObjectEntry(buffer.ToArray(), owners[0], EntryKind.Component, true));
                }
            }

            if (_isIncludingMaterials)
            {
                // 다중 선택의 머티리얼은 공유 여부가 제각각이라 합집합으로 모아 하나씩 편집한다.
                for (int i = 0; i < owners.Count; i++)
                    CollectMaterials(owners[i], group);
            }

            return group.Entries.Count > 0 ? group : null;
        }

        private void CollectSharedComponentTypes(List<GameObject> owners, Dictionary<System.Type, int> sharedCounts,
            List<System.Type> typeOrder)
        {
            Component[] first = owners[0].GetComponents<Component>();
            foreach (Component component in first)
            {
                if (component == null)
                    continue;

                System.Type type = component.GetType();
                if (sharedCounts.TryGetValue(type, out int count))
                {
                    sharedCounts[type] = count + 1;
                    continue;
                }

                sharedCounts.Add(type, 1);
                typeOrder.Add(type);
            }

            var otherCounts = new Dictionary<System.Type, int>();
            for (int i = 1; i < owners.Count; i++)
            {
                otherCounts.Clear();
                Component[] components = owners[i].GetComponents<Component>();
                foreach (Component component in components)
                {
                    if (component == null)
                        continue;

                    System.Type type = component.GetType();
                    otherCounts[type] = otherCounts.TryGetValue(type, out int count) ? count + 1 : 1;
                }

                for (int t = 0; t < typeOrder.Count; t++)
                {
                    System.Type type = typeOrder[t];
                    int shared = otherCounts.TryGetValue(type, out int count) ? count : 0;
                    sharedCounts[type] = Mathf.Min(sharedCounts[type], shared);
                }
            }

            // 어느 한 오브젝트에라도 없는 타입은 공통 편집이 성립하지 않는다.
            for (int t = typeOrder.Count - 1; t >= 0; t--)
            {
                if (sharedCounts[typeOrder[t]] <= 0)
                    typeOrder.RemoveAt(t);
            }
        }

        /// <summary>같은 타입 에셋끼리 묶어 한 번에 편집한다. AbilitySO 여러 개를 함께 고치는 흐름을 노린다.</summary>
        private void BuildMultiAssetGroups(List<UnityEngine.Object> targets)
        {
            var assetsByType = new Dictionary<System.Type, List<UnityEngine.Object>>();
            var typeOrder = new List<System.Type>();

            for (int i = 0; i < targets.Count; i++)
            {
                System.Type type = targets[i].GetType();
                if (!assetsByType.TryGetValue(type, out List<UnityEngine.Object> list))
                {
                    list = new List<UnityEngine.Object>();
                    assetsByType.Add(type, list);
                    typeOrder.Add(type);
                }

                list.Add(targets[i]);
            }

            foreach (System.Type type in typeOrder)
            {
                if (_isBudgetExceeded)
                    break;

                List<UnityEngine.Object> assets = assetsByType[type];
                UnityEngine.Object[] array = assets.ToArray();
                int key = MakeTargetsKey(array, 0);

                var group = new TargetGroup
                {
                    Kind = GroupKind.Asset,
                    Owner = assets.Count == 1 ? assets[0] : null,
                    Title = assets.Count == 1 ? $"{assets[0].name}   ({type.Name})" : $"{type.Name} × {assets.Count}",
                    Key = key,
                    IsExpanded = ResolveGroupExpanded(key, true),
                };

                if (assets.Count == 1 && assets[0] is Material material)
                    AddEntry(group, BuildMaterialEntry(material, null, -1));
                else
                    AddEntry(group, BuildObjectEntry(array, null, EntryKind.Asset, true));

                if (group.Entries.Count > 0)
                    _groups.Add(group);
            }
        }

        private void AddEntry(TargetGroup group, InspectedEntry entry)
        {
            if (entry == null)
                return;

            group.Entries.Add(entry);

            if (entry.Kind == EntryKind.Material)
                _materialEntryCount++;
            else
                _componentEntryCount++;
        }

        private void CollectMaterials(GameObject owner, TargetGroup group)
        {
            var renderers = owner.GetComponents<Renderer>();
            if (renderers.Length == 0)
                return;

            if (group.Kind != GroupKind.MultiSelection)
                _visitedMaterialIds.Clear();

            foreach (Renderer renderer in renderers)
            {
                if (_isBudgetExceeded)
                    break;

                // 에디터에서 renderer.materials를 읽으면 머티리얼 인스턴스가 새로 생기므로 sharedMaterials만 쓴다.
                Material[] materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    Material material = materials[slot];
                    if (material == null)
                        continue;

                    // 같은 머티리얼이 여러 슬롯·오브젝트에 걸리면 한 번만 보여준다.
                    if (!_visitedMaterialIds.Add(material.GetInstanceID()))
                        continue;

                    AddEntry(group, BuildMaterialEntry(material, owner, slot));
                }
            }
        }

        private InspectedEntry BuildObjectEntry(UnityEngine.Object[] targets, GameObject owner, EntryKind kind,
            bool defaultExpanded)
        {
            int key = MakeTargetsKey(targets, owner != null ? owner.GetInstanceID() : 0);
            InspectedEntry entry = TakeEntry(key, kind, targets, owner, -1);

            if (!IsSearching)
            {
                entry.Matches.Clear();
                entry.ScanSignature = null;
                entry.IsExpanded = ResolveExpanded(key, defaultExpanded);
                CommitEntry(key, entry);
                return entry;
            }

            if (!TryCollectMatchedPaths(entry))
            {
                DropEntry(key, entry);
                return null;
            }

            entry.IsExpanded = ResolveExpanded(key, true);
            CommitEntry(key, entry);
            return entry;
        }

        private InspectedEntry BuildMaterialEntry(Material material, GameObject owner, int slot)
        {
            int ownerId = owner != null ? owner.GetInstanceID() : 0;
            int key = MakeEntryKey(material.GetInstanceID(), ownerId);
            InspectedEntry entry = TakeEntry(key, EntryKind.Material, SingleTargetArray(material), owner, slot);

            if (!IsSearching)
            {
                entry.Matches.Clear();
                entry.ScanSignature = null;
                // 셰이더 프로퍼티가 수백 개인 머티리얼(lilToon 등)을 기본으로 펼치면 창이 바로 무거워진다.
                entry.IsExpanded = ResolveExpanded(key, false);
                CommitEntry(key, entry);
                return entry;
            }

            if (!TryCollectMatchedShaderProperties(entry, material))
            {
                DropEntry(key, entry);
                return null;
            }

            entry.IsExpanded = ResolveExpanded(key, true);
            CommitEntry(key, entry);
            return entry;
        }

        // ── 항목 재사용 ───────────────────────────────────────────────
        private static UnityEngine.Object[] SingleTargetArray(UnityEngine.Object target)
        {
            return new[] { target };
        }

        private static int MakeEntryKey(int targetInstanceId, int ownerInstanceId)
        {
            unchecked
            {
                return targetInstanceId * 397 ^ ownerInstanceId;
            }
        }

        private static int MakeTargetsKey(UnityEngine.Object[] targets, int ownerInstanceId)
        {
            unchecked
            {
                int hash = ownerInstanceId;
                for (int i = 0; i < targets.Length; i++)
                    hash = hash * 397 ^ targets[i].GetInstanceID();

                return hash;
            }
        }

        private static int MakeOwnersKey(List<GameObject> owners)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < owners.Count; i++)
                    hash = hash * 397 ^ owners[i].GetInstanceID();

                return hash;
            }
        }

        private InspectedEntry TakeEntry(int key, EntryKind kind, UnityEngine.Object[] targets, GameObject owner,
            int slot)
        {
            if (_reusableEntries.TryGetValue(key, out InspectedEntry cached) &&
                cached.Kind == kind && AreSameTargets(cached.Targets, targets))
            {
                cached.Owner = owner;
                cached.MaterialSlot = slot;
                return cached;
            }

            return new InspectedEntry
            {
                Kind = kind,
                Targets = targets,
                Owner = owner,
                Key = key,
                MaterialSlot = slot,
            };
        }

        private static bool AreSameTargets(UnityEngine.Object[] left, UnityEngine.Object[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }

        private void CommitEntry(int key, InspectedEntry entry)
        {
            entry.Key = key;
            _reusableEntries.Remove(key);
            _entryByKey[key] = entry;
        }

        /// <summary>이번 수집에서 버려진 항목을 정리한다. 재사용 목록에 남아 있으면 마지막 일괄 정리가 처리한다.</summary>
        private void DropEntry(int key, InspectedEntry entry)
        {
            if (!_reusableEntries.ContainsKey(key))
                DisposeEntry(entry);
        }

        private static void DisposeEntry(InspectedEntry entry)
        {
            entry.SerializedObject?.Dispose();
            entry.SerializedObject = null;

            // Editor는 UnityEngine.Object라서 명시적으로 파괴하지 않으면 누수된다.
            if (entry.Editor != null)
                UnityEngine.Object.DestroyImmediate(entry.Editor);
            entry.Editor = null;

            if (entry.MaterialEditor != null)
                UnityEngine.Object.DestroyImmediate(entry.MaterialEditor);
            entry.MaterialEditor = null;
        }

        private void DisposeAllEntries()
        {
            foreach (KeyValuePair<int, InspectedEntry> pair in _entryByKey)
                DisposeEntry(pair.Value);
            _entryByKey.Clear();

            foreach (KeyValuePair<int, InspectedEntry> pair in _reusableEntries)
                DisposeEntry(pair.Value);
            _reusableEntries.Clear();

            _groups.Clear();
        }

        private bool ResolveExpanded(int key, bool defaultExpanded)
        {
            return _expandedStateByKey.TryGetValue(key, out bool isExpanded) ? isExpanded : defaultExpanded;
        }

        private bool ResolveGroupExpanded(int key, bool isRoot)
        {
            if (_groupExpandedById.TryGetValue(key, out bool isExpanded))
                return isExpanded;

            // 검색 결과로 남은 그룹은 볼 이유가 있으니 펼치고, 전체 보기의 자식은 접어 둔다.
            return IsSearching || isRoot;
        }

        private static string BuildRelativePath(GameObject owner, GameObject root)
        {
            if (owner == root)
                return "";

            string path = owner.name;
            Transform cursor = owner.transform.parent;
            while (cursor != null && cursor.gameObject != root)
            {
                path = cursor.name + "/" + path;
                cursor = cursor.parent;
            }

            return path;
        }

        // ── 검색 매칭 ─────────────────────────────────────────────────
        private bool TryCollectMatchedPaths(InspectedEntry entry)
        {
            if (entry.SerializedObject == null || entry.SerializedObject.targetObject == null)
            {
                entry.SerializedObject?.Dispose();
                entry.SerializedObject = new SerializedObject(entry.Targets);
                entry.ScanSignature = null;
            }

            // 이름만 보는 검색은 값이 바뀌어도 결과가 그대로다. 값 검색일 때만 매번 다시 훑는다.
            if (entry.ScanSignature == _scanSignature && !_isSearchingValues)
            {
                _matchedFieldCount += entry.Matches.Count;
                return entry.Matches.Count > 0;
            }

            entry.SerializedObject.Update();
            entry.Matches.Clear();
            _matchedScoreByPath.Clear();

            SerializedProperty iterator = entry.SerializedObject.GetIterator();

            // NextVisible은 접힌 폴드아웃 내부를 건너뛰므로, 숨은 필드까지 찾으려면 Next로 전체를 훑어야 한다.
            while (iterator.Next(true))
            {
                if (++_scannedPropertyCount > PropertyScanBudget)
                {
                    _isBudgetExceeded = true;
                    break;
                }

                if (iterator.propertyPath == ScriptPropertyPath)
                    continue;

                int score = ScoreMatch(iterator.displayName, iterator.name,
                    _isSearchingValues ? PropertyValueToString(iterator) : null);
                if (score > 0)
                    _matchedScoreByPath[iterator.propertyPath] = score;
            }

            // 부모가 이미 걸렸으면 자식은 부모 렌더링에 포함되므로 따로 그리지 않는다.
            foreach (KeyValuePair<string, int> pair in _matchedScoreByPath)
            {
                if (!HasMatchedAncestor(pair.Key))
                    entry.Matches.Add(new MatchedField(pair.Key, pair.Value, CountDepth(pair.Key)));
            }

            entry.Matches.Sort(CompareMatches);
            entry.ScanSignature = _scanSignature;
            _matchedFieldCount += entry.Matches.Count;
            return entry.Matches.Count > 0;
        }

        /// <summary>머티리얼은 직렬화 필드가 아니라 셰이더 프로퍼티 단위로 저작하므로 셰이더 정의를 훑는다.</summary>
        private bool TryCollectMatchedShaderProperties(InspectedEntry entry, Material material)
        {
            Shader shader = material.shader;
            if (shader == null)
                return false;

            if (entry.ScanSignature == _scanSignature && !_isSearchingValues)
            {
                _matchedFieldCount += entry.Matches.Count;
                return entry.Matches.Count > 0;
            }

            entry.Matches.Clear();

            int propertyCount = shader.GetPropertyCount();
            for (int i = 0; i < propertyCount; i++)
            {
                if (++_scannedPropertyCount > PropertyScanBudget)
                {
                    _isBudgetExceeded = true;
                    break;
                }

                if ((shader.GetPropertyFlags(i) & ShaderPropertyFlags.HideInInspector) != 0)
                    continue;

                string name = shader.GetPropertyName(i);
                string description = shader.GetPropertyDescription(i);
                string value = _isSearchingValues ? ShaderValueToString(material, shader, i, name) : null;

                int score = ScoreMatch(description, name, value);
                if (score > 0)
                    entry.Matches.Add(new MatchedField(name, score, 0));
            }

            entry.Matches.Sort(CompareMatches);
            entry.ScanSignature = _scanSignature;
            _matchedFieldCount += entry.Matches.Count;
            return entry.Matches.Count > 0;
        }

        /// <summary>점수가 높은 순, 같으면 얕은 필드 순으로 세운다. 알파벳 정렬은 마지막 동점 처리에만 쓴다.</summary>
        private static int CompareMatches(MatchedField left, MatchedField right)
        {
            if (left.Score != right.Score)
                return right.Score.CompareTo(left.Score);

            if (left.Depth != right.Depth)
                return left.Depth.CompareTo(right.Depth);

            return string.CompareOrdinal(left.Path, right.Path);
        }

        private bool HasMatchedAncestor(string path)
        {
            int cut = path.LastIndexOf('.');
            while (cut > 0)
            {
                string parent = path.Substring(0, cut);
                if (_matchedScoreByPath.ContainsKey(parent))
                    return true;

                cut = parent.LastIndexOf('.');
            }

            return false;
        }

        private static int CountDepth(string path)
        {
            int depth = 0;
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i] == '.')
                    depth++;
            }

            return depth;
        }

        /// <summary>모든 토큰이 걸려야 매치로 인정하고, 토큰별 최고 점수를 합쳐 관련도로 쓴다.</summary>
        private int ScoreMatch(string displayName, string name, string value)
        {
            int total = 0;

            for (int i = 0; i < _queryTokens.Length; i++)
            {
                string token = _queryTokens[i];
                int best = Mathf.Max(ScoreName(displayName, token), ScoreName(name, token));

                if (best == 0 && value != null && Contains(value, token))
                    best = ValueMatchScore;

                if (best == 0)
                    return 0;

                total += best;
            }

            return total;
        }

        private static int ScoreName(string source, string token)
        {
            if (string.IsNullOrEmpty(source))
                return 0;

            int index = source.IndexOf(token, System.StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return 0;

            if (source.Length == token.Length)
                return ExactMatchScore;

            if (index == 0)
                return PrefixMatchScore;

            return IsWordStart(source, index) ? WordMatchScore : ContainsMatchScore;
        }

        /// <summary>'_baseDamage'의 Damage처럼 단어 첫머리에 걸린 매치를 중간에 낀 매치보다 위로 올린다.</summary>
        private static bool IsWordStart(string source, int index)
        {
            char previous = source[index - 1];
            if (previous == '_' || previous == ' ' || previous == '.')
                return true;

            return char.IsUpper(source[index]) && !char.IsUpper(previous);
        }

        private static bool Contains(string source, string token)
        {
            return !string.IsNullOrEmpty(source) &&
                   source.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string[] BuildQueryTokens(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return System.Array.Empty<string>();

            return query.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>질의와 옵션이 같으면 이전 스캔 결과를 그대로 쓴다는 판정에 쓰는 서명.</summary>
        private string BuildScanSignature()
        {
            return $"{_query}|{_isSearchingValues}|{_isIncludingMaterials}";
        }

        private static string ShaderValueToString(Material material, Shader shader, int index, string name)
        {
            switch (shader.GetPropertyType(index))
            {
                case ShaderPropertyType.Color:
                    return material.GetColor(name).ToString();
                case ShaderPropertyType.Vector:
                    return material.GetVector(name).ToString();
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    return material.GetFloat(name).ToString("R");
                case ShaderPropertyType.Int:
                    return material.GetInteger(name).ToString();
                case ShaderPropertyType.Texture:
                    Texture texture = material.GetTexture(name);
                    return texture != null ? texture.name : "None";
                default:
                    return null;
            }
        }

        private static string PropertyValueToString(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.ArraySize:
                    return property.intValue.ToString();
                case SerializedPropertyType.Boolean:
                    return property.boolValue.ToString();
                case SerializedPropertyType.Float:
                    return property.doubleValue.ToString("R");
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Color:
                    return property.colorValue.ToString();
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null ? property.objectReferenceValue.name : "None";
                case SerializedPropertyType.Enum:
                    return EnumValueToString(property);
                case SerializedPropertyType.Vector2:
                    return property.vector2Value.ToString();
                case SerializedPropertyType.Vector3:
                    return property.vector3Value.ToString();
                case SerializedPropertyType.Vector4:
                    return property.vector4Value.ToString();
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue.eulerAngles.ToString();
                case SerializedPropertyType.Rect:
                    return property.rectValue.ToString();
                case SerializedPropertyType.Bounds:
                    return property.boundsValue.ToString();
                case SerializedPropertyType.ManagedReference:
                    return property.managedReferenceFullTypename;
                default:
                    return null;
            }
        }

        /// <summary>enumValueIndex는 다중 값이나 유실된 항목에서 범위를 벗어날 수 있어 방어한다.</summary>
        private static string EnumValueToString(SerializedProperty property)
        {
            string[] names = property.enumDisplayNames;
            int index = property.enumValueIndex;
            return index >= 0 && index < names.Length ? names[index] : property.intValue.ToString();
        }
    }
}
