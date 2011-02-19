
/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Project;

namespace PHP.VisualStudio.PhalangerProject
{
	/// <summary>
	/// Reference container node for Iron Python references.
	/// </summary>
	public class PhalangerReferenceContainerNode : ReferenceContainerNode
	{
		public PhalangerReferenceContainerNode(ProjectNode project)
			: base(project)
		{
		}

		protected override ProjectReferenceNode CreateProjectReferenceNode(ProjectElement element)
		{
			return new PhalangerProjectReferenceNode(this.ProjectMgr, element);
		}

		protected override ProjectReferenceNode CreateProjectReferenceNode(VSCOMPONENTSELECTORDATA selectorData)
		{
			return new PhalangerProjectReferenceNode(this.ProjectMgr, selectorData.bstrTitle, selectorData.bstrFile, selectorData.bstrProjRef);
		}
	}
}
