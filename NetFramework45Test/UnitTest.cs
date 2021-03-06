﻿using System;
using System.Reflection;
using Mockingbird;
using NUnit.Framework;

namespace NetFramework45Test
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

            Assert.AreEqual(2, result);

            var methodInfo = typeof(Test).GetMethod("GetLargerNumber");
            MockEngine.Mock(methodInfo, act);

            var testAfter = new Test();
            var resultAfter = testAfter.GetLargerNumber(1, 2);

            Assert.AreEqual(1, resultAfter);
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
}
