using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Codex;
using UPlayGround.Data.Editor.Codex;

namespace UPlayGround.Editor.P09Builder
{
    public static partial class P09ProgressionBuilder
    {
        /// <summary>재실행 안전성·전체 GAS 연결을 확인한 뒤 신규 외형을 촬영한다.</summary>
        public static void VerifyAndCapture()
        {
            Build();
            ValidateAbilities();
            CapturePortraits();
        }

        /// <summary>새 몬스터의 실제 외형을 촬영해 도감에 연결한다. 생성된 초상화는 재사용한다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/캐릭터/P09/상위 장비 몬스터 초상화 촬영", priority = 1102)]
        public static void CapturePortraits()
        {
            var recipe = JsonUtility.FromJson<Recipe>(File.ReadAllText(RecipePath));
            var catalog = new P09AssetCatalog();
            catalog.Refresh();
            var database = Require<UPlayGround.Data.Actor.ActorDatabase>(DatabasePath);
            var codex = Require<MonsterCodexDatabaseSO>(CodexPath);
            Validate();
            foreach (Entry entry in Prepare(recipe, catalog, database, codex))
            {
                string path = entry.Folder + "/Portrait.png";
                var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (portrait == null)
                {
                    Texture2D capture = MonsterCodexThumbnailGenerator.RenderPrefab(database.GetDefinition(entry.Id).prefab);
                    try { File.WriteAllBytes(path, capture.EncodeToPNG()); }
                    finally { UnityEngine.Object.DestroyImmediate(capture); }
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                    portrait = Require<Sprite>(path);
                }
                MonsterCodexEntrySO codexEntry = codex.GetEntry(entry.Id);
                codexEntry.portrait = portrait;
                Save(codexEntry);
            }
            Debug.Log("[P09Progression] 신규 몬스터 초상화 연결 완료");
        }
    }
}
