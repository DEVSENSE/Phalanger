/*

 Copyright (c) 2004-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Collections;
using PHP;
using PHP.Core;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	/// <summary>
	/// Provides output buffering functionality. 
	/// </summary>
	[DebuggerNonUserCode]
	public class BufferedOutput : TextWriter
	{
		/// <summary>
		/// Position of a chunk of buffered data. 
		/// </summary>
		[Flags]
		public enum ChunkPosition
		{
			First = 1,
			Middle = 2,
			Last = 4
		}

		#region Nested Classes: BufferElement, LevelElement

		// data chunk on one level of buffering:
		private class BufferElement
		{
			internal int size;                   // the number of valid bytes/chars of the data array
			internal System.Array data;          // array containing buffered data
		}

		// representation of one level of buffering:
		private class LevelElement
		{
			internal LevelElement(int index)
			{
				this.index = index;
				this.buffers = new ArrayList();
			}

            /// <summary>
            /// Copies index, name and filter from the element.
            /// </summary>
            /// <param name="element"></param>
            internal LevelElement(LevelElement/*!*/element)
                :this(element.index)
            {
                filter = element.filter;
                levelName = element.levelName;
                userData = element.userData;
            }

			public readonly int index;         // the index of the level in levels array list
			public int size;                   // the size (chars + bytes) of all data stored in the buffers list
			public int[] freeSpace = { 0, 0 };    // the number of free bytes/chars in the last byte/char buffer of buffers
			public ArrayList buffers;          // the list of buffers where data are stored
			public bool containsByteData;      // whether any buffer in the buffers list is of type byte[]
			public bool containsCharData;      // whether any buffer in the buffers list is of type char[]
			public PhpCallback filter;         // user supplied filtering callback
			public object userData;            // arbitrary data supplied by the user
            public string levelName;           // the PHP name of the level, can be null
		}

		#endregion

		#region Fields and Properties

		// the list of LevelElements:
		private ArrayList levels;            // GENERICS: List<LevelElements>

		// the current level of buffering (usually the last one); null iff the buffering is disabled
		private LevelElement level;

		/// <summary>
		/// Minimal sizes of buffers. 
		/// </summary>
		internal readonly int[] minBufferSize = { 2 * 1024, 20 * 1024 };

		/// <summary>
		/// The writer through which character data will be written.
		/// </summary>
		public TextWriter CharSink { get { return charSink; } set { charSink = value; } }
		private TextWriter charSink;

		/// <summary>
		/// The stream through which binary data will be written.
		/// </summary>
		public Stream ByteSink { get { return byteSink; } set { byteSink = value; } }
		private Stream byteSink;

		/// <summary>
		/// Encoding used by <see cref="GetContentAsString"/> converting binary data to a string.
		/// </summary>
		public override Encoding Encoding { get { return encoding; } }
		private Encoding encoding;

		/// <summary>
		/// The buffered binary stream used as for loading binary data to buffers.
		/// </summary>
		public BufferedOutputStream Stream { get { return stream; } }
		private BufferedOutputStream stream;

		/// <summary>
		/// Current buffer level starting from 1. Zero if buffering is disabled.
		/// </summary>
		public int Level { get { return (level != null) ? level.index + 1 : 0; } }

		/// <summary>
		/// The total length of data written to the current level of buffering.
		/// Returns -1 if buffering is disabled.
		/// </summary>
		public int Length { get { return (level != null) ? level.size : -1; } }

		#endregion

		#region Construction

		/// <summary>
		/// Creates buffered output with specified sinks.
		/// </summary>
		/// <param name="enableBuffering">Whether to immediately enable buffering, i.e. increase the level.</param>
		/// <param name="charSink">A writer through which character data will be written.</param>
		/// <param name="byteSink">A stream through which binary data will be written.</param>
		/// <param name="encoding">A encoding used to transform binary data to strings.</param>
		public BufferedOutput(bool enableBuffering, TextWriter charSink, Stream byteSink, Encoding encoding)
		{
			this.charSink = charSink;
			this.byteSink = byteSink;
			this.encoding = encoding;
			stream = new BufferedOutputStream(this);
			levels = new ArrayList();

			if (enableBuffering)
				IncreaseLevel();
		}

		/// <summary>
		/// Creates an instance of <see cref="BufferedOutput"/> having enabled buffering and with sinks set to null sinks.
		/// </summary>
		public BufferedOutput()
			: this(true, TextWriter.Null, System.IO.Stream.Null, Configuration.Application.Globalization.PageEncoding)
		{
		}

		#endregion

		#region Buffer allocation, level changing

		/// <summary>
		/// Gets a buffer where data of requested size and type can be stored. 
		/// </summary>
		/// <param name="sizeNeeded">The number of characters or bytes to be allocated.</param>
		/// <param name="binary">Whether allocated data are bytes or chars.</param>
		/// <param name="buffer">Returns the buffer where data can be written to.</param>
		/// <param name="position">Returns the position where data can be written on.</param>
		/// <returns>The number of allocated characters or bytes.</returns>
		/// <remarks>
		/// The buffer may already exist or new one may be created.
		/// Works on the current level of buffering.
		/// </remarks>
		private int AllocateBuffer(int sizeNeeded, bool binary, out System.Array buffer, out int position)
		{
			Debug.Assert(level != null);

			BufferElement element;
			int chunk;
			int kind = binary ? 1 : 0;

			// close binary buffer:
			level.freeSpace[1 - kind] = 0;

			if (binary) level.containsByteData = true; else level.containsCharData = true;

			// no free space for characters found (no buffer exists, the top buffer isn't a character buffer
			// or the top buffer is full character buffer):
			if (level.freeSpace[kind] == 0)
			{
				// computes the size of buffer to be allocated as min{sizeNeeded,dafaultBufferSize}:
				int size = sizeNeeded;
				if (size < minBufferSize[kind])
				{
					size = minBufferSize[kind];
					level.freeSpace[kind] = size - sizeNeeded;
				}
				else
					level.freeSpace[kind] = 0; // all space in allocated buffer will be occupied

				// allocates a new buffer element for data:
				element = new BufferElement();
				if (binary) buffer = new byte[size]; else buffer = new char[size];
				element.data = buffer;
				element.size = sizeNeeded;   //sizeNeeded <= (buffer size)
				level.buffers.Add(element);

				position = 0;
				chunk = sizeNeeded;

			}
			else
			// some free space found:
			{
				Debug.Assert(level.buffers.Count > 0);

				// available space:
				chunk = (level.freeSpace[kind] < sizeNeeded) ? level.freeSpace[kind] : sizeNeeded;

				element = (BufferElement)level.buffers[level.buffers.Count - 1];
				buffer = element.data;
				position = element.data.Length - level.freeSpace[kind];
				element.size += chunk;
				level.freeSpace[kind] -= chunk;
			}
			level.size += chunk;
			return chunk;
		}


		/// <summary>
		/// Adds a new level of buffering on the top of the levels stack.
		/// </summary>
		/// <remarks>Returns the new level index.</remarks>
		public int IncreaseLevel()
		{
			return levels.Add(level = new LevelElement(levels.Count));
		}


		/// <summary>
		/// Destroys the top level of buffering. 
		/// </summary>
		/// <param name="flush">Whether to flush data on the current level. Data will be discarded if not set.</param>
		/// <remarks>Returns the current level index after decreasing.</remarks>
		public int DecreaseLevel(bool flush)
		{
			if (level == null) throw new InvalidOperationException(CoreResources.GetString("output_buffering_disabled"));

			if (flush) InternalFlush();

			int top = levels.Count - 1;
			levels.RemoveAt(top);

			if (top != 0)
			{
				level = (LevelElement)levels[top - 1];
				return top - 1;
			}
			else
			{
				level = null;
				return -1;
			}
		}

		#endregion

		#region Filtering

		/// <summary>
		/// Assignes an arbitrary data to the specified level of buffering.
		/// </summary>
		/// <param name="data">Null reference clears assigned data.</param>
		/// <param name="levelIndex">The level of buffering which the filter to associate with.</param>
		/// <remarks>Data are filtered when flushed.</remarks>
		public void SetUserData(object data, int levelIndex)
		{
			if (levelIndex < 0 || levelIndex >= levels.Count) throw new ArgumentOutOfRangeException("levelIndex");

			((LevelElement)levels[levelIndex]).userData = data;
		}


		/// <summary>
		/// Assignes the arbitrary data to the current level of buffering. 
		/// </summary>
		/// <param name="data">The reference to data.</param>
		/// <remarks>Data are filtered when flushed.</remarks>
		public void SetUserData(object data)
		{
			if (level == null) throw new InvalidOperationException(CoreResources.GetString("output_buffering_disabled"));
			level.userData = data;
		}


		/// <summary>
		/// Assignes the filtering callback to the specified level of buffering.
		/// </summary>
		/// <param name="filter">The filter. Null reference means no filter.</param>
		/// <param name="levelIndex">The level of buffering which the filter to associate with.</param>
		/// <remarks>Data are filtered when flushed.</remarks>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="levelIndex"/> is out of range.</exception>
		public void SetFilter(PhpCallback filter, int levelIndex)
		{
			if (levelIndex < 0 || levelIndex >= levels.Count)
				throw new ArgumentOutOfRangeException("levelIndex");

			((LevelElement)levels[levelIndex]).filter = filter;
		}


		/// <summary>
		/// Assignes the filtering callback to the current level of buffering. 
		/// </summary>
		/// <param name="filter">The filter. Null reference means no filter.</param>
		/// <remarks>Data are filtered when flushed.</remarks>
		/// <exception cref="InvalidOperationException">Output buffering is disabled.</exception>
		public void SetFilter(PhpCallback filter)
		{
			if (level == null)
				throw new InvalidOperationException(CoreResources.GetString("output_buffering_disabled"));

			level.filter = filter;
		}

		/// <summary>
		/// Gets the filtering callback defined on the specified level of buffering.
		/// </summary>
		/// <param name="levelIndex">The level of buffering which the filter to associate with.</param>
		/// <returns>The callback or <B>null</B> if no filter has been defined.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="levelIndex"/> is out of range.</exception>
		public PhpCallback GetFilter(int levelIndex)
		{
			if (levelIndex < 0 || levelIndex >= levels.Count)
				throw new ArgumentOutOfRangeException("levelIndex");

			return ((LevelElement)levels[levelIndex]).filter;
		}

		/// <summary>
		/// Gets the filtering callback defined on the current level of buffering.
		/// </summary>
		/// <returns>The callback or <B>null</B> if no filter has been defined.</returns>
		/// <exception cref="InvalidOperationException">Output buffering is disabled.</exception>
		public PhpCallback GetFilter()
		{
			if (level == null)
				throw new InvalidOperationException(CoreResources.GetString("output_buffering_disabled"));

			return level.filter;
		}

        /// <summary>
        /// Set the level name.
        /// </summary>
        /// <param name="levelIndex">Index of the level from 1.</param>
        /// <param name="levelName">New name of the level.</param>
        public void SetLevelName(int levelIndex, string levelName)
        {
            if (levelIndex < 0 || levelIndex >= Level)
                throw new ArgumentOutOfRangeException("levelIndex");

            LevelElement element = (LevelElement)levels[levelIndex];

            element.levelName = levelName;
        }

        /// <summary>
        /// Get the name of the level. If the level name is null, the filter.ToString() is used.
        /// </summary>
        /// <param name="levelIndex">Index of the level from 1.</param>
        /// <returns></returns>
        public string GetLevelName(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= Level)
                throw new ArgumentOutOfRangeException("levelIndex");

            LevelElement element = (LevelElement)levels[levelIndex];

            string levelName = element.levelName;

            if (levelName == null)
            {
			    PhpCallback filter = element.filter;
                levelName = (filter != null) ? ((IPhpConvertible)filter).ToString() : "default output handler";
            }

            return levelName;
        }

		#endregion

		#region Clean, Flush, FlushAll

		/// <summary>
		/// Discards data on the current level of buffering.
		/// </summary>
		public void Clean()
		{
			if (level == null) return;

			levels[level.index] = level = new LevelElement(level);
		}


		/// <summary>
		/// Flushes all data from all buffers to sinks. Discards all data and all levels of buffering.
		/// Disables output buffering.
		/// </summary>
		public void FlushAll()
		{
			for (int i = levels.Count - 1; i >= 0; i--)
			{
				level = (LevelElement)levels[i];
				InternalFlush();
			}
			levels.Clear();
			level = null;
		}


		/// <summary>
		/// Flushes data on current level of buffering to sinks or to the previous level and discards them.
		/// </summary>
		public override void Flush()
		{
			if (level == null) return;
			InternalFlush();
            Clean();
		}


		/// <summary>
		/// Flushes data on current level of buffering to the sinks or to the previous level.
		/// The current level clean up MUST follow this method's call.
		/// </summary>
		internal void InternalFlush()
		{
			Debug.Assert(level != null);

			if (level.filter != null)
			{
				ChunkPosition chunk_position = ChunkPosition.First | ChunkPosition.Middle | ChunkPosition.Last;

				// writes data to the current level of buffering or to sinks depending on the level count:
				if (level.index > 0)
				{
					// gets data from user's callback:
					object data = level.filter.Invoke(GetContent(), chunk_position);

					// store level to allow its restore:
					LevelElement old_level = level;

					// temporarily decreases the level of buffering toredirect writes to the lower level:
					level = (LevelElement)levels[level.index - 1];

					// checks whether the filtered data are binary at first; if not so, converts them to a string:
					PhpBytes bin = data as PhpBytes;
					if (bin != null)
						stream.Write(bin.ReadonlyData, 0, bin.Length);
					else
						this.Write(PHP.Core.Convert.ObjectToString(data));

					// restore the level of buffering:
					level = old_level;
				}
				else
				{
					// gets data from user's callback:
					object data = level.filter.Invoke(GetContent(), chunk_position);

					// checks whether the filtered data are binary at first; if not so, converts them to a string:
					PhpBytes bin = data as PhpBytes;
					if (bin != null)
					{
                        if (bin.Length > 0) byteSink.Write(bin.ReadonlyData, 0, bin.Length);
					}
					else
						charSink.Write(PHP.Core.Convert.ObjectToString(data));
				}
			}
			else
			{
				if (level.index > 0)
				{
					// joins levels (data are not copied => the current level MUST be cleaned up after the return from this method):
					if (level.size > 0)
					{
						LevelElement lower_level = (LevelElement)levels[level.index - 1];

						lower_level.buffers.AddRange(level.buffers);
						lower_level.size += level.size;
						lower_level.freeSpace = level.freeSpace;      // free space in the last buffer of the level
						lower_level.containsByteData |= level.containsByteData;
						lower_level.containsCharData |= level.containsCharData;
					}
				}
				else
				{
					// writes top-level data to sinks:
					for (int i = 0; i < level.buffers.Count; i++)
					{
						BufferElement element = (BufferElement)level.buffers[i];

						byte[] bytes = element.data as byte[];
						if (bytes != null)
							byteSink.Write(bytes, 0, element.size);
						else
							charSink.Write((char[])element.data, 0, element.size);
					}
				}
			}
		}

		#endregion

		#region GetContent

		/// <summary>
		/// Gets a content of buffers on current buffering level converted to string regardless of its type.
		/// </summary>
		/// <returns>
		/// The content converted to a string. Binary data are converted using <see cref="Encoding"/>.
		/// </returns>
		public string GetContentAsString()
		{
			if (level == null) return null;

			StringBuilder result = new StringBuilder(level.size, level.size);

			for (int i = 0; i < level.buffers.Count; i++)
			{
				BufferElement element = (BufferElement)level.buffers[i];

				byte[] bytes = element.data as byte[];
				if (bytes != null)
					result.Append(encoding.GetString(bytes, 0, element.size));
				else
					result.Append((char[])element.data, 0, element.size);
			}
			return result.ToString();
		}

		/// <summary>
		/// Gets a content of buffers on current buffering level.
		/// </summary>
		/// <returns>The content as <see cref="string"/> or <see cref="PhpBytes"/> or a 
		/// <b>null</b> reference if output buffering is disable.</returns>
		/// <remarks>
		/// Character data are returned unchanged, binary data are converted to string by 
		/// the <see cref="System.Text.Encoding.GetString"/> method of the current encoding.
		/// </remarks>
		public object GetContent()
		{
			if (level == null) return null;
			if (level.size == 0) return String.Empty;

			// contains characters only:
			if (!level.containsByteData)
			{
				StringBuilder result = new StringBuilder(level.size, level.size);

				for (int i = 0; i < level.buffers.Count; i++)
				{
					BufferElement element = (BufferElement)level.buffers[i];
					result.Append((char[])element.data, 0, element.size);
				}
				return result.ToString();
			}
			else
				// contains bytes only:
				if (!level.containsCharData)
				{
					var result = new byte[level.size];

					for (int i = 0, k = 0; i < level.buffers.Count; i++)
					{
						BufferElement element = (BufferElement)level.buffers[i];
						Array.Copy(element.data, 0, result, k, element.size);
						k += element.size;
					}
					return new PhpBytes(result);

				}
				else
				// contains both bytes and characters:
				{
					return GetContentAsString();
				}
		}

		#endregion

		#region Write, WriteBytes

		/// <summary>
		/// Writes an array of bytes to the output buffer.
		/// </summary>
		/// <param name="value">Bytes to be written.</param>
		public void WriteBytes(byte[] value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (level == null)
				throw new InvalidOperationException(CoreResources.GetString("output_buffering_disabled"));

			WriteInternal(value, true, 0, value.Length);
		}


		/// <summary>
		/// Writes a subarray of bytes to the output buffer.
		/// </summary>
		/// <param name="value">Bytes to be written.</param>
		/// <param name="index">Starting index in the array.</param>
		/// <param name="count">The number of characters to write.</param>
		public void WriteBytes(byte[] value, int index, int count)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (index < 0 || index + count > value.Length)
				throw new ArgumentOutOfRangeException("index");
			if (count < 0)
				throw new ArgumentOutOfRangeException("count");
			if (level == null)
				throw new InvalidOperationException(CoreResources.GetString("output_buffering_disabled"));

			WriteInternal(value, true, index, count);
		}


		/// <summary>
		/// Writes a subarray of characters to the output buffer.
		/// </summary>
		/// <param name="value">Characters to be written.</param>
		public override void Write(char[] value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (level == null)
				throw new InvalidOperationException(CoreResources.GetString("output_buffering_disabled"));

			WriteInternal(value, false, 0, value.Length);
		}


		/// <summary>
		/// Writes a subarray of characters to the output buffer.
		/// </summary>
		/// <param name="value">The value to be written.</param>
		/// <param name="index">Starting index in the array.</param>
		/// <param name="count">The number of characters to write.</param>
		public override void Write(char[] value, int index, int count)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (index < 0 || index + count > value.Length)
				throw new ArgumentOutOfRangeException("index");
			if (count < 0)
				throw new ArgumentOutOfRangeException("count");
			if (level == null)
				throw new InvalidOperationException(CoreResources.GetString("output_buffering_disabled"));

			WriteInternal(value, false, index, count);
		}

		/// <summary>
		/// Writes a subarray to the output buffer.
		/// </summary>
		/// <param name="value">The value to be written.</param>
		/// <param name="binary">The type of items in array (byte/char).</param>
		/// <param name="index">Starting index in the array.</param>
		/// <param name="count">The number of items to write.</param>
		internal void WriteInternal(Array value, bool binary, int index, int count)
		{
			int position;
			System.Array buffer;
			int length = count;
			int chunk;

			// writes initial sequence of characters to buffer:
			chunk = AllocateBuffer(length, binary, out buffer, out position);
			length -= chunk;
			Array.Copy(value, index, buffer, position, chunk);

			// if not all characters has been written writes the rest to the next buffer:
			if (length > 0)
			{
				AllocateBuffer(length, binary, out buffer, out position);
				Array.Copy(value, index + chunk, buffer, position, length);
			}
		}

		/// <summary>
		/// Writes a single character to the output buffer.
		/// </summary>
		/// <param name="value">The value to be written.</param>
		public override void Write(char value)
		{
			if (level == null)
				throw new InvalidOperationException(CoreResources.GetString("output_buffering_disabled"));

			int position;
			Array buffer;

			AllocateBuffer(1, false, out buffer, out position);
			((char[])buffer)[position] = value;
		}


		/// <summary>
		/// Writes a string value to the output buffer.
		/// </summary>
		/// <param name="value">The value to be written.</param>
		public override void Write(string value)
		{
			if (level == null)
				throw new InvalidOperationException(CoreResources.GetString("output_buffering_disabled"));

			if (value == null) value = String.Empty;

			int position;
			Array buffer;
			int length = value.Length;
			int chunk;

			// writes initial sequence of characters to buffer:
			chunk = AllocateBuffer(length, false, out buffer, out position);
			length -= chunk;
			value.CopyTo(0, (char[])buffer, position, chunk);

			// if not all characters written then writes the rest to the next buffer:
			if (length > 0)
			{
				AllocateBuffer(length, false, out buffer, out position);
				value.CopyTo(chunk, (char[])buffer, position, length);
			}
		}

		#endregion

		#region GetLevelInfo

		/// <summary>
		/// Gets some information about a specified level.
		/// </summary>
		/// <param name="levelIndex">Level index starting from 1.</param>
		/// <param name="filter">Filtering callback (if any).</param>
		/// <param name="size">Data size.</param>
		public void GetLevelInfo(int levelIndex, out PhpCallback filter, out int size)
		{
			if (levelIndex < 1 || levelIndex > Level)
				throw new ArgumentOutOfRangeException("levelIndex");

			LevelElement element = (LevelElement)levels[levelIndex - 1];
			filter = element.filter;
			size = element.size;
		}

        /// <summary>
        /// Find level index by the filter callback.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public int FindLevelByFilter(PhpCallback filter)
        {
            if (levels != null && filter != null)
                for (int i = 0; i < Level; ++i)
                    if (((LevelElement)levels[i]).filter == filter)
                        return i;

            return -1;
        }

		#endregion
	}


	/// <summary>
	/// Provides output buffering of streams.
	/// </summary>
	public class BufferedOutputStream : Stream
	{
		private BufferedOutput output;

		public BufferedOutputStream(BufferedOutput output)
		{
			this.output = output;
		}

		public override bool CanRead { get { return false; } }
		public override bool CanSeek { get { return false; } }
		public override bool CanWrite { get { return true; } }

		public override void Flush()
		{
			output.Flush();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			output.WriteBytes(buffer, offset, count);
		}


		#region Unsupported functionality

		public override long Length
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public override long Position
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		#endregion

	}
}
