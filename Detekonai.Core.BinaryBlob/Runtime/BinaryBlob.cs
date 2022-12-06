using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Net.Sockets;

namespace Detekonai.Core
{
	//TODO move to native read functions? Ignore endianess and just asume everything is little endian, we can gain some performance
	public sealed class BinaryBlob
    {
        private byte[] buffer;
		private int bufferOffset;
		private int bufferSize;
		private int lastIndex;
		private int prefixSize = 0;
		public BinaryBlobPool Owner { get; private set; }

		public bool InUse { get; private set; } = false;

		public int BytesWritten
		{
			get { return lastIndex; }
		}

		public int BufferSize
		{
			get { return bufferSize;  }
		}

		public int BufferAddress
		{
			get
			{
				return bufferOffset;
			}
		}

		public void PrefixBuffer(int amount) 
		{
			prefixSize = amount;
			JumpIndexToBegin();
		}

		public int RemoveBufferPrefix()
		{
			int old = prefixSize;
			prefixSize = 0;
			JumpIndexToBegin();
			return old;
		}

		public BinaryBlob(BinaryBlobPool owner)
		{
			buffer = owner.GetMemory();
			Owner = owner;
		}

		internal void Assign()
		{
			InUse = true;
		}
		internal void CancelAssign()
		{
			InUse = false;
		}
		internal void Configure(int offset, int size)
		{
			bufferOffset = offset;
			bufferSize = size;
		}

		private int index = 0;
		public int Index
		{
			get
			{
				return index;
			}
			set
			{
				if (value > bufferSize)
				{
					throw new IndexOutOfRangeException("We ran out of buffer space!");
				}
				if (value > lastIndex)
				{
					lastIndex = value;
				}
				index = value;
			}
		}
		public void AddByte(byte val)
		{
			buffer[bufferOffset + Index] = val;
			Index++;
		}

		public void AddShort(short val)
		{
			buffer[bufferOffset + Index] = (byte)val;
			buffer[bufferOffset + Index + 1] = (byte)(val >> 8);
			Index += 2;
		}

		public void AddUShort(ushort val)
		{
			AddShort((short)val);
		}

		public void AddInt(int val)
		{
			buffer[bufferOffset + Index + 0] = (byte)val;
			buffer[bufferOffset + Index + 1] = (byte)(val >> 8);
			buffer[bufferOffset + Index + 2] = (byte)(val >> 16);
			buffer[bufferOffset + Index + 3] = (byte)(val >> 24);
			Index += 4;
		}

		public void AddUInt(uint val)
		{
			AddInt((int)val);
		}
		public void AddLong(long val)
		{
			buffer[bufferOffset + Index + 0] = (byte)val;
			buffer[bufferOffset + Index + 1] = (byte)(val >> 8);
			buffer[bufferOffset + Index + 2] = (byte)(val >> 16);
			buffer[bufferOffset + Index + 3] = (byte)(val >> 24);
			buffer[bufferOffset + Index + 4] = (byte)(val >> 32);
			buffer[bufferOffset + Index + 5] = (byte)(val >> 40);
			buffer[bufferOffset + Index + 6] = (byte)(val >> 48);
			buffer[bufferOffset + Index + 7] = (byte)(val >> 56);
			Index += 8;
		}
		public void AddULong(ulong val)
		{
			AddLong((long)val);
		}
		public void AddBoolean(bool val)
		{
			AddByte(val ? (byte)111 : (byte)0);
		}

		public unsafe void AddSingle(float val)
		{
			AddInt(*(int*)&val);
		}

		public void AddString(string val)
		{
			if (val == null)
			{
				AddInt(-1);
			}
			else
			{
				AddInt(val.Length);
				Index += System.Text.Encoding.UTF8.GetBytes(val, 0, val.Length, buffer, bufferOffset + Index);
			}
		}
		
		public void AddFixedString(string val)
		{
			Index += System.Text.Encoding.UTF8.GetBytes(val, 0, val.Length, buffer, bufferOffset + Index);
		}

		public float ReadSingle()
		{
			float res = BitConverter.ToSingle(buffer, bufferOffset + Index);
			Index += 4;
			return res;
		}

		public byte ReadByte()
		{
			byte b = buffer[bufferOffset + Index];
			Index++;
			return b;
		}

		public short ReadShort()
		{
			short res = BitConverter.ToInt16(buffer, bufferOffset + Index);
			Index += 2;

			return res;
		}
		public int ReadInt()
		{
			int res = BitConverter.ToInt32(buffer, bufferOffset + Index);
			Index += 4;

			return res;
		}


		public ushort ReadUShort()
		{
			ushort res = BitConverter.ToUInt16(buffer, bufferOffset + Index);
			Index += 2;

			return res;
		}
		public uint ReadUInt()
		{
			uint res = BitConverter.ToUInt32(buffer, bufferOffset + Index);
			Index += 4;

			return res;
		}
		public long ReadLong()
		{
			long res = BitConverter.ToInt64(buffer, bufferOffset + Index);
			Index += 8;

			return res;
		}
		public ulong ReadULong()
		{
			ulong res = BitConverter.ToUInt64(buffer, bufferOffset + Index);
			Index += 8;

			return res;
		}

		public bool ReadBoolean()
		{
			byte res = buffer[bufferOffset + Index];
			Index++;

			return res == 111 ? true : false;
		}

		public string ReadString()
		{
			int length = ReadInt();
			if(length == -1)
            {
				return null;
            }
			return ReadFixedString(length);
		}

		public string ReadFixedString(int length)
        {
			string res = System.Text.Encoding.UTF8.GetString(buffer, bufferOffset + Index, length);
			Index += length;
			return res;
		}

		public void CopyDataFrom(byte[] data, int start, int length)
        {
			if (Index + length > bufferSize)
			{
				throw new IndexOutOfRangeException("We ran out of buffer space!");
			}
			Array.Copy(data, start, buffer, bufferOffset + Index, length);
			Index += length;
		}

		public void CopyDataFrom(BinaryBlob other, int length)
		{
			CopyDataFrom(other.buffer, other.bufferOffset+other.index, length);
			other.index += length;
		}

		public void JumpIndexToEnd()
		{
			Index = lastIndex;
		}

		public void JumpIndexToBegin()
		{
			Index = prefixSize;
		}

		public void Release()
		{
			if(InUse)
			{
				lastIndex = 0;
				Index = 0;
				prefixSize = 0;
				InUse = false;
				Owner.ReleaseBlob(this);
			}
		}
	}
}
