using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using NUnit.Framework;

namespace Detekonai.Core.Tests
{
	public class BinaryBlobTest
	{
		// A Test behaves as an ordinary method

		private BinaryBlob blob;
		private BinaryBlobPool pool;

		[SetUp]
		public void InitTest()
		{
			pool = new BinaryBlobPool(10, 64);
			blob = pool.GetBlob();
		}

		[Test]
		public void AddByte_ReadByte_Consistent()
		{
			// BinaryBlob blob = new BinaryBlob();
			blob.AddByte(255);
			Assert.That(blob.Index, Is.EqualTo(1), "Byte index check");
			// Assert.That(blob.Data[0], Is.EqualTo(255),"Byte raw data check");
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadByte(), Is.EqualTo(255), "Byte readback check");
		}

		[Test]
		public void Release_Returns_Blob_To_Pool()
		{
			// BinaryBlob blob = new BinaryBlob();
			blob.AddByte(1);
			blob.Release();

			Assert.That(pool.AvailableBlobs, Is.EqualTo(1), "Check if blob is back in the pool");
			Assert.That(pool.AvailableChunks, Is.EqualTo(10), "Check if memory chunk back in the pool");
		}

		[Test]
		public void Multiple_Release_Returns_Blob_To_Pool_Only_Once()
		{
			// BinaryBlob blob = new BinaryBlob();
			blob.AddByte(1);
			blob.Release();
			blob.Release();
			Assert.That(pool.AvailableBlobs, Is.EqualTo(1), "Check if blob is back in the pool");
			Assert.That(pool.AvailableChunks, Is.EqualTo(10), "Check if memory chunk back in the pool");
		}

