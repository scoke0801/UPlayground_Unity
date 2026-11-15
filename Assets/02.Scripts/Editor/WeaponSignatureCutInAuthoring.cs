using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Party;

namespace UPlayGround.EditorTools
{
    /// <summary>PNG·GUID를 보존하면서 정규 12명의 기존 얼굴 패널 이미지를 연결한다.</summary>
    public static class WeaponSignatureCutInAuthoring
    {
        private static readonly string[] Characters =
        { "Raon", "Arin", "Hwarin", "MyoRyeong", "Reine", "Lian", "SeolA", "Sera", "Lili", "YeonHoa", "Myomyo", "Yura" };

        public static string PrepareSprites()
        {
            CheckEditor();
            var report = new List<string>();
            foreach (string character in Characters)
            {
                string path = "Assets/04.Images/SkillCutIn/CutIn_" + character + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("컷인 PNG 누락: " + path);
                string guid = AssetDatabase.AssetPathToGUID(path), hash = Hash(path);
                if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                }
                if (guid != AssetDatabase.AssetPathToGUID(path) || hash != Hash(path))
                    throw new InvalidOperationException("컷인 원본·GUID 보존 실패: " + path);
                report.Add(character + " | " + guid + " | " + hash);
            }
            Directory.CreateDirectory("docs/work-status/weapon-signature");
            File.WriteAllLines("docs/work-status/weapon-signature/CUTIN_IMPORT.txt", report);
            return "컷인 Sprite 12종: PNG·GUID 보존 확인";
        }

        public static string Connect()
        {
            CheckEditor();
            var definitions = new List<PlayerCharacterDefinitionSO>();
            var sprites = new List<Sprite>();
            foreach (string character in Characters)
            {
                var definition = AssetDatabase.LoadAssetAtPath<PlayerCharacterDefinitionSO>("Assets/10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_" + character + ".asset");
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/04.Images/SkillCutIn/CutIn_" + character + ".png");
                if (definition == null || sprite == null) throw new InvalidOperationException("정의·Sprite 로드 실패: " + character);
                definitions.Add(definition); sprites.Add(sprite);
            }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            try
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    Undo.RegisterCompleteObjectUndo(definitions[i], "시그니처 얼굴 연결");
                    definitions[i].ultimateFaceSprite = sprites[i];
                    EditorUtility.SetDirty(definitions[i]);
                }
                foreach (var definition in definitions) AssetDatabase.SaveAssetIfDirty(definition);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (var definition in definitions) AssetDatabase.SaveAssetIfDirty(definition);
                throw;
            }
            return "정규 12명 얼굴 Sprite 연결 완료";
        }

        private static string Hash(string path)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
        }

        private static void CheckEditor()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("정상 컴파일된 편집 모드가 필요합니다.");
        }
    }
}
