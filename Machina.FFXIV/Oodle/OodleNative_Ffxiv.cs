﻿// Copyright © 2021 Ravahn - All Rights Reserved
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see<http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Machina.FFXIV.Oodle
{
    public class OodleNative_Ffxiv : IOodleNative
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibraryW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);
        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr hModule);

        private delegate int OodleNetwork1UDP_State_Size_Func();
        private delegate int OodleNetwork1_Shared_Size_Func(int htbits);
        private delegate void OodleNetwork1_Shared_SetWindow_Action(byte[] data, int htbits, byte[] window, int windowSize);
        private delegate void OodleNetwork1UDP_Train_Action(byte[] state, byte[] shared, IntPtr training_packet_pointers, IntPtr training_packet_sizes, int num_training_packets);
        private unsafe delegate bool OodleNetwork1UDP_Decode_Func(byte[] state, byte[] shared, byte* compressed, int compressedSize, byte[] raw, int rawSize);
        private delegate bool OodleNetwork1UDP_Encode_Func(byte[] state, byte[] shared, byte[] raw, int rawSize, byte[] compressed);
        private delegate IntPtr OodleMalloc_Func(IntPtr a, int b);
        private delegate void OodleFree_Action(IntPtr a);

        private static readonly int OodleMallocOffset = 0x1f21cf8;
        private static readonly int OodleFreeOffset = 0x1f21d00;
        private static readonly int OodleNetwork1_Shared_SizeOffset = 0x153edf0;
        private static readonly int OodleNetwork1_Shared_SetWindowOffset = 0x153ecc0;
        private static readonly int OodleNetwork1UDP_TrainOffset = 0x153d920;
        private static readonly int OodleNetwork1UDP_DecodeOffset = 0x153cdd0;
        private static readonly int OodleNetwork1UDP_EncodeOffset = 0x153ce20;
        private static readonly int OodleNetwork1UDP_State_SizeOffset = 0x153d470;

        private OodleNetwork1UDP_State_Size_Func _OodleNetwork1UDP_State_Size;
        private OodleNetwork1_Shared_Size_Func _OodleNetwork1_Shared_Size;
        private OodleNetwork1_Shared_SetWindow_Action _OodleNetwork1_Shared_SetWindow;
        private OodleNetwork1UDP_Train_Action _OodleNetwork1UDP_Train;
        private OodleNetwork1UDP_Encode_Func _OodleNetwork1UDP_Encode;
        private OodleNetwork1UDP_Decode_Func _OodleNetwork1UDP_Decode;
        private OodleMalloc_Func _OodleMalloc;
        private OodleFree_Action _OodleFree;

        private static unsafe IntPtr AllocAlignedMemory(IntPtr cb, int alignment)
        {
            // copied from https://github.com/dotnet/runtime/issues/33244#issuecomment-595848832
            if (alignment % 1 != 0)
                throw new ArgumentException($"{nameof(AllocAlignedMemory)}: {nameof(alignment)} % 1 != 0)");

            IntPtr block = Marshal.AllocHGlobal(checked(cb + sizeof(IntPtr) + (alignment - 1)));

            // Align the pointer
            IntPtr aligned = (IntPtr)((long)(block + sizeof(IntPtr) + (alignment - 1)) & ~(alignment - 1));

            // Store the pointer to the memory block to free right before the aligned pointer 
            *(((IntPtr*)aligned) - 1) = block;

            return aligned;
        }

        private static unsafe void FreeAlignedMemory(IntPtr p)
        {
            if (p != IntPtr.Zero)
                Marshal.FreeHGlobal(*(((IntPtr*)p) - 1));
        }

        private IntPtr _libraryHandle = IntPtr.Zero;
        private readonly object _librarylock = new object();

        public bool Initialized { get; set; }
        public void Initialize(string path)
        {
            try
            {
                lock (_librarylock)
                {
                    if (_libraryHandle != IntPtr.Zero)
                        return;

                    _libraryHandle = LoadLibraryW(path);
                    if (_libraryHandle == IntPtr.Zero)
                    {
                        Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: Cannot load ffxiv_dx11 executable at path {path}.", "DEBUG-MACHINA");
                        return;
                    }
                    else
                        Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: Loaded ffxiv_dx11 executable into ACT memory from path {path}.", "DEBUG-MACHINA");

                    _OodleMalloc = new OodleMalloc_Func(AllocAlignedMemory);
                    _OodleFree = new OodleFree_Action(FreeAlignedMemory);

                    IntPtr myMallocPtr = Marshal.GetFunctionPointerForDelegate(_OodleMalloc);
                    IntPtr myFreePtr = Marshal.GetFunctionPointerForDelegate(_OodleFree);

                    Marshal.Copy(BitConverter.GetBytes(myMallocPtr.ToInt64()), 0, _libraryHandle + OodleMallocOffset, IntPtr.Size);
                    Marshal.Copy(BitConverter.GetBytes(myFreePtr.ToInt64()), 0, _libraryHandle + OodleFreeOffset, IntPtr.Size);

                    _OodleNetwork1UDP_State_Size = (OodleNetwork1UDP_State_Size_Func)Marshal.GetDelegateForFunctionPointer(
                        _libraryHandle + OodleNetwork1UDP_State_SizeOffset, typeof(OodleNetwork1UDP_State_Size_Func));

                    _OodleNetwork1_Shared_Size = (OodleNetwork1_Shared_Size_Func)Marshal.GetDelegateForFunctionPointer(
                        _libraryHandle + OodleNetwork1_Shared_SizeOffset, typeof(OodleNetwork1_Shared_Size_Func));

                    _OodleNetwork1_Shared_SetWindow = (OodleNetwork1_Shared_SetWindow_Action)Marshal.GetDelegateForFunctionPointer(
                        _libraryHandle + OodleNetwork1_Shared_SetWindowOffset, typeof(OodleNetwork1_Shared_SetWindow_Action));

                    _OodleNetwork1UDP_Train = (OodleNetwork1UDP_Train_Action)Marshal.GetDelegateForFunctionPointer(
                        _libraryHandle + OodleNetwork1UDP_TrainOffset, typeof(OodleNetwork1UDP_Train_Action));

                    _OodleNetwork1UDP_Encode = (OodleNetwork1UDP_Encode_Func)Marshal.GetDelegateForFunctionPointer(
                        _libraryHandle + OodleNetwork1UDP_EncodeOffset, typeof(OodleNetwork1UDP_Encode_Func));

                    _OodleNetwork1UDP_Decode = (OodleNetwork1UDP_Decode_Func)Marshal.GetDelegateForFunctionPointer(
                        _libraryHandle + OodleNetwork1UDP_DecodeOffset, typeof(OodleNetwork1UDP_Decode_Func));

                    Initialized = true;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: Exception in {nameof(Initialize)}. Game path: {path}  Exception: {ex}", "DEBUG-MACHINA");

                UnInitialize();
            }
        }

        public void UnInitialize()
        {
            try
            {
                lock (_librarylock)
                {
                    if (_libraryHandle != IntPtr.Zero)
                    {
                        bool freed = FreeLibrary(_libraryHandle);
                        if (!freed)
                            Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: {nameof(FreeLibrary)} failed.", "DEBUG-MACHINA");
                        _libraryHandle = IntPtr.Zero;
                    }

                    _OodleMalloc = null;
                    _OodleFree = null;
                    _OodleNetwork1UDP_State_Size = null;
                    _OodleNetwork1_Shared_Size = null;
                    _OodleNetwork1_Shared_SetWindow = null;
                    _OodleNetwork1UDP_Train = null;
                    _OodleNetwork1UDP_Encode = null;
                    _OodleNetwork1UDP_Decode = null;

                    Initialized = false;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{nameof(OodleNative_Ffxiv)}: exception in {nameof(UnInitialize)}: {ex}", "DEBUG-MACHINA");
            }
        }

        public int OodleNetwork1UDP_State_Size()
        {
            return _OodleNetwork1UDP_State_Size?.Invoke() ?? 0;
        }

        public int OodleNetwork1_Shared_Size(int htbits)
        {
            return _OodleNetwork1_Shared_Size?.Invoke(htbits) ?? 0;
        }

        public void OodleNetwork1_Shared_SetWindow(byte[] data, int htbits, byte[] window, int windowSize)
        {
            _OodleNetwork1_Shared_SetWindow?.Invoke(data, htbits, window, windowSize);
        }

        public void OodleNetwork1UDP_Train(byte[] state, byte[] share, IntPtr training_packet_pointers, IntPtr training_packet_sizes, int num_training_packets)
        {
            _OodleNetwork1UDP_Train?.Invoke(state, share, training_packet_pointers, training_packet_sizes, num_training_packets);
        }

        public unsafe bool OodleNetwork1UDP_Decode(byte[] state, byte[] share, IntPtr compressed, int compressedSize, byte[] raw, int rawSize)
        {
            return _OodleNetwork1UDP_Decode?.Invoke(state, share, (byte*)compressed, compressedSize, raw, rawSize) ?? false;
        }
        public bool OodleNetwork1UDP_Encode(byte[] state, byte[] share, byte[] raw, int rawSize, byte[] compressed)
        {
            return _OodleNetwork1UDP_Encode?.Invoke(state, share, raw, rawSize, compressed) ?? false;

        }

    }
}
