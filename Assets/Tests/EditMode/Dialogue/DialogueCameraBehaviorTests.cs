using NUnit.Framework;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data;

namespace UPlayGround.Dialogue.Tests
{
    public sealed class DialogueCameraBehaviorTests
    {
        private GameObject _cameraObject;
        private GameObject _speakerObject;
        private GameObject _listenerObject;
        private DialogueCameraSettingsSO _settings;

        [TearDown]
        public void TearDown()
        {
            if (_settings != null)
                Object.DestroyImmediate(_settings);
            if (_listenerObject != null)
                Object.DestroyImmediate(_listenerObject);
            if (_speakerObject != null)
                Object.DestroyImmediate(_speakerObject);
            if (_cameraObject != null)
                Object.DestroyImmediate(_cameraObject);
        }

        [Test]
        public void 먼_화자는_인게임_추적_대상_없이도_인트로_팬_대신_즉시_잡는다()
        {
            _cameraObject = new GameObject("DialogueCameraTest");
            Camera camera = _cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 2f, -8f);

            _speakerObject = new GameObject("Speaker");
            _speakerObject.transform.position = new Vector3(20f, 0f, 12f);
            _listenerObject = new GameObject("Listener");
            _listenerObject.transform.position = Vector3.zero;

            _settings = DialogueCameraSettingsSO.CreateRuntimeDefault();

            var session = new DialogueShotSession();
            session.Begin(
                new[] { _listenerObject.transform, _speakerObject.transform },
                camera.transform.position);

            var context = new CameraContext(new CameraState())
            {
                MainCamera = camera,
                DialogueSettings = _settings,
                DialogueSession = session,
                Target = null,
            };
            var director = new CameraDirector(context);
            director.Register(new DialogueCameraBehavior());
            director.SetMode(CameraModeType.Dialogue, new CameraModeEnterParams
            {
                HasDialogueShot = true,
                DialogueShot = new DialogueShotRequest
                {
                    Speaker = _speakerObject.transform,
                    Listener = _listenerObject.transform,
                    ShotType = DialogueShotType.OverTheShoulderSpeaker,
                    SequenceId = 1,
                },
            });

            Assert.That(director.CanEvaluatePose(null), Is.True);

            CameraPose pose = director.EvaluatePose(1f / 60f, default);
            Vector3 expectedLookAt = _speakerObject.transform.position
                                     + _settings.ResolvePreset(
                                         DialogueShotType.OverTheShoulderSpeaker).lookAtOffset;
            Assert.That(Vector3.Distance(pose.PivotPosition, expectedLookAt), Is.LessThan(0.001f));
        }
    }
}
