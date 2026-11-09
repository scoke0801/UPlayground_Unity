import fs from 'node:fs';
import crypto from 'node:crypto';
const folder='output/RedForestR1/Integration';
const main='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity';
const review='Assets/01.Scenes/ArtReview/WorldThemes/RedForestR1.unity';
const before=fs.readFileSync(`${folder}/LakeVillageWorld.before.unity`,'utf8');
if(fs.readFileSync(main,'utf8')!==before)throw Error('통합 시작 이후 메인 변경 발견');
const parse=s=>new Map([...s.matchAll(/^--- !u!(\d+) &(-?\d+)[^\n]*\n[\s\S]*?(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>[m[2],{type:m[1],text:m[0]}]));
const a=parse(before),b=parse(fs.readFileSync(review,'utf8'));
const normalize=s=>s.replace(/(GlobalObjectId_V1-\d+-)[a-f0-9]+/g,'$1SCENE');
const guid=fs.readFileSync(main+'.meta','utf8').match(/guid: (\w+)/)[1];
const mappings=fs.readFileSync(`${folder}/asset-map.tsv`,'utf8').trim().split(/\r?\n/).map(l=>l.split('\t'));
const remap=s=>{for(const [old,next] of mappings)s=s.replaceAll(old,next);return s.replace(/(GlobalObjectId_V1-\d+-)[a-f0-9]+/g,'$1'+guid);};
const changes=[],additions=[];let result=before.slice(0,before.indexOf('--- !u!'));
for(const [id,old] of a){const next=b.get(id);if(!next)throw Error('기존 객체 누락 '+id);if(normalize(old.text)===normalize(next.text)){result+=old.text;continue;}
if(!['114','1001','154','218','1660057539'].includes(old.type))throw Error('예상 밖 수정 '+id+' '+old.type);
if(old.type==='1001'){
 const strip=text=>normalize(text).replace(/    - target: [^\n]+\n      propertyPath: ([^\n]+)\n      value: [^\n]*\n      objectReference: [^\n]+\n/g,(whole,path)=>/^'?m_(Materials\.Array\.data\[\d+\]'?|LocalPosition\.[xyz]|LocalRotation\.[xyzw]|LocalScale\.[xyz]|LocalEulerAnglesHint\.[xyz]|Enabled|IsActive)$/.test(path)?'':whole);
 if(strip(old.text)!==strip(next.text)){fs.writeFileSync(folder+'/unexpected-before.txt',strip(old.text));fs.writeFileSync(folder+'/unexpected-after.txt',strip(next.text));throw Error('비환경 프리팹 수정 발견 '+id);}
}else if(old.type==='114'){
 if(old.text.replace(/m_NavMeshData: [^\n]+/,'NAV')!==next.text.replace(/m_NavMeshData: [^\n]+/,'NAV'))throw Error('콘텐츠 변경 발견 '+id);
}else if(['154','218'].includes(old.type)){
 if(old.text.replace(/m_TerrainData: [^\n]+/,'TERRAIN')!==next.text.replace(/m_TerrainData: [^\n]+/,'TERRAIN'))throw Error('지형 연결 외 수정 '+id);
}
const oldLines=new Set(normalize(old.text).split('\n'));const addedLines=normalize(next.text).split('\n').filter(l=>!oldLines.has(l));
changes.push({id,type:old.type,addedLines});result+=remap(next.text);
}
for(const [id,next] of b)if(!a.has(id)){if(!['1','4','1001'].includes(next.type))throw Error('예상 밖 신규 유형 '+next.type);additions.push({id,type:next.type});result+=remap(next.text);}
for(const [old] of mappings)if(result.includes(old))throw Error('검토 자원 참조 잔존');
const report={beforeSha:crypto.createHash('sha256').update(before).digest('hex'),afterSha:crypto.createHash('sha256').update(result).digest('hex'),changes,additions};
fs.writeFileSync(`${folder}/merge-plan.json`,JSON.stringify(report,null,2));
fs.writeFileSync(`${folder}/LakeVillageWorld.merged.unity`,result);
console.log(JSON.stringify({changed:changes.length,added:additions.length,types:[...new Set(changes.map(c=>c.type))],nonvisualPrefabChanges:0,contentChanges:0}));
if(process.argv.includes('--apply')){fs.writeFileSync(main,result);console.log('메인 씬 변경 블록 병합 완료');}


