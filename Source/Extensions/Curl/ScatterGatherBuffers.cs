using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Library.Curl
{
    internal class ScatterGatherBuffers
    {
        // Fields
        private int chunkCount;
        private MemoryChunk currentChunk;
        private MemoryChunk headChunk;
        private int nextChunkLength;
        private int totalLength;

        // Properties
        private bool Empty
        {
            get
            {
                if (this.headChunk != null)
                {
                    return (this.chunkCount == 0);
                }
                return true;
            }
        }

        internal int Length
        {
            get { return this.totalLength; }
        }
        
        // Nested Types
        private class MemoryChunk
        {
            // Fields
            internal byte[] Buffer;
            internal int FreeOffset;
            internal ScatterGatherBuffers.MemoryChunk Next;

            // Methods
            internal MemoryChunk(int bufferSize)
            {
                this.Buffer = new byte[bufferSize];
            }
        }

        internal ScatterGatherBuffers(long totalSize)
        {
            this.nextChunkLength = 0x400;
            if (totalSize > 0L)
            {
                this.currentChunk = AllocateMemoryChunk((totalSize > 0x7fffffffL) ? 0x7fffffff : ((int)totalSize));
            }
        }

        private MemoryChunk AllocateMemoryChunk(int newSize)
        {
            if (newSize > this.nextChunkLength)
            {
                this.nextChunkLength = newSize;
            }
            MemoryChunk chunk = new MemoryChunk(this.nextChunkLength);
            if (this.Empty)
            {
                this.headChunk = chunk;
            }
            this.nextChunkLength *= 2;
            this.chunkCount++;
            return chunk;
        }

        internal void Write(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int num = this.Empty ? 0 : (currentChunk.Buffer.Length - currentChunk.FreeOffset);
                if (num == 0)
                {
                    MemoryChunk chunk = AllocateMemoryChunk(count);
                    if (currentChunk != null)
                    {
                        currentChunk.Next = chunk;
                    }
                    currentChunk = chunk;
                }
                int num2 = (count < num) ? count : num;
                Buffer.BlockCopy(buffer, offset, currentChunk.Buffer, currentChunk.FreeOffset, num2);
                offset += num2;
                count -= num2;

                totalLength += num2;
                currentChunk.FreeOffset += num2;
            }
        }


        /// <summary>
        /// Returns all the data as array of bytes
        /// </summary>
        /// <param name="headReserveToAllocate">Reserve to allocate at the begining of the array. (e.g. HTTP header can be places there)</param>
        public byte[] ToArray(int headReserveToAllocate = 0)
        {
            if (this.Empty)
            {
                return null;
            }

            byte[] dst = new byte[headReserveToAllocate + Length];

            int dstOffset = headReserveToAllocate;
            for (MemoryChunk chunk = this.headChunk; chunk != null; chunk = chunk.Next)
            {
                //chunk.FreeOffset can be considered size of chunk.Buffer

                Buffer.BlockCopy(chunk.Buffer, 0, dst, dstOffset, chunk.FreeOffset);
                dstOffset += chunk.FreeOffset;
            }
            return dst;
        }


    }
}
