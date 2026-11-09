import fs from 'node:fs';
for(const kind of ['distance','forced']){
let s=fs.readFileSync(`Tools/WorldThemes/ShadeCreekR2/probe-final-${kind}.csx`,'utf8').replaceAll('ShadeCreekR2','ShadeCreekR3');
s=s.replace('q.transform.position.x>1600&&q.transform.position.x<1650&&q.transform.position.z>770&&q.transform.position.z<830','q.gameObject.activeInHierarchy&&q.transform.position.x>1600&&q.transform.position.x<1650&&q.transform.position.z>770&&q.transform.position.z<830&&q.GetLODs().Length==4');
s=s.replace('sample.gameObject};var names=new[]{"original","green","review"}','sample.gameObject,sample.gameObject};var names=new[]{"original","green","r2","r3"}');
s=s.replace('if(i==2){','if(i>=2){');
s=s.replace('group.ForceLOD(',`if(i==2){foreach(var renderer in tree.GetComponentsInChildren<Renderer>(true)){renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{if(m==null)return m;string p=AssetDatabase.GetAssetPath(m);return p.Contains("/ShadeCreekR3/")?AssetDatabase.LoadAssetAtPath<Material>(p.Replace("/ShadeCreekR3/","/ShadeCreekR2/")):m;}).ToArray();}}
group.ForceLOD(`);
s=s.replaceAll('expectedLOD=','formulaOnlyLOD=');
s=s.replace('var rows=new List<string>();','var rows=new List<string>();rows.Add("Editor isolated static renders. formulaOnlyLOD is a screen-size estimate, NOT measured active GPU LOD. CPU/GPU performance and TPS unmeasured. FOV60 1280x720, fog disabled, same rotation/scale/light, no postprocessing.");');
fs.writeFileSync(`Tools/WorldThemes/ShadeCreekR3/lod-${kind}.csx`,s);
}
