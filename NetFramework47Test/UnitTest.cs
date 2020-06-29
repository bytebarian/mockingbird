using Mockingbird;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NetFramework47Test
{
    [TestFixture]
    public class UnitTest
    {
        public Func<int, int, int> act;

        [SetUp]
        public void Setup()
        {
            MockEngine.Initialize();

            act = (a, b) =>
            {
                if (a > b)
                {
                    return b;
                }
                else
                {
                    return a;
                }
            };
        }

        [Test]
        public void PublicMethodTest()
        {
            var test = new Test();
            var result = test.GetLargerNumber(1, 2);

            NUnit.Framework.Assert.AreEqual(2, result);

            var methodInfo = typeof(Test).GetMethod("GetLargerNumber");
            MockEngine.Mock(methodInfo, act);

            var testAfter = new Test();
            var resultAfter = testAfter.GetLargerNumber(1, 2);

            NUnit.Framework.Assert.AreEqual(1, resultAfter);
        }

        [Test]
        public void VirtualMethodTest()
        {
            var test = new Test();
            var result = test.GetLargerNumberVirtual(1, 2);

            Assert.AreEqual(2, result);

            var methodInfo = typeof(Test).GetMethod("GetLargerNumberVirtual");
            MockEngine.Mock(methodInfo, act);

            var testAfter = new Test();
            var resultAfter = testAfter.GetLargerNumberVirtual(1, 2);

            Assert.AreEqual(1, resultAfter);
        }

        [Test]
        public void StaticMethodTest()
        {
            var result = Test.GetLargerNumberStatic(1, 2);

            Assert.AreEqual(2, result);

            var methodInfo = typeof(Test).GetMethod("GetLargerNumberStatic", BindingFlags.Public | BindingFlags.Static);
            var mock = typeof(UnitTest).GetMethod("GetLargerNumberStatic", BindingFlags.Public | BindingFlags.Static);
            MockEngine.Mock(methodInfo, mock);

            var resultAfter = Test.GetLargerNumberStatic(1, 2);

            Assert.AreEqual(1, resultAfter);
        }

        [Test]
        public void PrivateMethodTest()
        {
            var test = new Test();
            var result = test.GetLargerNumberPrivateProxy(1, 2);

            Assert.AreEqual(2, result);

            var methodInfo = typeof(Test).GetMethod("GetLargerNumberPrivte", BindingFlags.NonPublic | BindingFlags.Instance);
            MockEngine.Mock(methodInfo, act);

            var testAfter = new Test();
            var resultAfter = testAfter.GetLargerNumberPrivateProxy(1, 2);

            Assert.AreEqual(1, resultAfter);
        }

        [Test]
        public void OverridedMethodTest()
        {
            var test = new ConcreteTest();
            var result = test.GetLargerNumberVirtual(1, 2);

            Assert.AreEqual(2, result);

            var methodInfo = typeof(ConcreteTest).GetMethod("GetLargerNumberVirtual");
            MockEngine.Mock(methodInfo, act);

            var testAfter = new Test();
            var resultAfter = testAfter.GetLargerNumberVirtual(1, 2);

            var concreteTest = new ConcreteTest();
            var concreteResult = concreteTest.GetLargerNumberVirtual(1, 2);

            Assert.AreEqual(2, resultAfter);
            Assert.AreEqual(1, concreteResult);
        }

        [Test]
        public void ProperyGetTest()
        {
            var test = new Test();
            test.SetNumber(5);

            Assert.AreEqual(5, test.NextNumber);

            Func<int> func = () => 10;

            var methodInfo = typeof(Test).GetProperty("NextNumber").GetGetMethod();
            MockEngine.Mock(methodInfo, func);

            var testAfter = new Test();

            Assert.AreEqual(10, testAfter.NextNumber);
        }
        [Test]
        public void ConstructorTest()
        {
            var test = new Test();

            Assert.AreEqual(5, test.Number);

            Action func = () => { };

            var constructor = typeof(Test).GetConstructor(Type.EmptyTypes);
            MockEngine.Mock(constructor, func);

            var testAfter = new Test();

            Assert.AreEqual(0, testAfter.Number);
        }
    }

    public class Test
    {
        public int Number { get; private set; }
        public int NextNumber { get; private set; }

        public Test()
        {
            Number = 5;
        }

        public void SetNumber(int number)
        {
            NextNumber = number;
        }

        public int GetLargerNumber(int a, int b)
        {
            if (a < b)
            {
                return b;
            }
            else
            {
                return a;
            }
        }

        public virtual int GetLargerNumberVirtual(int a, int b)
        {
            if (a < b)
            {
                return b;
            }
            else
            {
                return a;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int GetLargerNumberStatic(int a, int b)
        {
            if (a < b)
            {
                return b;
            }
            else
            {
                return a;
            }
        }

        private int GetLargerNumberPrivte(int a, int b)
        {
            if (a < b)
            {
                return b;
            }
            else
            {
                return a;
            }
        }

        public int GetLargerNumberPrivateProxy(int a, int b)
        {
            return GetLargerNumberPrivte(a, b);
        }

        public string GetLargerObject<T>(T a, T b) where T : IComparable
        {
            if (a.CompareTo(b) < 0)
            {
                return b.ToString();
            }
            else
            {
                return a.ToString();
            }
        }

        public string GenericMethodToBeReplaced<T, K>(T t, K k)
        {
            int a = 1;
            int b = 2;
            if (a > b)
                a = b;
            else
                b = a;

            return string.Format("Original generic method is being called!"
                , typeof(T).FullName
                , typeof(K).FullName
                );
        }

        public string GenericMethodSourceILCodeToBeCopiedFrom<T, K>(T t, K k)
        {
            return string.Format("Modifed generic method is being called! Type 1 = {0}; Type 2 = {1}"
                , typeof(T).FullName
                , typeof(K).FullName
                );
        }
    }

    public class ConcreteTest : Test
    {
        public override int GetLargerNumberVirtual(int a, int b)
        {
            if (a < b)
            {
                return b;
            }
            else
            {
                return a;
            }
        }
    }

    public class Test<T> where T : IComparable
    {
        public T GetLargerObject(T a, T b)
        {
            if (a.CompareTo(b) < 0)
            {
                return b;
            }
            else
            {
                return a;
            }
        }
    }

    public class TestSingleton
    {
        private static TestSingleton Instance;
        public int Number { get; private set; }

        private TestSingleton()
        {
            Number = 5;
        }

        public static TestSingleton GetInstance()
        {
            if (Instance == null) Instance = new TestSingleton();
            return Instance;
        }
    }
}
