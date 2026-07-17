using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace NAPClient
{
	public class MemorySource
	{
		public static bool ConnectedToGame;
		public Action MemoryError;

		// pointer offset from npp.dll to get to timer section
		public const int TimerPointerOffset1 = 0xB7A7B4;
		public const int TimerPointerOffset2 = 0x08;

		// This first pointer offset seems to be commonly used
		public const int CommonPointerOffset = 0xB7B178;

		// pointer offsets from timer section to timers
		public const int TimeRemainingOffset = 0x8528;
		public const int GoldCollectedInCurrentLevelOffset = 0x854C;
		public const int StartTimeOffset1 = TimerPointerOffset2;
		public const int StartTimeOffset2 = 0x1AC;
		public const int TimeGrantedByGoldOffset1 = TimerPointerOffset2;
		public const int TimeGrantedByGoldOffset2 = 0x388;

		// level data variables
		public const int LevelDataSize = 0x4CC; // level data is always 1228 bytes
		public const int LevelDataOffset1 = CommonPointerOffset;
		public const int LevelDataOffset2 = 0x0;
		public const int LevelDataOffset3 = 0x330;
		public const int LevelDataOffset4 = -0xACC;
		public List<LevelDataMemoryBridge> LevelData;

		public const int NppLevelDataOffset5 = 0x4 * 0xBF880;
		public const int LegacyLevelDataOffset5 = 0x8 * 0xBF880;
		public const int QLevelDataOffset5 = 0xC * 0xBF880;
		public const int UltimateLevelDataOffset5 = 0x10 * 0xBF880;
		public const int ELevelDataOffset5 = 0x14 * 0xBF880;
		public const int QELevelDataOffset5 = 0x98 * 0xBF880;
		public const int EQLevelDataOffset5 = 0x9C * 0xBF880;
		public const int CoopIntroLevelDataOffset5 = 0x1 * 0xBF880;
		public const int CoopNppLevelDataOffset5 = 0x5 * 0xBF880;
		public const int CoopLegacyLevelDataOffset5 = 0x9 * 0xBF880;
		public const int CoopTenppLevelDataOffset5 = 0x95 * 0xBF880;
		public const int RaceIntroLevelDataOffset5 = 0x2 * 0xBF880;
		public const int RaceNppLevelDataOffset5 = 0x6 * 0xBF880;
		public const int RaceLegacyLevelDataOffset5 = 0xA * 0xBF880;
		public const int RaceTenppLevelDataOffset5 = 0x96 * 0xBF880;

        // we need to track the original level order after we've shuffled the IDs
        public Dictionary<int, string> OriginalLevelMapping = new Dictionary<int, string>();
		// after levels have been shuffled, we'll want to easily get their new ID from their name
		public Dictionary<string, int> NewLevelMapping = new Dictionary<string, int>();

		// level profile data variables
		public const int LevelProfileSize = 0x30; // level profile data is always 48 bytes
		public const int LevelProfileOffset1 = CommonPointerOffset;
		public const int LevelProfileOffset2 = 0x810;
		public const int LevelProfileOffset3 = 0x80C11C;
		public List<LevelProfileMemoryBridge> LevelProfile;

		public const int EpisodeProfileOffset3 = LevelProfileOffset3 + 0x4E20 * 0x30;
		public List<EpisodeProfileMemoryBridge> EpisodeProfile;

		// pointer offsets for exits entered variable
		public const int VictoriesOffset1 = CommonPointerOffset;
		public const int VictoriesOffset2 = 0x810;
		public const int LevelVictoriesOffset3 = 0x100;
		public const int EpisodeVictoriesOffset3 = 0x110;

		// pointer offsets for palette index
		public const int PaletteIndexOffset1 = TimerPointerOffset1;
		public const int PaletteIndexOffset2 = TimerPointerOffset2;
		public const int PaletteIndexOffset3 = 0x2B4;

		// pointer offsets for current level information
		public const int CurrentLevelLogicOffset = CommonPointerOffset;
		public const int CurrentLevelIdOffset = 0x898;
		public const int InLevelViewOffset = 0x934;

		// string offsets
		public const int ProfileNameOffset = 0x587790;
		public const string OriginalProfileName = "nprofile.gz";
		public const string ReplacementProfileName = "NAPfile.gz ";

		// pointer offsets and values for disabling score submission
		public const int ScoreSubmitMethodPointer = 0x4DA4B0;
		public byte[] ReturnBytes = new byte[] { 0xC3 };
		public byte[] PushEbpBytes = new byte[] { 0x55 };

		// calculated once the program starts running
		public static int TimerBlockOffset;

		// variables that store memory access information
		public static Process NppProcess;
		public static IntPtr NppProcessHandle;
		public static ProcessModule NppProcessModule;
		public static ProcessModuleCollection NppProcessModuleCollection;
		public static IntPtr NppdllBaseAddress;

		const int PROCESS_VM_ALL = 0x001F0FFF;
		const int PAGE_READWRITE = 0x04;

		public IntPtrAddressValue FirstIntroLevelDataAddress;
		public IntPtrAddressValue FirstNppLevelDataAddress;
		public IntPtrAddressValue FirstLegacyLevelDataAddress;
		public IntPtrAddressValue FirstQLevelDataAddress;
		public IntPtrAddressValue FirstUltimateLevelDataAddress;
		public IntPtrAddressValue FirstELevelDataAddress;
		public IntPtrAddressValue FirstQELevelDataAddress;
		public IntPtrAddressValue FirstEQLevelDataAddress;
		public IntPtrAddressValue FirstCoopIntroLevelDataAddress;
		public IntPtrAddressValue FirstCoopNppLevelDataAddress;
		public IntPtrAddressValue FirstCoopLegacyLevelDataAddress;
		public IntPtrAddressValue FirstCoopTenppLevelDataAddress;
		public IntPtrAddressValue FirstRaceIntroLevelDataAddress;
		public IntPtrAddressValue FirstRaceNppLevelDataAddress;
		public IntPtrAddressValue FirstRaceLegacyLevelDataAddress;
		public IntPtrAddressValue FirstRaceTenppLevelDataAddress;
        public IntPtrAddressValue FirstLevelProfileAddress;

		public DoubleAddressValue CurrentTimeRemaining;
		public IntAddressValue GoldCollectedInCurrentLevel;
		public FloatAddressValue LevelStartTime;
		public FloatAddressValue TimeGrantedByGold;
		public IntAddressValue LevelVictories;
		public IntAddressValue EpisodeVictories;
		public IntAddressValue PaletteIndex;
		public IntAddressValue CurrentSelectedLevel;
		public IntAddressValue InLevelView;

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool VirtualProtectEx(int hProcess, int lpBaseAddress, int dwSize, int flNewProtect, out int lpflOldProtect);

		public bool HookMemory()
		{
			ConnectedToGame = false;

			int bytesRead = 0;
			byte[] offsetPointer = new byte[8];

			if (!FindProcessNPP()) { return false; }

			NppProcessHandle = OpenProcess(PROCESS_VM_ALL, false, NppProcess.Id);
			// if OpenProcess failed
			if (NppProcessHandle == (IntPtr)0)
			{
				string caption = "Error in accessing application";
				string errorMessage = "Cannot access N++ process!";
				var dialog = new AcceptDialog()
				{
					Title = caption,
					DialogText = errorMessage
				};
				dialog.PopupCentered();
				return false;
			}

			if (!FindnppModule()) { return false; }
			ConnectedToGame = true;

			// combines the nppdll address with the timer block offset, sets the value in offsetPointer
			ReadProcessMemory((int)NppProcessHandle, (int)(NppdllBaseAddress + TimerPointerOffset1), offsetPointer, offsetPointer.Length, ref bytesRead);
			// saves offsetPointer into TimerBlockOffset
			TimerBlockOffset = BitConverter.ToInt32(offsetPointer, 0);
			DisableScoreSubmission();
			DisableProfileWriting();
			InitializeAllValues();

			return true;
		}

		// finds the N++ process
		// returns true if process is found
		// returns false if process is not found and user quits application
		bool FindProcessNPP()
		{
            while (Process.GetProcessesByName("N++").Length == 0)
			{
				// if GetProcessesByName failed
				string errorMessage = "Cannot find application N++!\nOpen the game and try again!";
				string caption = "Error in finding application";
				OS.Alert(errorMessage, caption);
				return false;
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
				var dialog = new AcceptDialog()
				{
					Title = caption,
					DialogText = errorMessage
				};
				dialog.PopupCentered();
				return false;
			}
			return true;
		}

		void DisableScoreSubmission()
		{
			WriteProcessMemory((int)MemorySource.NppProcessHandle, NppdllBaseAddress.ToInt32() + ScoreSubmitMethodPointer, ReturnBytes, sizeof(byte), out var bytesWritten);
		}

		public void ReenableScoreSubmission()
		{
			WriteProcessMemory((int)MemorySource.NppProcessHandle, NppdllBaseAddress.ToInt32() + ScoreSubmitMethodPointer, PushEbpBytes, sizeof(byte), out var bytesWritten);
		}

		void DisableProfileWriting()
		{
			VirtualProtectEx((int)MemorySource.NppProcessHandle, NppdllBaseAddress.ToInt32() + ProfileNameOffset, 11, PAGE_READWRITE, out var oldProtections);
			var written = WriteProcessMemory((int)MemorySource.NppProcessHandle, NppdllBaseAddress.ToInt32() + ProfileNameOffset, Encoding.UTF8.GetBytes(ReplacementProfileName), 11, out var bytesWritten);
			VirtualProtectEx((int)MemorySource.NppProcessHandle, NppdllBaseAddress.ToInt32() + ProfileNameOffset, 11, oldProtections, out var _);

			int error = Marshal.GetLastWin32Error();
			if (!written)
			{
				string caption = "Error disabling profile!";
				string errorMessage = "Error number: " + error.ToString() +
					"\nFailed to disable writing to your profile!\nClose NAP and N++, and contact the developers\nContinuing to use the program could damage your regular N++ profile";
				var dialog = new AcceptDialog()
				{
					Title = caption,
					DialogText = errorMessage
				};
				dialog.PopupCentered();
			}
		}

		void InitializeAllValues()
		{
			// Some of these values start one pointer deep already. If you are looking at cheat engine, "npp.dll+179F24" (or whatever value) is already done in TimerBlockOffset.
			// From there, you are adding the specific pointer offset. For example, for CurrentRemainingTime, FA8.
			// If you wanted to start one address higher, you would need to start at the NppdllBaseAddress, and add the initial offset to that. i.e. "{ NppdllBaseAddress + TimerPointerOffsets, TimeRemainingOffset }"
			CurrentTimeRemaining = new DoubleAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + CommonPointerOffset, TimeRemainingOffset } };
			GoldCollectedInCurrentLevel = new IntAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + CommonPointerOffset, GoldCollectedInCurrentLevelOffset } };
			LevelStartTime = new FloatAddressValue() { Offsets = new List<int> { TimerBlockOffset + StartTimeOffset1, StartTimeOffset2 } };
			TimeGrantedByGold = new FloatAddressValue() { Offsets = new List<int> { TimerBlockOffset + TimeGrantedByGoldOffset1, TimeGrantedByGoldOffset2 } };
			
			FirstIntroLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 } };
            FirstNppLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + NppLevelDataOffset5 } };
            FirstLegacyLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + LegacyLevelDataOffset5 } };
            FirstQLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + QLevelDataOffset5 } };
            FirstUltimateLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + UltimateLevelDataOffset5 } };
            FirstELevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + ELevelDataOffset5 } };
            FirstQELevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + QELevelDataOffset5 } };
            FirstEQLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + EQLevelDataOffset5 } };
            FirstCoopIntroLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + CoopIntroLevelDataOffset5 } };
            FirstCoopNppLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + CoopNppLevelDataOffset5 } };
            FirstCoopLegacyLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + CoopLegacyLevelDataOffset5 } };
            FirstCoopTenppLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + CoopTenppLevelDataOffset5 } };
            FirstRaceIntroLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + RaceIntroLevelDataOffset5 } };
            FirstRaceNppLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + RaceNppLevelDataOffset5 } };
            FirstRaceLegacyLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + RaceLegacyLevelDataOffset5 } };
            FirstRaceTenppLevelDataAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelDataOffset1, LevelDataOffset2, LevelDataOffset3, LevelDataOffset4 + RaceTenppLevelDataOffset5 } };
			
			FirstLevelProfileAddress = new IntPtrAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + LevelProfileOffset1, LevelProfileOffset2 } };
			LevelVictories = new IntAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + VictoriesOffset1, VictoriesOffset2, LevelVictoriesOffset3 } };
			EpisodeVictories = new IntAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + VictoriesOffset1, VictoriesOffset2, EpisodeVictoriesOffset3 } };
			PaletteIndex = new IntAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + PaletteIndexOffset1, PaletteIndexOffset2, PaletteIndexOffset3 } };
			CurrentSelectedLevel = new IntAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + CurrentLevelLogicOffset, CurrentLevelIdOffset } };
            InLevelView = new IntAddressValue() { Offsets = new List<int> { NppdllBaseAddress.ToInt32() + CurrentLevelLogicOffset, InLevelViewOffset } };

            AddressValue.MemoryError += OnMemoryError;

			ReadLevelData();
			ReadLevelProfile();
			ReadEpisodeProfile();
		}

		void ReadLevelData()
		{
			LevelData = new List<LevelDataMemoryBridge>();
			FirstIntroLevelDataAddress.UpdateValue();
			FirstNppLevelDataAddress.UpdateValue();
            FirstLegacyLevelDataAddress.UpdateValue();
            FirstQLevelDataAddress.UpdateValue();
            FirstUltimateLevelDataAddress.UpdateValue();
            FirstELevelDataAddress.UpdateValue();
            FirstQELevelDataAddress.UpdateValue();
            FirstEQLevelDataAddress.UpdateValue();
            FirstCoopIntroLevelDataAddress.UpdateValue();
            FirstCoopNppLevelDataAddress.UpdateValue();
            FirstCoopLegacyLevelDataAddress.UpdateValue();
            FirstCoopTenppLevelDataAddress.UpdateValue();
            FirstRaceIntroLevelDataAddress.UpdateValue();
            FirstRaceNppLevelDataAddress.UpdateValue();
            FirstRaceLegacyLevelDataAddress.UpdateValue();
            FirstRaceTenppLevelDataAddress.UpdateValue();

            for (int i = 0; i < 12150; i++)
			{
				var address = 0x00;
				if (i < 125)
				{
                    address = FirstIntroLevelDataAddress.AsInt() + i * LevelDataSize;
				}
                else if (i < 600)
				{
					LevelData.Add(null);
					continue;
				}
				else if (i < 1200)
				{
                    address = FirstNppLevelDataAddress.AsInt() + (i - 600) * LevelDataSize;
                }
                else if (i < 1800)
                {
                    address = FirstLegacyLevelDataAddress.AsInt() + (i - 1200) * LevelDataSize;
                }
                else if (i < 1920)
                {
                    address = FirstQLevelDataAddress.AsInt() + (i - 1800) * LevelDataSize;
                }
                else if (i < 2400)
                {
                    LevelData.Add(null);
                    continue;
                }
                else if (i < 3000)
                {
                    address = FirstUltimateLevelDataAddress.AsInt() + (i - 2400) * LevelDataSize;
                }
                else if (i < 3120)
                {
                    address = FirstELevelDataAddress.AsInt() + (i - 3000) * LevelDataSize;
                }
                else if (i < 3600)
                {
                    LevelData.Add(null);
                    continue;
                }
                else if (i < 3625)
                {
                    address = FirstQELevelDataAddress.AsInt() + (i - 3600) * LevelDataSize;
                }
                else if (i < 3650)
                {
                    address = FirstEQLevelDataAddress.AsInt() + (i - 3625) * LevelDataSize;
                }
                else if (i < 4200)
                {
                    LevelData.Add(null);
                    continue;
                }
                else if (i < 4250)
                {
                    address = FirstCoopIntroLevelDataAddress.AsInt() + (i - 4200) * LevelDataSize;
                }
                else if (i < 4800)
                {
                    LevelData.Add(null);
                    continue;
                }
                else if (i < 5400)
                {
                    address = FirstCoopNppLevelDataAddress.AsInt() + (i - 4800) * LevelDataSize;
                }
                else if (i < 5730)
                {
                    address = FirstCoopLegacyLevelDataAddress.AsInt() + (i - 5400) * LevelDataSize;
                }
                else if (i < 7800)
                {
                    LevelData.Add(null);
                    continue;
                }
                else if (i < 7950)
                {
                    address = FirstCoopTenppLevelDataAddress.AsInt() + (i - 7800) * LevelDataSize;
                }
                else if (i < 8400)
                {
                    LevelData.Add(null);
                    continue;
                }
                else if (i < 8425)
                {
                    address = FirstRaceIntroLevelDataAddress.AsInt() + (i - 8400) * LevelDataSize;
                }
                else if (i < 9000)
                {
                    LevelData.Add(null);
                    continue;
                }
                else if (i < 9600)
                {
                    address = FirstRaceNppLevelDataAddress.AsInt() + (i - 9000) * LevelDataSize;
                }
                else if (i < 10170)
                {
                    address = FirstRaceLegacyLevelDataAddress.AsInt() + (i - 9600) * LevelDataSize;
                }
                else if (i < 12000)
                {
                    LevelData.Add(null);
                    continue;
                }
                else if (i < 12150)
                {
                    address = FirstRaceTenppLevelDataAddress.AsInt() + (i - 12000) * LevelDataSize;
                }

                var level = new LevelDataMemoryBridge(address);
                LevelData.Add(level);
                level.UpdateValue();
                OriginalLevelMapping[level.GetLevelId()] = level.GetLevelName();
                NewLevelMapping[level.GetLevelName()] = level.GetLevelId();
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

		void ReadEpisodeProfile()
		{
			EpisodeProfile = new List<EpisodeProfileMemoryBridge>();
			for (int i = 0; i < 25; i++)
			{
				var episode = new EpisodeProfileMemoryBridge(FirstLevelProfileAddress.AsInt() + EpisodeProfileOffset3 + i * LevelProfileSize);
				EpisodeProfile.Add(episode);
				episode.UpdateValue();
			}
		}

		public void ApplyStartTimeValue(double newValue)
		{
			CurrentTimeRemaining.SetValue(newValue);
		}

		public void SwapLevels(int first, int second)
		{
			if (first == second) 
				return;

			var firstLevelData = new byte[LevelDataSize];
			LevelData[first].TotalLevelData.Value.CopyTo(firstLevelData, 0);
			var secondLevelData = new byte[LevelDataSize];
			LevelData[second].TotalLevelData.Value.CopyTo(secondLevelData, 0);

			MemorySource.WriteProcessMemory((int)MemorySource.NppProcessHandle, LevelData[second].BaseLevelPointer, firstLevelData, LevelDataSize, out var bytesWritten);
			MemorySource.WriteProcessMemory((int)MemorySource.NppProcessHandle, LevelData[first].BaseLevelPointer, secondLevelData, LevelDataSize, out bytesWritten);

			LevelData[first].UpdateValue();
			LevelData[second].UpdateValue();

			var firstId = LevelData[first].GetLevelId();
			var secondId = LevelData[second].GetLevelId();
			LevelData[first].SetLevelId(secondId);
			LevelData[second].SetLevelId(firstId);

			LevelData[first].UpdateValue();
			LevelData[second].UpdateValue();

			NewLevelMapping[LevelData[first].GetLevelName()] = LevelData[first].GetLevelId();
			NewLevelMapping[LevelData[second].GetLevelName()] = LevelData[second].GetLevelId();
		}

		public void UpdateLevelProfileValue(int levelIndex, int byteIndex, int value)
		{
			MemorySource.WriteProcessMemory((int)MemorySource.NppProcessHandle, FirstLevelProfileAddress.AsInt() + LevelProfileOffset3 + levelIndex * LevelProfileSize + byteIndex, BitConverter.GetBytes(value), sizeof(int), out var bytesWritten);
		}

		void OnMemoryError()
		{
			ConnectedToGame = false;
			MemoryError?.Invoke();
		}
	}
}
