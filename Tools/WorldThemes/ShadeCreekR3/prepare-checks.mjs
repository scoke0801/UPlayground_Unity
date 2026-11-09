import fs from 'node:fs';
for(const name of ['refresh-contract-baseline','validate-progress','check-walking-support','check-enemies','check-enemy-paths','bake-final']){
 let s=fs.readFileSync(`Tools/WorldThemes/ShadeCreekR2/${name}.csx`,'utf8').replaceAll('ShadeCreekR2','ShadeCreekR3');
 if(name==='check-walking-support')s=s.replace('ThemeStudies/ShadeCreekR1/Terrain.asset','ThemeStudies/ShadeCreekR2/Terrain.asset').replace('R1 대비 R2','R2 대비 R3');
 if(name==='bake-final')s=s.replaceAll('NavigationR2Final','NavigationR3Final');
 if(name==='refresh-contract-baseline')s=s.replace('return rows.Length;',`System.IO.File.WriteAllLines("output/ShadeCreekR3/ids-current-canonical.tsv",all.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).Select(e=>GlobalObjectId.GetGlobalObjectIdSlow(e).targetObjectId.ToString()+"\\t"+e.Guid));return rows.Length;`);
 fs.writeFileSync(`Tools/WorldThemes/ShadeCreekR3/${name}.csx`,s);
}
