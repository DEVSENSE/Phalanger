/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;

using Microsoft.VisualStudio.TextManager.Interop;

namespace PHP.VisualStudio.PhalangerLanguageService
{
	public class PhpColorableItem : IVsColorableItem
	{
		#region Fields & Construction

		private string displayName;
		private COLORINDEX background;
		private COLORINDEX foreground;
		private FONTFLAGS fontFlags;

		public PhpColorableItem(string displayName, COLORINDEX foreground, COLORINDEX background, FONTFLAGS fontFlags)
		{
			this.displayName = displayName;
			this.background = background;
			this.foreground = foreground;
			this.fontFlags = fontFlags;
		}

		#endregion

		#region IVsColorableItem Members

		public int GetDefaultColors(COLORINDEX[] piForeground, COLORINDEX[] piBackground)
		{
			if (null == piForeground)
			{
				throw new ArgumentNullException("piForeground");
			}
			if (0 == piForeground.Length)
			{
				throw new ArgumentOutOfRangeException("piForeground");
			}
			piForeground[0] = foreground;

			if (null == piBackground)
			{
				throw new ArgumentNullException("piBackground");
			}
			if (0 == piBackground.Length)
			{
				throw new ArgumentOutOfRangeException("piBackground");
			}
			piBackground[0] = background;

			return Microsoft.VisualStudio.VSConstants.S_OK;
		}

		public int GetDefaultFontFlags(out uint pdwFontFlags)
		{
			pdwFontFlags = (uint)fontFlags;
			return Microsoft.VisualStudio.VSConstants.S_OK;
		}

		public int GetDisplayName(out string pbstrName)
		{
			pbstrName = displayName;
			return Microsoft.VisualStudio.VSConstants.S_OK;
		}

		#endregion
	}
}
