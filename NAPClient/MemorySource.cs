using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace NAPClient
{
    public class MemorySource
    {
        // pointer offset from npp.dll to get to timer section
        public const int TimerPointerOffsets = 0x179F24;

        // pointer offsets from timer section to timers
        public const int TimeRemainingOffset = 0xFA8;
        public const int StartTimeOffset = 0xFB0;

        // calculated once the program starts running
        public static int TimerBlockOffset;

        // variables that store memory access information
        public static Process NppProcess;
        public static IntPtr NppProcessHandle;
        public static ProcessModule NppProcessModule;
        public static ProcessModuleCollection NppProcessModuleCollection;
        public static IntPtr NppdllBaseAddress;

        const int PROCESS_VM_ALL = 0x001F0FFF;

        public bool EpisodeTimeValuesChanged;
        public bool PlayerActivityChanged;

        public DoubleAddressValue CurrentTimeRemaining;
        public DoubleAddressValue LevelStartTime;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        //first parameter player number, second parameter is current frame count, third parameter is player bonus time, fourth is gold collected
        public Action<int, int, double, int> PlayerFinished;
        public Action LevelFinished;
        public Action StartNewLevel;
        public Action EpisodeStarted;
        public Action EpisodeFinished;
        public bool LevelInProgress;
        public bool MaxedOutEpisode;

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
        }

        void StartNewEpisode()
        {
            MaxedOutEpisode = false;
            EpisodeStarted();
        }

        public void ResetValues()
        {
            MaxedOutEpisode = false;
            LevelInProgress = false;
        }

        public void ApplyStartTimeValue(double newValue)
        {
            CurrentTimeRemaining.SetValue(newValue);
        }
    }
}
