using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Manager;

namespace UPlayGround.Contracts.Tests
{
    public interface IServicesTestContract : IGameService
    {
    }

    public interface IServicesSecondaryTestContract : IGameService
    {
    }

    public sealed class ServicesTestService : IServicesTestContract
    {
    }

    public sealed class ServicesMultiContractTestService :
        IServicesTestContract,
        IServicesSecondaryTestContract
    {
    }

    public sealed class ServicesTestComponent : MonoBehaviour, IServicesTestContract
    {
    }

    public class ServicesTests
    {
        private GameObject _serviceObject;

        [SetUp]
        public void SetUp()
        {
            Services.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Services.Clear();
            if (_serviceObject != null)
                UnityEngine.Object.DestroyImmediate(_serviceObject);
        }

        [Test]
        public void Register_같은인스턴스재등록은_기존바인딩을유지한다()
        {
            var service = new ServicesTestService();

            Services.Register(service);
            Services.Register(service);

            Assert.That(Services.Get<IServicesTestContract>(), Is.SameAs(service));
        }

        [Test]
        public void Register_다른인스턴스가같은계약을등록하면_실패한다()
        {
            var registered = new ServicesTestService();
            var duplicate = new ServicesTestService();
            Services.Register(registered);

            Assert.That(
                () => Services.Register(duplicate),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(Services.Get<IServicesTestContract>(), Is.SameAs(registered));
        }

        [Test]
        public void Register_계약하나가충돌하면_다른계약도부분등록하지않는다()
        {
            var registered = new ServicesTestService();
            var duplicate = new ServicesMultiContractTestService();
            Services.Register(registered);

            Assert.That(
                () => Services.Register(duplicate),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(Services.TryGet<IServicesSecondaryTestContract>(out _), Is.False);
        }

        [Test]
        public void Get_파괴된Unity서비스는_null을반환하고바인딩을제거한다()
        {
            _serviceObject = new GameObject(nameof(ServicesTestComponent));
            var destroyed = _serviceObject.AddComponent<ServicesTestComponent>();
            Services.Register(destroyed);
            UnityEngine.Object.DestroyImmediate(_serviceObject);
            _serviceObject = null;

            LogAssert.Expect(
                LogType.Warning,
                new Regex("\\[Services\\] 등록되지 않은 서비스 계약 요청:"));
            Assert.That(Services.Get<IServicesTestContract>(), Is.Null);

            var replacement = new ServicesTestService();
            Services.Register(replacement);
            Assert.That(Services.Get<IServicesTestContract>(), Is.SameAs(replacement));
        }

        [Test]
        public void Unregister_등록되지않은인스턴스는_기존바인딩을해제하지않는다()
        {
            var registered = new ServicesTestService();
            var unrelated = new ServicesTestService();
            Services.Register(registered);

            Services.Unregister(unrelated);

            Assert.That(Services.Get<IServicesTestContract>(), Is.SameAs(registered));
        }
    }
}
