﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace Mockingbird
{
    public static class MockEngine
    {
        public enum Status : int
        {
            Ready = 1,
            Uninitialized = 0,
            Error_HookCompileMethodFailed = -1,
            Error_LoadedMethodDescIteratorInitializationFailed = -2,
            Error_MethodDescInitializationFailed = -3,
            Error_DbgHelpNotFound = -4,
            Error_JITNotFound = -5,
            Error_DownloadPDBFailed = -6,
            Error_CLRNotFound = -7,
        }

        #region P/Invoke
        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibraryW(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        #endregion

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool UpdateILCodesDelegate(IntPtr pMethodTable, IntPtr pMethodHandle, int md, IntPtr pBuffer, int dwSize);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate Status GetStatusDelegate();

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate Status WaitForIntializationCompletionDelegate();

        private static IntPtr _moduleHandle;
        private static UpdateILCodesDelegate _updateILCodesMethod;
        private static GetStatusDelegate _getStatusDelegate;
        private static WaitForIntializationCompletionDelegate _waitForIntializationCompletionDelegate;
        private delegate void InitializationCompletedDelegate(Status status);
        private static ManualResetEvent _mre = new ManualResetEvent(false);

        public static void Initialize()
        {
            InternalInitialize();

            Thread thread = new Thread(WaitForInitialization);
            thread.Start();

            _mre.WaitOne();
        }

        private static void WaitForInitialization()
        {
            var status = WaitForIntializationCompletion();
            var del = new InitializationCompletedDelegate(InitializationCompleted);
            del(status);
        }

        private static void InitializationCompleted(Status status)
        {
            _mre.Set();
        }

        private static void InternalInitialize()
        {
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            currentDir = Regex.Replace(currentDir, @"^(file\:\\)", string.Empty);

            // Environment.Is64BitProcess
            string path = Path.Combine(currentDir, (IntPtr.Size == 8) ? "Injection64.dll" : "Injection32.dll");

#if DEBUG
            Debug.WriteLine("The injection.dll is only designed for .Net release mode process. it is not supposed to be used for debug mode.");
#endif

            _moduleHandle = LoadLibraryW(path);
            if (_moduleHandle == IntPtr.Zero)
            {
                var err = GetLastError();
                Console.WriteLine(err);
                throw new FileNotFoundException(string.Format("Failed to load [{0}]", path));
            }

            IntPtr ptr = GetProcAddress(_moduleHandle, "UpdateILCodes");
            if (ptr == IntPtr.Zero)
                throw new MethodAccessException("Failed to locate UpdateILCodes function!");
            _updateILCodesMethod = (UpdateILCodesDelegate)Marshal.GetDelegateForFunctionPointer(ptr, typeof(UpdateILCodesDelegate));


            ptr = GetProcAddress(_moduleHandle, "GetStatus");
            if (ptr == IntPtr.Zero)
                throw new MethodAccessException("Failed to locate GetStatus function!");
            _getStatusDelegate = (GetStatusDelegate)Marshal.GetDelegateForFunctionPointer(ptr, typeof(GetStatusDelegate));



            ptr = GetProcAddress(_moduleHandle, "WaitForIntializationCompletion");
            if (ptr == IntPtr.Zero)
                throw new MethodAccessException("Failed to locate WaitForIntializationCompletion function!");
            _waitForIntializationCompletionDelegate = (WaitForIntializationCompletionDelegate)Marshal.GetDelegateForFunctionPointer(ptr, typeof(WaitForIntializationCompletionDelegate));
        }

        public static void Uninitialize()
        {
            if (_moduleHandle != IntPtr.Zero)
            {
                FreeLibrary(_moduleHandle);
                _moduleHandle = IntPtr.Zero;
            }
        }


        public static void Mock(MethodBase method, Delegate mock)
        {
            UpdateILCodes(method, mock.Method.GetMethodBody().GetILAsByteArray());
        }

        public static void Mock(MethodBase method, MethodBase mock)
        {
            UpdateILCodes(method, mock.GetMethodBody().GetILAsByteArray());
        }

        public static void Mock(MethodBase method, MethodBody mock)
        {
            UpdateILCodes(method, mock.GetILAsByteArray());
        }

        public static void Mock(MethodBase method, byte[] mock)
        {
            UpdateILCodes(method, mock);
        }

        private static void UpdateILCodes(MethodBase method, byte[] ilCodes)
        {
            if (_updateILCodesMethod == null)
                throw new Exception("Please Initialize() first.");

            IntPtr pMethodTable = IntPtr.Zero;
            if (method.DeclaringType != null)
                pMethodTable = method.DeclaringType.TypeHandle.Value;

            IntPtr pMethodHandle = IntPtr.Zero;
            if (method is DynamicMethod)
            {
                pMethodHandle = GetDynamicMethodHandle(method);
            }
            else
            {
                pMethodHandle = method.MethodHandle.Value;
            }

            IntPtr pBuffer = Marshal.AllocHGlobal(ilCodes.Length);
            if (pBuffer == IntPtr.Zero)
                throw new OutOfMemoryException();

            Marshal.Copy(ilCodes, 0, pBuffer, ilCodes.Length);

            int token = 0;
            try
            {
                token = method.MetadataToken;
            }
            catch
            {
            }


            if (!_updateILCodesMethod(pMethodTable, pMethodHandle, token, pBuffer, ilCodes.Length))
                throw new Exception("UpdateILCodes() failed, please check the initialization is failed or uncompleted.");
        }

        public static Status WaitForIntializationCompletion()
        {
            return _waitForIntializationCompletionDelegate();
        }

        public static Status GetStatus()
        {
            return _getStatusDelegate();
        }

        private static IntPtr GetDynamicMethodHandle(MethodBase method)
        {
            // .Net 4.0
            {
                FieldInfo fieldInfo = typeof(DynamicMethod).GetField("m_methodHandle", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    object runtimeMethodInfoStub = fieldInfo.GetValue(method);
                    if (runtimeMethodInfoStub != null)
                    {
                        fieldInfo = runtimeMethodInfoStub.GetType().GetField("m_value", BindingFlags.Instance | BindingFlags.Public);
                        if (fieldInfo != null)
                        {
                            object internalRuntimeMethodHandle = fieldInfo.GetValue(runtimeMethodInfoStub);
                            if (internalRuntimeMethodHandle != null)
                            {
                                fieldInfo = internalRuntimeMethodHandle.GetType().GetField("m_handle", BindingFlags.NonPublic | BindingFlags.Instance);
                                if (fieldInfo != null)
                                {
                                    return (IntPtr)fieldInfo.GetValue(internalRuntimeMethodHandle);
                                }
                            }
                        }
                    }
                }
            }

            // .Net 2.0
            {
                FieldInfo fieldInfo = typeof(DynamicMethod).GetField("m_method", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    return ((RuntimeMethodHandle)fieldInfo.GetValue(method)).Value;
                }
            }


            return IntPtr.Zero;
        }
    }
}
