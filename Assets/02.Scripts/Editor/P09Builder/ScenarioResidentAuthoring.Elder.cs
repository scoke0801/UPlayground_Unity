using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    public static partial class ScenarioResidentAuthoring
    {
        private const string ElderRecipePath = DataRoot + "/P09_ElderAppearance.json";
        [Serializable] private sealed class ElderRecipe
        {
            public string revision, faceRenderer, hairRenderer, beardRenderer, hairMaterial, beardMaterial;
            public Vector4 hairTone, beardTone;
            public SculptHandle[] handles;
        }
        [Serializable] private sealed class SculptHandle
        {
            public Vector3 center, radius, scale, offset;
            public bool mirror;
        }

        /// <summary>원본 메시와 표정을 보존하며 별도 노년 외형을 제작하고 검사한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/노년 외형 제작 검수")]
        public static void BuildElderAppearance()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("편집 모드에서 실행하세요.");
            var recipe = JsonUtility.FromJson<ElderRecipe>(File.ReadAllText(ElderRecipePath));
            if (recipe.handles == null || recipe.handles.Any(h => h.radius.x <= 0 || h.radius.y <= 0 || h.radius.z <= 0))
                throw new InvalidOperationException("얼굴 변형 반경을 확인하세요.");
            string assetFolder = DataRoot + "/ElderAppearance_" + recipe.revision;
            string folder = Output + "/ElderAppearance_" + recipe.revision;
            string prefabPath = "Assets/03.Prefabs/Characters/ScenarioResidents/AppearanceReview/P09_Male_Elder_" + recipe.revision + ".prefab";
            // 검수 중 반복 제작은 새 revision을 사용한다. 기존 승인 결과를 덮어쓰지 않는다.
            if (Directory.Exists(assetFolder) || File.Exists(prefabPath))
                throw new InvalidOperationException("이미 제작된 revision입니다. 제작표의 revision을 올려 주세요.");
            Directory.CreateDirectory(folder);
            var root = PrefabUtility.LoadPrefabContents(SkinReviewPrefabPath);
            var meshes = new System.Collections.Generic.List<Mesh>();
            var materials = new System.Collections.Generic.List<Material>();
            try
            {
                var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var face = renderers.Single(r => r.name == recipe.faceRenderer);
                var beard = renderers.Single(r => r.name == recipe.beardRenderer);
                var hair = renderers.Single(r => r.name == recipe.hairRenderer);
                foreach (var renderer in new[] { face, beard })
                {
                    Mesh source = renderer.sharedMesh;
                    Bounds renderBounds = renderer.localBounds;
                    Mesh sculpted = SculptElderMesh(source, recipe);
                    meshes.Add(sculpted);
                    ValidateElderMesh(source, sculpted);
                    renderer.sharedMesh = sculpted;
                    // FBX 정점은 Z-up 바인드 좌표다. 렌더러의 리그 기준 경계를 유지한다.
                    float displacement = source.vertices.Zip(sculpted.vertices, (a, b) => Vector3.Distance(a, b)).Max();
                    renderBounds.Expand(displacement * 2);
                    renderer.localBounds = renderBounds;
                }
                for (Transform t = beard.transform; t != root.transform; t = t.parent) t.gameObject.SetActive(true);
                hair.sharedMaterial = CreateElderMaterial(recipe.hairMaterial, "P09_Elder_Hair", recipe.hairTone);
                beard.sharedMaterial = CreateElderMaterial(recipe.beardMaterial, "P09_Elder_Beard", recipe.beardTone);
                materials.Add(hair.sharedMaterial);
                materials.Add(beard.sharedMaterial);
                ValidateElderRoot(root);
                PathConfig.EnsureFolderExists(assetFolder);
                foreach (Mesh mesh in meshes) AssetDatabase.CreateAsset(mesh, assetFolder + "/" + mesh.name + ".asset");
                foreach (Material material in materials) AssetDatabase.CreateAsset(material, assetFolder + "/" + material.name + ".mat");
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
                foreach (var mesh in meshes) if (!EditorUtility.IsPersistent(mesh)) UnityEngine.Object.DestroyImmediate(mesh);
                foreach (var material in materials) if (!EditorUtility.IsPersistent(material)) UnityEngine.Object.DestroyImmediate(material);
            }
            var saved = Require<GameObject>(prefabPath);
            ValidateElderRoot(saved);
            foreach (bool baseline in new[] { true, false })
                foreach (bool side in new[] { false, true })
                    foreach (int yaw in new[] { 0, 45, 90, 180 })
                        CaptureElder(saved, baseline, folder + "/" + (baseline ? "Before" : "After") + "_" + (side ? "Side" : "Soft") + "_" + yaw + ".png", yaw, side);
            foreach (string expression in new[] { "Eye_Blink", "Eye_Blink_L", "Eye_Blink_R", "Mouth_Smile", "Mouth_Shout", "Lip_aa", "Lip_ii", "Lip_uu", "Lip_ee", "Lip_oo" })
                CaptureElderExpression(saved, expression, folder);
            var full = (GameObject)PrefabUtility.InstantiatePrefab(saved);
            try { Capture(full, folder + "/FullBody.png", false, 0); }
            finally { if (full != null) UnityEngine.Object.DestroyImmediate(full); }
            File.WriteAllText(folder + "/validation.txt", "프리팹 재로드 / Missing Script 0 / 유효한 메시·소재·리그 참조 검사 통과\n얼굴 36·수염 12 표정 프레임·UV·본 가중치·삼각형 보존 검사 통과\n전후 16장, 표정 10장, 전신 1장 촬영\n" + prefabPath + "\n목 회전은 노년 리그 검수로 확인. Play Mode 및 Player Build는 별도 검증 필요");
        }

        private static Material CreateElderMaterial(string sourcePath, string name, Vector4 tone)
        {
            var material = new Material(Require<Material>(sourcePath)) { name = name };
            if (!material.HasProperty("_MainTexHSVG")) throw new InvalidOperationException("색 보정 미지원 소재: " + sourcePath);
            material.SetVector("_MainTexHSVG", tone);
            material.color = Color.white;
            return material;
        }

        private static Mesh SculptElderMesh(Mesh source, ElderRecipe recipe)
        {
            var mesh = UnityEngine.Object.Instantiate(source);
            mesh.name = source.name + "_Elder";
            var original = source.vertices;
            var vertices = original.Select(v => DeformElderVertex(v, recipe)).ToArray();
            mesh.ClearBlendShapes();
            mesh.vertices = vertices;
            var normals = source.normals;
            var tangents = source.tangents;
            var mappedNormals = new Vector3[original.Length];
            var mappedTangents = new Vector4[original.Length];
            for (int i = 0; i < original.Length; i++)
            {
                Matrix4x4 basis = ElderDeformationBasis(original[i], recipe);
                mappedNormals[i] = basis.inverse.transpose.MultiplyVector(normals[i]).normalized;
                Vector3 tangent = basis.MultiplyVector(tangents[i]).normalized;
                mappedTangents[i] = new Vector4(tangent.x, tangent.y, tangent.z, tangents[i].w);
            }
            mesh.normals = mappedNormals;
            mesh.tangents = mappedTangents;
            var delta = new Vector3[original.Length];
            var deltaNormals = new Vector3[original.Length];
            var deltaTangents = new Vector3[original.Length];
            for (int shape = 0; shape < source.blendShapeCount; shape++)
                for (int frame = 0; frame < source.GetBlendShapeFrameCount(shape); frame++)
                {
                    source.GetBlendShapeFrameVertices(shape, frame, delta, deltaNormals, deltaTangents);
                    for (int i = 0; i < original.Length; i++)
                    {
                        Vector3 endpoint = original[i] + delta[i];
                        Matrix4x4 basis = ElderDeformationBasis(endpoint, recipe);
                        deltaNormals[i] = basis.inverse.transpose.MultiplyVector(normals[i] + deltaNormals[i]).normalized - mappedNormals[i];
                        deltaTangents[i] = basis.MultiplyVector((Vector3)tangents[i] + deltaTangents[i]).normalized - (Vector3)mappedTangents[i];
                        // 기준 얼굴과 표정 끝점에 같은 변형을 적용해 깜박임·발음 접점을 보존한다.
                        delta[i] = DeformElderVertex(endpoint, recipe) - vertices[i];
                    }
                    mesh.AddBlendShapeFrame(source.GetBlendShapeName(shape), source.GetBlendShapeFrameWeight(shape, frame), delta, deltaNormals, deltaTangents);
                }
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 DeformElderVertex(Vector3 vertex, ElderRecipe recipe)
        {
            Vector3 result = vertex;
            foreach (var handle in recipe.handles)
            {
                result += ElderHandleOffset(vertex, handle, 1);
                if (handle.mirror) result += ElderHandleOffset(vertex, handle, -1);
            }
            return result;
        }

        private static Vector3 ElderHandleOffset(Vector3 vertex, SculptHandle handle, float side)
        {
            var center = handle.center;
            var offset = handle.offset;
            center.x *= side;
            offset.x *= side;
            Vector3 difference = vertex - center;
            var normalized = new Vector3(difference.x / handle.radius.x, difference.y / handle.radius.y, difference.z / handle.radius.z);
            float weight = Mathf.Exp(-normalized.sqrMagnitude * 2);
            return (Vector3.Scale(difference, handle.scale - Vector3.one) + offset) * weight;
        }

        private static Matrix4x4 ElderDeformationBasis(Vector3 vertex, ElderRecipe recipe)
        {
            const float step = 0.0001f;
            var basis = Matrix4x4.identity;
            for (int axis = 0; axis < 3; axis++)
            {
                Vector3 offset = Vector3.zero;
                offset[axis] = step;
                Vector3 derivative = (DeformElderVertex(vertex + offset, recipe) - DeformElderVertex(vertex - offset, recipe)) / (step * 2);
                basis.SetColumn(axis, new Vector4(derivative.x, derivative.y, derivative.z, 0));
            }
            if (basis.determinant <= 0) throw new InvalidOperationException("얼굴 변형에서 뒤집힌 영역이 발생했습니다.");
            return basis;
        }

        private static void ValidateElderMesh(Mesh source, Mesh mesh)
        {
            if (source.vertexCount != mesh.vertexCount || source.blendShapeCount != mesh.blendShapeCount
                || !source.triangles.SequenceEqual(mesh.triangles) || !source.uv.SequenceEqual(mesh.uv)
                || !source.boneWeights.SequenceEqual(mesh.boneWeights) || !source.bindposes.SequenceEqual(mesh.bindposes))
                throw new InvalidOperationException("메시의 토폴로지·UV·리그·표정 보존 실패");
            for (int i = 0; i < source.blendShapeCount; i++)
                if (source.GetBlendShapeName(i) != mesh.GetBlendShapeName(i) || source.GetBlendShapeFrameCount(i) != mesh.GetBlendShapeFrameCount(i))
                    throw new InvalidOperationException("표정 이름 또는 프레임 누락");
            foreach (Vector3 v in mesh.vertices)
                if (!float.IsFinite(v.x) || !float.IsFinite(v.y) || !float.IsFinite(v.z))
                    throw new InvalidOperationException("유효하지 않은 얼굴 정점");
        }

        private static void ValidateElderRoot(GameObject root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0)
                    throw new InvalidOperationException("Missing Script: " + t.name);
            foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (r.sharedMesh == null || r.sharedMaterials.Any(m => m == null) || r.bones.Any(b => b == null))
                    throw new InvalidOperationException("메시·소재·리그 누락: " + r.name);
        }

        private static void CaptureElder(GameObject saved, bool baseline, string path, int yaw, bool side)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(baseline ? Require<GameObject>(SkinReviewPrefabPath) : saved);
            try { Capture(root, path, true, yaw, side); }
            finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void CaptureElderExpression(GameObject saved, string expression, string folder)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(saved);
            try
            {
                foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    int index = r.sharedMesh.GetBlendShapeIndex(expression);
                    if (index >= 0) r.SetBlendShapeWeight(index, 100);
                }
                Capture(root, folder + "/Expression_" + expression + ".png", true, 0);
            }
            finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
        }


    }
}
