import fs from 'node:fs';
import assert from 'node:assert/strict';

// 기존 저작값을 작업 전 사본에서 보존하고 수직 군락의 필드만 적용한다.
const root = 'output/WeatherSky/CloudHeight/';
const normalize = s => s.replaceAll('\r\n', '\n');
const guidAt = path => fs.readFileSync(path + '.meta', 'utf8').match(/guid: (\w+)/)[1];
const formationGuid = guidAt('Assets/10.Datas/World/Sky/CloudFormations.asset');
const reports = [];
for (const path of fs.readFileSync(root + 'materials.txt', 'utf8').trim().split(/\r?\n/)) {
    let source = normalize(fs.readFileSync(root + 'backup/' + guidAt(path) + '.mat', 'utf8'));
    assert.ok(!source.includes('    - _CloudFormation:'), '군락 적용 전 사본이어야 한다');
    source = source.replace('    m_Ints:', `    - _CloudFormation:\n        m_Texture: {fileID: 11700000, guid: ${formationGuid}, type: 2}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}\n    m_Ints:`);
    const fields = { _CloudVolumeEnabled: 1, _CloudDepth: 4200, _CloudRaySteps: 96,
        _CloudFormationEnabled: 1, _CloudFormationScale: 24000, _CloudEdgeWidth: 60 };
    for (const [key, value] of Object.entries(fields)) {
        const pattern = new RegExp(`^    - ${key}:.*$`, 'm');
        if (pattern.test(source)) source = source.replace(pattern, `    - ${key}: ${value}`);
        else if (source.includes('    m_Floats: []')) source = source.replace('    m_Floats: []', `    m_Floats:\n    - ${key}: ${value}`);
        else source = source.replace('    m_Floats:\n', `    m_Floats:\n    - ${key}: ${value}\n`);
    }
    fs.writeFileSync(path, source);
    reports.push({ path, preserved: '기존 팔레트·노이즈·바람·구름량·GUID', changed: ['_CloudFormation', '_CloudDepth', ...Object.keys(fields)] });
}
for (const path of fs.readFileSync(root + 'profiles.txt', 'utf8').trim().split(/\r?\n/)) {
    let source = normalize(fs.readFileSync(root + 'backup/' + guidAt(path) + '.asset', 'utf8')).trimEnd() + '\n';
    const fields = { cloudDepth: 4200, cloudFormationScale: 24000, cloudEdgeWidth: 60 };
    for (const [key, value] of Object.entries(fields)) {
        const pattern = new RegExp(`^  ${key}:.*$`, 'm');
        source = pattern.test(source) ? source.replace(pattern, `  ${key}: ${value}`) : source + `  ${key}: ${value}\n`;
    }
    fs.writeFileSync(path, source);
    reports.push({ path, preserved: '기존 지역 설정·그라데이션·노이즈 저작값·GUID', changed: Object.keys(fields) });
}
fs.writeFileSync(root + 'asset-diff-audit.json', JSON.stringify(reports, null, 2));
console.log(JSON.stringify({ materials: 2, profiles: 5, formationGuid }));
