/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.
  
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
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.ComponentModel;
using PHP.Core.Emit;
using PHP.Core.Reflection;

using PHP.CoreCLR;
//using System.Windows.Browser.Net;
using System.Net;
using System.Windows.Browser;

namespace PHP.Core
{
	/// <summary>
	/// The context of an executing script. Contains data associated with a request.
	/// </summary>
	public sealed partial class ScriptContext : MarshalByRefObject
	{
        #region Constants

		private void InitConstants(DualDictionary<string, object> _constants)
		{
			// SILVERLIGHT: ??
			
			//_constants.Add("PHALANGER", Assembly.GetExecutingAssembly().GetName().Version.ToString(), false);
			//_constants.Add("PHP_VERSION", PhpVersion.Current, false);
			//_constants.Add("PHP_OS", Environment.OSVersion.Platform == PlatformID.Win32NT ? "WINNT" : "WIN32", false); // TODO: GENERICS (Unix)
			//_constants.Add("DIRECTORY_SEPARATOR", Path.DirectorySeparatorChar.ToString(), false);
			//_constants.Add("PATH_SEPARATOR", Path.PathSeparator.ToString(), false);

			//_constants.Add("STDIN", InputOutputStreamWrapper.In, false);
			//_constants.Add("STDOUT", InputOutputStreamWrapper.Out, false);
			//_constants.Add("STDERR", InputOutputStreamWrapper.Error, false);
		}

		#endregion

		#region Initialization

		/// <summary>
		/// 
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static EventHandler RunSilverlightApplication(System.Windows.Controls.Canvas c, string source)
		{
			ApplicationContext app_context = ApplicationContext.Default;

			// try to preload configuration (to prevent exceptions during InitApplication)
			Configuration.Load(app_context);
			ApplicationConfiguration app_config = Configuration.Application;
            

            string url = HtmlPage.Document.DocumentUri.AbsoluteUri;
			int lastSlash = url.Replace('\\','/').LastIndexOf('/');
			app_config.Compiler.SourceRoot = new FullPath(url.Substring(0, lastSlash), false);

            int sourcelastSlash = source.Replace('\\', '/').LastIndexOf('/');
            string sourceRelPath = source.Substring(lastSlash+1);

            

			// Silverlight language features
			app_config.Compiler.LanguageFeatures = LanguageFeatures.PhpClr;

			// ..
			ScriptContext context = InitApplication(app_context);
            
            Debug.Fail("Update versions below!");
            ConfigurationContext.AddLibrary("mscorlib, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", null, "");
            ConfigurationContext.AddLibrary("System, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", null, "");
            ConfigurationContext.AddLibrary("System.Windows, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", null, "");
            ConfigurationContext.AddLibrary("System.Net, Version=2.0.5.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e", null, "");
            //ConfigurationContext.AddLibrary("System.SilverLight, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", null, "");
            //ConfigurationContext.AddLibrary("agclr, Version=0.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", null, "");

            ConfigurationContext.AddLibrary("PhpNetClassLibrary, Version=3.0.0.0, Culture=neutral, PublicKeyToken=4af37afe3cde05fb", null, "");

			//
			Configuration.Application.Compiler.Debug = true;

			// ..
			Dictionary<string, object> vars = new Dictionary<string, object>();

			currentContext.AutoGlobals.Canvas.Value = ClrObject.Wrap(c);
            currentContext.AutoGlobals.Addr.Value = ClrObject.Wrap(app_config.Compiler.SourceRoot.ToString());

			//Operators.SetVariableRef(currentContext, vars, "_CANVAS", Operators.GetItemRef("_CANVAS", ref currentContext.AutoGlobals.Globals.value));
			//Operators.SetVariable(currentContext, vars, "_CANVAS", ClrObject.Wrap(c));


            context.DynamicInclude(source, sourceRelPath, vars, null, null, InclusionTypes.RunSilverlight);

			return new EventHandler(delegate(object sender, EventArgs e)
				{
					if (context.ResolveFunction("OnLoad", null, true) != null)
					{
						PhpCallback load = new PhpCallback("OnLoad");
						load.Invoke(sender, e);
					}
				});
		}


		public static ScriptContext/*!*/ InitApplication(ApplicationContext/*!*/ appContext)
		{
			// loads configuration into the given application context 
			// (applies only if the config has not been loaded yet by the current thread):
			Configuration.Load(appContext);
			ApplicationConfiguration app_config = Configuration.Application;

			// takes a writable copy of a global configuration:
			LocalConfiguration config = (LocalConfiguration)Configuration.DefaultLocal.DeepCopy();

			ScriptContext result = new ScriptContext(appContext, config, TextWriter.Null, Stream.Null);
			result.IsOutputBuffered = result.config.OutputControl.OutputBuffering;
			result.AutoGlobals.Initialize();
			result.ThrowExceptionOnError = true;
			result.config.ErrorControl.HtmlMessages = false;

			return ScriptContext.CurrentContext = result;
		}

