// 이번 회차의 덮어쓸 수 없는 기준 자료를 먼저 보존한다.
import fs from 'node:fs';
import crypto from 'node:crypto';
const folder='output/OpenWorld/Exploration/Iteration04';
const r=JSON.parse(fs.readFileSync(folder+'/input-before.json','utf8'));
const paths=['Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity','Assets/01.Scenes/Scenario/DroughtVillage.unity',...r.cities.map(c=>c.sourcePath)];
const hashes=Object.fromEntries(paths.map(p=>[p,crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex')]));
if (!fs.existsSync(folder+'/source-before.json')) fs.writeFileSync(folder+'/source-before.json',JSON.stringify(hashes,null,2));
let capture=fs.readFileSync('Tools/OpenWorld/capture-exploration.csx','utf8');
capture=capture.replaceAll('output/OpenWorld/Exploration/','output/OpenWorld/Exploration/Iteration04/');
const a=capture.indexOf('var names='),b=capture.indexOf('var go=',a);
capture=capture.slice(0,a)+`var names=new[]{"entry","decision","approach","activity","return","clearing","north","atlas"};
var coordinates=new[]{new Vector3(1540,0,510),new Vector3(1700,0,740),new Vector3(1640,0,760),new Vector3(1609,0,801),new Vector3(1625,0,825),new Vector3(1750,0,870),new Vector3(1680,0,1030),new Vector3(1200,3800,700)};
var directions=new[]{new Vector3(1,0,1),new Vector3(-1,0,.22f),new Vector3(-.6f,0,1),new Vector3(-1,-.18f,1),new Vector3(1,.08f,.5f),new Vector3(-.4f,0,1),new Vector3(-.5f,0,1),Vector3.down};
`+capture.slice(b);
fs.writeFileSync('Tools/OpenWorld/capture-forest-discovery.csx',capture);
fs.writeFileSync(folder+'/capture-phase.txt','before');
