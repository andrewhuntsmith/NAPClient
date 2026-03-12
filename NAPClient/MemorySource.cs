using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

namespace NAPClient
{
    public class MemorySource
    {
        // pointer offset from npp.dll to get to timer section
        public const int TimerPointerOffsets = 0x179F24;

        // This first pointer offset seems to be commonly used
        public const int CommonPointerOffset = 0xB7B178;

        // pointer offsets from timer section to timers
        public const int TimeRemainingOffset = 0xFA8;
        public const int StartTimeOffset = 0xFB0;

        // level data variables
        public const int LevelDataSize = 0x4CC; // level data is always 1228 bytes
        public const int LevelDataOffset1 = CommonPointerOffset;
        public const int LevelDataOffset2 = 0x0;
        public const int LevelDataOffset3 = 0x330;
        public const int LevelDataOffset4 = -0xACC;
        public const int LevelDataOffset5 = 0x8;
        public List<LevelDataMemoryBridge> LevelData;

        // level profile data variables
        public const int LevelProfileSize = 0x30; // level profile data is always 48 bytes
        public const int LevelProfileOffset1 = CommonPointerOffset;
        public const int LevelProfileOffset2 = 0x810;
        public const int LevelProfileOffset3 = 0x80C11C;
        public List<LevelProfileMemoryBridge> LevelProfile;

        // pointer offsets for exits entered variable
        public const int ExitsEnteredOffset1 = CommonPointerOffset;
        public const int ExitsEnteredOffset2 = 0x810;
        public const int ExitsEnteredOffset3 = 0x100;

        // pointer offsets for palette index
        public const int PaletteIndexOffset1 = 0xB7A7B4;
        public const int PaletteIndexOffset2 = 0x0;
        public const int PaletteIndexOffset3 = 0x142C4;

        // calculated once the program starts running
        public static int TimerBlockOffset;

        // variables that store memory access information
        public static Process NppProcess;
        public static IntPtr NppProcessHandle;
        public static ProcessModule NppProcessModule;
        public static ProcessModuleCollection NppProcessModuleCollection;
        public static IntPtr NppdllBaseAddress;

        const int PROCESS_VM_ALL = 0x001F0FFF;

        public IntPtrAddressValue FirstLevelDataAddress;
        public IntPtrAddressValue FirstLevelProfileAddress;

        public DoubleAddressValue CurrentTimeRemaining;
        public DoubleAddressValue LevelStartTime;
        public IntAddressValue ExitsEntered;
        public IntAddressValue PaletteIndex;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        public void HookMemory()
        {
            int bytesRead = 0;
            byte[] offsetPointer = new byte[8];

            if (!FindProcessNPP()) { return; }

            NppProcessHandle = OpenProcess(PROCESS_VM_ALL, false, NppProcess.Id);
            // if OpenProcess failed
            if (NppProcessHandle == (IntPtr)0)
            {
                string errorMessage = "Cannot access N++ process!";
                string caption = "Error in accessing application";
                MessageBox.Show(errorMessage, caption);
                return;
            }

            if (!FindnppModule()) { return; }

            // combines the nppdll address with the timer block offset, sets the value in offsetPointer
            ReadProcessMemory((int)NppProcessHandle, (int)(NppdllBaseAddress + TimerPointerOffsets), offsetPointer, offsetPointer.Length, ref bytesRead);
            // saves offsetPointer into TimerBlockOffset
            TimerBlockOffset = BitConverter.ToInt32(offsetPointer, 0);
            InitializeAllValues();
        }

        // finds the N++ process
        // returns true if process is found
        // returns false if process is not found and user quits application
        bool FindProcessNPP()
        {
            while (Process.GetProcessesByName("N++").Length == 0)
            {
                // if GetProcessesByName failed
                string errorMessage = "Cannot find application N++!\nOpen the game and click 'Yes', or give up and click 'No'.";
                string caption = "Error in finding application";
                var result = MessageBox.Show(errorMessage, caption, MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return false;
                }
            }
            NppProcess = Process.GetProcessesByName("N++")[0];
            return true;
        }

