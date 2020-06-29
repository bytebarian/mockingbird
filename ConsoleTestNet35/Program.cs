using Mockingbird;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ConsoleTestNet35
{
    class Program
    {
        static void Main(string[] args)
        {
            MockEngine.Initialize();

            var test = new Test();
            test.GenericMethodTest();

            Console.ReadKey();
        }
    }

    public class Test
    {

        public void GenericMethodTest()
        {
            Type type = this.GetType();
            MethodInfo destMethodInfo = type.GetMethod("GenericMethodToBeReplaced", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo srcMethodInfo = type.GetMethod("GenericMethodSourceILCodeToBeCopiedFrom", BindingFlags.NonPublic | BindingFlags.Instance);

            byte[] ilCodes = srcMethodInfo.GetMethodBody().GetILAsByteArray();

            Console.WriteLine(string.Format(@"Generic methods are most complicated, see the article for details.

{0}
-----------------------------

"
                , GenericMethodToBeReplaced<string, int>("11", 2)
                ));


            MockEngine.Mock(destMethodInfo, ilCodes);

            Console.WriteLine(string.Format(@"After updating the IL Code which is copied from another generic method.

{0}

{1}
-----------------------------
"
                , GenericMethodToBeReplaced<string, int>("11", 2)
                , GenericMethodToBeReplaced<long, int>(1, 2)
                ));
        }

        protected string GenericMethodToBeReplaced<T, K>(T t, K k)
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

        protected string GenericMethodSourceILCodeToBeCopiedFrom<T, K>(T t, K k)
        {
            return string.Format("Modifed generic method is being called! Type 1 = {0}; Type 2 = {1}"
                , typeof(T).FullName
                , typeof(K).FullName
                );
        }
    }
}
