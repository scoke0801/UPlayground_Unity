import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

const output='output/WorldThemeProduction/ShadeCreek';
fs.mkdirSync(output,{recursive:true});
const load=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const disk=load('output/WorldThemeProduction/A12/disk-baseline-environment.json');
const live=load('output/WorldThemeProduction/A00/live-baseline.json');
const root='AuthoredEnvironment/월드 구간/그늘숲';
const position=r=>{const p={};for(const m of r.transformOverrides??[]){const match=m.property.match(/^m_LocalPosition\.([xyz])$/);if(match)p[match[1]]=Number(m.value);}return p;};
const rows=disk.environmentInstances.filter(r=>r.parentPath?.startsWith(root)).map(r=>({instanceFileId:r.instanceFileId,parentPath:r.parentPath,name:r.overrideName,source:r.sourcePrefabPath,guid:r.sourcePrefabGuid,positionOverride:position(r)}));
const types={};for(const r of rows){const key=r.source??r.name;types[key]=(types[key]??0)+1;}
const candidatePaths=[
 'Assets/03.Prefabs/Environment/RedTreeCreek/CreekArt.prefab',
 'Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/BroadleafTree_01_Green.prefab',
 'Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/BroadleafTree_03_Green.prefab',
 'Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/Bush_01_01.prefab',
 'Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/Reeds_01.prefab',
 'Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/Reeds_02.prefab',
 'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Foliage/PF_Farm_ForestSmallGrass.prefab',
 'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Foliage/Trees/Osmanthus/PF_EastLands_Osmanthus_Bush_01.prefab',
 'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Stones/RiverRocks/PF_EastLands_RiverRock_01.prefab',
 'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Water/EastLands_SmallRiver.prefab',
 'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Scenes/Terrain/TerrainLayers/TerrainLayer_ForestGround.terrainlayer',
 'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Scenes/Terrain/TerrainLayers/TerrainLayer_RiverBed.terrainlayer'
];
const candidates=candidatePaths.map(path=>({path,exists:fs.existsSync(path),guid:fs.existsSync(path+'.meta')?fs.readFileSync(path+'.meta','utf8').match(/^guid: (\w+)/m)?.[1]:null,sha256:fs.existsSync(path)?crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex'):null,status:'파일 조사만 수행. 외형·URP·LOD·충돌·성능은 미검증'}));
const spatialReference=live.branches.filter(r=>r.position[0]>=1500&&r.position[0]<=1870&&r.position[2]>=510&&r.position[2]<=1100);
const streamReferences=live.branches.filter(r=>/그늘숲|목재|수로|물길|Creek|forest-water/i.test(r.path));
const nearIds=live.ids.filter(r=>r.position[0]>=1500&&r.position[0]<=1870&&r.position[2]>=510&&r.position[2]<=1100);
const finite=axis=>rows.map(r=>r.positionOverride[axis]).filter(Number.isFinite);
const data={status:'A05 파일 사전조사. Editor 미호출·mask 미확정·제작 미실행',root,scene:live.scene,sourceBaseline:'output/WorldThemeProduction/A00/live-baseline.json',diskSceneHash:disk.scene.sha256,instanceCount:rows.length,serializedOverrideBounds:{x:[Math.min(...finite('x')),Math.max(...finite('x'))],z:[Math.min(...finite('z')),Math.max(...finite('z'))]},topTypes:Object.entries(types).sort((a,b)=>b[1]-a[1]),candidates,streamReferences,spatialReference,protectedEntities:nearIds,limits:['instance override 좌표는 상위 변환·prefab offset·실제 renderer bounds를 Unity에서 대조하기 전 월드 편집권이 아니다.','문서의 X1500..1870/Z510..1100는 현황 탐색 참고일 뿐 core가 아니다.','CreekArt와 서사 수위·후퇴 MPB/vertex color 연결은 별도 조사 후 보존한다.','보호 개체 ID/좌표/접지와 모든 기존 물/다리/길/목재 상호작용을 보존한다.']};
assert.equal(new Set(candidatePaths).size,candidatePaths.length);
assert.equal(live.terrain.scatter,'InstanceCountMode');
assert.equal(live.terrain.details.length,6);
fs.writeFileSync(output+'/survey.json',JSON.stringify(data,null,2));
fs.writeFileSync(output+'/disk-shadecreek-instances.json',JSON.stringify(rows,null,2));
console.log(JSON.stringify({instances:rows.length,bounds:data.serializedOverrideBounds,topTypes:data.topTypes.slice(0,10),streamReferenceCount:streamReferences.length,spatialReferenceCount:spatialReference.length,protectedCount:nearIds.length,candidateExisting:candidates.filter(c=>c.exists).length},null,2));
