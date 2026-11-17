// 원본은 읽기만 하고 수직 범위별 중앙차분 경사를 측정한다.
const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto');
const root = 'docs/전달용/OpenWorld_HeightMap_Design_Regional_v1_1_Handoff';
if(process.argv.includes('--verify-fixtures')) {
  const baseline=JSON.parse(fs.readFileSync('output/WorldBuilder/p00-audit.json','utf8'));
  for(const [file,expected] of Object.entries(baseline.fixtureHashes)) {
    const actual=crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
    if(actual!==expected)throw Error('B 원본 변경: '+file);
  }
  console.log('B 원본 '+Object.keys(baseline.fixtureHashes).length+'파일 SHA 보존 통과');
  return;
}
const file = root+'/atlas/01_Regional_Heightmap_2049_R16LE_SouthFirst.raw';
const raw = fs.readFileSync(file), n=2049;
if(raw.length !== n*n*2) throw Error('RAW 크기 불일치');
const sample = (x,z) => raw.readUInt16LE(2*(z*n+x));
const slopes=[]; let min=65535,max=0;
for(let z=0;z<n;z++) for(let x=0;x<n;x++) {
  const value=sample(x,z); min=Math.min(min,value); max=Math.max(max,value);
  const left=Math.max(0,x-1),right=Math.min(n-1,x+1),south=Math.max(0,z-1),north=Math.min(n-1,z+1);
  const dx=(sample(right,z)-sample(left,z))/(right-left)*700/65535;
  const dz=(sample(x,north)-sample(x,south))/(north-south)*700/65535;
  slopes.push(Math.atan(Math.hypot(dx,dz))*180/Math.PI);
}
slopes.sort((a,b)=>a-b);
const percentile=q=>slopes[Math.floor((slopes.length-1)*q)];
function files(dir) { return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.isDirectory()?files(path.join(dir,e.name)):[path.join(dir,e.name)]); }
const fixtureHashes=Object.fromEntries(files(root+'/core/samples').map(f=>[f.replaceAll('\\','/'),crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex')]));
const report={input:file,sha256:crypto.createHash('sha256').update(raw).digest('hex'),method:'전 2049² 표본, 1m 간격, 내부 중앙차분/외곽 단측차분, atan(hypot(dx,dz)), 순위 백분위',verticalRange:700,heightMin:min/65535*700,heightMax:max/65535*700,sampleCount:slopes.length,median:percentile(.5),p90:percentile(.9),p99:percentile(.99),max:slopes.at(-1),over12:slopes.filter(x=>x>12).length/slopes.length,over45:slopes.filter(x=>x>45).length/slopes.length,over60:slopes.filter(x=>x>60).length/slopes.length,fixtureHashes};
fs.mkdirSync('output/WorldBuilder',{recursive:true}); fs.writeFileSync('output/WorldBuilder/p00-audit.json',JSON.stringify(report,null,2)); console.log(JSON.stringify({...report,fixtureHashes:`${Object.keys(fixtureHashes).length}개 원본 해시 보관`},null,2));
