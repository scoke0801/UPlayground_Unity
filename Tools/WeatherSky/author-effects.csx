// execute_code로 실행한다. 기존 프리팹은 백업하고 누락된 날씨 자식만 추가한다.
if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
    throw new System.InvalidOperationException("컴파일이 끝난 Edit Mode에서 실행해야 합니다.");
string root = "Assets/10.Datas/World/Sky/";
string backup = "output/WeatherSky/backup";
System.IO.Directory.CreateDirectory(backup);
var snowShader = Shader.Find("UPlayGround/Environment/Weather Particle");
var boltShader = Shader.Find("UPlayGround/Environment/Lightning Bolt");
if (snowShader == null || boltShader == null || UnityEditor.ShaderUtil.ShaderHasError(snowShader) || UnityEditor.ShaderUtil.ShaderHasError(boltShader))
    throw new System.InvalidOperationException("날씨 셰이더를 먼저 복구해야 합니다.");
var snowMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(root + "Weather_Snow.mat");
if (snowMaterial == null)
{
    snowMaterial = new Material(snowShader);
    snowMaterial.SetTexture("_BaseMap", UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ExternalAssets/FX/Hovl Studio/HSFiles/Textures/Snow4.png"));
    snowMaterial.SetColor("_BaseColor", new Color(0.88f, 0.93f, 1, 0.6f));
    snowMaterial.SetFloat("_LuminanceAlpha", 1);
    snowMaterial.SetFloat("_CameraFadeNear", 0.4f);
    snowMaterial.SetFloat("_CameraFadeFar", 1.8f);
    UnityEditor.AssetDatabase.CreateAsset(snowMaterial, root + "Weather_Snow.mat");
}
var boltMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(root + "Weather_Lightning.mat");
if (boltMaterial == null)
{
    boltMaterial = new Material(boltShader);
    UnityEditor.AssetDatabase.CreateAsset(boltMaterial, root + "Weather_Lightning.mat");
}
var thunder = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/06.Sounds/Environment/Weather_Thunder_Rumble.wav");
if (thunder == null) throw new System.InvalidOperationException("천둥 음원이 누락되었습니다.");
var reports = new System.Collections.Generic.List<string>();
foreach (string name in new[] { "EnvironmentEffects", "LakeOfLifeEnvironment", "DroughtVillageEnvironment" })
{
    string path = "Assets/03.Prefabs/LivingWorld/Environment/" + name + ".prefab";
    string saved = backup + "/" + name + ".prefab";
    if (!System.IO.File.Exists(saved)) System.IO.File.Copy(path, saved);
    var contents = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    try
    {
        var effects = contents.GetComponentInChildren<UPlayGround.Gameplay.World.LivingWorld.EnvironmentEffects>(true);
        if (effects == null) throw new System.InvalidOperationException(path + " 환경 효과 누락");
        var data = new UnityEditor.SerializedObject(effects);
        if (data.FindProperty("_snow").objectReferenceValue == null)
        {
            var go = new GameObject("Snow");
            go.transform.SetParent(effects.transform, false);
            var snow = go.AddComponent<ParticleSystem>();
            snow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = snow.main;
            main.playOnAwake = false;
            main.loop = true;
            main.maxParticles = 1600;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6, 9);
            main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = snow.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30, 3, 30);
            var emission = snow.emission;
            emission.rateOverTime = 0;
            var velocity = snow.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = 0; velocity.y = -1.5f; velocity.z = 0;
            var noise = snow.noise;
            noise.enabled = true;
            noise.strength = 0.45f;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.15f;
            noise.quality = ParticleSystemNoiseQuality.Medium;
            var fade = snow.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.12f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) });
            fade.color = gradient;
            var renderer = snow.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = snowMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            data.FindProperty("_snow").objectReferenceValue = snow;
        }
        var snowSystem = (ParticleSystem)data.FindProperty("_snow").objectReferenceValue;
        var sheet = snowSystem.textureSheetAnimation;
        sheet.enabled = true;
        sheet.numTilesX = 2;
        sheet.numTilesY = 2;
        sheet.startFrame = new ParticleSystem.MinMaxCurve(0, 1);
        sheet.frameOverTime = 0;
        foreach (string field in new[] { "_lightningBolt", "_lightningBranch" })
        {
            if (data.FindProperty(field).objectReferenceValue != null) continue;
            var go = new GameObject(field == "_lightningBolt" ? "LightningBolt" : "LightningBranch");
            go.transform.SetParent(effects.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = boltMaterial;
            line.useWorldSpace = true;
            line.widthMultiplier = field == "_lightningBolt" ? 1.1f : 0.45f;
            line.widthCurve = AnimationCurve.Linear(0, 1, 1, 0.1f);
            line.positionCount = 0;
            line.numCornerVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            data.FindProperty(field).objectReferenceValue = line;
        }
        if (data.FindProperty("_lightningLight").objectReferenceValue == null)
        {
            var go = new GameObject("LightningFill");
            go.transform.SetParent(effects.transform, false);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.63f, 0.75f, 1);
            light.range = 140;
            light.shadows = LightShadows.None;
            light.intensity = 0;
            light.enabled = false;
            data.FindProperty("_lightningLight").objectReferenceValue = light;
        }
        if (data.FindProperty("_thunderAudio").objectReferenceValue == null)
        {
            var go = new GameObject("ThunderAudio");
            go.transform.SetParent(effects.transform, false);
            var audio = go.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.loop = false;
            audio.spatialBlend = 0;
            audio.volume = 1;
            data.FindProperty("_thunderAudio").objectReferenceValue = audio;
        }
        var clips = data.FindProperty("_thunderClips");
        if (clips.arraySize == 0) { clips.arraySize = 1; clips.GetArrayElementAtIndex(0).objectReferenceValue = thunder; }
        data.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.PrefabUtility.SaveAsPrefabAsset(contents, path);
        reports.Add(path);
    }
    finally { UnityEditor.PrefabUtility.UnloadPrefabContents(contents); }
}
return reports;
