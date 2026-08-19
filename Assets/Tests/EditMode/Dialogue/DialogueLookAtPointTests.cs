using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data;

namespace UPlayGround.Dialogue.Tests
{
    /// <summary>특정 포인트 주시가 대화 구도 위치를 흔들지 않고 시선만 바꾸는지 검증한다.</summary>
    public sealed class DialogueLookAtPointTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
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

            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    Object.DestroyImmediate(_spawned[i]);
            }

            _spawned.Clear();
        }

        [Test]
        public void 특정_지점_주시는_카메라_위치를_유지하고_회전만_바꾼다()
        {
            Transform speaker = CreatePoint("Speaker", new Vector3(0f, 0f, 3f));
            Transform listener = CreatePoint("Listener", Vector3.zero);
            Transform lookAtPoint = CreatePoint("GroundPoint", new Vector3(1f, 0f, 1f));

            var session = new DialogueShotSession();
            session.Begin(new[] { listener, speaker }, new Vector3(-4f, 2f, -4f));

            var context = new CameraContext(new CameraState());
            var baseRequest = new DialogueShotRequest
            {
                Speaker = speaker,
                Listener = listener,
                ShotType = DialogueShotType.OverTheShoulderSpeaker,
                SequenceId = 1
            };

            DialogueShotComposer.FramedPose basePose = DialogueShotComposer.Compose(
                context,
                _settings,
                session,
                baseRequest,
                DialogueShotType.OverTheShoulderSpeaker,
                useCollision: false);

            var pointRequest = baseRequest;
            pointRequest.LookAtTarget = lookAtPoint;
            pointRequest.LookAtWorldOffset = new Vector3(0f, 0.2f, 0f);

            DialogueShotComposer.FramedPose pointPose = DialogueShotComposer.Compose(
                context,
                _settings,
                session,
                pointRequest,
                DialogueShotType.OverTheShoulderSpeaker,
                useCollision: false);

            Vector3 expectedLookAt = lookAtPoint.position + pointRequest.LookAtWorldOffset;
            Vector3 expectedForward = (expectedLookAt - pointPose.Position).normalized;

            Assert.That(Vector3.Distance(pointPose.Position, basePose.Position), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(pointPose.LookAt, expectedLookAt), Is.LessThan(0.001f));
            Assert.That(Vector3.Angle(pointPose.Rotation * Vector3.forward, expectedForward), Is.LessThan(0.01f));
        }

        [Test]
        public void 첫_라인의_특정_지점_주시는_대화_인트로를_재생하지_않는다()
        {
            _settings.enableIntroSequence = true;

            Transform speaker = CreatePoint("Speaker", new Vector3(0f, 0f, 2f));
            Transform listener = CreatePoint("Listener", Vector3.zero);
            Transform lookAtPoint = CreatePoint("GroundPoint", new Vector3(0f, 0f, 1f));

            var session = new DialogueShotSession();
            session.Begin(new[] { listener, speaker }, new Vector3(-4f, 2f, -4f));

            var request = new DialogueShotRequest
            {
                Speaker = speaker,
                Listener = listener,
                LookAtTarget = lookAtPoint,
                SequenceId = 1
            };

            DialogueShotDirector.Decision decision =
                DialogueShotDirector.Decide(_settings, session, request);

            Assert.That(decision.PlayIntro, Is.False);
        }

        private Transform CreatePoint(string name, Vector3 position)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.position = position;
            _spawned.Add(gameObject);
            return gameObject.transform;
        }
    }
}
