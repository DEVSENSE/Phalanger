/*

 Copyright (c) 2006 Tomas Matousek. Based on Visual Studio 2005 SDK IronPython sample.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.ComponentModel.Design;

namespace PHP.VisualStudio.PhalangerProject
{
	/// <summary>
	/// CommandIDs matching the commands defined items from PkgCmdID.h and guids.h
	/// </summary>
	public sealed class PhalangerMenus
	{
		internal static readonly Guid guidPhalangerProjectCmdSet = new Guid(GuidList.GuidPhpProjectCmdSetString);
		internal static readonly CommandID SetAsMain = new CommandID(guidPhalangerProjectCmdSet, 0x3001);
	}
}

