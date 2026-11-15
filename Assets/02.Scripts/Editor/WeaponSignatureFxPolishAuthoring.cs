using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Event;

namespace UPlayGround.Editor
{
    /// <summary>구매 원본을 보존하며 시그니처의 외곽 궤적·파편·투척 형상 표현을 보완한다.</summary>
    public static class WeaponSignatureFxPolishAuthoring
    {
        private const string Root = "Assets/03.Prefabs/Effects/Signature";
        private const string Private = Root + "/Polish";
        private const string Motions = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Signature/";
        private const string GroundHit = "Assets/ExternalAssets/FX/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR2 Ground Hit.prefab";
        private const string Flash = "Assets/ExternalAssets/FX/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 10 blue laser.prefab";
        private const string Holy = "Assets/ExternalAssets/FX/Hovl Studio/Magic circles/Prefabs/Magic shield holy.prefab";
        private const string SourceSword = "Assets/03.Prefabs/Weapon/GreatSword/GreatSword_001.prefab";
        private const string Group = "시그니처 표현 보완";
        private static readonly Dictionary<string, byte[]> Originals = new();
        private static readonly List<string> Receipt = new();

        /// <summary>전용 에셋과 연결 이벤트를 같은 Undo 단위로 갱신하고 실패하면 파일까지 복구한다.</summary>
        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("정상 편집 모드에서만 저작합니다.");
            RequireBone("Hwarin", "Hand.l"); RequireBone("Hwarin", "Hand.r"); RequireBone("Lian", "Line Tip");
            Originals.Clear(); Receipt.Clear();
            Directory.CreateDirectory(Private);
            AssetDatabase.Refresh();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("시그니처 FX 표현 보완");
            try
            {
                Material glow = ParticleMaterial(Flash);
                Material dust = ParticleMaterial(GroundHit);
                Material blade = Source<GameObject>(SourceSword).GetComponentInChildren<Renderer>(true).sharedMaterial;
                Mesh rock = EnsureMesh("Rock", false), shard = EnsureMesh("BladeShard", true);
                BuildGreatSword(glow, blade, shard);
                BuildThrownProps(glow);
                foreach (string weapon in new[] { "Spear", "SwordShield", "GreatSword" })
                {
                    float radius = weapon == "SwordShield" ? 4.5f : weapon == "GreatSword" ? 4f : 3.5f;
                    EditPrefab(Root + "/" + weapon + "_Impact_Signature.prefab", root =>
                        AddImpactLayers(NewGroup(root.transform, weapon != "GreatSword"), radius, dust, blade, glow, rock));
                }
                EditPrefab(Root + "/DualBlade_Signature.prefab", root =>
                {
                    Transform group = NewGroup(root.transform);
                    var ps = Particles(group, "바깥 칼날 파편", glow, 3f, .5f, 0f, new Color(.55f, .8f, 1f, .75f));
                    var main = ps.main; main.startSize = new ParticleSystem.MinMaxCurve(.22f, .4f);
                    main.startRotation3D = true; main.startRotationY = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
                    var emission = ps.emission; emission.rateOverTime = 26;
                    var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle;
                    shape.radius = 2.8f; shape.radiusThickness = .05f; shape.rotation = new Vector3(90, 0, 0);
                    shape.position = new Vector3(0, 1.1f, 0);
                    var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local;
                    velocity.radial = .65f; velocity.orbitalY = 3f; velocity.y = 1.5f;
                    MeshParticles(ps, shard, glow);
                });
                string axeImpact = Private + "/DoubleAxe_Impact.prefab";
                SaveNewPrefab(axeImpact, root =>
                {
                    CopyParticles(Source<GameObject>(GroundHit), root.transform, "구매 바닥 강타", Vector3.one * 2f);
                    AddImpactLayers(root.transform, 3.5f, dust, blade, glow, rock);
                });
                string axeDust = Private + "/DoubleAxe_Dust.prefab";
                SaveNewPrefab(axeDust, root => AddDust(root.transform, dust, 2.5f, 2.5f, true));
                AppendParticle("DoubleAxe", axeImpact, 87f / 30f, 108f / 30f, Vector3.zero, false, true);
                AppendParticle("DoubleAxe", axeDust, .4f, 2.9f, Vector3.zero, true, false);
                string whipRing = Private + "/Whip_Rim.prefab";
                SaveNewPrefab(whipRing, root => AddRing(root.transform, glow, 5.5f, .07f, new Color(.65f, .85f, 1, .55f)));
                var whip = Motion("Whip"); Touch(whip);
                var oldWhipFx = Source<GameObject>(Root + "/Whip_Signature.prefab");
                whip.motionSet.globalEvents.RemoveAll(e => e is BeginParticleEvent p
                    && p.particlePrefab == oldWhipFx);
                AppendParticle("Whip", whipRing, 12f / 30f, 21f / 30f, Vector3.up * .7f, true, false);
                AppendParticle("Whip", whipRing, 27f / 30f, 36f / 30f, Vector3.up * .7f, true, false);
                string whipImpact = Private + "/Whip_Impact.prefab";
                SaveNewPrefab(whipImpact, root =>
                {
                    CopyParticles(Source<GameObject>(GroundHit), root.transform, "전방 강타", Vector3.one * .8f);
                    AddCracks(root.transform, glow, .9f, 5);
                });
                AppendParticle("Whip", whipImpact, 42f / 30f, 57f / 30f, new Vector3(0, 0, 6.5f), false, true);
                string trail = Private + "/Weapon_Trail.prefab";
                SaveNewPrefab(trail, root => AddTrail(root, glow, .25f));
                AppendParticle("DoubleAxe", trail, .4f, 2.9f, Vector3.zero, true, false, "Hand.l");
                AppendParticle("DoubleAxe", trail, .4f, 2.9f, Vector3.zero, true, false, "Hand.r");
                AppendParticle("Whip", trail, .4f, 1.5f, Vector3.zero, true, false, "Line Tip");
                foreach (string weapon in new[] { "GreatSword", "DoubleAxe", "Whip" })
                    AssetDatabase.SaveAssetIfDirty(Motion(weapon));
                Undo.CollapseUndoOperations(undoGroup);
                return string.Join("\n", Receipt.Distinct());
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                foreach (var entry in Originals.Reverse())
                {
                    if (entry.Value == null) AssetDatabase.DeleteAsset(entry.Key);
                    else { File.WriteAllBytes(entry.Key, entry.Value); AssetDatabase.ImportAsset(entry.Key, ImportAssetOptions.ForceUpdate); }
                }
                throw;
            }
            finally { Originals.Clear(); }
        }

