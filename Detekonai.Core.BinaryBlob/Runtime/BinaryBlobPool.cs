﻿using Detekonai.Core.Common;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Detekonai.Core
{
	public class BinaryBlobPool
	{
		public class BufferOverflowException : Exception
		{
			public BufferOverflowException()
			{
			}

			public BufferOverflowException(string message)
				: base(message)
			{
			}

			public BufferOverflowException(string message, Exception inner)
				: base(message, inner)
			{
			}
		}

		public int AvailableChunks => freeIndexes.Count;
		public int AvailableBlobs => blobs.Count;
		public int BlobSize { get; set; }
		private readonly byte[] memory;
		private readonly ConcurrentQueue<int> freeIndexes = new ConcurrentQueue<int>();
		private readonly ConcurrentBag<BinaryBlob> blobs = new ConcurrentBag<BinaryBlob>();
		private ConcurrentQueue<TaskCompletionSource<BinaryBlob>> tcsQueue = new ConcurrentQueue<TaskCompletionSource<BinaryBlob>>();
		public ILogger Logger { get; set; }

		public byte[] GetMemory()
		{
			return memory;
		}

		public BinaryBlobPool(int chunckAmount, int chunkSize)
		{
			memory = new byte[chunckAmount * chunkSize];
			BlobSize = chunkSize;
			//we use more memory this way but save some complexity and cpu during "Set"
			for (int i = 0; i < chunckAmount; i++)
			{
				freeIndexes.Enqueue(i * chunkSize);
			}
		}

		public BinaryBlob GetBlob()
		{
			if (freeIndexes.TryDequeue(out int offset))
			{
				return GetBlob(offset);
			}
			else
			{
				throw new BufferOverflowException("We ran out of space, increase the buffer size!");
			}
		}

		//async???????????
		public bool TryToGetBlob(out BinaryBlob blob)
		{
			if (freeIndexes.TryDequeue(out int offset))
			{
				blob = GetBlob(offset);
				return true;
			}
			else
			{
				blob = null;
				return false;
			}
		}

		public async Task<BinaryBlob> GetBlobAsync()
		{
			if(freeIndexes.TryDequeue(out int offset))
			{
				return GetBlob(offset);
			}
            else 
			{ 
				var tcs = new TaskCompletionSource<BinaryBlob>();
				tcsQueue.Enqueue(tcs);
				return await tcs.Task;
			}
		}

		private BinaryBlob GetBlob(int offset)
		{
			BinaryBlob blob;
			if (!blobs.TryTake(out blob))
			{
				blob = new BinaryBlob(this);
			}
			blob.Configure(offset, BlobSize);
			blob.Assign();
			Logger?.Log(this, $"{offset} is assigned to a blob");
			return blob;
		}

		internal void ReleaseBlob(BinaryBlob blob)
		{
			if (blob.Owner != this)
			{
				throw new InvalidOperationException("This blob belongs to a different pool!");
			}
			Logger?.Log(this, $"Blob return to the pool, memory chunck {blob.BufferAddress} freed, blobCount: {blobs.Count + 1} freeIndexCount:{freeIndexes.Count + 1}");
			if(tcsQueue.TryDequeue(out TaskCompletionSource<BinaryBlob> tcs))
            {
				blob.Assign();
				if(tcs.TrySetResult(blob))
                {
					return;
                }
				blob.CancelAssign();
			}

			freeIndexes.Enqueue(blob.BufferAddress);
			blob.Configure(0, 0);
			blobs.Add(blob);

		}
	}
}
