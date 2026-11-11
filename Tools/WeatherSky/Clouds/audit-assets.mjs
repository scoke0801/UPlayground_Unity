import fs from 'node:fs';
import assert from 'node:assert/strict';

// 기존 팔레트·GUID를 작업 전 사본에서 보존하고 이번 볼륨 필드만 반영한다.
const backup = 'output/WeatherSky/Clouds/backup/';
const normalize = value => value.replaceAll('\r\n', '\n');
const guidAt = path => fs.readFileSync(path + '.meta', 'utf8').match(/guid: (\w+)/)[1];
const volumeGuid = guidAt('Assets/10.Datas/World/Sky/CloudVolume.asset');
const reports = [];
for (const path of fs.readFileSync('output/WeatherSky/Clouds/materials.txt', 'utf8').trim().split(/\r?\n/)) {
    let source = normalize(fs.readFileSync(backup + guidAt(path) + '.mat', 'utf8'));
    assert.ok(!source.includes('    - _CloudVolume:'), '볼륨 적용 전 사본이어야 한다');
    source = source.replace('    m_Ints:', `    - _CloudVolume:\n        m_Texture: {fileID: 11700000, guid: ${volumeGuid}, type: 2}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}\n    m_Ints:`);
    const fields = { _CloudVolumeEnabled: 1, _CloudDepth: 1600, _CloudDensity: 2.4, _CloudErosion: 0.6,
        _CloudRaySteps: 48, _CloudMaxDistance: 90000, _CloudForwardScattering: 0.65 };
    source = source.replace('    m_Floats:\n', '    m_Floats:\n' + Object.entries(fields).map(([key, value]) => `    - ${key}: ${value}\n`).join(''));
    fs.writeFileSync(path, source);
    reports.push({ path, preserved: '기존 팔레트·텍스처·운송·GUID', changed: ['_CloudVolume', ...Object.keys(fields)] });
}
for (const path of fs.readFileSync('output/WeatherSky/Clouds/profiles.txt', 'utf8').trim().split(/\r?\n/)) {
    let source = normalize(fs.readFileSync(backup + guidAt(path) + '.asset', 'utf8')).trimEnd() + '\n';
    const fields = { cloudDepth: 1600, cloudDensity: 2.4, cloudErosion: 0.6 };
    for (const [key, value] of Object.entries(fields)) {
        const pattern = new RegExp(`^  ${key}:.*$`, 'm');
        source = pattern.test(source) ? source.replace(pattern, `  ${key}: ${value}`) : source + `  ${key}: ${value}\n`;
    }
    fs.writeFileSync(path, source);
    reports.push({ path, preserved: '기존 그라데이션·구름량·운송·지역 설정·GUID', changed: Object.keys(fields) });
}
fs.writeFileSync('output/WeatherSky/Clouds/asset-diff-audit.json', JSON.stringify(reports, null, 2));
console.log(JSON.stringify({ materials: 2, profiles: 5, unrelatedValues: '작업 전 사본으로 보존' }));
