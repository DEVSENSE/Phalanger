/*

 Copyright (c) 2006 Tomas Matousek. Based on Visual Studio 2005 SDK IronPython sample.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

using System.ComponentModel.Design;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace PHP.VisualStudio.PhalangerLanguageService 
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[ProvideLoadKey(PhalangerConstants.PLKMinEdition, PhalangerConstants.PLKProductVersion, PhalangerConstants.PLKProductName, PhalangerConstants.PLKCompanyName, PhalangerConstants.PLKResourceID)]
	[DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\9.0Exp")]
	[ProvideService(typeof(PhpLanguage), ServiceName = "Phalanger")]
	[ProvideService(typeof(IPhalangerLibraryManager))]
    [Debugger.RegisterExpressionEvaluator(typeof(Debugger.PHPExpressionEvaluator), Core.Reflection.CompilationUnitBase.PhalangerLanguageGuid,Core.Reflection.CompilationUnitBase.MicrosoftVendorGuid)]
	[ProvideLanguageService(typeof(PhpLanguage), "Phalanger", 100,
            CodeSense = true,
			DefaultToInsertSpaces = true,
			EnableCommenting = true,
			MatchBraces = true,
            MatchBracesAtCaret = true,
            ShowCompletion = true,
			ShowMatchingBrace = true,
            QuickInfo = true,
            AutoOutlining = true,
            DebuggerLanguageExpressionEvaluator = Core.Reflection.CompilationUnitBase.PhalangerLanguageGuid
    )]
	[ProvideLanguageExtension(typeof(PhpLanguage), PhalangerConstants.phpFileExtension)]
	[ProvideLanguageExtension(typeof(PhpLanguage), PhalangerConstants.phpxFileExtension)]
	[ProvideIntellisenseProvider(typeof(PhalangerIntellisenseProvider), "PhalangerCodeProvider", "Phalanger", ".php", "Phalanger;PHP", "Phalanger")]
	[ProvideIntellisenseProvider(typeof(PhalangerIntellisenseProvider), "PhalangerCodeProvider", "Phalanger", ".phpx", "Phalanger;PHP", "Phalanger")]
	[ProvideObject(typeof(PhalangerIntellisenseProvider))]
	[Guid(PhalangerConstants.packageGuidString)]
	[RegisterSnippetsAttribute(PhalangerConstants.languageServiceGuidString, false, 131, "PHP", @"CodeSnippets\SnippetsIndex.xml", @"CodeSnippets\Snippets\", @"CodeSnippets\Snippets\")]
	[SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
	public class PhalangerPackage : Package, IOleComponent
	{
		private uint componentID;
		private PhalangerLibraryManager libraryManager;

		public PhalangerPackage()
		{
			IServiceContainer container = this as IServiceContainer;
			ServiceCreatorCallback callback = new ServiceCreatorCallback(CreateService);
			container.AddService(typeof(PhpLanguage), callback, true);
			container.AddService(typeof(IPhalangerLibraryManager), callback, true);
		}

		private void RegisterForIdleTime()
		{
			IOleComponentManager mgr = GetService(typeof(SOleComponentManager)) as IOleComponentManager;
			if (componentID == 0 && mgr != null)
			{
				OLECRINFO[] crinfo = new OLECRINFO[1];
				crinfo[0].cbSize = (uint)Marshal.SizeOf(typeof(OLECRINFO));
				crinfo[0].grfcrf = (uint)_OLECRF.olecrfNeedIdleTime|(uint)_OLECRF.olecrfNeedPeriodicIdleTime;
				crinfo[0].grfcadvf = (uint)_OLECADVF.olecadvfModal|(uint)_OLECADVF.olecadvfRedrawOff|(uint)_OLECADVF.olecadvfWarningsOff;
				crinfo[0].uIdleTimeInterval = 1000;
				int hr = mgr.FRegisterComponent(this, crinfo, out componentID);
			}
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (componentID != 0)
				{
					IOleComponentManager mgr = GetService(typeof(SOleComponentManager)) as IOleComponentManager;
					if (mgr != null)
					{
						mgr.FRevokeComponent(componentID);
					}
					componentID = 0;
				}
				if (null != libraryManager)
				{
					libraryManager.Dispose();
					libraryManager = null;
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		private object CreateService(IServiceContainer container, Type serviceType)
		{
			object service = null;
			if (typeof(PhpLanguage) == serviceType)
			{
				PhpLanguage language = new PhpLanguage();
				language.SetSite(this);
				RegisterForIdleTime();
				service = language;
			}
			else if (typeof(IPhalangerLibraryManager) == serviceType)
			{
				libraryManager = new PhalangerLibraryManager(this);
				service = libraryManager as IPhalangerLibraryManager;
			}
			return service;
		}

		#region IOleComponent Members

		public int FContinueMessageLoop(uint uReason, IntPtr pvLoopData, MSG[] pMsgPeeked)
		{
			return 1;
		}

		public int FDoIdle(uint grfidlef)
		{
			PhpLanguage pl = GetService(typeof(PhpLanguage)) as PhpLanguage;
			if (pl != null)
			{
				pl.OnIdle((grfidlef & (uint)_OLEIDLEF.oleidlefPeriodic) != 0);
			}
			if (null != libraryManager)
			{
				libraryManager.OnIdle();
			}
			return 0;
		}

		public int FPreTranslateMessage(MSG[] pMsg)
		{
			return 0;
		}

		public int FQueryTerminate(int fPromptUser)
		{
			return 1;
		}

		public int FReserved1(uint dwReserved, uint message, IntPtr wParam, IntPtr lParam)
		{
			return 1;
		}

		public IntPtr HwndGetWindow(uint dwWhich, uint dwReserved)
		{
			return IntPtr.Zero;
		}

		public void OnActivationChange(IOleComponent pic, int fSameComponent, OLECRINFO[] pcrinfo, int fHostIsActivating, OLECHOSTINFO[] pchostinfo, uint dwReserved)
		{
		}

		public void OnAppActivate(int fActive, uint dwOtherThreadID)
		{
		}

		public void OnEnterState(uint uStateID, int fEnter)
		{
		}

		public void OnLoseActivation()
		{
		}

		public void Terminate()
		{
		}

		#endregion
	}
}
