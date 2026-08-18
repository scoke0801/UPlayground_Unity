using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data;

namespace UPlayGround.Dialogue.Tests
{
    /// <summary>
    /// 3인 이상 대화에서 카메라가 세션 고정 반평면을 벗어나지 않는지 검증한다.
    /// 가상선은 활성 pair마다 다시 잡히지만 카메라가 머무는 쪽은 세션이 소유한다는 계약이 핵심이다.
    /// </summary>
    public sealed class DialogueShotSessionTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    Object.DestroyImmediate(_spawned[i]);
            }

            _spawned.Clear();
        }

        private Transform CreateActor(string name, Vector3 position)
        {
            var actor = new GameObject(name);
            actor.transform.position = position;
            _spawned.Add(actor);
            return actor.transform;
        }

        /// <summary>카메라가 축 기준 어느 쪽에 있는지. 부호가 같으면 같은 반평면이다.</summary>
        private static float CameraSideOf(DialogueShotSession session)
        {
            Vector3 side = session.AxisRight * session.SideSign;
            return Vector3.Dot(side, session.StageRight);
        }

        [Test]
        public void 활성_pair가_바뀌어도_카메라는_세션_반평면에_남는다()
        {
            Transform player = CreateActor("Player", Vector3.zero);
            Transform npcA = CreateActor("NpcA", new Vector3(3f, 0f, 0f));
            Transform npcB = CreateActor("NpcB", new Vector3(0f, 0f, 3f));

            var session = new DialogueShotSession();
            session.Begin(new[] { player, npcA, npcB }, new Vector3(-5f, 2f, -5f));

            Assert.That(CameraSideOf(session), Is.GreaterThan(0f), "세션 시작 시 카메라 쪽이 StageRight와 같아야 한다.");

            // 축을 90° 돌리는 pair 교체
            session.SetActivePair(player, npcB);
            Assert.That(CameraSideOf(session), Is.GreaterThan(0f), "pair가 바뀌어도 카메라는 같은 반평면에 남아야 한다.");

            // 플레이어를 빼고 NPC끼리 주고받는 축
            session.SetActivePair(npcA, npcB);
            Assert.That(CameraSideOf(session), Is.GreaterThan(0f), "플레이어가 빠진 pair에서도 반평면이 유지돼야 한다.");
        }

        [Test]
        public void 세_인물_pair_순환_전체에서_반평면이_일관된다()
        {
            Transform a = CreateActor("A", new Vector3(-2f, 0f, 0f));
            Transform b = CreateActor("B", new Vector3(2f, 0f, 0f));
            Transform c = CreateActor("C", new Vector3(0f, 0f, 3.5f));

            var session = new DialogueShotSession();
            session.Begin(new[] { a, b, c }, new Vector3(0f, 2f, -6f));

            var pairs = new[]
            {
                (a, b), (a, c), (b, c), (c, a), (b, a), (c, b)
            };

            foreach ((Transform subject, Transform partner) in pairs)
            {
                session.SetActivePair(subject, partner);
                Assert.That(
                    CameraSideOf(session),
                    Is.GreaterThan(0f),
                    $"{subject.name}-{partner.name} 축에서 카메라가 반평면을 넘었다.");
            }
        }

        [Test]
        public void 화자와_청자가_뒤바뀐_리버스_샷은_축_전환으로_보지_않는다()
        {
            Transform player = CreateActor("Player", Vector3.zero);
            Transform npc = CreateActor("Npc", new Vector3(0f, 0f, 3f));

            var session = new DialogueShotSession();
            session.Begin(new[] { player, npc }, new Vector3(-5f, 2f, 0f));

            float changeAngle = session.SetActivePair(npc, player);

            Assert.That(changeAngle, Is.EqualTo(0f), "리버스 샷은 같은 가상선이므로 확립 전환을 유발하면 안 된다.");
        }

        [Test]
        public void 일렬로_선_배치에서_pair가_바뀌어도_축_전환으로_보지_않는다()
        {
            // 세 인물이 한 직선 위에 있으면 어떤 pair를 잡아도 가상선은 같은 선이다.
            Transform a = CreateActor("A", new Vector3(0f, 0f, 0f));
            Transform b = CreateActor("B", new Vector3(0f, 0f, 3f));
            Transform c = CreateActor("C", new Vector3(0f, 0f, 6f));

            var session = new DialogueShotSession();
            session.Begin(new[] { a, b, c }, new Vector3(-5f, 2f, 3f));

            float changeAngle = session.SetActivePair(b, c);

            Assert.That(changeAngle, Is.LessThan(1f), "같은 직선 위의 pair 교체는 축 전환이 아니다.");
        }

        [Test]
        public void 직교하는_pair_교체는_축_전환_각도를_보고한다()
        {
            Transform player = CreateActor("Player", Vector3.zero);
            Transform npcA = CreateActor("NpcA", new Vector3(0f, 0f, 3f));
            Transform npcB = CreateActor("NpcB", new Vector3(3f, 0f, 0f));

            var session = new DialogueShotSession();
            session.Begin(new[] { player, npcA, npcB }, new Vector3(-5f, 2f, 0f));

            float changeAngle = session.SetActivePair(player, npcB);

            Assert.That(changeAngle, Is.EqualTo(90f).Within(0.01f));
        }

        [Test]
        public void 같은_pair_재설정은_축_전환을_보고하지_않는다()
        {
            Transform player = CreateActor("Player", Vector3.zero);
            Transform npc = CreateActor("Npc", new Vector3(0f, 0f, 3f));

            var session = new DialogueShotSession();
            session.Begin(new[] { player, npc }, new Vector3(-5f, 2f, 0f));

            Assert.That(session.SetActivePair(player, npc), Is.EqualTo(0f));
        }

        [Test]
        public void 참여자는_중복_등록되지_않고_무게중심에_반영된다()
        {
            Transform a = CreateActor("A", new Vector3(-3f, 0f, 0f));
            Transform b = CreateActor("B", new Vector3(3f, 0f, 0f));
            Transform c = CreateActor("C", new Vector3(0f, 0f, 6f));

            var session = new DialogueShotSession();
            session.Begin(new[] { a, b }, new Vector3(0f, 2f, -6f));

            Assert.That(session.Center.magnitude, Is.LessThan(0.001f));

            session.RegisterParticipant(c);
            session.RegisterParticipant(c);

            Assert.That(session.Participants.Count, Is.EqualTo(3));
            Assert.That(session.Center.z, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void 플레이어가_없는_대화도_가상선을_확보한다()
        {
            Transform npcA = CreateActor("NpcA", new Vector3(-2f, 0f, 0f));
            Transform npcB = CreateActor("NpcB", new Vector3(2f, 0f, 0f));

            var session = new DialogueShotSession();
            session.Begin(new[] { npcA, npcB }, new Vector3(0f, 2f, -6f));

            Assert.That(session.HasActivePair, Is.True);
            Assert.That(session.HasAxis, Is.True);
        }
    }

    /// <summary>
    /// 축 전환이 컷이 아니라 확립 전환으로 처리되는지, 저작 오버라이드가 그 규칙을 이기는지 검증한다.
    /// </summary>
    public sealed class DialogueShotDirectorAxisChangeTests
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

        private Transform CreateActor(string name, Vector3 position)
        {
            var actor = new GameObject(name);
            actor.transform.position = position;
            _spawned.Add(actor);
            return actor.transform;
        }

        /// <summary>축이 90° 돈 상태의 세션을 만든다. LineIndex를 올려 첫 라인 취급을 피한다.</summary>
        private DialogueShotSession BuildRotatedSession(
            out Transform player, out Transform npcA, out Transform npcB)
        {
            player = CreateActor("Player", Vector3.zero);
            npcA = CreateActor("NpcA", new Vector3(0f, 0f, 3f));
            npcB = CreateActor("NpcB", new Vector3(3f, 0f, 0f));

            var session = new DialogueShotSession();
            session.Begin(new[] { player, npcA, npcB }, new Vector3(-5f, 2f, 0f));
            session.LineIndex = 1;
            session.SetActivePair(player, npcB);

            return session;
        }

        [Test]
        public void 축이_크게_바뀐_라인은_컷_대신_확립_전환이_된다()
        {
            DialogueShotSession session = BuildRotatedSession(
                out Transform player, out Transform _, out Transform npcB);

            var request = new DialogueShotRequest
            {
                Speaker = npcB,
                Listener = player,
                SequenceId = 2
            };

            DialogueShotDirector.Decision decision =
                DialogueShotDirector.Decide(_settings, session, request);

            Assert.That(decision.Transition, Is.EqualTo(DialogueShotTransition.Establish));
        }

        [Test]
        public void 축_전환_정책이_None이면_기존_규칙을_따른다()
        {
            _settings.axisChangePolicy = DialogueAxisChangePolicy.None;

            DialogueShotSession session = BuildRotatedSession(
                out Transform player, out Transform _, out Transform npcB);

            var request = new DialogueShotRequest
            {
                Speaker = npcB,
                Listener = player,
                SequenceId = 2
            };

            DialogueShotDirector.Decision decision =
                DialogueShotDirector.Decide(_settings, session, request);

            Assert.That(decision.Transition, Is.Not.EqualTo(DialogueShotTransition.Establish));
        }

        [Test]
        public void 노드가_전환을_지정하면_축_전환_규칙보다_우선한다()
        {
            DialogueShotSession session = BuildRotatedSession(
                out Transform player, out Transform _, out Transform npcB);

            var request = new DialogueShotRequest
            {
                Speaker = npcB,
                Listener = player,
                Transition = DialogueShotTransition.Cut,
                SequenceId = 2
            };

            DialogueShotDirector.Decision decision =
                DialogueShotDirector.Decide(_settings, session, request);

            Assert.That(decision.Transition, Is.EqualTo(DialogueShotTransition.Cut));
        }

        [Test]
        public void EstablishWide_정책은_축_전환_라인의_구도를_와이드로_올린다()
        {
            _settings.axisChangePolicy = DialogueAxisChangePolicy.EstablishWide;

            DialogueShotSession session = BuildRotatedSession(
                out Transform player, out Transform _, out Transform npcB);

            var request = new DialogueShotRequest
            {
                Speaker = npcB,
                Listener = player,
                SequenceId = 2
            };

            DialogueShotDirector.Decision decision =
                DialogueShotDirector.Decide(_settings, session, request);

            Assert.That(decision.Shot, Is.EqualTo(DialogueShotType.Wide));
        }

        [Test]
        public void 노드가_구도를_지정하면_와이드_승격보다_우선한다()
        {
            _settings.axisChangePolicy = DialogueAxisChangePolicy.EstablishWide;

            DialogueShotSession session = BuildRotatedSession(
                out Transform player, out Transform _, out Transform npcB);

            var request = new DialogueShotRequest
            {
                Speaker = npcB,
                Listener = player,
                ShotType = DialogueShotType.Closeup,
                SequenceId = 2
            };

            DialogueShotDirector.Decision decision =
                DialogueShotDirector.Decide(_settings, session, request);

            Assert.That(decision.Shot, Is.EqualTo(DialogueShotType.Closeup));
        }
    }
}
