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
using Microsoft.Samples.VisualStudio.IronPythonInference;

using Microsoft.VisualStudio.Package;

namespace PHP.VisualStudio.PhalangerLanguageService {
    class PythonMethods : Methods {
        private IList<FunctionInfo> methods;

        public PythonMethods(IList<FunctionInfo> methods) {
            this.methods = methods;
        }

        public override int GetCount() {
            return methods != null ? methods.Count : 0;
        }

        public override string GetDescription(int index) {
            return methods != null && 0 <= index && index < methods.Count ? methods[index].Description : "";
        }

        public override string GetType(int index) {
            return methods != null && 0 <= index && index < methods.Count ? methods[index].Type : "";
        }

        public override int GetParameterCount(int index) {
            return methods != null && 0 <= index && index < methods.Count ? methods[index].ParameterCount : 0;
        }

        public override void GetParameterInfo(int index, int parameter, out string name, out string display, out string description) {
            if (methods != null && 0 <= index && index < methods.Count) {
                methods[index].GetParameterInfo(parameter, out name, out display, out description);
            } else {
                name = display = description = string.Empty;
            }
        }

        public override string GetName(int index) {
            return methods != null && 0 <= index && index < methods.Count ? methods[index].Name : "";
        }
    }
}
