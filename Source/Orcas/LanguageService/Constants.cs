/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/
using System;

namespace PHP.VisualStudio.PhalangerLanguageService 
{
    /// <summary>
    /// Language integration constants.
    /// </summary>
	internal static class PhalangerConstants 
	{
		public const string phpFileExtension = ".php";
		public const string phpxFileExtension = ".phpx";
        public const string phpprojFileExtension = ".phpproj";
		public const string phpCodeDomProviderName = "Phalanger";
		public const string packageGuidString = "2eea826f-071b-41b6-a133-743df69cad91";
		public const string languageServiceGuidString = "16b0638d-251a-4705-98d2-5251112c4139";
		public const string libraryManagerGuidString = "6207Be59-e935-48ae-8c57-c3e9cd6650D3";
		public const string libraryManagerServiceGuidString = "cfbaf3f9-b5d2-467f-a36d-ded3e065ebdf";
		public const string intellisenseProviderGuidString = "a6ee83a2-51ec-4b0a-8300-0523174a757b";
		public const string PLKMinEdition = "standard";
		public const string PLKCompanyName = "Phalanger Team";
		public const string PLKProductName = "Phalanger Visual Studio Integration";
		public const string PLKProductVersion = "2.0";
		public const int PLKResourceID = 101;
	}
}