		#endregion

		#region Inclusions

		/// <summary>
		/// Called in place where a script is dynamically included. For internal purposes only.
		/// </summary>
		/// <param name="includedFilePath">A source path to the included script.</param>
        /// <param name="includerFileRelPath">A source path to the script issuing the inclusion relative to the source root.</param>
		/// <param name="variables">A run-time variables table.</param>
		/// <param name="self">A current object in which method an include is called (if applicable).</param>
		/// <param name="includer">A current class type desc in which method an include is called (if applicable).</param>
		/// <param name="inclusionType">A type of an inclusion.</param>
		[Emitted, EditorBrowsable(EditorBrowsableState.Never)]
		public object DynamicInclude(
			string includedFilePath,
            string includerFileRelPath,//TODO: Now it's not relative because RelativePath class doesn't work properly with HTTP addresses
			Dictionary<string, object> variables,
			DObject self,
			DTypeDesc includer,
			InclusionTypes inclusionType)
		{
			ApplicationConfiguration app_config = Configuration.Application;

			// determines inclusion behavior:
			PhpError error_severity = InclusionTypesEnum.IsMustInclusion(inclusionType) ? PhpError.Error : PhpError.Warning;
			bool once = InclusionTypesEnum.IsOnceInclusion(inclusionType);


            System.Threading.AutoResetEvent downloadFinished = null;
            string eval = string.Empty;

            downloadFinished = new System.Threading.AutoResetEvent(false);
             
            WebClient webclient = new WebClient();
            webclient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(
                delegate(object sender, DownloadStringCompletedEventArgs downEventArgs)
                {
                    if (downEventArgs.Error == null)
                    {
                        eval = downEventArgs.Result;

                        // workaround for Firefox BrowserHttpWebRequest bug - 
                        // assuming that we're downloading PHP source starting with '<?' 
                        int srcStart = 0;
                        while ( !(eval[srcStart] == '<' && eval[srcStart + 1] == '?') && (srcStart+1 < eval.Length)  ) srcStart++;
                        eval = eval.Substring(srcStart);

                        downloadFinished.Set();
                    }
                }
                );

            Uri baseUri = new Uri(app_config.Compiler.SourceRoot+"/", UriKind.Absolute);
            Uri uriFile = new Uri(includedFilePath, UriKind.RelativeOrAbsolute);
            Uri uri = new Uri(baseUri, uriFile);

            webclient.DownloadStringAsync(uri);

            ThreadStart ts = new ThreadStart(()=>{
                    downloadFinished.WaitOne();

                    try
                    {
                        DynamicCode.EvalFile(eval, ScriptContext.CurrentContext, variables, self, null, includedFilePath, 0, 0, -1);
                    }
                    catch (Exception ex)
                    {
                        var canvas = ((ClrObject)ScriptContext.CurrentContext.AutoGlobals.Canvas.Value).RealObject as System.Windows.Controls.Canvas;

                        canvas.Dispatcher.BeginInvoke(() =>
                        {
                            throw ex;
                        });
                    }
  

                });

            if (inclusionType == InclusionTypes.RunSilverlight) // new thread have to be created
                new Thread(ts).Start();
            else
            {
                ts(); // just continue on this thread
            }

            return null;
		}

		#endregion

		#region Current Context

		/// <summary>
		/// The instance of <see cref="ScriptContext"/> associated with the current logical thread.
		/// </summary>
		/// <remarks>
		/// If no instance is associated with the current logical thread
		/// a new one is created, added to call context and returned. 
		/// The slot allocated by some instance is freed
		/// by setting this property to a <B>null</B> reference.
		/// </remarks>                                                  
		public static ScriptContext CurrentContext
		{
			[Emitted]
			get
			{
				// no script context in call context => create an empty one:
				if (currentContext == null)
					currentContext = new ScriptContext(ApplicationContext.Default);
				return currentContext;
			}
			set
			{
				currentContext = value;
			}
		}
		private static ScriptContext currentContext = null;

		#endregion

		#region Platform Dependent

		void InitPlatformSpecific()
		{
		}

		#endregion

		#region Session Handling

		/// <summary>
		/// Adds session variables aliases to global variables - N/A on SL
		/// </summary>
		public void RegisterSessionGlobals()
		{
		}

		#endregion
	}
}
