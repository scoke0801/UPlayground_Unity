import fs from 'node:fs';
import crypto from 'node:crypto';
const out='output/QuarryPassR1';
const p=`${out}/references-final.json`;
const data=JSON.parse(fs.readFileSync(p,'utf8'));
let done=0;
for(const row of data.resources){
  const h=crypto.createHash('sha256');
  for await(const chunk of fs.createReadStream(row.path))h.update(chunk);
  row.sha256=h.digest('hex').toUpperCase();
  if(++done%500===0)console.log(`${done}/${data.resources.length}`);
}
data.hashRecordedUtc=new Date().toISOString();
fs.writeFileSync(p,JSON.stringify(data,null,2));
console.log('참조 해시 완료');
