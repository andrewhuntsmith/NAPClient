using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;

namespace NAPClient
{
    public abstract class AddressValue<T>
    {
        public List<int> Offsets;
        public T Value;
        public T PreviousValue;
        public Action<T, T> ValueUpdated;

        public void UpdateValue()
        {
            int bytesRead = 0;
            int i;
            List<byte[]> bufferList = new List<byte[]>();
            for (i = 0; i < Offsets.Count; i++)
            {
                int pointer = i == 0 ? 0 : BitConverter.ToInt32(bufferList[i - 1], 0);
                bufferList.Add(new byte[8]);
                MemorySource.ReadProcessMemory((int)MemorySource.NppProcessHandle, pointer + Offsets[i], bufferList[i], bufferList[i].Length, ref bytesRead);
            }

            T value = ConvertToType(bufferList[i - 1]);
            PreviousValue = Value;
            Value = value;
            ValueUpdated?.Invoke(Value, PreviousValue);
        }

        public void SetValue(T input)
        {
            int bytesRead = 0;
            int i;
            List<byte[]> bufferList = new List<byte[]>();
            int pointer = 0;
            for (i = 0; i < Offsets.Count - 1; i++)
            {
                pointer = i == 0 ? 0 : BitConverter.ToInt32(bufferList[i - 1], 0);
                bufferList.Add(new byte[8]);
                MemorySource.ReadProcessMemory((int)MemorySource.NppProcessHandle, pointer + Offsets[i], bufferList[i], bufferList[i].Length, ref bytesRead);
            }

            pointer = i == 0 ? 0 : BitConverter.ToInt32(bufferList[i - 1], 0);
            MemorySource.WriteProcessMemory((int)MemorySource.NppProcessHandle, pointer + Offsets[i], ConvertFromType(input), ConvertFromType(input).Length, out var bytesWritten);

            //error checking code that probably shouldn't ever come up
            int error = Marshal.GetLastWin32Error();
            if (bytesWritten == 0)
            {
                string caption = "Error writing memory!";
                string errorMessage = "Error number: " + error.ToString();
                MessageBox.Show(errorMessage, caption);
            }
        }

        protected abstract T ConvertToType(byte[] buffer);
        protected abstract byte[] ConvertFromType(T value);
    }

    public class DoubleAddressValue : AddressValue<double>
    {
        protected override double ConvertToType(byte[] buffer)
        {
            return BitConverter.ToDouble(buffer, 0);
        }

        protected override byte[] ConvertFromType(double value)
        {
            return BitConverter.GetBytes(value);
        }
    }

    public class IntAddressValue : AddressValue<int>
    {
        protected override int ConvertToType(byte[] buffer)
        {
            return BitConverter.ToInt32(buffer, 0);
        }

        protected override byte[] ConvertFromType(int value)
        {
            return BitConverter.GetBytes(value);
        }
    }

    public class BoolAddressValue : AddressValue<bool>
    {
        protected override bool ConvertToType(byte[] buffer)
        {
            return BitConverter.ToBoolean(buffer, 0);
        }

        protected override byte[] ConvertFromType(bool value)
        {
            return BitConverter.GetBytes(value);
        }
    }

}