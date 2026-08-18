using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.Dialogue.Tests
{
    public sealed class DialogueCameraSettingsTests
    {
        private DialogueCameraSettingsSO _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = DialogueCameraSettingsSO.CreateRuntimeDefault();
        }

        [TearDown]
        public void TearDown()
        {
            if (_settings != null)
                Object.DestroyImmediate(_settings);
        }

        [Test]
        public void 투샷은_거리_상한_안의_두_인물을_프레이밍할_수_있다()
        {
            DialogueShotPreset preset = _settings.ResolvePreset(DialogueShotType.TwoShot);

            Assert.That(_settings.CanFrameBothActors(preset, 4f), Is.True);
        }

        [Test]
        public void 투샷은_요구_거리가_상한을_넘으면_프레이밍할_수_없다()
        {
            DialogueShotPreset preset = _settings.ResolvePreset(DialogueShotType.TwoShot);

            Assert.That(_settings.CanFrameBothActors(preset, 8f), Is.False);
        }

        [Test]
        public void 거리_오버라이드가_상한을_넘어도_프레이밍할_수_없다()
        {
            DialogueShotPreset preset = _settings.ResolvePreset(DialogueShotType.Wide);

            Assert.That(_settings.CanFrameBothActors(preset, 2f, 10f), Is.False);
        }
    }
}
