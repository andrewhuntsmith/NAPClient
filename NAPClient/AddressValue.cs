using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace NAPClient
{
    public abstract class AddressValue<T>
    {
        public List<int> Offsets;
        public T Value;
        public T PreviousValue;
        public Action<T, T> ValueUpdated;
        protected abstract int DataSize();

        public void UpdateValue()
        {
            int bytesRead = 0;
            int i;
            List<byte[]> bufferList = new List<byte[]>();
            for (i = 0; i < Offsets.Count; i++)
            {
                int pointer = i == 0 ? 0 : BitConverter.ToInt32(bufferList[i - 1], 0);
                var byteArray = i < Offsets.Count - 1 ? new byte[8] : new byte[DataSize()];
                bufferList.Add(byteArray);
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
                var byteArray = new byte[8];
                bufferList.Add(byteArray);
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
        protected override int DataSize() => sizeof(double);

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
        protected override int DataSize() => sizeof(int);

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
        protected override int DataSize() => sizeof(bool);

        protected override bool ConvertToType(byte[] buffer)
        {
            return BitConverter.ToBoolean(buffer, 0);
        }

        protected override byte[] ConvertFromType(bool value)
        {
            return BitConverter.GetBytes(value);
        }
    }

    public class IntPtrAddressValue : AddressValue<IntPtr>
    {
        protected override int DataSize() => sizeof(int);

        protected override IntPtr ConvertToType(byte[] buffer)
        {
            return (IntPtr)BitConverter.ToInt32(buffer, 0);
        }

        protected override byte[] ConvertFromType(IntPtr value)
        {
            return BitConverter.GetBytes(value.ToInt32());
        }

        public int AsInt()
        {
            return Value.ToInt32();
        }
    }

    public class StringAddressValue : AddressValue<string>
    {
        public int StringLength;
        protected override int DataSize() => StringLength;

        protected override byte[] ConvertFromType(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        protected override string ConvertToType(byte[] buffer)
        {
            var strArray = new byte[buffer.Length];
            Array.Copy(buffer, strArray, strArray.Length);
            return Encoding.UTF8.GetString(strArray);
        }
    }

    public class ByteArrayAddressValue : AddressValue<byte[]>
    {
        public int ArraySize;
        protected override int DataSize() => ArraySize;

        protected override byte[] ConvertFromType(byte[] value)
        {
            return value.ToArray();
        }

        protected override byte[] ConvertToType(byte[] buffer)
        {
            return buffer.ToArray();
        }
    }
}