        // finds the base address for npp.dll
        // returns true if found
        // returns false if error
        bool FindnppModule()
        {
            string nppdllFilePath = "npp.dll";
            NppdllBaseAddress = (IntPtr)0;
            NppProcessModuleCollection = NppProcess.Modules;

            //Console.WriteLine("Base addresses of modules associated with N++ are:");
            for (int i = 0; i < NppProcessModuleCollection.Count; i++)
            {
                NppProcessModule = NppProcessModuleCollection[i];
                //Console.WriteLine(nppProcessModule.FileName+" : "+nppProcessModule.BaseAddress);
                if (NppProcessModule.FileName.Contains(nppdllFilePath))
                {
                    NppdllBaseAddress = NppProcessModule.BaseAddress;
                }
            }

            // if the npp.dll module was not found
            if (NppdllBaseAddress == (IntPtr)0)
            {
                string errorMessage = "Cannot access npp.dll module!";
                string caption = "Error in accessing memory";
                MessageBox.Show(errorMessage, caption);
                return false;
            }
            return true;
        }

        void InitializeAllValues()
        {
            // These values start one pointer deep already. If you are looking at cheat engine, "npp.dll+179F24" (or whatever value) is already done in TimerBlockOffset.
            // From there, you are adding the specific pointer offset. For example, for CurrentRemainingTime, FB0.
            // If you wanted to start one address higher, you would need to start at the NppdllBaseAddress, and add the initial offset to that. i.e. "{ NppdllBaseAddress + TimerPointerOffsets, TimeRemainingOffset }"
            CurrentTimeRemaining = new DoubleAddressValue() { Offsets = new List<int> { TimerBlockOffset + TimeRemainingOffset } };
            LevelStartTime = new DoubleAddressValue() { Offsets = new List<int> { TimerBlockOffset + StartTimeOffset } };
            FirstLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 } };
            FirstLevelProfileAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelProfileOffset1, LevelProfileOffset2 } };
            ExitsEntered = new IntAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + ExitsEnteredOffset1, ExitsEnteredOffset2, ExitsEnteredOffset3 } };
            PaletteIndex = new IntAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + PaletteIndexOffset1, PaletteIndexOffset2, PaletteIndexOffset3 } };
            ReadLevelData();
            ReadLevelProfile();
        }

        void ReadLevelData()
        {
            LevelData = new List<LevelDataMemoryBridge>();
            FirstLevelDataAddress.UpdateValue();
            for (int i = 0; i < 125; i++)
            {
                var level = new LevelDataMemoryBridge(FirstLevelDataAddress.AsInt() + i * LevelDataSize);
                LevelData.Add(level);
                level.UpdateValue();
            }
        }

        void ReadLevelProfile()
        {
            LevelProfile = new List<LevelProfileMemoryBridge>();
            FirstLevelProfileAddress.UpdateValue();
            for (int i = 0; i < 125; i++)
            {
                var level = new LevelProfileMemoryBridge(FirstLevelProfileAddress.AsInt() + LevelProfileOffset3 + i * LevelProfileSize);
                LevelProfile.Add(level);
                level.UpdateValue();
            }
        }

        public void ApplyStartTimeValue(double newValue)
        {
            CurrentTimeRemaining.SetValue(newValue);
        }

        public void SwapLevels(int first, int second)
        {
            var firstLevelData = new byte[LevelDataSize];
            LevelData[first].TotalLevelData.Value.CopyTo(firstLevelData, 0);
            var secondLevelData = new byte[LevelDataSize];
            LevelData[second].TotalLevelData.Value.CopyTo(secondLevelData, 0);

            MemorySource.WriteProcessMemory((int)MemorySource.NppProcessHandle, LevelData[second].BaseLevelPointer, firstLevelData, LevelDataSize, out var bytesWritten);
            MemorySource.WriteProcessMemory((int)MemorySource.NppProcessHandle, LevelData[first].BaseLevelPointer, secondLevelData, LevelDataSize, out bytesWritten);

            LevelData[first].UpdateValue();
            LevelData[second].UpdateValue();
        }

        public void UpdateLevelProfileValue(int levelIndex, int byteIndex, int value)
        {
            MemorySource.WriteProcessMemory((int)MemorySource.NppProcessHandle, FirstLevelProfileAddress.AsInt() + LevelProfileOffset3 + levelIndex * LevelProfileSize + byteIndex, BitConverter.GetBytes(value), sizeof(int), out var bytesWritten);
        }
    }
}
