const fs=require('fs');
const root='Logs/StartupProfiling/StaticBatchCanonicalR1/';
const plan=JSON.parse(fs.readFileSync(root+'staging-manifest.json'));
function docs(file){const s=fs.readFileSync(file,'utf8');const matches=[...s.matchAll(/^--- !u!(\d+) &(-?\d+)([^\r\n]*)\r?\n/gm)];const result=new Map();for(let i=0;i<matches.length;i++){const m=matches[i],end=i+1<matches.length?matches[i+1].index:s.length;const key=m[1]+':'+m[2];if(result.has(key))throw Error('중복 문서 '+key);result.set(key,{type:m[1],id:m[2],text:s.slice(m.index,end)});}return result;}
const a=docs(plan.sourcePath),b=docs(plan.stagingPath),changed=[],added=[],removed=[],counts={};
for(const [key,x]of a){const y=b.get(key);if(!y){removed.push(key);continue;}if(x.text!==y.text){changed.push(key);counts[x.type]=(counts[x.type]||0)+1;}}
for(const key of b.keys())if(!a.has(key))added.push(key);
const samples=changed.slice(0,5).map(key=>{const x=a.get(key).text,y=b.get(key).text;let i=0;while(i<Math.min(x.length,y.length)&&x[i]===y[i])i++;return{key,before:x.slice(Math.max(0,i-100),i+400),after:y.slice(Math.max(0,i-100),i+500)}});
const result={before:a.size,after:b.size,changed:changed.length,changedTypes:counts,added:added.length,addedSamples:added.slice(0,10),removed:removed.length,removedSamples:removed.slice(0,10),samples};
fs.writeFileSync(root+'raw-scene-diff.json',JSON.stringify(result,null,2));console.log(result);

