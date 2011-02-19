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
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Flavor;
using System.Runtime.InteropServices;

namespace PHP.VisualStudio.PhalangerProject.WPFProviders
{
    [Guid(PythonWPFProjectFactory.PythonWPFProjectFactoryGuid)]
    public class PythonWPFProjectFactory : FlavoredProjectFactoryBase
    {
        public const string PythonWPFProjectFactoryGuid = "229B3E77-97E9-4f6d-9151-E6D103EA4D4A";

        private IServiceProvider site;
        public PythonWPFProjectFactory(IServiceProvider site)
            :base()
        {
            this.site = site;
        }

        /// <summary>
        /// Create an instance of our project. The initialization will be done later
        /// when VS calls InitalizeForOuter on it.
        /// </summary>
        /// <param name="outerProjectIUnknown">This is only useful if someone else is subtyping us</param>
        /// <returns>An uninitialized instance of our project</returns>
        protected override object PreCreateForOuter(IntPtr outerProjectIUnknown)
        {
            return new PythonWPFFlavor(site);
        }
    }
}
