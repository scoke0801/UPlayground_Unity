using System;
using System.Linq;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public abstract class ActorDatabaseMotionPreviewPopulatorBase :
        IMotionPreviewCatalogPopulator
    {
        protected abstract ActorType Filter { get; }
        protected abstract string FilterLabel { get; }

        public string ButtonLabel =>
            $"ActorDatabase에서 채우기 ({FilterLabel})";

        public void Populate(MotionPreviewCatalogSO catalog)
        {
            if (catalog == null)
                return;

            ActorDatabase database = FindDatabase();
            if (database == null)
            {
                Debug.LogWarning(
                    "[MotionPreview] 프로젝트에서 ActorDatabase를 찾지 못했습니다.");
                return;
            }

            int added = 0;
            foreach (ActorDefinitionSO definition in database.All)
            {
                if (definition == null || definition.prefab == null)
                    continue;
                if (Filter != ActorType.None &&
                    !definition.actorType.HasFlag(Filter))
                    continue;

                string definitionPath = AssetDatabase.GetAssetPath(definition);
                string id = AssetDatabase.AssetPathToGUID(definitionPath);
                if (string.IsNullOrEmpty(id))
                    id = definition.name;
                if (catalog.subjects.Any(entry =>
                        string.Equals(entry.id, id, StringComparison.Ordinal)))
                    continue;

                catalog.subjects.Add(new MotionPreviewCatalogSO.SubjectEntry
                {
                    id = id,
                    displayName = definition.name,
                    source = MotionPreviewCatalogSO.SubjectSource.ScenePrefab,
                    prefab = definition.prefab,
                    spawnOffset = Vector3.zero,
                });
                added++;
            }

            EditorUtility.SetDirty(catalog);
            Debug.Log(
                $"[MotionPreview] ActorDatabase {FilterLabel}: " +
                $"{added}개 추가 (총 {catalog.subjects.Count}개)");
        }

        private static ActorDatabase FindDatabase()
        {
            string path = AssetDatabase.FindAssets("t:ActorDatabase")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(candidate => !string.IsNullOrEmpty(candidate));
            return string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<ActorDatabase>(path);
        }
    }

    public sealed class AllActorMotionPreviewPopulator :
        ActorDatabaseMotionPreviewPopulatorBase
    {
        protected override ActorType Filter => ActorType.None;
        protected override string FilterLabel => "전체";
    }

    public sealed class MonsterMotionPreviewPopulator :
        ActorDatabaseMotionPreviewPopulatorBase
    {
        protected override ActorType Filter => ActorType.Monster;
        protected override string FilterLabel => "Monster";
    }

    public sealed class PlayerMotionPreviewPopulator :
        ActorDatabaseMotionPreviewPopulatorBase
    {
        protected override ActorType Filter => ActorType.Player;
        protected override string FilterLabel => "Player";
    }

    public sealed class NpcMotionPreviewPopulator :
        ActorDatabaseMotionPreviewPopulatorBase
    {
        protected override ActorType Filter => ActorType.NPC;
        protected override string FilterLabel => "NPC";
    }
}
