import fs from 'node:fs';
import crypto from 'node:crypto';
const root='output/WorldThemeProduction/ShadeCreek/ConnectedProtection';
const manifestPath=root+'/manifest.json',m=JSON.parse(fs.readFileSync(manifestPath,'utf8'));
const inspect=JSON.parse(fs.readFileSync(root+'/unresolved-prefab-source-inspection.json','utf8'));
const scalar=(s,k)=>s.match(new RegExp(`^  ${k}: ?(.*)$`,'m'))?.[1]?.trim();
const fid=s=>s?.match(/fileID: (-?\d+)/)?.[1];
const ref=s=>s?.match(/guid: (\w+)/)?.[1];
const vector=s=>Object.fromEntries([...s.matchAll(/([xyzw]): ([\d.eE+-]+)/g)].map(m=>[m[1],Number(m[2])]));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex').toUpperCase();
const decode=s=>{try{return s?.startsWith('"')?JSON.parse(s):s;}catch{return s;}};
const cache=new Map();
const guidIndex=new Map();
function indexPrefabs(dir){for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=dir+'/'+e.name;if(e.isDirectory())indexPrefabs(p);else if(e.name.endsWith('.prefab.meta')){const g=fs.readFileSync(p,'utf8').match(/^guid: (\w+)/m)?.[1];if(g){const paths=guidIndex.get(g)??[];paths.push(p.slice(0,-5));guidIndex.set(g,paths);}}}}
indexPrefabs('Assets/03.Prefabs');
indexPrefabs('Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA');
const exactPath=guid=>{const paths=guidIndex.get(guid)??[];if(paths.length!==1)throw new Error('exact inherited prefab GUID unresolved/ambiguous: '+guid);return paths[0];};
function parse(path){if(cache.has(path))return cache.get(path);const text=fs.readFileSync(path,'utf8'),heads=[...text.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\r?$/gm)],docs=new Map(heads.map((h,i)=>[h[2],{id:h[2],type:h[1],stripped:!!h[3],text:text.slice(h.index,heads[i+1]?.index??text.length)}]));const result={path,hash:sha(path),docs};cache.set(path,result);return result;}
const cross=(a,b)=>[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]];
const apply=(p,pose)=>{const q=pose.rotation,s=pose.scale,pos=pose.position;let v=p.map((x,i)=>x*s[['x','y','z'][i]]);const u=[q.x,q.y,q.z],t=cross(u,v).map(x=>x*2),c=cross(u,t);return v.map((x,i)=>x+q.w*t[i]+c[i]+pos[['x','y','z'][i]]);};
const poseOf=text=>({position:vector(scalar(text,'m_LocalPosition')??''),rotation:vector(scalar(text,'m_LocalRotation')??''),scale:vector(scalar(text,'m_LocalScale')??'')});
const valid=p=>['position','scale'].every(k=>['x','y','z'].every(a=>Number.isFinite(p[k][a])))&&['x','y','z','w'].every(a=>Number.isFinite(p.rotation[a]));
const modifications=text=>[...text.matchAll(/    - target: \{fileID: (-?\d+), guid: (\w+), type: \d+\}\r?\n      propertyPath: ([^\r\n]*)\r?\n      value: ([^\r\n]*)\r?\n      objectReference: ([^\r\n]*)/g)].map(m=>({targetFileId:m[1],targetGuid:m[2],property:m[3],value:m[4],reference:m[5]}));
function applyOverrides(pose,mods,guid,id){for(const mod of mods.filter(m=>m.targetGuid===guid&&m.targetFileId===id)){const match=mod.property.match(/^m_Local(Position|Rotation|Scale)\.([xyzw])$/);if(!match)continue;const field={Position:'position',Rotation:'rotation',Scale:'scale'}[match[1]],value=Number(mod.value);if(!Number.isFinite(value))throw new Error('nonfinite inherited override');pose[field][match[2]]=value;}return pose;}
function resolveAssetRoot(asset,seen=[]){
 if(seen.includes(asset.path))throw new Error('prefab source cycle');
 const native=[...asset.docs.values()].filter(d=>d.type==='4'&&!d.stripped&&fid(scalar(d.text,'m_Father'))==='0');
 if(native.length===1){const t=native[0],goId=fid(scalar(t.text,'m_GameObject')),pose=poseOf(t.text);if(!valid(pose))throw new Error('native prefab root pose incomplete');return {transformId:t.id,goId,pose,name:decode(scalar(asset.docs.get(goId)?.text??'','m_Name')),chain:[{path:asset.path,hash:asset.hash,kind:'native root',transformId:t.id,gameObjectId:goId,pose}]};}
 if(native.length>1)throw new Error('multiple native source roots; no fallback');
 const roots=[...asset.docs.values()].filter(d=>d.type==='1001'&&fid(d.text.match(/m_TransformParent: (.*)/)?.[1])==='0');if(roots.length!==1)throw new Error('source variant root instance not unique');
 const instance=roots[0],baseGuid=ref(scalar(instance.text,'m_SourcePrefab')),base=parse(exactPath(baseGuid)),resolved=resolveAssetRoot(base,[...seen,asset.path]);
 const mapped=(type,id)=>[...asset.docs.values()].filter(d=>d.type===type&&d.stripped&&fid(scalar(d.text,'m_PrefabInstance'))===instance.id&&fid(scalar(d.text,'m_CorrespondingSourceObject'))===id&&ref(scalar(d.text,'m_CorrespondingSourceObject'))===baseGuid);
 const ts=mapped('4',resolved.transformId),gos=mapped('1',resolved.goId);if(ts.length!==1||gos.length!==1)throw new Error('variant root correspondence is incomplete/nonunique: '+JSON.stringify({path:asset.path,baseGuid,basePath:base.path,baseTransform:resolved.transformId,baseGo:resolved.goId,transforms:ts.length,gameObjects:gos.length}));
 const mods=modifications(instance.text),pose=applyOverrides(JSON.parse(JSON.stringify(resolved.pose)),mods,baseGuid,resolved.transformId);
 const name=decode(mods.find(m=>m.targetGuid===baseGuid&&m.targetFileId===resolved.goId&&m.property==='m_Name')?.value??resolved.name);
 return {transformId:ts[0].id,goId:gos[0].id,pose,name,chain:[...resolved.chain,{path:asset.path,hash:asset.hash,kind:'exact prefab variant root',instanceId:instance.id,baseGuid,basePath:base.path,baseHash:base.hash,baseRootTransformId:resolved.transformId,baseRootGameObjectId:resolved.goId,transformId:ts[0].id,gameObjectId:gos[0].id,rootTransformOverrides:mods.filter(m=>m.targetGuid===baseGuid&&m.targetFileId===resolved.transformId),effectivePose:pose}]};
}
const resolved=[],unresolved=[];
for(const item of inspect){try{
 const scene=parse(item.sourceScene),asset=parse(item.sourcePrefabPath);if(scene.hash!==m.scenes.find(s=>s.path===item.sourceScene)?.sha256.toUpperCase())throw new Error('source scene hash changed');
 if(fs.readFileSync(item.sourcePrefabPath+'.meta','utf8').match(/^guid: (\w+)/m)?.[1]!==item.sourcePrefabGuid)throw new Error('source prefab exact GUID mismatch');
 const sourceRoot=resolveAssetRoot(asset),sourceTransform={id:sourceRoot.transformId},rootGoId=sourceRoot.goId,sourceGo=asset.docs.get(rootGoId);
 const correspondingGo=item.go?fid(scalar(item.go,'m_CorrespondingSourceObject')):null;
 if(correspondingGo&&correspondingGo!==rootGoId)throw new Error('identity belongs to a source child, root-only resolution rejected');
 if(item.go&&ref(scalar(item.go,'m_CorrespondingSourceObject'))!==item.sourcePrefabGuid)throw new Error('stripped source GUID differs; inherited source chain not inferred');
 if(!item.go){const instance=scene.docs.get(item.instanceId);const idTargets=[...instance.text.matchAll(/target: \{fileID: (-?\d+), guid: (\w+), type: \d+\}\r?\n      propertyPath: _guid\r?\n      value: ([^\r\n]+)/g)].filter(x=>x[3]===item.identity);if(idTargets.length!==1)throw new Error('identity override target is not unique');const identityDoc=asset.docs.get(idTargets[0][1]);if(idTargets[0][2]!==item.sourcePrefabGuid||!identityDoc||fid(scalar(identityDoc.text,'m_GameObject'))!==rootGoId)throw new Error('identity override is not the exact source root component');}
 const pose=JSON.parse(JSON.stringify(sourceRoot.pose));if(!valid(pose))throw new Error('source root pose incomplete');
 const applied=[];for(const mod of item.instanceRootModifications.filter(r=>r.targetGuid===item.sourcePrefabGuid&&r.targetFileId===sourceTransform.id)){
  const p=mod.property.match(/^m_Local(Position|Rotation|Scale)\.([xyzw])$/);if(!p)continue;const field={Position:'position',Rotation:'rotation',Scale:'scale'}[p[1]],value=Number(mod.value);if(!Number.isFinite(value))throw new Error('invalid root override value');pose[field][p[2]]=value;applied.push(mod);
 }
 let point=apply([0,0,0],pose),parentId=item.parentTransform,parentChain=[],names=[];for(let i=0;i<40&&parentId&&parentId!=='0';i++){
  const t=scene.docs.get(parentId);if(!t||t.type!=='4'||t.stripped)throw new Error('native parent chain incomplete/stripped');const p=poseOf(t.text);if(!valid(p))throw new Error('parent pose incomplete');point=apply(point,p);const go=scene.docs.get(fid(scalar(t.text,'m_GameObject')));names.unshift(decode(scalar(go?.text??'','m_Name'))??'unknown');parentChain.push({transformFileId:parentId,pose:p,gameObjectFileId:go?.id});parentId=fid(scalar(t.text,'m_Father'));
 }
 const renamed=item.instanceRootModifications.find(r=>r.property==='m_Name'&&r.targetFileId===rootGoId&&r.targetGuid===item.sourcePrefabGuid)?.value;names.push(decode(renamed??scalar(sourceGo?.text??'','m_Name')??sourceRoot.name));
 const evidence={identity:item.identity,mapId:item.mapId,sourceScene:item.sourceScene,sourceSceneHash:scene.hash,instanceFileId:item.instanceId,sourcePrefabPath:item.sourcePrefabPath,sourcePrefabGuid:item.sourcePrefabGuid,sourcePrefabHash:asset.hash,sourceRootGameObjectFileId:rootGoId,sourceRootTransformFileId:sourceTransform.id,rootIdentityVerified:true,sourceRootPose:sourceRoot.pose,sourcePrefabRootInheritance:sourceRoot.chain,appliedExactRootOverrides:applied,effectiveInstanceRootPose:pose,nativeParentChain:parentChain,worldPosition:point,objectPath:names.join('/'),limits:'정확 native/variant prefab root+instance override+native parent chain의 파일 좌표만 해결. Unity 실제 로드·Collider·접지·루틴/진행은 미검증'};
 resolved.push(evidence);
 const row=m.editModeIdentities.find(r=>r.entityGuid===item.identity&&r.sourceScene===item.sourceScene);if(!row)throw new Error('manifest identity owner not found');const oldPath=row.objectPath;
 Object.assign(row,{worldPosition:point,poseComplete:true,objectPath:evidence.objectPath,prefabSourceResolution:evidence});
 for(const c of m.contentComponents)if(c.sourceScene===item.sourceScene&&c.objectPath===oldPath&&oldPath){c.worldPosition=point;c.poseComplete=true;c.objectPath=evidence.objectPath;c.prefabSourceResolutionKey=item.identity;}
 for(const c of m.contentComponents.filter(c=>c.sourceScene===item.sourceScene))for(const b of c.sceneBindings??[])if(oldPath&&b.path===oldPath){b.pose={position:point,complete:true,resolution:'exact prefab source root chain'};b.path=evidence.objectPath;}
 }catch(e){unresolved.push({identity:item.identity,mapId:item.mapId,sourceScene:item.sourceScene,sourcePrefabPath:item.sourcePrefabPath,reason:e.message});}}
for(const r of m.placementRecords)for(const group of r.groupOwners??[]){const identity=m.editModeIdentities.find(i=>i.ownerKey===group.ownerKey);if(identity){group.worldPosition=identity.worldPosition;group.path=identity.objectPath;}}
m.prefabPoseResolutionPass={method:'exact native prefab root identity + source pose + exact target GUID/fileID instance overrides + native parent chain',resolved:resolved.length,unresolved:unresolved.length,evidence:'prefab-pose-resolution.json'};
fs.writeFileSync(root+'/manifest.json',JSON.stringify(m,null,2));fs.writeFileSync(root+'/prefab-pose-resolution.json',JSON.stringify({status:'정확 소스 체인 파일 좌표 해결. Unity/접지/진행 검증 아님',sourceFilesRead:[...cache.values()].map(a=>({path:a.path,sha256:a.hash,guid:fs.existsSync(a.path+'.meta')?fs.readFileSync(a.path+'.meta','utf8').match(/^guid: (\w+)/m)?.[1]:null})),resolved,unresolved},null,2));
console.log(JSON.stringify({resolved:resolved.map(r=>({id:r.identity,mapId:r.mapId,position:r.worldPosition,path:r.objectPath})),unresolved},null,2));