        private static void BuildGreatSword(Material glow, Material blade, Mesh shard)
        {
            string materialPath = Private + "/GreatSword_Dissolve.mat";
            Track(materialPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Source<Material>("Assets/11.Material/DissolveMaterial.mat"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            Undo.RecordObject(material, "거대 검 소멸 재질");
            if (!material.HasProperty("_DissolveAmount") || !material.HasProperty("_BaseMap") || !material.HasProperty("_EdgeColor"))
                throw new InvalidOperationException("소멸 셰이더 프로퍼티가 변경됐습니다.");
            material.SetFloat("_DissolveAmount", 0f);
            material.SetFloat("_EdgeWidth", .12f);
            material.SetColor("_EdgeColor", new Color(2.4f, 3.5f, 5f, 1));
            Texture baseTexture = blade.HasProperty("_BaseMap") ? blade.GetTexture("_BaseMap") : null;
            if (baseTexture == null && blade.HasProperty("_MainTex")) baseTexture = blade.GetTexture("_MainTex");
            if (baseTexture == null) throw new InvalidOperationException("원본 대검의 색상 텍스처가 없습니다.");
            material.SetTexture("_BaseMap", baseTexture);
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
            EditPrefab(Root + "/GreatSword_Signature.prefab", root =>
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++) materials[i] = material;
                    renderer.sharedMaterials = materials;
                }
            });
            var motion = Motion("GreatSword"); Touch(motion);
            TimedPropEvent prop = motion.motionSet.globalEvents.OfType<TimedPropEvent>().Single();
            prop.dissolveProperty = "_DissolveAmount";
            float duration = prop.endTime - prop.startTime;
            prop.dissolveAmount = new AnimationCurve(new Keyframe(0, 1), new Keyframe(.14f / duration, 0),
                new Keyframe(1 - .15f / duration, 0), new Keyframe(1, 1));
            string path = Private + "/GreatSword_Disintegration.prefab";
            SaveNewPrefab(path, root =>
            {
                var ps = Particles(root.transform, "외향 검 파편", glow, .1f, .3f, 0, new Color(.5f, .8f, 1, .8f));
                Burst(ps, 32);
                var main = ps.main; main.startSize = new ParticleSystem.MinMaxCurve(.1f, .24f);
                var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(2.5f, 2.5f, 8); shape.position = new Vector3(0, 1.5f, -1.5f);
                var velocity = ps.velocityOverLifetime; velocity.enabled = true;
                velocity.radial = 3; velocity.y = 1.2f;
                MeshParticles(ps, shard, glow);
            });
            AppendParticle("GreatSword", path, 45f / 30f, 55f / 30f, new Vector3(0, 0, 7), false, false);
        }

        private static void BuildThrownProps(Material glow)
        {
            foreach (string weapon in new[] { "Spear", "SwordShield" })
                EditPrefab(Root + "/" + weapon + "_Signature.prefab", root =>
                {
                    Transform group = NewGroup(root.transform);
                    AddTrail(group.gameObject, glow, weapon == "Spear" ? .18f : .35f);
                    if (weapon == "SwordShield")
                        CopyParticles(Source<GameObject>(Holy), group, "성스러운 방패 문양", Vector3.one * .28f);
                });
        }

        private static void AddImpactLayers(Transform parent, float radius, Material dust, Material rockMaterial, Material glow, Mesh rock)
        {
            var ps = Particles(parent, "방사 바위 파편", rockMaterial, .1f, .65f, 0, Color.white);
            Burst(ps, 18);
            var main = ps.main; main.startSize = new ParticleSystem.MinMaxCurve(.13f, .32f);
            main.gravityModifier = 1f; main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * .25f; shape.rotation = new Vector3(90, 0, 0);
            var velocity = ps.velocityOverLifetime; velocity.enabled = true;
            velocity.radial = new ParticleSystem.MinMaxCurve(radius * 2, radius * 3); velocity.y = 3.5f;
            MeshParticles(ps, rock, rockMaterial);
            AddDust(parent, dust, radius, .65f, false);
            AddCracks(parent, glow, radius, 11);
        }

        private static void AddDust(Transform parent, Material material, float radius, float duration, bool continuous)
        {
            var ps = Particles(parent, "낮은 바닥 먼지", material, duration, .55f, 0, new Color(.6f, .53f, .42f, .25f));
            var main = ps.main; main.startSize = new ParticleSystem.MinMaxCurve(.4f, .8f);
            var emission = ps.emission;
            if (continuous) emission.rateOverTime = 18; else Burst(ps, 24);
            var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * .65f; shape.radiusThickness = .2f; shape.rotation = new Vector3(90, 0, 0);
            shape.position = Vector3.up * .08f;
            var velocity = ps.velocityOverLifetime; velocity.enabled = true;
            velocity.radial = continuous ? .6f : radius * .6f; velocity.y = .2f;
        }

        private static ParticleSystem Particles(Transform parent, string name, Material material, float duration, float lifetime, float speed, Color color)
        {
            var child = new GameObject(name); child.transform.SetParent(parent, false);
            var ps = child.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.duration = duration; main.loop = false; main.playOnAwake = true;
            main.startLifetime = lifetime; main.startSpeed = speed; main.startColor = color;
            main.maxParticles = 128; main.stopAction = ParticleSystemStopAction.None;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            var emission = ps.emission; emission.rateOverTime = 0;
            var shape = ps.shape; shape.enabled = false;
            var alpha = ps.colorOverLifetime; alpha.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.6f, .55f), new GradientAlphaKey(0, 1) });
            alpha.color = gradient;
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            return ps;
        }

        private static void Burst(ParticleSystem ps, short count)
        {
            var emission = ps.emission; emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, count) });
        }

        private static void MeshParticles(ParticleSystem ps, Mesh mesh, Material material)
        {
            var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = mesh; renderer.sharedMaterial = material; renderer.alignment = ParticleSystemRenderSpace.Local;
        }

        private static void AddRing(Transform parent, Material material, float radius, float width, Color color)
        {
            var ring = new GameObject("외곽 판정 테두리"); ring.transform.SetParent(parent, false);
            var line = ring.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = true;
            line.positionCount = 96; line.startWidth = line.endWidth = width;
            line.sharedMaterial = material; line.startColor = line.endColor = color;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, .025f, Mathf.Sin(angle) * radius));
            }
        }

        private static void AddCracks(Transform parent, Material material, float radius, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var crack = new GameObject("지면 방사 균열 " + i); crack.transform.SetParent(parent, false);
                var line = crack.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.positionCount = 5;
                line.sharedMaterial = material; line.startWidth = .09f; line.endWidth = .015f;
                line.startColor = line.endColor = new Color(.4f, .65f, 1, .4f);
                float angle = i * Mathf.PI * 2 / count;
                for (int j = 0; j < 5; j++)
                {
                    float bend = angle + (j % 2 == 0 ? .08f : -.08f);
                    float reach = radius * j / 4;
                    line.SetPosition(j, new Vector3(Mathf.Cos(bend) * reach, .03f, Mathf.Sin(bend) * reach));
                }
            }
        }

        private static void AddTrail(GameObject root, Material material, float width)
        {
            var trail = root.AddComponent<TrailRenderer>(); trail.sharedMaterial = material;
            trail.time = .16f; trail.minVertexDistance = .03f; trail.startWidth = width; trail.endWidth = 0;
            trail.startColor = new Color(.55f, .8f, 1, .7f); trail.endColor = new Color(.35f, .6f, 1, 0);
            trail.emitting = true; trail.autodestruct = false; trail.numCornerVertices = 2;
        }

        private static void CopyParticles(GameObject source, Transform parent, string name, Vector3 scale)
        {
            var copy = UnityEngine.Object.Instantiate(source, parent); copy.name = name;
            copy.transform.localPosition = Vector3.zero; copy.transform.localRotation = source.transform.localRotation;
            copy.transform.localScale = scale;
            foreach (var script in copy.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
            foreach (var ps in copy.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main; main.loop = false; main.playOnAwake = true; main.stopAction = ParticleSystemStopAction.None;
            }
        }

        private static Transform NewGroup(Transform parent, bool cancelPrefabRotation = true)
        {
            Transform previous = parent.Find(Group);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var group = new GameObject(Group); group.transform.SetParent(parent, false);
            Vector3 scale = parent.localScale;
            if (Mathf.Abs(scale.x * scale.y * scale.z) < .000001f)
                throw new InvalidOperationException("FX 원본 루트 스케일이 0입니다.");
            group.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
            // 구매 토네이도·착탄 프리팹의 축/스케일을 새 지면 형상에 중복 적용하지 않는다.
            var worldFrame = new GameObject("월드 좌표"); worldFrame.transform.SetParent(group.transform, false);
            worldFrame.transform.localRotation = cancelPrefabRotation ? Quaternion.Inverse(parent.localRotation) : Quaternion.identity;
            return worldFrame.transform;
        }

        private static Mesh EnsureMesh(string name, bool blade)
        {
            string path = Private + "/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
            {
                if (mesh.uv.Length != mesh.vertexCount)
                {
                    Track(path);
                    Undo.RecordObject(mesh, "파편 발광 중심 UV 보완");
                    mesh.uv = Enumerable.Repeat(new Vector2(.5f, .5f), mesh.vertexCount).ToArray();
                    EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh);
                }
                return mesh;
            }
            Track(path);
            mesh = new Mesh { name = "시그니처 " + name };
            mesh.vertices = blade
                ? new[] { new Vector3(-.1f, 0, -.7f), new Vector3(.1f, 0, -.7f), new Vector3(0, 0, .7f), new Vector3(0, .12f, -.4f) }
                : new[] { Vector3.up, Vector3.down * .65f, Vector3.left, Vector3.right, Vector3.forward * .8f, Vector3.back * .7f };
            mesh.triangles = blade ? new[] { 0, 2, 1, 0, 1, 3, 1, 2, 3, 2, 0, 3 }
                : new[] { 0, 2, 4, 0, 4, 3, 0, 3, 5, 0, 5, 2, 1, 4, 2, 1, 3, 4, 1, 5, 3, 1, 2, 5 };
            // 작은 파편은 발광 텍스처의 불투명 중심을 읽어 UV 기본값(0,0)의 투명 소실을 막는다.
            mesh.uv = Enumerable.Repeat(new Vector2(.5f, .5f), mesh.vertexCount).ToArray();
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }

        private static void AppendParticle(string weapon, string path, float start, float end, Vector3 offset, bool attach, bool ground, string bone = null)
        {
            var motion = Motion(weapon); Touch(motion);
            var prefab = Source<GameObject>(path);
            motion.motionSet.globalEvents.RemoveAll(e => e is BeginParticleEvent p && p.particlePrefab == prefab
                && string.Equals(p.spawnPointName ?? string.Empty, bone ?? string.Empty, StringComparison.Ordinal)
                && Mathf.Abs(p.startTime - start) < .001f);
            motion.motionSet.globalEvents.Add(new BeginParticleEvent { particlePrefab = prefab, startTime = start, endTime = end,
                offset = offset, attachToTarget = attach, destroyOnFinish = true, projectToGround = ground,
                spawnPointName = bone, groundLayers = (1 << 0) | (1 << 3), groundProbeDistance = 4f });
            EditorUtility.SetDirty(motion);
        }

        private static void EditPrefab(string path, Action<GameObject> edit)
        {
            Track(path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try { edit(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void SaveNewPrefab(string path, Action<GameObject> build)
        {
            Track(path);
            var root = new GameObject(Path.GetFileNameWithoutExtension(path));
            try { build(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void Touch(UnityEngine.Object asset)
        {
            Track(AssetDatabase.GetAssetPath(asset)); Undo.RegisterCompleteObjectUndo(asset, "시그니처 FX 연결");
        }

        private static void Track(string path)
        {
            if (!path.StartsWith(Root + "/", StringComparison.Ordinal) && !path.StartsWith(Motions, StringComparison.Ordinal))
                throw new InvalidOperationException("전용 저작 범위 밖 경로: " + path);
            if (!Originals.ContainsKey(path)) Originals.Add(path, File.Exists(path) ? File.ReadAllBytes(path) : null);
            Receipt.Add("산출물 | " + path);
        }

        private static MotionSetAsset Motion(string weapon) => Source<MotionSetAsset>(Motions + weapon + "_Signature.asset");

        private static Material ParticleMaterial(string path)
            => Source<GameObject>(path).GetComponentsInChildren<ParticleSystemRenderer>(true).First(r => r.sharedMaterial != null).sharedMaterial;

        private static T Source<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("저작 원본 누락: " + path);
            Receipt.Add("출처 | " + path + " | " + AssetDatabase.AssetPathToGUID(path)); return asset;
        }

        private static void RequireBone(string character, string name)
        {
            string path = "Assets/03.Prefabs/Actor/Player/Models/PlayerModel_" + character + ".prefab";
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null || !model.GetComponentsInChildren<Transform>(true).Any(t => t.name == name))
                throw new InvalidOperationException("FX 부착 본 누락: " + character + "/" + name);
        }
    }
}
