/*

 Copyright (c) 2004-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Web;
using PHP.Core;

namespace PHP.Library
{
	/// <summary>
	/// PHP output control functions implementation. 
	/// </summary>
	/// <threadsafety static="true"/>
    [ImplementsExtension(LibraryDescriptor.ExtCore)] 
	public static class Output
	{
		[Flags]
		public enum _ChunkPosition
		{
			[ImplementsConstant("PHP_OUTPUT_HANDLER_START")]
			First = BufferedOutput.ChunkPosition.First,
			[ImplementsConstant("PHP_OUTPUT_HANDLER_CONT")]
			Middle = BufferedOutput.ChunkPosition.Middle,
			[ImplementsConstant("PHP_OUTPUT_HANDLER_END")]
			Last = BufferedOutput.ChunkPosition.Last
		}

		#region fprintf, vprintf

		/// <summary>
		/// Generates output according to the specified formatting string.
		/// </summary>
        /// <param name="format">The formatting string. See also the <b>sprintf</b> function (<see cref="PhpStrings.Format"/>).</param>
		/// <param name="args">Variables to format.</param>
        /// <returns>Returns the length of the outputted string. </returns>
		[ImplementsFunction("printf")]
		public static int PrintFormatted(string format, params object[] args)
		{
            string formattedString = PhpStrings.FormatInternal(format, args);

            ScriptContext.CurrentContext.Output.Write(formattedString);

            return formattedString.Length;
		}

		/// <summary>
		/// Generates output according to the specified formatting string.
		/// </summary>
		/// <param name="format">The formatting string.</param>
		/// <param name="args">Array of variables to format.</param>
        /// <returns>Returns the length of the outputted string. </returns>
		[ImplementsFunction("vprintf")]
		public static int PrintFormatted(string format, PhpArray args)
		{
            string formattedString = PhpStrings.Format(format, args);

			ScriptContext.CurrentContext.Output.Write(formattedString);

            return formattedString.Length;
		}

		#endregion

		#region ob_start

		/// <summary>
		/// Increases the level of buffering and enables output buffering if disabled.
		/// </summary>
		/// <returns>Always true.</returns>
		[ImplementsFunction("ob_start")]
		public static bool Start()
		{
			return Start(null, 0, true);
		}

		/// <summary>
		/// Increases the level of buffering, enables output buffering if disabled and assignes the filtering callback
		/// to the new level of buffering.
		/// </summary>
		/// <param name="filter">The filtering callback. Ignores invalid callbacks.</param>
		/// <returns>Whether the filter is empty or a valid callback.</returns>
		[ImplementsFunction("ob_start")]
		public static bool Start(PhpCallback filter)
		{
			return Start(filter, 0, true);
		}

		/// <summary>
		/// Increases the level of buffering, enables output buffering if disabled and assignes the filtering callback
		/// to the new level of buffering.
		/// </summary>
		/// <param name="filter">The filtering callback. Ignores invalid callbacks.</param>
		/// <param name="chunkSize">Not supported.</param>
		/// <returns>Whether the filter is empty or a valid callback.</returns>
		[ImplementsFunction("ob_start")]
		public static bool Start(PhpCallback filter, int chunkSize)
		{
			return Start(filter, chunkSize, true);
		}

		/// <summary>
		/// Increases the level of buffering, enables output buffering if disabled and assignes the filtering callback
		/// to the new level of buffering.
		/// </summary>
		/// <param name="filter">The filtering callback. Ignores invalid callbacks.</param>
		/// <param name="chunkSize">Not supported.</param>
		/// <param name="erase">Not supported.</param>
		/// <returns>Whether the filter is valid callback.</returns>
		[ImplementsFunction("ob_start")]
		public static bool Start(PhpCallback filter, int chunkSize, bool erase)
		{
			if (chunkSize != 0)
				PhpException.ArgumentValueNotSupported("chunkSize");
			if (!erase)
				PhpException.ArgumentValueNotSupported("erase");

			ScriptContext context = ScriptContext.CurrentContext;

			context.BufferedOutput.IncreaseLevel();

			bool result = true;

			// skips filter setting if filter is not specified or valid:
			if (filter != null && (result = filter.Bind()))
				context.BufferedOutput.SetFilter(filter);

			context.IsOutputBuffered = true;
			return result;
		}

		#endregion

		#region ob_clean, ob_end_clean, ob_end_flush

		/// <summary>
		/// Discards the contents of the current level of buffering.
        /// No value is returned.
		/// </summary>
		[ImplementsFunction("ob_clean")]
		public static void Clean()
		{
			ScriptContext.CurrentContext.BufferedOutput.Clean();
		}

		/// <summary>
		/// Discards the contents of the current level of buffering and decreases the level.
		/// </summary>
		/// <returns>Whether the content was discarded and the level was decreased.</returns>
		[ImplementsFunction("ob_end_clean")]
		public static bool EndAndClean()
		{
			return EndInternal(ScriptContext.CurrentContext, false);
		}

		/// <summary>
		/// Flushes the contents of the current level of buffering and decreases the level.
		/// </summary>
		/// <returns>Whether the content was discarded and the level was decreased.</returns>
		[ImplementsFunction("ob_end_flush")]
		public static bool EndAndFlush()
		{
			return EndInternal(ScriptContext.CurrentContext, true);
		}

		/// <summary>
		/// Decreases the level of buffering and discards or flushes data on the current level of buffering.
		/// </summary>
		/// <param name="context">Current script context.</param>
		/// <param name="flush">Whether to flush data.</param>
		/// <returns>Whether the content was discarded and the level was decreased.</returns>
		private static bool EndInternal(ScriptContext/*!*/ context, bool flush)
		{
			BufferedOutput buf = context.BufferedOutput;

			if (buf.Level == 0)
			{
				PhpException.Throw(PhpError.Notice, CoreResources.GetString("output_buffering_disabled"));
				return false;
			}

			if (buf.DecreaseLevel(flush) < 0)
				context.IsOutputBuffered = false;

			return true;
		}

		#endregion

		#region ob_get_clean, ob_get_contents, ob_get_flush, ob_get_level, ob_get_length, ob_get_status

		/// <summary>
		/// Gets the contents of the current buffer and cleans it.
		/// </summary>
		/// <returns>The content of type <see cref="string"/> or <see cref="PhpBytes"/>.</returns>
		[ImplementsFunction("ob_get_clean")]
		[return: CastToFalse]
		public static object GetAndClean()
		{
			ScriptContext context = ScriptContext.CurrentContext;
			BufferedOutput bo = context.BufferedOutput;

			object result = bo.GetContent();
			bo.Clean();
			EndInternal(context, true);
			return result;
		}

		/// <summary>
		/// Gets the content of the current buffer.
		/// </summary>
		/// <returns>The content of type <see cref="string"/> or <see cref="PhpBytes"/>.</returns>
		[ImplementsFunction("ob_get_contents")]
		[return: CastToFalse]
		public static object GetContents()
		{
			return ScriptContext.CurrentContext.BufferedOutput.GetContent();
		}

		/// <summary>
		/// Gets the content of the current buffer and decreases the level of buffering.
		/// </summary>
		/// <returns>The content of the buffer.</returns>
		[ImplementsFunction("ob_get_flush")]
		public static object GetAndFlush()
		{
			ScriptContext context = ScriptContext.CurrentContext;
			BufferedOutput bo = context.BufferedOutput;

			object result = bo.GetContent();
			EndInternal(context, true);
			return result;
		}

		/// <summary>
		/// Retrieves the level of buffering.
		/// </summary>
		/// <returns>The level of buffering.</returns>
		[ImplementsFunction("ob_get_level")]
		public static int GetLevel()
		{
			return ScriptContext.CurrentContext.BufferedOutput.Level;
		}

		/// <summary>
		/// Retrieves the length of the output buffer.
		/// </summary>
		/// <returns>The length of the contents in the output buffer or <B>false</B>, if output buffering isn't active.</returns>
		[ImplementsFunction("ob_get_length")]
		[return: CastToFalse]
		public static int GetLength()
		{
			return ScriptContext.CurrentContext.BufferedOutput.Length;
		}

		/// <summary>
		/// Get the status of the current or all output buffers.
		/// </summary>
		/// <returns>The array of name => value pairs containing information.</returns>
		[ImplementsFunction("ob_get_status")]
		public static PhpArray GetStatus()
		{
			return GetStatus(false);
		}

		/// <summary>
		/// Get the status of the current or all output buffers.
		/// </summary>
		/// <param name="full">Whether to retrieve extended information about all levels of buffering or about the current one.</param>
		/// <returns>The array of name => value pairs containing information.</returns>
		[ImplementsFunction("ob_get_status")]
		public static PhpArray GetStatus(bool full)
		{
			BufferedOutput bo = ScriptContext.CurrentContext.BufferedOutput;
			PhpArray result;

			if (full)
			{
				result = new PhpArray(bo.Level, 0);
				for (int i = 1; i <= bo.Level; i++)
					result.Add(i, GetLevelStatus(bo, i));
			}
			else if (bo.Level > 0)
			{
				result = GetLevelStatus(bo, bo.Level);
				result.Add("level", bo.Level);
			}
			else
				result = new PhpArray(0, 0);
			return result;
		}

		private static PhpArray/*!*/ GetLevelStatus(BufferedOutput/*!*/ bo, int index)
		{
			PhpArray result = new PhpArray(0, 3);

			PhpCallback filter;
			int size;
			bo.GetLevelInfo(index, out filter, out size);

			if (filter != null)
			{
				result.Add("type", 1);
				result.Add("name", ((IPhpConvertible)filter).ToString());
			}
			else
			{
				result.Add("type", 0);
			}
			result.Add("buffer_size", size);

			return result;
		}

		#endregion

		#region flush, ob_flush

		/// <summary>
		/// Flush the output buffer of the HTTP server. Has no effect on data buffered in Phalanger output buffers.
        /// No value is returned.
		/// </summary>
		[ImplementsFunction("flush")]
		public static void FlushHttpBuffers()
		{
			HttpContext http_context = HttpContext.Current;
			if (http_context != null) http_context.Response.Flush();
		}

		/// <summary>
		/// Flushes data from the current level of buffering to the previous one or to the client 
		/// if the current one is the first one. Applies the filter assigned to the current level (if any).
        /// No value is returned.
		/// </summary>
		[ImplementsFunction("ob_flush")]
		public static void FlushOutputBuffer()
		{
			ScriptContext.CurrentContext.BufferedOutput.Flush();
		}

		#endregion

		#region ob_implicit_flush

		/// <summary>
		/// Switches implicit flushing on. 
        /// No value is returned.
		/// </summary>
		/// <remarks>Affects the current script context.</remarks>
        [ImplementsFunction("ob_implicit_flush")]
		public static void ImplicitFlush()
		{
			HttpContext http_context = HttpContext.Current;
			if (http_context != null) http_context.Response.BufferOutput = true;
		}

		/// <summary>
		/// Switches implicit flushing on or off.
        /// No value is returned.
		/// </summary>
		/// <param name="doFlush">Do flush implicitly?</param>
		/// <remarks>
		/// Affects the current script context.
		///
		/// There is a bug in the PHP implementation of this function: 
		/// "Turning implicit flushing on will disable output buffering, the output buffers current output 
		/// will be sent as if ob_end_flush() had been called."
		/// Actually, this is not true (PHP doesn't do that) and in fact it is nonsense because 
		/// ob_end_flush only flushes and destroys one level of buffering. 
		/// It would be more meaningful if ob_implicit_flush function had flushed and destroyed all existing buffers
		/// and so disabled output buffering. 
		/// </remarks>  
		[ImplementsFunction("ob_implicit_flush")]
		public static void ImplicitFlush(bool doFlush)
		{
			HttpContext http_context = HttpContext.Current;
			if (http_context != null) http_context.Response.BufferOutput = doFlush;
		}

		#endregion

		#region ob_list_handlers

		[ImplementsFunction("ob_list_handlers")]
		public static PhpArray GetHandlers()
		{
			BufferedOutput bo = ScriptContext.CurrentContext.BufferedOutput;
			PhpArray result = new PhpArray(bo.Level, 0);

			for (int i = 0; i < bo.Level; i++)
			{
				result.Add(bo.GetLevelName(i));
			}

			return result;
		}

		#endregion

		#region ob_gzhandler (NS)

		/// <summary>
		/// Compresses data by gzip compression. Not supported.
		/// </summary>
		/// <param name="data">Data to compress.</param>
		/// <returns>Compressed data.</returns>
		[ImplementsFunction("ob_gzhandler")]
		public static PhpBytes GzipHandler(string data)
		{
			return GzipHandler(data, 4);
		}

		/// <summary>
		/// Compresses data by gzip compression. Not supported.
		/// </summary>
		/// <param name="data">Data to compress.</param>
		/// <param name="mode">Compression mode.</param>
		/// <returns>Compressed data.</returns>
		[ImplementsFunction("ob_gzhandler")]
		public static PhpBytes GzipHandler(string data, int mode)
		{
			ScriptContext context = ScriptContext.CurrentContext;

			// redirects output to the sink to allow error reporting:
			context.IsOutputBuffered = false;
			PhpException.FunctionNotSupported(PhpError.Notice);
			context.IsOutputBuffered = true;

			if (data == null) return null;
			return new PhpBytes(Configuration.Application.Globalization.PageEncoding.GetBytes(data));
		}

		#endregion
	}
}
