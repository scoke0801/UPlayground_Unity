import fs from 'node:fs';
import crypto from 'node:crypto';
const output='output/WorldThemeProduction/ShadeCreek';
const guid=fs.readFileSync('Assets/03.Prefabs/Environment/RedTreeCreek/CreekArt.prefab.meta','utf8').match(/^guid: (\w+)/m)[1];
const scenes=['Assets/01.Scenes/Scenario/DroughtVillage.unity','Assets/01.Scenes/Scenario/OpenWorld/Regions/DroughtVillageRegion.unity'];
const scalar=(s,k)=>s.match(new RegExp(`^  ${k}: ?(.*)$`,'m'))?.[1]?.trim();
const fid=s=>s?.match(/fileID: (-?\d+)/)?.[1];
const decode=s=>{try{return s?.startsWith('"')?JSON.parse(s):s;}catch{return s;}};
const rows=[];
for(const scene of scenes){
 const text=fs.readFileSync(scene,'utf8');const heads=[...text.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\r?$/gm)];
 const docs=new Map(heads.map((h,i)=>[h[2],{id:h[2],type:h[1],stripped:!!h[3],text:text.slice(h.index,heads[i+1]?.index??text.length)}]));
 const names=new Map([...docs.values()].filter(d=>d.type==='1').map(d=>[d.id,decode(scalar(d.text,'m_Name'))]));
 const transforms=new Map([...docs.values()].filter(d=>d.type==='4').map(d=>[fid(scalar(d.text,'m_GameObject')),d]));
 const pathFor=goId=>{let parts=[],current=goId;for(let i=0;i<35&&current;i++){parts.unshift(names.get(current)??`fileID:${current}`);const t=transforms.get(current);const parent=docs.get(fid(scalar(t?.text??'','m_Father')));current=parent?fid(scalar(parent.text,'m_GameObject')):null;}return parts.join('/');};
 const creekInstances=[...docs.values()].filter(d=>d.type==='1001'&&scalar(d.text,'m_SourcePrefab')?.includes(guid)).map(d=>({fileId:d.id,parentTransform:fid(d.text.match(/m_TransformParent: (.*)/)?.[1]),modifications:d.text.split(/\r?\n/).filter(l=>/target:|propertyPath:|value:|objectReference:/.test(l))}));
 const selected=[...docs.values()].filter(d=>d.type==='1'&&/Creek|Pool|Water|Roots|R08/i.test(names.get(d.id)??'')).map(d=>{
  const ids=[...d.text.matchAll(/component: \{fileID: (-?\d+)\}/g)].map(m=>m[1]);
  return {fileId:d.id,name:names.get(d.id),path:pathFor(d.id),active:scalar(d.text,'m_IsActive'),transform:transforms.get(d.id)?.text,components:ids.map(id=>docs.get(id)).filter(x=>x&&x.type!=='4').map(x=>({fileId:x.id,type:x.type,script:x.text.match(/m_Script:.*guid: (\w+)/)?.[1],text:x.text}))};
 });
 const bindings=[...docs.values()].filter(d=>d.type==='114'&&(/_Recession|[Cc]reek|[Ww]ater/.test(d.text)&&d.text.includes('lake.red_tree'))).map(d=>({fileId:d.id,objectName:names.get(fid(scalar(d.text,'m_GameObject'))),path:pathFor(fid(scalar(d.text,'m_GameObject'))),text:d.text}));
 rows.push({scene,sha256:crypto.createHash('sha256').update(fs.readFileSync(scene)).digest('hex'),creekInstances,selectedObjects:selected,bindings});
}
fs.writeFileSync(output+'/creek-region-content.json',JSON.stringify({status:'원본 Scenario와 Streaming Region의 정확 GUID/직렬화 연결 조사. 실행/MPB/수위/vertex color 효과 미검증',rows},null,2));
console.log(JSON.stringify(rows.map(r=>({scene:r.scene,creekInstances:r.creekInstances.length,selected:r.selectedObjects.map(x=>({name:x.name,path:x.path,components:x.components.map(c=>({type:c.type,script:c.script}))})),bindingObjects:r.bindings.map(x=>({fileId:x.fileId,path:x.path}))})),null,2));
