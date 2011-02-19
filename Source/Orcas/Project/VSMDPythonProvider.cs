/*

 Copyright (c) 2006 Tomas Matousek. Based on Visual Studio 2005 SDK IronPython sample.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.CodeDom.Compiler;
using Microsoft.VisualStudio.Designer.Interfaces;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using IServiceProvider = System.IServiceProvider;

namespace PHP.VisualStudio.PhalangerProject
{
	internal class VSMDPhalangerProvider : IVSMDCodeDomProvider, IDisposable
	{
		private PHP.Core.CodeDom.PhpCodeProvider provider;
		private VSLangProj.VSProject vsproject;

		public VSMDPhalangerProvider(VSLangProj.VSProject project)
		{
			if (project == null)
				throw new ArgumentNullException("project");

			vsproject = project;

			provider = new PHP.Core.CodeDom.PhpCodeProvider();

			// Create the provider
			this.ReferencesEvents_ReferenceRemoved(null);
			vsproject.Events.ReferencesEvents.ReferenceAdded += new VSLangProj._dispReferencesEvents_ReferenceAddedEventHandler(ReferencesEvents_ReferenceAdded);
			vsproject.Events.ReferencesEvents.ReferenceRemoved += new VSLangProj._dispReferencesEvents_ReferenceRemovedEventHandler(ReferencesEvents_ReferenceRemoved);
			vsproject.Events.ReferencesEvents.ReferenceChanged += new VSLangProj._dispReferencesEvents_ReferenceChangedEventHandler(ReferencesEvents_ReferenceRemoved);
		}

		#region Event Handlers

		/// <summary>
		/// When a reference is added, add it to the provider
		/// </summary>
		/// <param name="reference">Reference being added</param>
		void ReferencesEvents_ReferenceAdded(VSLangProj.Reference reference)
		{
			// TODO: provider.AddReference(reference.Path);
		}

		/// <summary>
		/// When a reference is removed/changed, let the provider know
		/// </summary>
		/// <param name="reference">Reference being removed</param>
		void ReferencesEvents_ReferenceRemoved(VSLangProj.Reference reference)
		{
			// TODO: 
			//// Because our provider only has an AddReference method and no way to
			//// remove them, we end up having to recreate it.
			//provider = new IronPython.CodeDom.PythonProvider();
			//if (vsproject.References != null)
			//{
			//  foreach (VSLangProj.Reference currentReference in vsproject.References)
			//  {
			//    provider.AddReference(currentReference.Path);
			//  }
			//}
		}
		#endregion

		#region IVSMDCodeDomProvider Members

		object IVSMDCodeDomProvider.CodeDomProvider
		{
			get { return provider; }
		}

		#endregion

		#region IDisposable Members

		void IDisposable.Dispose()
		{
			if (vsproject != null)
			{
				vsproject = null;
				vsproject.Events.ReferencesEvents.ReferenceAdded -= new VSLangProj._dispReferencesEvents_ReferenceAddedEventHandler(ReferencesEvents_ReferenceAdded);
				vsproject.Events.ReferencesEvents.ReferenceRemoved -= new VSLangProj._dispReferencesEvents_ReferenceRemovedEventHandler(ReferencesEvents_ReferenceRemoved);
				vsproject.Events.ReferencesEvents.ReferenceChanged -= new VSLangProj._dispReferencesEvents_ReferenceChangedEventHandler(ReferencesEvents_ReferenceRemoved);
			}
		}

		#endregion
	}
}
