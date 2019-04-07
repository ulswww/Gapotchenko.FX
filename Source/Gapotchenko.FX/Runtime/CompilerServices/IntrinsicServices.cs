﻿using Gapotchenko.FX.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Gapotchenko.FX.Runtime.CompilerServices
{
    /// <summary>
    /// Provides intrinsic compilation services.
    /// </summary>
    static unsafe class IntrinsicServices
    {
        static Patcher _Patcher = _CreatePatcher();

        static Patcher _CreatePatcher()
        {
            if (!CodeSafetyStrategy.UnsafeCodeRecommended)
                return null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var arch = RuntimeInformation.ProcessArchitecture;
                switch (arch)
                {
                    case Architecture.X64:
                        return new PatcherWindowsX64();
                }
            }

            return null;
        }

        /// <summary>
        /// Intrinsic patcher base.
        /// </summary>
        abstract class Patcher
        {
            public abstract void PatchMethod(MethodInfo method, byte[] code);

            protected static byte* Write(byte* dest, params byte[] data)
            {
                int size = data.Length;
                fixed (byte* src = data)
                    Block.Copy(src, dest, size);
                return dest + size;
            }
        }

        /// <summary>
        /// Intrinsic patcher for Windows OS on AMD-based 64-bit processor architecture.
        /// </summary>
        sealed class PatcherWindowsX64 : Patcher
        {
            static byte* _GetMethodInstructionPointer(MethodInfo method)
            {
                // Compile the method.
                RuntimeHelpers.PrepareMethod(method.MethodHandle);

                // Get pointer to the first instruction.
                var p = (byte*)method.MethodHandle.GetFunctionPointer();

                // Skip all jumps.
                while (*p == 0xe9)
                {
                    var delta = *(int*)(p + 1) + 5;
                    p += delta;
                }

                return p;
            }

            static readonly byte[][] _Prologues = new[]
            {
                new byte[] { 0x48, 0x89, 0x54, 0x24 },
                new byte[] { 0x48, 0x83, 0xec, 0x28 },
                new byte[] { 0x53, 0x48, 0x83, 0xec, 0x20 },
                new byte[] { 0x55, 0x48, 0x83, 0xec, 0x20 },
                new byte[] { 0x55, 0x57, 0x56, 0x48, 0x83, 0xec, 0x30 },
                new byte[] { 0x56, 0x48, 0x83, 0xec, 0x20 },
                new byte[] { 0x57, 0x56, 0x48, 0x83, 0xec, 0x28 }, // NGEN 4.7.2
            };

            static bool _IsSupportedPrologue(byte* buffer)
            {
                foreach (var prologue in _Prologues)
                {
                    var match = true;
                    for (var i = 0; i < prologue.Length; i++)
                    {
                        if (buffer[i] != prologue[i])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                        return true;
                }
                return false;
            }

            public override void PatchMethod(MethodInfo method, byte[] code)
            {
                var p = _GetMethodInstructionPointer(method);
                if (!_IsSupportedPrologue(p))
                    return;

                int codeSize = code.Length;

                // Ensure that changes in code are atomic by using the constrained region.
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                }
                finally
                {
                    // Temporarily allow memory modification in order to apply the intrinsic code.
                    using (var scope = new VirtualProtectScope(p, codeSize + 1, NativeMethods.Page.ExecuteReadWrite))
                    {
                        // Put the intrinsic code.
                        p = Write(p, code);

                        // End method with a 'ret' instruction.
                        Write(p, 0xc3);
                    }
                }
            }

            struct VirtualProtectScope : IDisposable
            {
                public VirtualProtectScope(void* address, int size, NativeMethods.Page protect)
                {
                    m_Address = new IntPtr(address);
                    m_Size = new IntPtr(size);

                    if (!NativeMethods.VirtualProtect(m_Address, m_Size, protect, out m_OldProtect))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                IntPtr m_Address;
                IntPtr m_Size;
                NativeMethods.Page m_OldProtect;

                public void Dispose()
                {
                    NativeMethods.VirtualProtect(m_Address, m_Size, m_OldProtect, out _);
                }
            }

            static class NativeMethods
            {
                [Flags]
                public enum Page : uint
                {
                    Execute = 0x10,
                    ExecuteRead = 0x20,
                    ExecuteReadWrite = 0x40,
                    ExecuteWriteCopy = 0x80,
                    NoAccess = 0x01,
                    ReadOnly = 0x02,
                    ReadWrite = 0x04,
                    WriteCopy = 0x08,
                    Guard = 0x100,
                    NoCache = 0x200,
                    WriteCombine = 0x400
                }

                [DllImport("kernel32.dll", SetLastError = true)]
                public static extern bool VirtualProtect(
                    IntPtr lpAddress,
                    IntPtr dwSize,
                    Page flNewProtect,
                    out Page lpflOldProtect);
            }
        }

        /// <summary>
        /// Initializes intrinsic methods of a specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        public static void InitializeType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            var patcher = _Patcher;
            if (patcher == null)
                return;

            var arch = RuntimeInformation.ProcessArchitecture;

            var methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                foreach (var attr in method.GetCustomAttributes<MachineCodeIntrinsicAttribute>(false))
                {
                    if (attr.Architecture != arch)
                        continue;

                    try
                    {
                        patcher.PatchMethod(method, attr.Code);
                    }
                    catch (Exception e) when (!e.IsControlFlowException())
                    {
                        _Patcher = null;
                        return;
                    }

                    break;
                }
            }
        }
    }
}