		[Test]
		public void AddByte_MultipleValues_ReadByte_Consistent()
		{
			// BinaryBlob blob = new BinaryBlob();
			blob.AddByte(1);
			blob.AddByte(2);
			blob.AddByte(3);
			blob.AddByte(4);
			Assert.That(blob.Index, Is.EqualTo(4), "Byte index check");
			// Assert.That(blob.Data[0], Is.EqualTo(255),"Byte raw data check");
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadByte(), Is.EqualTo(1), "Byte readback check");
			Assert.That(blob.ReadByte(), Is.EqualTo(2), "Byte readback check");
			Assert.That(blob.ReadByte(), Is.EqualTo(3), "Byte readback check");
			Assert.That(blob.ReadByte(), Is.EqualTo(4), "Byte readback check");
		}

		[Test]
		public void AddInt_ReadInt_Consistent()
		{
			blob.AddInt(-12345);
			Assert.That(blob.Index, Is.EqualTo(4), "Index check");
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadInt(), Is.EqualTo(-12345), "Readback check");
		}

		[Test]
		public void AddUShort_ReadUShort_Consistent()
		{
			blob.AddUShort(12314);
			Assert.That(blob.Index, Is.EqualTo(2), "Index check");
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadUShort(), Is.EqualTo(12314), "Readback check");
		}

		[Test]
		public void AddShort_ReadShort_Consistent()
		{
			blob.AddShort(-12314);
			Assert.That(blob.Index, Is.EqualTo(2), "Index check");
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadShort(), Is.EqualTo(-12314), "Readback check");
		}

		[Test]
		public void AddUInt_ReadUInt_Consistent()
		{
			blob.AddUInt(1234512314);
			Assert.That(blob.Index, Is.EqualTo(4), "Index check");
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadUInt(), Is.EqualTo(1234512314), "Readback check");
		}

		[Test]
		public void AddSingle_ReadSingle_Consistent()
		{
			blob.AddSingle(1234.1234f);
			Assert.That(blob.Index, Is.EqualTo(4), "Index check");
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadSingle(), Is.EqualTo(1234.1234f), "Readback check");
		}

		[Test]
		public void AddULong_ReadULong_Consistent()
		{
			blob.AddULong(12345112342314);
			Assert.That(blob.Index, Is.EqualTo(8), "Index check");
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadLong(), Is.EqualTo(12345112342314), "Readback check");
		}

		[Test]
		public void AddLong_ReadLong_Consistent()
		{
			blob.AddLong(-12345112342314);
			Assert.That(blob.Index, Is.EqualTo(8), "Index check");
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadLong(), Is.EqualTo(-12345112342314), "Readback check");
		}

		[Test]
		public void AddBoolean_ReadBoolean_Consistent()
		{
			blob.AddBoolean(true);
			Assert.That(blob.Index, Is.EqualTo(1), "Index check");
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadBoolean(), Is.EqualTo(true), "Readback check");
		}

		[Test]
		public void AddString_ReadString_Consistent()
		{
			blob.AddString("The quick brown fox jumps over the lazy dog");
			Assert.That(blob.Index, Is.EqualTo("The quick brown fox jumps over the lazy dog".Length + 4), "Index check");
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadString(), Is.EqualTo("The quick brown fox jumps over the lazy dog"), "Readback check");
		}

		[Test]
		public void AddString_ReadString_Consistent_if_string_is_null()
		{
			blob.AddString(null);
			Assert.That(blob.Index, Is.EqualTo(4), "Index check");
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadString(), Is.Null, "Readback check");
		}

		[Test]
		public void AddFixedString_ReadFixedString_Consistent()
		{
			blob.AddFixedString("The quick brown fox jumps over the lazy dog");
			Assert.That(blob.Index, Is.EqualTo("The quick brown fox jumps over the lazy dog".Length), "Index check");
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadFixedString("The quick brown fox jumps over the lazy dog".Length), Is.EqualTo("The quick brown fox jumps over the lazy dog"), "Readback check");
		}

		[Test]
		public void PrefixBuffer_Reserve_space_properly()
		{
			blob.PrefixBuffer(4);
			blob.AddString("The quick brown fox jumps over the lazy dog");
			Assert.That(blob.Index, Is.EqualTo("The quick brown fox jumps over the lazy dog".Length + 8), "Index check");
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadString(), Is.EqualTo("The quick brown fox jumps over the lazy dog"), "Readback check1");
			blob.RemoveBufferPrefix();
			blob.JumpIndexToBegin();
			blob.AddInt(1234);
			blob.JumpIndexToBegin();
			Assert.That(blob.ReadInt(), Is.EqualTo(1234), "Readback check2");
			Assert.That(blob.ReadString(), Is.EqualTo("The quick brown fox jumps over the lazy dog"), "Readback check3");
		}

		[Test]
		//[Ignore("We only need to use this time to time")]
		public void Performance_Check1()
		{
			byte[] bwBuffer = new byte[64];

			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			int loopCount = 1000000;

			MemoryStream stream = new MemoryStream(bwBuffer);
			using(BinaryWriter writer = new BinaryWriter(stream))
			{
				sw.Start();
				for(int i = 0; i < loopCount; i++)
				{
					writer.BaseStream.Position = 0;
					writer.Write("alma");
					writer.Write(1234);
				}

				sw.Stop();
				Console.WriteLine($"[BW] write {sw.ElapsedMilliseconds} ms");
				sw.Reset();
				sw.Start();

				using(BinaryReader reader = new BinaryReader(stream))
				{
					for(int i = 0; i < loopCount; i++)
					{
						reader.BaseStream.Position = 0;
						reader.ReadString();
						reader.ReadInt32();
					}
				}
				sw.Stop();
			}
			Console.WriteLine($"[BW] read {sw.ElapsedMilliseconds} ms");
			sw.Reset();
			stream = new MemoryStream(bwBuffer);
			using (BinaryWriter writer = new BinaryWriter(stream))
			{
				sw.Start();
				for (int i = 0; i < loopCount; i++)
				{
					writer.BaseStream.Position = 4;
					writer.Write("alma");
					writer.Write(1234);
				}
				for (int i = 0; i < loopCount; i++)
				{
					writer.BaseStream.Position = 0;
					writer.Write(3456);
				}
				sw.Stop();
				Console.WriteLine($"[BW-prefix] write {sw.ElapsedMilliseconds} ms");
				sw.Reset();
				sw.Start();

				using (BinaryReader reader = new BinaryReader(stream))
				{
					for (int i = 0; i < loopCount; i++)
					{
						reader.BaseStream.Position = 0;
						reader.ReadInt32();
						reader.ReadString();
						reader.ReadInt32();
					}
				}
				sw.Stop();
			}
			Console.WriteLine($"[BW-prefix] read {sw.ElapsedMilliseconds} ms");
			sw.Reset();
			sw.Start();

			for(int i = 0; i < loopCount; i++)
			{
				blob.JumpIndexToBegin();
				blob.AddString("alma");
				blob.AddInt(1234);
			}
			sw.Stop();
			Console.WriteLine($"[BL] write {sw.ElapsedMilliseconds} ms");

			sw.Reset();
			sw.Start();
			for(int i = 0; i < loopCount; i++)
			{
				blob.JumpIndexToBegin();
				blob.ReadString();
				blob.ReadUInt();
			}
			sw.Stop();
			Console.WriteLine($"[BL] read {sw.ElapsedMilliseconds} ms");



			blob.PrefixBuffer(4);
			for (int i = 0; i < loopCount; i++)
			{
				blob.JumpIndexToBegin();
				blob.AddString("alma");
				blob.AddInt(1234);
			}
			blob.RemoveBufferPrefix();
			for (int i = 0; i < loopCount; i++)
			{
				blob.JumpIndexToBegin();
				blob.AddInt(3456);
			}
			sw.Stop();
			Console.WriteLine($"[BL-prefix] write {sw.ElapsedMilliseconds} ms");

			sw.Reset();
			sw.Start();
			for (int i = 0; i < loopCount; i++)
			{
				blob.JumpIndexToBegin();
				blob.ReadInt();
				blob.ReadString();
				blob.ReadInt();
			}
			sw.Stop();
			Console.WriteLine($"[BL-prefix] read {sw.ElapsedMilliseconds} ms");
		}

	}
}
