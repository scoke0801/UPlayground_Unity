// 계획의 64비트 fileID를 숫자로 바꾸지 않고 외부 Unity 참조를 검사한다.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const assert = require('assert/strict');

function keys(text) {
  const result = [];
  for (const block of text.replace(/\\"/g, '"').matchAll(/\{[^{}]{0,512}\}/g)) {
    const id = block[0].match(/["']?fileID["']?\s*:\s*["']?(-?\d+)/);
    const guid = block[0].match(/["']?guid["']?\s*:\s*["']?([a-f\d]{32})/i);
    if (id && guid) result.push(guid[1].toLowerCase() + ':' + id[1]);
  }
  return result;
}

const guid = '0123456789abcdef0123456789abcdef';
assert.deepEqual(keys('{fileID: 9007199254740993, guid: ' + guid + ', type: 3}'), [guid + ':9007199254740993']);
assert.deepEqual(keys('{"guid":"' + guid + '","fileID":"9007199254740993"}'), [guid + ':9007199254740993']);
assert.deepEqual(keys('{fileID: 123}'), []);

if (process.argv[2] === '--self-test') {
  process.stdout.write('참조 파서 검사 3건 통과\n');
  process.exit(0);
}
const planPath = process.argv[2];
if (!planPath) throw Error('계획 JSON 경로가 필요합니다.');
const raw = fs.readFileSync(planPath);
const plan = JSON.parse(raw);
const owners = new Map();
for (const entry of plan.entries) {
  for (const key of entry.removedReferenceKeys) owners.set(key, entry.path);
}
const guids = [...new Set(plan.entries.map(e => e.guid))];
const guidPattern = new RegExp(guids.join('|'), 'i');
const suffixes = new Set(['.asset', '.prefab', '.unity', '.controller', '.overridecontroller', '.anim', '.playable', '.mat', '.preset', '.json', '.meta']);
const binaryAssets = [], skippedLinks = [], hits = [];
let filesScanned = 0, bytesScanned = 0;
function scan(directory) {
  for (const entry of fs.readdirSync(directory, {withFileTypes: true})) {
    const file = path.join(directory, entry.name).replace(/\\/g, '/');
    if (entry.isSymbolicLink()) { skippedLinks.push(file); continue; }
    if (entry.isDirectory()) { scan(file); continue; }
    if (!suffixes.has(path.extname(file).toLowerCase())) continue;
    const content = fs.readFileSync(file);
    filesScanned++;
    bytesScanned += content.length;
    if (content.subarray(0, 256).includes(0)) { binaryAssets.push(file); continue; }
    const text = content.toString('utf8');
    if (!guidPattern.test(text)) continue;
    for (const key of keys(text)) {
      const owner = owners.get(key);
      if (owner && file !== owner) hits.push(file + ' -> ' + key);
    }
  }
}
scan('Assets');
scan('Packages');
const result = {planHash: crypto.createHash('sha256').update(raw).digest('hex').toUpperCase(), passed: false,
  filesScanned, bytesScanned, hits: [...new Set(hits)], binaryAssets, skippedLinks, binaryDependenciesChecked: false};
const output = path.join(plan.reportFolder, 'external-references.json');
if (fs.existsSync(output)) throw Error('기존 외부 참조 검사 결과는 덮어쓰지 않습니다.');
fs.writeFileSync(output, JSON.stringify(result, null, 2));
process.stdout.write(JSON.stringify({output, filesScanned, bytesScanned, hits: result.hits.length, binaryAssets: binaryAssets.length, skippedLinks: skippedLinks.length}) + '\n');
