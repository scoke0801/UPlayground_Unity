using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.World;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class AmbientFaunaAuthoring
    {
        /// <summary>Blender에서 만든 전용 곤충 메시를 정규화하고 종별 프로필을 연결한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/종별 외형 연결")]
        public static void CompleteSpeciesAssets()
        {
            string[] names = { "Butterfly", "Gnat", "Firefly", "AirMote" };
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                var profile = AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(DataRoot + "/" + name + ".asset");
                Undo.RecordObject(profile, "환경 생물 종별 설정");
                profile.Behavior = (AmbientFaunaBehavior)i;
                profile.Priority = i == 0 ? 30 : i == 3 ? 5 : 20;
                profile.HoverFraction = i == 0 ? 0.1f : i == 1 ? 0.65f : 0;
                profile.WanderFrequency = i == 1 ? 6 : i == 2 ? 0.22f : 0.5f;
                profile.WanderRadius = i == 1 ? 0.22f : i == 2 ? 0.45f : 0.58f;
                profile.RecoverySeconds = i == 1 ? 0.15f : i == 2 ? 1.2f : 0.8f;
                if (i == 1 || i == 2)
                {
                    profile.Mesh = DeriveInsect(name);
                    profile.SizeRange = i == 1 ? new Vector2(0.012f, 0.02f) : new Vector2(0.025f, 0.035f);
                    profile.SpeedRange = i == 1 ? new Vector2(0.06f, 0.16f) : new Vector2(0.04f, 0.10f);
                    profile.Acceleration = i == 1 ? 3 : 0.8f;
                    profile.FleeSpeed = i == 1 ? 1.4f : 0.6f;
                    profile.AvoidRadius = i == 1 ? 1.4f : 2;
                    profile.FlapAngle = i == 1 ? 28 : 32;
                    profile.FlapFrequency = i == 1 ? 35 : 18;
                    profile.WindInfluence = i == 1 ? 0.03f : 0.05f;
                    profile.Material.SetFloat("_GlowFrequency", 0.48f);
                    profile.Material.SetFloat("_Emission", i == 2 ? 2 : 0);
                    EditorUtility.SetDirty(profile.Material);
                }
                if (i == 3)
                {
                    profile.WindInfluence = 0.65f;
                    profile.ParticleLifetime = 10;
                    profile.FlapFrequency = profile.FlapAngle = 0;
                }
                if (i == 1) profile.HourDensity = new AnimationCurve(new Keyframe(0, 0.1f), new Keyframe(6, 0.2f),
                    new Keyframe(9, 0.7f), new Keyframe(17, 1), new Keyframe(21, 0.3f), new Keyframe(24, 0.1f));
                if (!profile.TryValidate(out string error)) throw new InvalidOperationException(error);
                EditorUtility.SetDirty(profile);
            }
            string qualityPath = DataRoot + "/Quality.asset";
            if (AssetDatabase.LoadAssetAtPath<AmbientFaunaQualitySO>(qualityPath) == null)
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<AmbientFaunaQualitySO>(), qualityPath);
            AssetDatabase.SaveAssets();
        }

        private static Mesh DeriveInsect(string name)
        {
            string sourcePath = ArtRoot + "/" + name + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(sourcePath);
            importer.generateSecondaryUV = false;
            importer.isReadable = true;
            importer.SaveAndReimport();
            var filter = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath).GetComponentInChildren<MeshFilter>();
            Mesh source = filter.sharedMesh;
            if (source.uv2.Length != source.vertexCount || source.colors.Length != source.vertexCount)
                throw new InvalidOperationException("Blender UV/색상 채널이 누락됐습니다: " + name);
            var vertices = source.vertices;
            var normals = source.normals;
            var wings = source.uv2;
            var hinges = source.uv3;
            Matrix4x4 matrix = filter.transform.localToWorldMatrix;
            for (int i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
            float width = vertices.Max(v => v.x) - vertices.Min(v => v.x);
            if (width <= 0) throw new InvalidOperationException("정규화할 폭이 없습니다.");
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] /= width;
                normals[i] = matrix.MultiplyVector(normals[i]).normalized;
                if (wings[i].y <= 0) continue;
                wings[i].x = Mathf.Sign(vertices[i].x);
                hinges[i] = new Vector2(wings[i].x * 0.07f, 0.05f);
            }
            var derived = new Mesh { name = name + "Gpu", vertices = vertices, normals = normals, uv = source.uv,
                uv2 = wings, uv3 = hinges, colors = source.colors, triangles = source.triangles };
            derived.RecalculateBounds();
            string path = ArtRoot + "/" + name + "Gpu.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(derived, path); return derived; }
            EditorUtility.CopySerialized(derived, existing);
            UnityEngine.Object.DestroyImmediate(derived);
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}
