// ----------------------------------------------------------------------------
// <auto-generated>
// This is autogenerated code by CppSharp.
// Do not edit this file or all your changes will be lost after re-generation.
// </auto-generated>
// ----------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using System.Security;
using __CallingConvention = global::System.Runtime.InteropServices.CallingConvention;
using __IntPtr = global::System.IntPtr;

#pragma warning disable CS0109 // Member does not hide an inherited member; new keyword is not required

namespace AssimpNet.Unmanaged
{
    public unsafe partial class Matrix3x3 : IDisposable
    {
        [StructLayout(LayoutKind.Sequential, Size = 36)]
        public partial struct __Internal
        {
            internal float a1;
            internal float a2;
            internal float a3;
            internal float b1;
            internal float b2;
            internal float b3;
            internal float c1;
            internal float c2;
            internal float c3;
        }

        public __IntPtr __Instance { get; protected set; }

        internal static readonly new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::AssimpNet.Unmanaged.Matrix3x3> NativeToManagedMap =
            new global::System.Collections.Concurrent.ConcurrentDictionary<IntPtr, global::AssimpNet.Unmanaged.Matrix3x3>();

        internal static void __RecordNativeToManagedMapping(IntPtr native, global::AssimpNet.Unmanaged.Matrix3x3 managed)
        {
            NativeToManagedMap[native] = managed;
        }

        internal static bool __TryGetNativeToManagedMapping(IntPtr native, out global::AssimpNet.Unmanaged.Matrix3x3 managed)
        {
    
            return NativeToManagedMap.TryGetValue(native, out managed);
        }

        protected bool __ownsNativeInstance;

        internal static Matrix3x3 __CreateInstance(__IntPtr native, bool skipVTables = false)
        {
            if (native == __IntPtr.Zero)
                return null;
            return new Matrix3x3(native.ToPointer(), skipVTables);
        }

        internal static Matrix3x3 __GetOrCreateInstance(__IntPtr native, bool saveInstance = false, bool skipVTables = false)
        {
            if (native == __IntPtr.Zero)
                return null;
            if (__TryGetNativeToManagedMapping(native, out var managed))
                return (Matrix3x3)managed;
            var result = __CreateInstance(native, skipVTables);
            if (saveInstance)
                __RecordNativeToManagedMapping(native, result);
            return result;
        }

        internal static Matrix3x3 __CreateInstance(__Internal native, bool skipVTables = false)
        {
            return new Matrix3x3(native, skipVTables);
        }

        private static void* __CopyValue(__Internal native)
        {
            var ret = Marshal.AllocHGlobal(sizeof(__Internal));
            *(__Internal*) ret = native;
            return ret.ToPointer();
        }

        private Matrix3x3(__Internal native, bool skipVTables = false)
            : this(__CopyValue(native), skipVTables)
        {
            __ownsNativeInstance = true;
            __RecordNativeToManagedMapping(__Instance, this);
        }

        protected Matrix3x3(void* native, bool skipVTables = false)
        {
            if (native == null)
                return;
            __Instance = new __IntPtr(native);
        }

        public void Dispose()
        {
            Dispose(disposing: true, callNativeDtor : __ownsNativeInstance );
        }

        partial void DisposePartial(bool disposing);

        internal protected virtual void Dispose(bool disposing, bool callNativeDtor )
        {
            if (__Instance == IntPtr.Zero)
                return;
            NativeToManagedMap.TryRemove(__Instance, out _);
            DisposePartial(disposing);
            if (__ownsNativeInstance)
                Marshal.FreeHGlobal(__Instance);
            __Instance = IntPtr.Zero;
        }

        public float A1
        {
            get
            {
                return ((__Internal*)__Instance)->a1;
            }

            set
            {
                ((__Internal*)__Instance)->a1 = value;
            }
        }

        public float A2
        {
            get
            {
                return ((__Internal*)__Instance)->a2;
            }

            set
            {
                ((__Internal*)__Instance)->a2 = value;
            }
        }

        public float A3
        {
            get
            {
                return ((__Internal*)__Instance)->a3;
            }

            set
            {
                ((__Internal*)__Instance)->a3 = value;
            }
        }

        public float B1
        {
            get
            {
                return ((__Internal*)__Instance)->b1;
            }

            set
            {
                ((__Internal*)__Instance)->b1 = value;
            }
        }

        public float B2
        {
            get
            {
                return ((__Internal*)__Instance)->b2;
            }

            set
            {
                ((__Internal*)__Instance)->b2 = value;
            }
        }

        public float B3
        {
            get
            {
                return ((__Internal*)__Instance)->b3;
            }

            set
            {
                ((__Internal*)__Instance)->b3 = value;
            }
        }

        public float C1
        {
            get
            {
                return ((__Internal*)__Instance)->c1;
            }

            set
            {
                ((__Internal*)__Instance)->c1 = value;
            }
        }

        public float C2
        {
            get
            {
                return ((__Internal*)__Instance)->c2;
            }

            set
            {
                ((__Internal*)__Instance)->c2 = value;
            }
        }

        public float C3
        {
            get
            {
                return ((__Internal*)__Instance)->c3;
            }

            set
            {
                ((__Internal*)__Instance)->c3 = value;
            }
        }
    }
}