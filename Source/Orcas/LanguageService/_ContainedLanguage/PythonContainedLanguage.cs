/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell.Interop;

using Microsoft.Samples.VisualStudio.CodeDomCodeModel;
using VSConstants = Microsoft.VisualStudio.VSConstants;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace PHP.VisualStudio.PhalangerLanguageService {
	/// <summary>
	/// Factory class to create an instance of the PythonContainedLanguage class.
	/// </summary>
	public class PythonContainedLanguageFactory : IVsContainedLanguageFactory {
		private Dictionary<ModuleId, PythonContainedLanguage> languages;
		private PhalangerIntellisenseProvider intellisenseProject;
		internal PythonContainedLanguageFactory(PhalangerIntellisenseProvider intellisenseProject) {
			languages = new Dictionary<ModuleId, PythonContainedLanguage>();
			this.intellisenseProject = intellisenseProject;
		}
		public int GetLanguage(IVsHierarchy pHierarchy, uint itemid, IVsTextBufferCoordinator pBufferCoordinator, out IVsContainedLanguage ppLanguage) {
			ModuleId id = new ModuleId(pHierarchy, itemid);
			PythonContainedLanguage lang;
			if (!languages.TryGetValue(id, out lang)) {
				lang = new PythonContainedLanguage(pBufferCoordinator, intellisenseProject, itemid);
				languages.Add(id, lang);
			}
			ppLanguage = lang;
			return VSConstants.S_OK;
		}
	}

	/// <summary>
	/// The contained language implementation.
	/// This object is the one responsible to provide the colorizer for a specific file and the
	/// command filter for a specific text view.
	/// It also implements IVsContainedCode so that the buffer coordinator can map text spans
	/// between the primary and secondary buffer.
	/// </summary>
	public partial class PythonContainedLanguage : IVsContainedLanguage, IVsContainedCode {
		private IVsTextBufferCoordinator bufferCoordinator;
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		private PhalangerIntellisenseProvider intellisenseProject;
		private IVsContainedLanguageHost languageHost;
		private PhpLanguage language;
		private uint itemId;

		public PythonContainedLanguage(IVsTextBufferCoordinator bufferCoordinator, PhalangerIntellisenseProvider intellisenseProject, uint itemId) {
			if (null == bufferCoordinator) {
				throw new ArgumentNullException("bufferCoordinator");
			}
			if (null == intellisenseProject) {
				throw new ArgumentNullException("intellisenseProject");
			}
			this.bufferCoordinator = bufferCoordinator;
			this.intellisenseProject = intellisenseProject;
			this.itemId = itemId;
			// Make sure that the secondary buffer uses the IronPython language service.
			IVsTextLines buffer;
			ErrorHandler.ThrowOnFailure(bufferCoordinator.GetSecondaryBuffer(out buffer));
			Guid languageGuid;
			this.GetLanguageServiceID(out languageGuid);
			ErrorHandler.ThrowOnFailure(buffer.SetLanguageServiceID(ref languageGuid));
		}

		public int GetColorizer(out IVsColorizer ppColorizer) {
			ppColorizer = null;
			if (null == LanguageService) {
				// We should always be able to get the language service.
				return VSConstants.E_UNEXPECTED;
			}
			IVsTextLines buffer;
			ErrorHandler.ThrowOnFailure(bufferCoordinator.GetSecondaryBuffer(out buffer));
			return LanguageService.GetColorizer(buffer, out ppColorizer);
		}

		public int GetLanguageServiceID(out Guid pguidLangService) {
			pguidLangService = new Guid(PhalangerConstants.languageServiceGuidString);
			return VSConstants.S_OK;
		}

		public int GetTextViewFilter(IVsIntellisenseHost pISenseHost, IOleCommandTarget pNextCmdTarget, out IVsTextViewFilter pTextViewFilter) {
			IVsTextLines buffer;
			ErrorHandler.ThrowOnFailure(bufferCoordinator.GetSecondaryBuffer(out buffer));
			bool doOutlining = LanguageService.Preferences.AutoOutlining;
			LanguageService.Preferences.AutoOutlining = false;
			Source source = LanguageService.CreateSource(buffer) as Source;
			LanguageService.Preferences.AutoOutlining = doOutlining;
			CodeWindowManager windowMgr = LanguageService.CreateCodeWindowManager(null, source);
			language.AddCodeWindowManager(windowMgr);
			TextViewWrapper view = new TextViewWrapper(languageHost, pISenseHost, bufferCoordinator, pNextCmdTarget);
			windowMgr.OnNewView(view);
		  
						// python?
						//language.AddSpecialSource(source, view);

			pTextViewFilter = view.InstalledFilter;
			PythonViewFilter pythonFilter = pTextViewFilter as PythonViewFilter;
			if (null != pythonFilter) {
				pythonFilter.BufferCoordinator = this.bufferCoordinator;
			}
			return VSConstants.S_OK;
		}

		public int Refresh(uint dwRefreshMode) {
			// TODO: Handle this method.
			return VSConstants.S_OK;
		}

		public int SetBufferCoordinator(IVsTextBufferCoordinator pBC) {
			bufferCoordinator = pBC;
			return VSConstants.S_OK;
		}

		public int SetHost(IVsContainedLanguageHost pHost) {
			languageHost = pHost;
			return VSConstants.S_OK;
		}

		public int WaitForReadyState() {
			// Do Nothing
			return VSConstants.S_OK;
		}

		public int EnumOriginalCodeBlocks(out IVsEnumCodeBlocks ppEnum) {
			IVsTextLines buffer;
			ErrorHandler.ThrowOnFailure(bufferCoordinator.GetSecondaryBuffer(out buffer));
			ppEnum = new CodeBlocksEnumerator(buffer);
			return VSConstants.S_OK;
		}

		public int HostSpansUpdated() {
			return VSConstants.S_OK;
		}


		private PhpLanguage LanguageService {
			get {
				if (null == language) {
					// Try to get the language service using the global service provider.
					language = PhalangerPackage.GetGlobalService(typeof(PhpLanguage)) as PhpLanguage;
				}
				return language;
			}
		}
	}
}
