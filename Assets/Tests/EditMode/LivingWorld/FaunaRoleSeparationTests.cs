using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace UPlayGround.Core.Tests
{
    /// <summary>중첩 CPU 서식지 소유권과 미준비 GPU 그룹의 등록 경계를 검사한다.</summary>
    public sealed class FaunaRoleSeparationTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly List<object> _roots = new();
        private static Type GameObjectType => Type.GetType("UnityEngine.GameObject, UnityEngine.CoreModule", true);
        private static Type HabitatType => RuntimeType("WildlifeHabitat");
        private static Type CreatureType => RuntimeType("WildlifeCreature");
        private static Type RuntimeType(string name) => Type.GetType(
            "UPlayGround.Gameplay.World.LivingWorld." + name + ", Assembly-CSharp", true);

        private object CreateInactive(string name, object parent = null)
        {
            object gameObject = Activator.CreateInstance(GameObjectType, new object[] { name });
            GameObjectType.GetMethod("SetActive").Invoke(gameObject, new object[] { false });
            if (parent == null) _roots.Add(gameObject);
            else
            {
                object transform = GameObjectType.GetProperty("transform").GetValue(gameObject);
                object parentTransform = GameObjectType.GetProperty("transform").GetValue(parent);
                transform.GetType().GetProperty("parent").SetValue(transform, parentTransform);
            }
            return gameObject;
        }

        private static object Add(object gameObject, Type type) => GameObjectType
            .GetMethod("AddComponent", new[] { typeof(Type) }).Invoke(gameObject, new object[] { type });

        private static void InitializeHabitat(object habitat) => HabitatType
            .GetMethod("Awake", PrivateInstance).Invoke(habitat, null);

        private static Array ReadCreatures(object habitat) => (Array)HabitatType
            .GetField("_creatures", PrivateInstance).GetValue(habitat);

        [TearDown]
        public void Cleanup()
        {
            Type unityObject = Type.GetType("UnityEngine.Object, UnityEngine.CoreModule", true);
            var destroy = unityObject.GetMethod("DestroyImmediate", new[] { unityObject, typeof(bool) });
            for (int i = _roots.Count - 1; i >= 0; i--)
                destroy.Invoke(null, new[] { _roots[i], (object)false });
            _roots.Clear();
        }

        [Test]
        public void 중첩_CPU_서식지는_개체와_이웃_배열을_공유하지_않는다()
        {
            object parent = CreateInactive("FaunaTestParent");
            object outer = Add(parent, HabitatType);
            object outerCreature = Add(CreateInactive("OuterCreature", parent), CreatureType);
            object child = CreateInactive("FaunaTestChild", parent);
            object inner = Add(child, HabitatType);
            object innerCreature = Add(CreateInactive("InnerCreature", child), CreatureType);
            // 부모가 나중에 초기화돼도 자식의 바인딩을 덮어쓰지 않아야 한다.
            InitializeHabitat(inner);
            InitializeHabitat(outer);
            CollectionAssert.AreEqual(new[] { outerCreature }, ReadCreatures(outer));
            CollectionAssert.AreEqual(new[] { innerCreature }, ReadCreatures(inner));
            Assert.AreSame(inner, CreatureType.GetField("_habitat", PrivateInstance).GetValue(innerCreature));
            CollectionAssert.AreEqual(new[] { innerCreature },
                (Array)CreatureType.GetField("_neighbours", PrivateInstance).GetValue(innerCreature));
        }

        [Test]
        public void 중간_폴더의_비활성_CPU_개체도_직속_서식지가_소유한다()
        {
            object root = CreateInactive("FaunaTestRoot");
            object habitat = Add(root, HabitatType);
            object folder = CreateInactive("Folder", root);
            object creature = Add(CreateInactive("InactiveCreature", folder), CreatureType);
            InitializeHabitat(habitat);
            CollectionAssert.AreEqual(new[] { creature }, ReadCreatures(habitat));
        }

        [Test]
        public void 프로필_없는_GPU_그룹은_준비와_등록을_거절한다()
        {
            object root = CreateInactive("FaunaGpuTest");
            Type habitatType = RuntimeType("AmbientFaunaHabitat");
            Type groupType = RuntimeType("AmbientFaunaGroup");
            object habitat = Add(root, habitatType);
            object group = Add(CreateInactive("GpuGroup", root), groupType);
            object[] arguments = { null };
            Assert.IsFalse((bool)groupType.GetMethod("TryPrepare").Invoke(group, arguments));
            Assert.IsNotEmpty((string)arguments[0]);
            Assert.IsFalse((bool)groupType.GetProperty("IsReady").GetValue(group));
            Assert.IsFalse((bool)habitatType.GetMethod("TryRegister").Invoke(habitat, new[] { group }));
        }

        [Test]
        public void GPU_그룹은_하위_CPU_생물의_혼합을_거절한다()
        {
            object root = CreateInactive("FaunaMixedTest");
            Type groupType = RuntimeType("AmbientFaunaGroup");
            object group = Add(root, groupType);
            Add(CreateInactive("CpuCreature", root), CreatureType);
            object[] arguments = { null };
            Assert.IsFalse((bool)groupType.GetMethod("TryPrepare").Invoke(group, arguments));
            StringAssert.Contains("WildlifeCreature", (string)arguments[0]);
            Assert.IsFalse((bool)groupType.GetProperty("IsReady").GetValue(group));
        }
    }
}
