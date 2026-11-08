import fs from 'node:fs';
import crypto from 'node:crypto';
const output='output/WorldThemeProduction/RedTreeForest';
fs.mkdirSync(output,{recursive:true});
const disk=JSON.parse(fs.readFileSync('output/WorldThemeProduction/A12/disk-baseline-environment.json','utf8'));
const live=JSON.parse(fs.readFileSync('output/WorldThemeProduction/A00/live-baseline.json','utf8'));
const redRoot='AuthoredEnvironment/월드 구간/붉은나무숲';
const position=r=>{const p={};for(const m of r.transformOverrides??[]){const k=m.property.match(/^m_LocalPosition\.([xyz])$/);if(k)p[k[1]]=Number(m.value);}return p;};
const rows=disk.environmentInstances.filter(r=>r.parentPath?.startsWith(redRoot)).map(r=>({instanceFileId:r.instanceFileId,parentPath:r.parentPath,name:r.overrideName,source:r.sourcePrefabPath,guid:r.sourcePrefabGuid,serializedPositionOverride:position(r),evidenceSource:'A12 disk instance snapshot'}));
const allInstances=disk.environmentInstances;
const rootsAndHeroes=allInstances.filter(r=>/HeroTree|GroundLeaves_HeroTree|Roots_01|Cave/.test(r.sourcePrefabPath??'')).map(r=>({name:r.overrideName,parent:r.parentPath,source:r.sourcePrefabPath,position:position(r)}));
const typeCounts={};for(const r of disk.environmentInstances.filter(r=>r.parentPath?.startsWith(redRoot))){const k=r.sourcePrefabPath?.split('/').pop()??r.overrideName;typeCounts[k]=(typeCounts[k]??0)+1;}
const candidates=[
 'Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/BroadleafTree_01_Red.prefab',
 'Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/BroadleafTree_02_Red.prefab',
 'Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/BroadleafTree_03_Red.prefab',
 'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Foliage/Trees/PF_EastLands_HeroTree.prefab',
 'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Foliage/PF_EastLands_GroundLeaves_HeroTree_01.prefab',
 'Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Scenes/Terrain/TerrainLayers/TerrainLayer_ForestGround.terrainlayer'
].map(path=>({path,exists:fs.existsSync(path),guid:fs.existsSync(path+'.meta')?fs.readFileSync(path+'.meta','utf8').match(/^guid: (\w+)/m)?.[1]:null,sha256:fs.existsSync(path)?crypto.createHash('sha256').update(fs.readFileSync(path)).digest('hex'):null,status:'파일 조사만 수행 — 실제 렌더/URP/충돌/LOD/성능 미검증'}));
const proposedCore=[[1110,520],[1180,520],[1180,585],[1110,585]];
const nearbyIds=live.ids.filter(e=>e.position[0]>1075&&e.position[0]<1215&&e.position[2]>485&&e.position[2]<620);
const data={status:'A04 사전조사. Editor 미호출·mask 미확정·제작 미실행',redRoot,rootCounts:{diskPrefabInstances:rows.length,liveDirectChildren:[{path:redRoot+'/기존 지형과 소품',children:491},{path:redRoot+'/식생',children:838}]},serializedBounds:{x:[Math.min(...rows.map(r=>r.serializedPositionOverride.x).filter(Number.isFinite)),Math.max(...rows.map(r=>r.serializedPositionOverride.x).filter(Number.isFinite))],z:[Math.min(...rows.map(r=>r.serializedPositionOverride.z).filter(Number.isFinite)),Math.max(...rows.map(r=>r.serializedPositionOverride.z).filter(Number.isFinite))]},typeCounts,rootsAndHeroes,candidates,proposedCore,coreStatus:'실제 디스크 배치에서 고른 후보일 뿐 편집 권한 아님. A00 mask/경로·서사뿌리/VFX live 확인 필요',nearbyProtectedIds:nearbyIds,limits:['serial position override는 identity 상위 그룹에서 월드와 일치하는 것으로 보이나 prefab source offset/실제 bounds는 live 검증 전이다.','기존 HeroTree는 호수와 사당의 생명의 호수 상징목이며 붉은숲에 새 거목을 임의 복제하지 않는다.','뿌리·동굴·서사 VFX는 이름만으로 소유권을 정하지 않고 live 객체/참조 조사 필요']};
fs.writeFileSync(`${output}/survey.json`,JSON.stringify(data,null,2));
fs.writeFileSync(`${output}/disk-redforest-instances.json`,JSON.stringify(rows,null,2));
console.log(JSON.stringify({status:data.status,instances:rows.length,bounds:data.serializedBounds,topTypes:Object.entries(typeCounts).sort((a,b)=>b[1]-a[1]).slice(0,10),landmarks:rootsAndHeroes,proposedCore,nearbyIds:nearbyIds.length,candidateFiles:candidates.filter(c=>c.exists).length},null,2));
