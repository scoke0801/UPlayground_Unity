// 읽기 전용 보조 조사. 최종 연결 판정은 Unity에서 로드한 결과를 사용한다.
import fs from 'node:fs';
const roots=['Assets/10.Datas','Assets/03.Prefabs','Assets/07.Animations'];
const files=[];
function walk(p){for(const e of fs.readdirSync(p,{withFileTypes:true})){const q=p+'/'+e.name;if(e.isDirectory())walk(q);else files.push(q);}}
for(const root of roots)walk(root);
const index=new Map();for(const p of files.filter(p=>p.endsWith('.meta'))){const g=fs.readFileSync(p,'utf8').match(/^guid: (\w+)/m);if(g)index.set(g[1],p.slice(0,-5));}
const resolve=g=>index.get(g)||g;
const read=p=>fs.readFileSync(p,'utf8');
const targets=files.filter(p=>/\.asset$/.test(p)&&(/Monster(Sera|SeolA|Reien)|AlternateSelfNenmir/.test(p)));
const lines=[];
for(const p of targets){lines.push('\n'+p);for(const l of read(p).split('\n'))if(/actorId:|prefab:|abilitySet:|baseSet:|monsterProfile:|behaviorData:/.test(l))lines.push(l.replace(/guid: (\w+)/g,(_,g)=>'path: '+resolve(g)));}
for(const p of files.filter(p=>/MonsterActor_(Sera|SeolA|Reien).*prefab$/.test(p))){lines.push('\n'+p);for(const l of read(p).split('\n'))if(/_motionSet:|propertyPath:.*motion|objectReference:|_groupId:|hitboxGroupId:/.test(l))lines.push(l.replace(/guid: (\w+)/g,(_,g)=>'path: '+resolve(g)));}
fs.mkdirSync('Logs/HumanoidGrade/Remaining',{recursive:true});fs.writeFileSync('Logs/HumanoidGrade/Remaining/text-audit.txt',lines.join('\n'));console.log(lines.join('\n'));
