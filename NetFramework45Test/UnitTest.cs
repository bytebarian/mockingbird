using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mockingbird;

namespace NetFramework45Test
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void TestMethod()
        {
            MockEngine.Initialize();

            Func<int, int, int> act = (a, b) =>
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

            var methodInfo = typeof(Test).GetMethod("GetLargerNumber");
            MockEngine.Mock(methodInfo, act);

            var test = new Test();
            var result = test.GetLargerNumber(1, 2);

            Assert.AreEqual(1, result);
        }
    }

    public class Test
    {
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
    }
}
