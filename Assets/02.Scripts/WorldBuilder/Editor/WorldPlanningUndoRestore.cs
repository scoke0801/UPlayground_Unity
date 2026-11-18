using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    /// <summary>저작 창을 닫아도 계획 Undo의 적용 스냅샷을 복원한다.</summary>
    [InitializeOnLoad]
    internal static class WorldPlanningUndoRestore
    {
        private static readonly Dictionary<WorldDefinition, string> Applied = new Dictionary<WorldDefinition, string>();
        static WorldPlanningUndoRestore()
        {
            Undo.undoRedoPerformed += Restore;
            EditorApplication.delayCall += () =>
            {
                foreach (var preview in UnityEngine.Object.FindObjectsByType<WorldPlanningPreview>(FindObjectsSortMode.None))
                    if (preview.definition != null) Track(preview.definition);
            };
        }
        internal static void Track(WorldDefinition definition) => Applied[definition] = definition.AppliedTerrainHash;
        private static void Restore()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (var pair in new List<KeyValuePair<WorldDefinition, string>>(Applied))
            {
                var definition = pair.Key;
                if (definition == null) { Applied.Remove(definition); continue; }
                if (definition.AppliedTerrainHash == pair.Value) continue;
                try
                {
                    bool loaded = false;
                    for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                        if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path == AssetDatabase.GUIDToAssetPath(definition.PreviewSceneGuid)) loaded = true;
                    if (!loaded) continue;
                    var adapter = new WorldTerrainStateAdapter(definition.Profile, WorldBuilderAuthoringAssets.FindTerrains(definition));
                    string actual = adapter.Read().ComputeHash();
                    if (actual != definition.AppliedTerrainHash)
                    {
                        if (actual != pair.Value) throw new InvalidOperationException("UncapturedTerrainEdits: 계획 Undo가 외부 지형 편집을 덮어쓸 수 없습니다.");
                        string path = "Library/WorldBuilder/" + definition.WorldGuid + "/Applied/" + definition.AppliedTerrainHash + ".bin";
                        if (!File.Exists(path)) throw new IOException("계획 Undo 스냅샷이 없습니다.");
                        var snapshot = TerrainSnapshotCodec.Decode(File.ReadAllBytes(path));
                        if (snapshot.ComputeHash() != definition.AppliedTerrainHash) throw new IOException("계획 Undo 스냅샷이 손상됐습니다.");
                        adapter.Apply(snapshot);
                        foreach (var terrain in adapter.Terrains) AssetDatabase.SaveAssetIfDirty(terrain.terrainData);
                    }
                    Track(definition); EditorUtility.SetDirty(definition); AssetDatabase.SaveAssetIfDirty(definition);
                }
                catch (Exception exception) { Debug.LogException(exception); }
            }
        }
    }
}
