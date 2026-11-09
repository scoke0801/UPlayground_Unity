import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const out='output/HarborReedR1',own='Assets/10.Datas/World/ThemeStudies/HarborReedR1';
const load=p=>JSON.parse(fs.readFileSync(p,'utf8').replace(/^\uFEFF/,''));
const save=(n,o)=>fs.writeFileSync(`${out}/${n}.json`,JSON.stringify(o,null,2));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const list=d=>fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?list(`${d}/${e.name}`):[`${d}/${e.name}`]);
const guidPaths=new Map();
for(const p of list('Assets').filter(p=>p.endsWith('.meta'))){const match=fs.readFileSync(p,'utf8').match(/^guid: (\w+)/m);if(match)guidPaths.set(match[1],p.slice(0,-5));}
const resources=list(own), dependencies=[];
for(const path of resources.filter(p=>/\.(mat|terrainlayer|shader|prefab)$/.test(p))){const ids=[...new Set([...fs.readFileSync(path,'utf8').matchAll(/guid: (\w+)/g)].map(m=>m[1]))];dependencies.push({path,guid:fs.readFileSync(path+'.meta','utf8').match(/^guid: (\w+)/m)?.[1],references:ids.map(guid=>{const dependency=guidPaths.get(guid);return {guid,path:dependency??null,mode:dependency?.startsWith(own)?'C-owned':'shared-readonly',sha256:dependency&&fs.existsSync(dependency)&&fs.statSync(dependency).isFile()?sha(dependency):null};})});}
const refs=load(`${out}/references-final.json`);refs.newResourceDependencies=dependencies;save('references-final',refs);
const state=load(`${out}/editor-final-state.json`);state.postReloadPathCheckUtc=fs.statSync(out+'/paths-after.json').mtime.toISOString();state.lockReleased=true;state.scope='C 작업권 반환 직전 상태. 이후 A/B Editor 상태를 뜻하지 않음.';save('editor-final-state',state);
const validation=load(`${out}/validation.json`);validation.referenceAudit=refs;validation.navmesh.completeBefore=validation.navmesh.before.filter(p=>p.complete).length;validation.navmesh.completeAfter=validation.navmesh.after.filter(p=>p.complete).length;save('validation',validation);
const manifest=load(`${out}/merge-manifest.json`);manifest.resourceDependencies='references-final.json';manifest.terrainSerialization='terrain-delta.json은 저장·재로드 후 읽은 실제 값. 기존20layer를 GUID로 매칭하고 신규2layer를 연결한다.';manifest.sceneOwnerPolicy='검토 씬 GUID로 변한 owner는 통합 대상에서 제외한다. 기존323 콘텐츠 GUID는 보존됐다.';save('merge-manifest',manifest);
const contracts=load(`${out}/content-contracts.json`);save('connected-enemies-and-routes',{region:'Assets/01.Scenes/Scenario/OpenWorld/Regions/SaltwindHarborRegion.unity',hostPlacement:validation.enemies,regionActors:validation.regionActors,routeEvidence:'paths-before.json / paths-after.json',flightScope:'실제 연결 WorldPlacementData에서 C 조사 영역 actorId 레코드0. 비행 적 검사는 이 영역에서 해당없음; 월드 전체 적 부재를 의미하지 않음.',contentContracts:'content-contracts.json',contentContractKeys:Object.keys(contracts)});
const checks={canonicalPreserved:load(`${out}/preservation.json`).sourceFiles.every(r=>r.same),sharedResourcesPreserved:load(`${out}/shared-resource-final.json`).rows.every(r=>r.same),contentIds:refs.ids,idFailures:refs.idFailures.length,sceneSaved:!state.isDirty,temporaryCaptureObjects:state.temporaryCaptureObjects,matchedPoses:fs.readFileSync(`${out}/before/poses.txt`,'utf8')===fs.readFileSync(`${out}/after/poses.txt`,'utf8'),allSixImagesPresent:['before','after'].every(s=>['entry','activity','return'].every(v=>fs.statSync(`${out}/${s}/${v}.png`).size>1000)),heightCells:load(`${out}/terrain-delta.json`).heightChanges.length,alphaCells:load(`${out}/terrain-delta.json`).alphaChanges.length};
assert(checks.canonicalPreserved&&checks.sharedResourcesPreserved&&checks.idFailures===0&&checks.sceneSaved&&checks.allSixImagesPresent);save('delivery-checks',checks);
const paths=[...resources,own+'.meta','Assets/01.Scenes/ArtReview/WorldThemes/HarborReedR1.unity','Assets/01.Scenes/ArtReview/WorldThemes/HarborReedR1.unity.meta',...list('Tools/WorldThemes/HarborReedR1'),...list(out).filter(p=>!p.endsWith('.unity')&&!p.includes('/first-pass/')&&!/\.(zip|sha256)$/.test(p)&&!p.endsWith('/package-files.json'))].filter(p=>fs.existsSync(p));
save('package-files',{purpose:'선택 통합 검토 패키지. 씬·Terrain 전체는 독립 검토용이며 정규 덮어쓰기 금지. 원본 공유 의존성은 같은 기준 프로젝트 필요.',files:[...new Set(paths)].sort().map(path=>({path,bytes:fs.statSync(path).size,sha256:sha(path)}))});
console.log(checks);
