import fs from 'node:fs';
const scene='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const text=fs.readFileSync(scene,'utf8');
const heads=[...text.matchAll(/^--- !u!(\d+) &(-?\d+)( stripped)?\r?$/gm)];
const docs=new Map(heads.map((h,i)=>[h[2],{id:h[2],type:h[1],stripped:!!h[3],text:text.slice(h.index,heads[i+1]?.index??text.length)}]));
const scalar=(s,k)=>s.match(new RegExp(`^  ${k}: ?(.*)$`,'m'))?.[1]?.trim();
const fid=s=>s?.match(/fileID: (-?\d+)/)?.[1];
const decode=s=>{try{return s?.startsWith('"')?JSON.parse(s):s;}catch{return s;}};
const vec=s=>Object.fromEntries([...s?.matchAll(/([xyzw]): ([-+\d.eE]+)/g)??[]].map(m=>[m[1],Number(m[2])]));
const gos=new Map([...docs.values()].filter(d=>d.type==='1').map(d=>[d.id,d]));
const byGo=new Map([...docs.values()].filter(d=>d.type==='4').map(d=>[fid(scalar(d.text,'m_GameObject')),d]));
const rotate=(v,q)=>{const u=[q.x??0,q.y??0,q.z??0],w=q.w??1,c=(a,b)=>[a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]],uv=c(u,v),uuv=c(u,uv);return v.map((n,i)=>n+2*(w*uv[i]+uuv[i]));};
const point=(v,t)=>{const p=vec(scalar(t.text,'m_LocalPosition')),r=vec(scalar(t.text,'m_LocalRotation')),s=vec(scalar(t.text,'m_LocalScale'));return rotate(v.map((n,i)=>n*(s[['x','y','z'][i]]??1)),r).map((n,i)=>n+(p[['x','y','z'][i]]??0));};
const names=/^(Roots_|RootCluster|RedTreeRoot|Cave|.*CaveEntrance|R0[578].*root|R08_FlowerState|R0[578].*shrine)/;
const rows=[];
for(const go of gos.values()){
  const name=decode(scalar(go.text,'m_Name'));if(!names.test(name??''))continue;
  let t=byGo.get(go.id),p=[0,0,0],chain=[],complete=true,loops=0;
  while(t&&loops++<30){
    const parentGo=gos.get(fid(scalar(t.text,'m_GameObject')));chain.push({transformId:t.id,name:decode(scalar(parentGo?.text??'','m_Name')),position:vec(scalar(t.text,'m_LocalPosition')),stripped:t.stripped});
    if(t.stripped){complete=false;break;}p=point(p,t);const father=fid(scalar(t.text,'m_Father'));if(father==='0')break;t=docs.get(father);if(!t){complete=false;break;}
  }
  rows.push({name,fileId:go.id,worldPosition:complete?p:null,complete,chain});
}
fs.mkdirSync('output/WorldThemeProduction/RedTreeForest',{recursive:true});
fs.writeFileSync('output/WorldThemeProduction/RedTreeForest/native-roots-and-caves.json',JSON.stringify({status:'현재 디스크 native GameObject/Transform만 조사. stripped prefab은 미해결로 명시.',scene,rows},null,2));
console.log(JSON.stringify(rows.map(r=>({name:r.name,worldPosition:r.worldPosition,complete:r.complete,ancestors:r.chain.map(t=>t.name)})),null,2));
