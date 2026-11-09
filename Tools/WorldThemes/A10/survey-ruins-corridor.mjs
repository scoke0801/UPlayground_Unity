import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const output='output/WorldThemeProduction/RuinsCorridor';fs.mkdirSync(output,{recursive:true});
const load=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const live=load('output/WorldThemeProduction/A00/live-baseline.json'),disk=load('output/WorldThemeProduction/A12/disk-baseline-environment.json');
const recipe=load('Tools/OpenWorld/landmark-sites-plan.json');
const hostText=fs.readFileSync(live.scene,'utf8'),heads=[...hostText.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\r?$/gm)];
const transformDocs=new Map(heads.flatMap((h,index)=>h[1]==='4'?[[h[2],{stripped:!!h[3],text:hostText.slice(h.index,heads[index+1]?.index??hostText.length)}]]:[]));
const scalar=(s,k)=>s.match(new RegExp(`^  ${k}: ?(.*)$`,'m'))?.[1]?.trim();
const vector=s=>Object.fromEntries([...s.matchAll(/([xyzw]): ([\d.eE+-]+)/g)].map(m=>[m[1],Number(m[2])]));
const cross=(a,b)=>[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]];
const nativeWorld=(local,parentId)=>{let p=local,t=transformDocs.get(parentId),complete=!!t&&['x','y','z'].every(k=>Number.isFinite(local[k])),chain=[];for(let i=0;i<40&&t;i++){if(t.stripped){complete=false;break;}const pos=vector(scalar(t.text,'m_LocalPosition')??''),q=vector(scalar(t.text,'m_LocalRotation')??''),s=vector(scalar(t.text,'m_LocalScale')??'');const v=[p.x*(s.x??1),p.y*(s.y??1),p.z*(s.z??1)],u=[q.x??0,q.y??0,q.z??0],twice=cross(u,v).map(x=>x*2),c=cross(u,twice);p=Object.fromEntries(['x','y','z'].map((k,j)=>[k,v[j]+(q.w??1)*twice[j]+c[j]+(pos[k]??0)]));chain.push({position:pos,rotation:q,scale:s});const parent=scalar(t.text,'m_Father')?.match(/fileID: (-?\d+)/)?.[1];if(parent&&parent!=='0'&&!transformDocs.has(parent))complete=false;t=transformDocs.get(parent);}return {position:complete?[p.x,p.y,p.z]:null,complete,parentChain:chain};};
const facilities=[
 {key:'OldCorridor',name:'옛 회랑',root:'AuthoredEnvironment/월드 구간/던전/옛 회랑',terrainOwner:'A00 확정. 남서 회랑의 주변 지형 소유와 분리'},
 {key:'OldCitadel',name:'북부 옛 성채',root:'AuthoredEnvironment/랜드마크 유적 구역/북부 옛 성채',terrainOwner:'A07',recipeSite:'north'},
 {key:'Sarcophagus',name:'석관 묘역과 무너진 묘실',root:'AuthoredEnvironment/던전 공간 재저작/석관 묘역과 무너진 묘실',terrainOwner:'A05/A00 확정',recipeSite:'northeast'},
 {key:'ForestFort',name:'숲 보루의 전실과 병영 폐허',root:'AuthoredEnvironment/던전 공간 재저작/숲 보루의 전실과 병영 폐허',terrainOwner:'A05/A00 확정',recipeSite:'east'},
 {key:'Waystation',name:'버려진 역참의 숙영 회랑',root:'AuthoredEnvironment/던전 공간 재저작/버려진 역참의 숙영 회랑',terrainOwner:'A00 확정',recipeSite:'southeast'},
 {key:'Monastery',name:'남서부 무너진 수도원',root:'AuthoredEnvironment/랜드마크 유적 구역/남서부 무너진 수도원',terrainOwner:'A00 확정',recipeSite:'southwest'}
];
const localPose=r=>{const pose={};for(const m of r.transformOverrides??[]){const axis=m.property.match(/^m_LocalPosition\.([xyz])$/)?.[1];if(axis)pose[axis]=Number(m.value);}return pose;};
const summarizeBounds=rows=>{if(rows.length===0)return null;const axis=i=>rows.map(r=>r.position[i]).filter(Number.isFinite);return {min:[0,1,2].map(i=>Math.min(...axis(i))),max:[0,1,2].map(i=>Math.max(...axis(i)))};};
for(const site of facilities){
 site.liveBranches=live.branches.filter(r=>r.path===site.root||r.path.startsWith(site.root+'/'));
 site.liveChildren=site.liveBranches.filter(r=>r.path!==site.root);
 site.pointBounds=summarizeBounds(site.liveChildren);
 site.environmentInstances=disk.environmentInstances.filter(r=>r.parentPath===site.root||r.parentPath.startsWith(site.root+'/')).map(r=>({...r,serializedLocalPosition:localPose(r)}));
 site.resolvedPrefabRootPoints=site.environmentInstances.map(r=>({instanceFileId:r.instanceFileId,name:r.overrideName,parentPath:r.parentPath,prefab:r.sourcePrefabPath,...nativeWorld(r.serializedLocalPosition,r.parentTransform)}));
 site.prefabRootPointBounds=summarizeBounds(site.resolvedPrefabRootPoints.filter(r=>r.complete));
 const types={};for(const r of site.environmentInstances)types[r.sourcePrefabPath]=(types[r.sourcePrefabPath]??0)+1;site.topTypes=Object.entries(types).sort((a,b)=>b[1]-a[1]);
 site.recipeReference=recipe.sites.find(r=>r.id===site.recipeSite)??null;
 const b=site.pointBounds??site.prefabRootPointBounds;
 site.protectedEntities=b?live.ids.filter(r=>r.position[0]>=b.min[0]-25&&r.position[0]<=b.max[0]+25&&r.position[2]>=b.min[2]-25&&r.position[2]<=b.max[2]+25):[];
 site.scopeStatus='계층/현재 native 월드 point 범위는 조사 참고. Renderer/Collider footprint·core 권한 아님';
}
const followup=live.branches.filter(r=>/^(AuthoredEnvironment\/(환경 후속 개선|사면과 거점 후속 저작))\//.test(r.path)&&/묘역|보루|역참|성채|수도원|회랑/.test(r.path));
const dungeonCandidates=['Dng_Arch','Dng_Stairs','Dng_Brazier','Dng_Bookshelf','Dng_Altar','Dng_Wall_Plain','Dng_Wall_Ruined','Dng_Floor_4x4','Dng_Pillar'].map(n=>'Assets/03.Prefabs/Environment/Dungeon/'+n+'.prefab');
const actualCorridorCandidates=facilities[0].topTypes.slice(0,5).map(r=>r[0]);
const candidates=[...new Set([...dungeonCandidates,...actualCorridorCandidates])].map(path=>{
 const exists=fs.existsSync(path),s=exists?fs.readFileSync(path,'utf8'):'';
 return {path,exists,guid:exists?fs.readFileSync(path+'.meta','utf8').match(/^guid: (\w+)/m)?.[1]:null,sha256:exists?hash(path):null,nativeCounts:{meshRenderer:(s.match(/^--- !u!23 /gm)??[]).length,skinnedMeshRenderer:(s.match(/^--- !u!137 /gm)??[]).length,meshCollider:(s.match(/^--- !u!64 /gm)??[]).length,boxCollider:(s.match(/^--- !u!65 /gm)??[]).length,lodGroup:(s.match(/^--- !u!205 /gm)??[]).length,monoBehaviour:(s.match(/^--- !u!114 /gm)??[]).length,light:(s.match(/^--- !u!108 /gm)??[]).length},meshReferences:[...new Set([...s.matchAll(/m_Mesh: \{fileID: (-?\d+), guid: (\w+)/g)].map(m=>m[2]+':'+m[1]))],status:'파일/직렬화 조사. nested model/prefab 재귀 로드·실제 외형/URP/충돌/LOD/성능 미검증'};
});
const preservationFiles=['Tools/OpenWorld/README.md','output/OpenWorld/EnvironmentForm/placements-after.txt','output/OpenWorld/EnvironmentForm/RESULT.md','output/OpenWorld/EnvironmentPolish/RESULT.md','Assets/03.Prefabs/Environment/Dungeon/Dng_Wall_Plain.prefab'];
const data={status:'A10 사전 조사. Unity 호출/Assets 생성/독립 제작 없음',sourceScene:live.scene,sourceSceneHash:hash(live.scene),sourceTerrainHash:hash('Assets/10.Datas/World/OpenWorld/Terrain.asset'),facilities,followup,candidates,preservationFiles:preservationFiles.map(path=>({path,exists:fs.existsSync(path),sha256:fs.existsSync(path)?hash(path):null})),excluded:[{scope:'A09 Stonegate 본성·성문 접합',reason:'기존 교정 완료. 재수정 제외'},{scope:'폐채석장 지형·작업 회랑',reason:'A06 조사와 중복 방지. 건축 도움 필요 시 A00 정확 객체 지정 후 협의'}],scatterMode:live.terrain.scatter,detailPrototypeCount:live.terrain.details.length,limits:['주변 Terrain은 테마 담당 소유. A10 height/alpha/detail 편집 금지','시설별 독립 하위 씬·데이터를 사용하고 하나의 포괄 core로 지역 전체를 소유하지 않는다','native point bounds와 old recipe는 edit mask·충돌/접지 통과 근거가 아니다','기존 UV 파생 터널/벽 기단 공면 교정/유적37개 정렬·거점 후속16개와 생활소품12개 재배치를 보존한다']};
assert.equal(data.scatterMode,'InstanceCountMode');assert.equal(data.detailPrototypeCount,6);assert.ok(facilities.every(s=>s.liveBranches.length>0));
fs.writeFileSync(output+'/survey.json',JSON.stringify(data,null,2));
console.log(JSON.stringify({facilities:facilities.map(s=>({key:s.key,instances:s.environmentInstances.length,nativeBranches:s.liveBranches.length,pointBounds:s.pointBounds??s.prefabRootPointBounds,protectedCount:s.protectedEntities.length,topTypes:s.topTypes.slice(0,3)})),followupCount:followup.length,candidates:candidates.map(c=>({path:c.path,exists:c.exists,nativeCounts:c.nativeCounts}))},null,2));
