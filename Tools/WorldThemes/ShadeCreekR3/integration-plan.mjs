import fs from 'node:fs';import crypto from 'node:crypto';
const out='output/ShadeCreekR3/Integration';fs.mkdirSync(out,{recursive:true});
const main='Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity',review='Assets/01.Scenes/ArtReview/WorldThemes/ShadeCreekR3.unity';
const hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const baseline=JSON.parse(fs.readFileSync('output/ShadeCreekR3/baseline.json'));
const aText=fs.readFileSync(main,'utf8'),bText=fs.readFileSync(review,'utf8');
export const parse=s=>new Map([...s.matchAll(/^--- !u!(\d+) &(-?\d+)[^\n]*\n[\s\S]*?(?=^--- !u!|$(?![\s\S]))/gm)].map(m=>[m[2],{type:m[1],text:m[0]}]));
const a=parse(aText),b=parse(bText);if(a.size<1000||b.size<1000)throw Error('씬 파싱 실패');
const changed=[],added=[],missing=[];
for(const [id,old] of a){const next=b.get(id);if(!next)missing.push(id);else if(old.text!==next.text){const oldLines=new Set(old.text.split('\n'));changed.push({id,type:old.type,addedLines:next.text.split('\n').filter(l=>!oldLines.has(l))});}}
for(const [id,next] of b)if(!a.has(id))added.push({id,type:next.type});
const result={mainHash:hash(aText),reviewHash:hash(bText),canonicalUnchangedSinceReviewBaseline:hash(aText)===baseline.rows.find(r=>r.path===main).sha256,changed,added,missing};
fs.writeFileSync(out+'/plan.json',JSON.stringify(result,null,2));
const backup=out+'/LakeVillageWorld.before.unity';
if(fs.existsSync(backup)){if(hash(fs.readFileSync(backup))!==hash(aText))throw Error('이전 통합 백업과 현재 씬 다름');}else{fs.copyFileSync(main,backup);fs.copyFileSync(main+'.meta',backup+'.meta');fs.writeFileSync(out+'/review-before.sha256',hash(fs.readFileSync(review)));}
console.log({canonicalUnchanged:result.canonicalUnchangedSinceReviewBaseline,changed:changed.reduce((a,r)=>(a[r.type]=(a[r.type]||0)+1,a),{}),added:added.reduce((a,r)=>(a[r.type]=(a[r.type]||0)+1,a),{}),missing});
