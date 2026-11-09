import fs from 'node:fs';
let s=fs.readFileSync('Tools/WorldThemes/ShadeCreekR2/capture.csx','utf8').replaceAll('ShadeCreekR2','ShadeCreekR3');
s=s.replace('var rows=new List<string>();',`var rows=new List<string>();
var capturePipeline=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/LakeVillage_RPAsset.asset"));
capturePipeline.hideFlags=HideFlags.HideAndDontSave;
var pipelineSerialized=new SerializedObject(capturePipeline);var drawer=pipelineSerialized.FindProperty("m_GPUResidentDrawerMode");if(drawer!=null){drawer.intValue=0;pipelineSerialized.ApplyModifiedPropertiesWithoutUndo();}
var rendererMaterials=new Dictionary<Renderer,Material[]>();var materialCopies=new Dictionary<Material,Material>();
var lodStates=new Dictionary<LODGroup,bool>();
foreach(var group in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LODGroup>(true))){lodStates[group]=group.animateCrossFading;group.animateCrossFading=false;}
foreach(var renderer in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true))){
var originals=renderer.sharedMaterials;rendererMaterials[renderer]=originals;
renderer.sharedMaterials=originals.Select(m=>{if(m==null)return m;if(materialCopies.ContainsKey(m))return materialCopies[m];var copy=new Material(m);copy.hideFlags=HideFlags.HideAndDontSave;copy.DisableKeyword("_USE_WPO");for(int j=0;j<copy.shader.GetPropertyCount();j++){string property=copy.shader.GetPropertyName(j);var type=copy.shader.GetPropertyType(j);if(property.ToLowerInvariant().Contains("wind")&&(type==UnityEngine.Rendering.ShaderPropertyType.Float||type==UnityEngine.Rendering.ShaderPropertyType.Range))copy.SetFloat(property,0);}materialCopies[m]=copy;return copy;}).ToArray();
}
rows.Add("CloudClock=0; water speed=0; cloned renderer materials: WPO disabled, wind float properties=0; animated LOD crossfade disabled. Shader internal time, terrain detail wind and static dither not fully controlled. Leaf/shadow pixel differences are not art evidence.");`);
s=s.replace('QualitySettings.renderPipeline=Resources.FindObjectsOfTypeAll<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>().Single(v=>v.name=="ShadeCreekCaptureRP");','QualitySettings.renderPipeline=capturePipeline;');
s=s.replace('}finally{if(creekRenderer!=null)', '}finally{foreach(var pair in rendererMaterials)if(pair.Key!=null)pair.Key.sharedMaterials=pair.Value;foreach(var pair in materialCopies)UnityEngine.Object.DestroyImmediate(pair.Value);foreach(var pair in lodStates)if(pair.Key!=null)pair.Key.animateCrossFading=pair.Value;if(creekRenderer!=null)');
s=s.replace('QualitySettings.renderPipeline=oldPipeline;','QualitySettings.renderPipeline=oldPipeline;UnityEngine.Object.DestroyImmediate(capturePipeline);');
fs.writeFileSync('Tools/WorldThemes/ShadeCreekR3/capture.csx',s);
fs.writeFileSync('Tools/WorldThemes/ShadeCreekR3/stage.txt','before');
