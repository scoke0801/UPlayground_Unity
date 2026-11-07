import fs from 'node:fs';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';

// 디스크 직렬화 기록만 읽는다. 접지/내부 Terrain 텍스처/실제 로드 상태는 Unity에서 별도로 확인한다.
const out = 'output/WorldThemeProduction/A12';
const scenePath = 'Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const idScript = 'a0da5891b60c73f4fa221d55aa60c1f2';
const source = fs.readFileSync(scenePath, 'utf8');
const hash = b => crypto.createHash('sha256').update(b).digest('hex');
const scalar = (s, key) => s.match(new RegExp(`^  ${key}: ?(.*)$`, 'm'))?.[1]?.trim();
const fileId = s => s?.match(/fileID: (-?\d+)/)?.[1];
const guid = s => s?.match(/guid: ([a-f0-9]{32})/)?.[1];
const decode = s => { if (!s) return s; try { return s.startsWith('"') ? JSON.parse(s) : s; } catch { return s; } };
const vector = s => Object.fromEntries([...s?.matchAll(/([xyzw]): ([-+\d.eE]+)/g) ?? []].map(m => [m[1], Number(m[2])]));
function parse(s) {
  const heads = [...s.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\r?$/gm)];
  return new Map(heads.map((h,i)=>[h[2],{id:h[2],type:h[1],stripped:!!h[3],text:s.slice(h.index,heads[i+1]?.index??s.length)}]));
}
const docs = parse(source);
const assetPaths = new Map();
const scanErrors = [];
function walk(dir) {
  let entries; try { entries = fs.readdirSync(dir,{withFileTypes:true}); } catch(e) { scanErrors.push({dir,error:e.code});return; }
  for (const e of entries) {
    const path = `${dir}/${e.name}`;
    if(e.isDirectory()) walk(path);
    else if(e.name.endsWith('.meta')) {
      try {const g=fs.readFileSync(path,'utf8').match(/^guid: ([a-f0-9]{32})/m)?.[1];if(g)assetPaths.set(g,path.slice(0,-5));} catch(e){scanErrors.push({path,error:e.code});}
    }
  }
}
walk('Assets');
const assets = new Map();
function asset(g) {
  if(assets.has(g))return assets.get(g);
  const path=assetPaths.get(g);
  if(!path || !path.endsWith('.prefab'))return null;
  const a={path,docs:parse(fs.readFileSync(path,'utf8'))};assets.set(g,a);return a;
}
const gos = new Map([...docs.values()].filter(d=>d.type==='1').map(d=>[d.id,d]));
const transforms = new Map([...docs.values()].filter(d=>d.type==='4'||d.type==='224').map(d=>[d.id,d]));
const byGo = new Map([...transforms.values()].map(d=>[fileId(scalar(d.text,'m_GameObject')),d]));
const instances = new Map([...docs.values()].filter(d=>d.type==='1001').map(d=>[d.id,d]));
const modsCache = new Map();
function mods(inst) {
  if(modsCache.has(inst.id))return modsCache.get(inst.id);
  const rows=[...inst.text.matchAll(/    - target: \{fileID: (-?\d+), guid: ([a-f0-9]{32}), type: \d+\}\r?\n      propertyPath: (.*)\r?\n      value: ?(.*)\r?\n      objectReference: (.*)/g)].map(m=>({target:m[1],guid:m[2],property:m[3],value:decode(m[4]),reference:m[5]}));
  modsCache.set(inst.id,rows);return rows;
}
const poseFrom = d => ({position:vector(scalar(d.text,'m_LocalPosition')),rotation:vector(scalar(d.text,'m_LocalRotation')),scale:vector(scalar(d.text,'m_LocalScale'))});
function applyMods(pose, rows, tid) {
  for(const r of rows.filter(r=>r.target===tid)) {
    const m=r.property.match(/^m_Local(Position|Rotation|Scale)\.([xyzw])$/);
    if(m)pose[m[1].toLowerCase()][m[2]]=Number(r.value);
  }
  return pose;
}
function rotate(v,q) {
  const u=[q.x??0,q.y??0,q.z??0],w=q.w??1;
  const c=(a,b)=>[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]];
  const uv=c(u,v),uuv=c(u,uv);return v.map((n,i)=>n+2*(w*uv[i]+uuv[i]));
}
function transformPoint(v,p) {
  const r=rotate(v.map((n,i)=>n*(p.scale[['x','y','z'][i]]??1)),p.rotation);
  return r.map((n,i)=>n+(p.position[['x','y','z'][i]]??0));
}
function sourceChain(a,tid,rows) {
  const chain=[];const seen=new Set();let t=a?.docs.get(tid);
  while(t&&!seen.has(t.id)) {
    seen.add(t.id);
    if(t.stripped)return {chain,complete:false,reason:'nested prefab transform'};
    chain.push({sourceTransform:t.id,pose:applyMods(poseFrom(t),rows,t.id)});
    const parent=fileId(scalar(t.text,'m_Father'));if(parent==='0')return {chain,complete:true};t=a.docs.get(parent);
  }
  return {chain,complete:false,reason:'source parent unresolved'};
}
function sceneChain(tid,seen=new Set()) {
  if(!tid||tid==='0')return {chain:[],complete:true};
  if(seen.has(tid))return {chain:[],complete:false,reason:'cycle'};
  seen.add(tid);const t=transforms.get(tid);if(!t)return {chain:[],complete:false,reason:`missing scene transform ${tid}`};
  if(!t.stripped) {
    const parent=sceneChain(fileId(scalar(t.text,'m_Father')),seen);
    return {...parent,chain:[{sceneTransform:tid,pose:poseFrom(t)},...parent.chain]};
  }
  const inst=instances.get(fileId(scalar(t.text,'m_PrefabInstance')));
  if(!inst)return {chain:[],complete:false,reason:'stripped transform instance missing'};
  const a=asset(guid(scalar(t.text,'m_CorrespondingSourceObject')));
  const local=sourceChain(a,fileId(scalar(t.text,'m_CorrespondingSourceObject')),mods(inst));
  const parent=sceneChain(fileId(inst.text.match(/m_TransformParent: (.*)/)?.[1]),seen);
  return {chain:[...local.chain,...parent.chain],complete:local.complete&&parent.complete,reason:local.reason??parent.reason};
}
function objectPose(goId) {
  const go=gos.get(goId);let t=byGo.get(goId);
  if(t) {
    const c=sceneChain(t.id);let p=[0,0,0];for(const item of c.chain)p=transformPoint(p,item.pose);
    return {worldPosition:c.complete?{x:p[0],y:p[1],z:p[2]}:null,serializedTransformChain:c.chain,complete:c.complete,reason:c.reason};
  }
  const inst=instances.get(fileId(scalar(go?.text??'','m_PrefabInstance')));
  if(inst) {
    const ref=scalar(go.text,'m_CorrespondingSourceObject'),a=asset(guid(ref));
    const srcGo=fileId(ref);const st=[...a?.docs.values()??[]].find(d=>(d.type==='4'||d.type==='224')&&fileId(scalar(d.text,'m_GameObject'))===srcGo);
    const local=sourceChain(a,st?.id,mods(inst));const parent=sceneChain(fileId(inst.text.match(/m_TransformParent: (.*)/)?.[1]));
    const chain=[...local.chain,...parent.chain];let p=[0,0,0];for(const item of chain)p=transformPoint(p,item.pose);
    return {worldPosition:local.complete&&parent.complete?{x:p[0],y:p[1],z:p[2]}:null,serializedTransformChain:chain,complete:local.complete&&parent.complete,reason:local.reason??parent.reason};
  }
  return {worldPosition:null,complete:false,reason:'transform not serialized or resolved'};
}
function nameOf(goId) {
  const go=gos.get(goId);if(!go)return null;
  const n=scalar(go.text,'m_Name');if(n)return decode(n);
  const inst=instances.get(fileId(scalar(go.text,'m_PrefabInstance')));if(!inst)return null;
  const a=asset(guid(scalar(go.text,'m_CorrespondingSourceObject'))),sid=fileId(scalar(go.text,'m_CorrespondingSourceObject'));
  return mods(inst).find(r=>r.target===sid&&r.property==='m_Name')?.value??decode(scalar(a?.docs.get(sid)?.text??'','m_Name'));
}
function pathOf(tid) {
  const names=[];const seen=new Set();let t=transforms.get(tid);
  while(t&&!seen.has(t.id)) {
    seen.add(t.id);names.unshift(nameOf(fileId(scalar(t.text,'m_GameObject')))??`[Transform:${t.id}]`);
    const inst=instances.get(fileId(scalar(t.text,'m_PrefabInstance')));
    t=transforms.get(t.stripped?fileId(inst?.text.match(/m_TransformParent: (.*)/)?.[1]):fileId(scalar(t.text,'m_Father')));
  }
  return names.join('/');
}
function fileRecord(path) {
  if(!path||!fs.existsSync(path))return {path,exists:false};
  const st=fs.statSync(path);if(st.isDirectory())return {path,directory:true};
  const meta=path+'.meta';return {path,bytes:st.size,sha256:hash(fs.readFileSync(path)),meta:fs.existsSync(meta)?{path:meta,guid:fs.readFileSync(meta,'utf8').match(/^guid: (.*)/m)?.[1],sha256:hash(fs.readFileSync(meta))}:null};
}
const identities=[];
for(const d of docs.values())if(d.type==='114'&&guid(scalar(d.text,'m_Script'))===idScript) {
  const gid=fileId(scalar(d.text,'m_GameObject'));
  identities.push({componentFileId:d.id,gameObjectFileId:gid,name:nameOf(gid),entityGuid:scalar(d.text,'_guid'),ownerKey:scalar(d.text,'_ownerKey'),pose:objectPose(gid),groundContract:{status:'미수행',terrainHeight:null,supportCollider:null,note:'디스크 YAML로 Terrain/Collider 실제 접지 판정 불가'},serializedComponentHash:hash(d.text)});
}
const guidOverrides=[];
for(const inst of instances.values())for(const r of mods(inst).filter(r=>r.property==='_guid'))guidOverrides.push({instanceFileId:inst.id,...r,note:'Prefab override identity; live component/좌표 검증 미수행'});
const references=[];
for(const d of docs.values())for(const m of d.text.matchAll(/^  (m_TerrainData|m_NavMeshData): (.*)$/gm)) {
  const g=guid(m[2]);references.push({documentId:d.id,typeId:d.type,property:m[1],reference:m[2],asset:g?fileRecord(assetPaths.get(g)):{nullReference:true},gameObjectFileId:fileId(scalar(d.text,'m_GameObject'))});
}
const environment=[];
const important=/AuthoredEnvironment|LakeAbundanceArt|ApproachMeadow|환경|후속|사면|거점|생활권|월드 구간|PoorVillage|LakeWater|DeepLake|OverflowWater|CoastalWater|CreekArt|Morgan|모건|ShorelineClusters|Village_.*(?:_R2|_Collision)|SceneContext|탐험 콘텐츠|진행|R0[5-8]_/;
for(const go of gos.values()) {
  const name=nameOf(go.id);const t=byGo.get(go.id);
  if(!name||(!important.test(name)&&(!t||fileId(scalar(t.text,'m_Father'))!=='0')))continue;
  const path=t?pathOf(t.id):name;
  environment.push({gameObjectFileId:go.id,transformFileId:t?.id,name,path,active:scalar(go.text,'m_IsActive'),pose:objectPose(go.id),serializedHash:hash(go.text),prefabInstance:fileId(scalar(go.text,'m_PrefabInstance'))});
}
// 루트 프리팹 인스턴스와 환경 하위 인스턴스를 기록해 standalone GameObject만으로 누락되는 배치를 보완한다.
const environmentInstances=[];
for(const inst of instances.values()) {
  const parentId=fileId(inst.text.match(/m_TransformParent: (.*)/)?.[1]);const parentPath=pathOf(parentId);
  const pg=guid(scalar(inst.text,'m_SourcePrefab'));const p=assetPaths.get(pg);const rows=mods(inst);const n=rows.find(r=>r.property==='m_Name')?.value;
  if(parentId==='0'||/AuthoredEnvironment|LakeAbundanceArt|ApproachMeadow|Morgan|모건|생활과 주민/.test(parentPath??''))environmentInstances.push({instanceFileId:inst.id,parentTransform:parentId,parentPath,overrideName:n,sourcePrefabPath:p,sourcePrefabGuid:pg,serializedInstanceHash:hash(inst.text),transformOverrides:rows.filter(r=>/^m_Local(Position|Rotation|Scale)\./.test(r.property)),activeOverrides:rows.filter(r=>r.property==='m_IsActive')});
}
const preservePaths=[scenePath,'Assets/10.Datas/World/OpenWorld/Terrain.asset','Assets/10.Datas/World/OpenWorld/EnvironmentFormNavigation.asset','Assets/05.Models/LivingWorld/MorganCargo/MorganCargo.fbx','Assets/04.Images/LivingWorld/MorganCargo/MorganCargoAtlas.png','Tools/OpenWorld/README.md','Tools/OpenWorld/fix-village-building-grounding.csx','output/MorganCargo/README.md'];
for(const dir of ['output/OpenWorld/BuildingGrounding','output/MorganCargo'])if(fs.existsSync(dir))for(const e of fs.readdirSync(dir,{withFileTypes:true}))if(e.isFile()&&/\.(txt|md|json)$/.test(e.name))preservePaths.push(`${dir}/${e.name}`);
const base={status:'디스크 기준선 / 디스크 정적 조사; live 상태·실제 로드·접지·경로·렌더 미수행',createdUtc:new Date().toISOString(),scene:fileRecord(scenePath),sceneDocumentCount:docs.size,limits:['열린 씬 미저장 변경 포함 안 됨','Prefab 내부 상속/중첩은 실제 Unity 로드 검사 필요','Terrain 바이너리 높이/alpha/detail/tree 및 접지는 미검증','mask/편집 권한을 부여하는 자료 아님'],scanErrors};
fs.mkdirSync(out,{recursive:true});
fs.writeFileSync(`${out}/disk-baseline-identities.json`,JSON.stringify({...base,directSerializedIdentityCount:identities.length,identityOverrideCount:guidOverrides.length,worldPositionResolved:identities.filter(i=>i.pose.complete).length,identities,guidOverrides},null,2));
fs.writeFileSync(`${out}/disk-baseline-environment.json`,JSON.stringify({...base,environment,environmentInstances},null,2));
fs.writeFileSync(`${out}/disk-baseline-assets.json`,JSON.stringify({...base,references,preservationAssets:[...new Set(preservePaths)].map(fileRecord)},null,2));
assert(identities.length>0);assert(references.some(r=>r.property==='m_TerrainData'&&r.asset?.exists!==false));
assert.equal(fileRecord(scenePath).sha256,base.scene.sha256,'조사 중 디스크 씬 변경');
console.log(JSON.stringify({sceneHash:base.scene.sha256,documents:docs.size,directIdentities:identities.length,identityOverrides:guidOverrides.length,resolvedPositions:identities.filter(i=>i.pose.complete).length,environmentObjects:environment.length,environmentInstances:environmentInstances.length,references:references.map(r=>({property:r.property,path:r.asset?.path,guid:r.asset?.meta?.guid})),scanErrors:scanErrors.length}));
