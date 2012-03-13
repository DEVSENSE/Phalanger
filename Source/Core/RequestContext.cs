/*

 Copyright (c) 2005-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using PHP.Core.Reflection;
using PHP.Core.Emit;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	/// <summary>
	/// Represents a set of data associated with the current web request targeting PHP scripts.
	/// </summary>
	[Serializable]
	public sealed partial class RequestContext
	{
		#region Fields, Properties, Events

		/// <summary>
		/// Set when the context started finalization.
		/// </summary>
		private bool disposed = false;

		/// <summary>
		/// Current script context.
		/// </summary>
		public ScriptContext/*!*/ ScriptContext { get { return scriptContext; } }
		internal ScriptContext/*!*/ scriptContext;

		/// <summary>
		/// Gets the original value of response encoding set in ASP.NET configuration.
		/// </summary>
		public Encoding DefaultResponseEncoding { get { return defaultResponseEncoding; } }
		private Encoding defaultResponseEncoding;

		/// <summary>
		/// An event fired on the very end of the request. 
		/// </summary>
        public static event Action RequestEnd;

		/// <summary>
		/// An event fired on the beginning of the request after the script context is initialized.
		/// </summary>
        public static event Action RequestBegin;

		#endregion

		#region Resources Management

		/// <summary>
		/// Lazily initialized list of <see cref="PhpResource"/>s created during this web request.
		/// </summary>
		/// <remarks>
		/// The resources are disposed of when the request is over.
		/// <seealso cref="RegisterResource"/><seealso cref="CleanUpResources"/>
		/// </remarks>
		private LinkedList<PhpResource> resources;

		/// <summary>
		/// Registers a resource that should be disposed of when the request is over.
		/// </summary>
		/// <param name="res">The resource.</param>
		internal LinkedListNode<PhpResource> RegisterResource(PhpResource/*!*/res)
		{
            Debug.Assert(res != null);

            if (resources == null)
                resources = new LinkedList<PhpResource>();

            return resources.AddFirst(res);
		}

        /// <summary>
        /// Unregisters disposed resource.
        /// </summary>
        internal void UnregisterResource(LinkedListNode<PhpResource>/*!*/node)
        {
            Debug.Assert(node != null);
            Debug.Assert(this.resources != null);

            this.resources.Remove(node);
        }

		/// <summary>
		/// Disposes of <see cref="PhpResource"/>s created during this web request.
		/// </summary>
		private void CleanUpResources()
		{
			if (resources != null)
			{
                for (var p = resources.First; p != null; )
                {
                    var next = p.Next;
                    p.Value.Close();
                    p = next;
                }

                resources = null;
			}
		}

		#endregion

		#region Request Processing
#if !SILVERLIGHT
		/// <summary>
		/// Performs PHP inclusion on a specified script. Equivalent to <see cref="PHP.Core.ScriptContext.IncludeScript"/>. 
		/// </summary>
		/// <param name="relativeSourcePath">
		/// Path to the target script source file relative to the web application root.
		/// </param>
		/// <param name="script">
		/// Script info (i.e. type called <c>Default</c> representing the target script) or any type from 
		/// the assembly where the target script is contained. In the latter case, the script type is searched in the 
		/// assembly using value of <paramref name="relativeSourcePath"/>.
		/// </param>
		/// <returns>The value returned by the global code of the target script.</returns>
		/// <exception cref="InvalidOperationException">Request context has been disposed.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="relativeSourcePath"/> or <paramref name="script"/> are <B>null</B> references.</exception>
		/// <exception cref="ArgumentException">Script type cannot be resolved.</exception>
		/// <exception cref="InvalidScriptAssemblyException">The target assembly is not a valid Phalanger compiled assembly.</exception>
		public object IncludeScript(string/*!*/ relativeSourcePath, ScriptInfo/*!*/ script)
		{
			if (disposed)
				throw new InvalidOperationException(CoreResources.GetString("instance_disposed"));

            return scriptContext.IncludeScript(relativeSourcePath, script);
		}
#endif

		/// <summary>
		/// Finalizes (disposes) the request context.
		/// </summary>
		/// <remarks>
		/// Finalization comprises of the following actions (executed in the order):
		/// <list type="number">
		/// <term>Output buffers are flushed. This action may include calls to user defined filters (see <c>ob_start</c> function).</term>
		/// <term>Shutdown callbacks are invoked (if added by <c>register_shutdown_function</c> function).</term>
		/// <term>Session is closed. User defined session handling function may be invoked (see <c>session_set_save_handler</c> function).</term>
		/// <term>PHP objects are destroyed.</term>
		/// <term>HTTP Headers are flushed (if it wasn't done earlier).</term>
		/// <term>PHP resources are disposed.</term>
		/// <term>Per-request temporary files are deleted.</term>
		/// <term><see cref="RequestEnd"/> event is fired.</term>
		/// <term>Current request and script contexts are nulled.</term>
		/// </list>
		/// Multiple invocations of the method are ignored.
		/// Since session data need to be written to the session store (<c>HttpContext.Session</c>) this method has to be 
		/// called before the ASP.NET session is ended for the request.
		/// </remarks>
		public void Dispose()
		{
			if (!disposed)
			{
				try
				{
					scriptContext.GuardedCall<object, object>(scriptContext.ProcessShutdownCallbacks, null, false);

					// Session is ended after destructing objects since PHP 5.0.5, use two-phase finalization:
                    scriptContext.GuardedCall<object, object>(scriptContext.FinalizePhpObjects, null, false);
                    scriptContext.GuardedCall<object, object>(scriptContext.FinalizeBufferedOutput, null, false);

					TryDisposeBeforeFinalization();

					// finalize objects created during session closing and output finalization:
					scriptContext.GuardedCall<object, object>(scriptContext.FinalizePhpObjects, null, false);

					// Platforms-specific dispose
					TryDisposeAfterFinalization();
				}
				finally
				{
					CleanUpResources();

					// Platforms-specific finally dispose
					FinallyDispose();

					if (RequestEnd != null) RequestEnd();

                    // remember the max capacity of dictionaries to preallocate next time:
                    if (scriptContext != null && scriptContext.MainScriptInfo != null)
                        scriptContext.MainScriptInfo.SaveMaxCounts(scriptContext);                        

					// cleans this instance:
					disposed = true;
					this.scriptContext = null;
					ScriptContext.CurrentContext = null;

					Debug.WriteLine("REQUEST", "-- disposed ----------------------");
				}
			}
		}

		#endregion
	}
}
