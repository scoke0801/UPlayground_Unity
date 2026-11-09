import fs from 'node:fs';import crypto from 'node:crypto';
const baseline=JSON.parse(fs.readFileSync('output/ShadeCreekR3/baseline.json'));
const target=baseline.rows.find(r=>r.path==='ProjectSettings/QualitySettings.asset').sha256;
const current=fs.readFileSync('ProjectSettings/QualitySettings.asset','utf8');
const hash=t=>crypto.createHash('sha256').update(t).digest('hex');const found=[];
const guids=fs.readdirSync('Assets/Settings').filter(p=>p.endsWith('.meta')).map(p=>fs.readFileSync('Assets/Settings/'+p,'utf8').match(/guid: (\w+)/)?.[1]).filter(Boolean);
const values=[...new Set([...Array.from({length:257},(_,i)=>String(i)),...guids,'-1','0.5','0.1'])];
for(const match of current.matchAll(/(?<=: )(-?\d+(?:\.\d+)?|[a-f0-9]{32})(?=\r?\n|,|})/g)){
for(const value of values){const candidate=current.slice(0,match.index)+value+current.slice(match.index+match[0].length);for(const text of [candidate,candidate.replace(/\r?\n/g,'\r\n')])if(hash(text)===target)found.push({field:current.slice(current.lastIndexOf('\n',match.index)+1,match.index),current:match[0],baseline:value});}}
fs.writeFileSync('output/ShadeCreekR3/quality-delta-investigation.json',JSON.stringify({target,current:hash(current),found,scope:'시작 해시와 정확히 일치하는 단일 직렬화 값 차이 탐색. 설정 파일 쓰기 없음.'},null,2));console.log(found);
