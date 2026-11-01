var oldPaths=new[]{"/DreamscapeEastLands/","/Toon Golden Valley/","/Fantastic Dungeon Pack/","/LowPolyFantasyArena2/","/Idyllic Fantasy Nature/"};
var scenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt).Where(s=>s.path.Contains("/Scenario/OpenWorld/")).ToArray();
var renderers=scenes.SelectMany(s=>s.GetRootGameObjects()).SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>!(r is SkinnedMeshRenderer)&&r.sharedMaterials.Any(m=>m!=null&&oldPaths.Any(p=>AssetDatabase.GetAssetPath(m).Contains(p)))).ToArray();
var glow=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ExternalAssets/FX/Trails VFX URP/VFX/Textures/T_VFX_Glow_01.tga");
var flame=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ExternalAssets/FX/FreeQuickEffectsVol1/Textures/Flame02.png");
var smoke=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ExternalAssets/Character/Enemy Skeleton Warrior/Textures/T_Smoke_2_D.tga");
if(glow==null||flame==null||smoke==null)throw new Exception("대체 FX 텍스처 누락");
var materials=new Dictionary<Material,Material>();
foreach(var renderer in renderers){
 var slots=renderer.sharedMaterials;
 for(int i=0;i<slots.Length;i++){
  var source=slots[i];if(source==null||!oldPaths.Any(p=>AssetDatabase.GetAssetPath(source).Contains(p)))continue;
  Material material;if(!materials.TryGetValue(source,out material)){
   material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));material.name="WorldFX_"+materials.Count;
   var label=source.name.ToLowerInvariant();var texture=label.Contains("fire")?flame:label.Contains("steam")||label.Contains("fog")?smoke:glow;
   var color=source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.HasProperty("_TintColor")?source.GetColor("_TintColor"):source.HasProperty("_Color")?source.GetColor("_Color"):Color.white;
   material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",color);material.SetFloat("_Surface",1);material.SetFloat("_ZWrite",0);
   material.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
   material.SetOverrideTag("RenderType","Transparent");material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");material.renderQueue=3000;
   AssetDatabase.CreateAsset(material,"Assets/10.Datas/World/OpenWorld/ResourceOverhaul/"+material.name+".mat");materials.Add(source,material);
  }slots[i]=material;
 }
 renderer.sharedMaterials=slots;EditorUtility.SetDirty(renderer);
 var particle=renderer as ParticleSystemRenderer;if(particle!=null){particle.renderMode=ParticleSystemRenderMode.Billboard;particle.mesh=null;var system=particle.GetComponent<ParticleSystem>();if(system!=null){var sheet=system.textureSheetAnimation;sheet.enabled=false;EditorUtility.SetDirty(system);}}
 PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
}
foreach(var scene in scenes)UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "환경·상호작용 FX 렌더러 "+renderers.Length+" / 새 FX 재질 "+materials.Count+" / 캐릭터·야생동물 스킨 유지";

