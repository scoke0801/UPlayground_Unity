using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Components;
using UPlayGround.Data.Cinematic;
using UPlayGround.Manager;

namespace UPlayGround.Cinematic.Tests
{
    public sealed class CinematicStageCoreTests
    {
        [Test]
        public void Ticket_Zero만_무효다()
        {
            Assert.That(default(CinematicStageTicket).IsValid, Is.False);
            Assert.That(new CinematicStageTicket(10).IsValid, Is.True);
            Assert.That(
                new CinematicStageTicket(10),
                Is.EqualTo(new CinematicStageTicket(10)));
        }

        [Test]
        public void TargetSize_높이_경계에_따라_분류한다()
        {
            var stage = ScriptableObject.CreateInstance<CinematicStageSO>();
            stage.smallHeight = 1.2f;
            stage.largeHeight = 3.5f;
            stage.giantHeight = 7f;

            Assert.That(stage.ClassifyTarget(1f), Is.EqualTo(UltimateTargetSize.Small));
            Assert.That(stage.ClassifyTarget(2f), Is.EqualTo(UltimateTargetSize.Medium));
            Assert.That(stage.ClassifyTarget(4f), Is.EqualTo(UltimateTargetSize.Large));
            Assert.That(stage.ClassifyTarget(8f), Is.EqualTo(UltimateTargetSize.Giant));

            Object.DestroyImmediate(stage);
        }

        [Test]
        public void ActorPresentation_중첩_숨김이_끝나면_기존_상태를_복구한다()
        {
            var actor = new GameObject("Actor");
            var visibleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visibleObject.transform.SetParent(actor.transform);
            var hiddenObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hiddenObject.transform.SetParent(actor.transform);
            Renderer visible = visibleObject.GetComponent<Renderer>();
            Renderer hidden = hiddenObject.GetComponent<Renderer>();
            hidden.enabled = false;
            ActorPresentation presentation = actor.AddComponent<ActorPresentation>();

            presentation.Hide();
            presentation.Hide();
            Assert.That(visible.enabled, Is.True);
            Assert.That(hidden.enabled, Is.False);
            Assert.That(visible.forceRenderingOff, Is.True);
            Assert.That(hidden.forceRenderingOff, Is.True);

            presentation.Show();
            Assert.That(visible.forceRenderingOff, Is.True);

            presentation.Show();
            Assert.That(visible.enabled, Is.True);
            Assert.That(hidden.enabled, Is.False);
            Assert.That(visible.forceRenderingOff, Is.False);
            Assert.That(hidden.forceRenderingOff, Is.False);

            Object.DestroyImmediate(actor);
        }

        [Test]
        public void ActorPresentation_숨김중_논리가시성과_새렌더러를_보존한다()
        {
            var actor = new GameObject("Actor");
            var originalObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            originalObject.transform.SetParent(actor.transform);
            Renderer original = originalObject.GetComponent<Renderer>();
            ActorPresentation presentation = actor.AddComponent<ActorPresentation>();

            presentation.Hide();
            original.enabled = false;

            var addedObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            addedObject.transform.SetParent(actor.transform);
            Renderer added = addedObject.GetComponent<Renderer>();
            Assert.That(added.forceRenderingOff, Is.True);

            presentation.Show();
            Assert.That(original.enabled, Is.False);
            Assert.That(original.forceRenderingOff, Is.False);
            Assert.That(added.enabled, Is.True);
            Assert.That(added.forceRenderingOff, Is.False);

            Object.DestroyImmediate(actor);
        }

        [TestCase(CameraModeType.Dialogue, true)]
        [TestCase(CameraModeType.DialogueCameraReplay, true)]
        [TestCase(CameraModeType.InGame, false)]
        [TestCase(CameraModeType.Free, false)]
        [TestCase(CameraModeType.Cinematic, false)]
        [TestCase(CameraModeType.CameraSnapshotSequence, false)]
        public void ActorCameraProximityDither_대화_카메라에서만_중지한다(
            CameraModeType cameraMode,
            bool expected)
        {
            MethodInfo method = typeof(ActorCameraProximityDither).GetMethod(
                "IsDialogueCameraMode",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            Assert.That(method.Invoke(null, new object[] { cameraMode }), Is.EqualTo(expected));
        }

    }
}
