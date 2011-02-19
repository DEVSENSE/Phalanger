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
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.VisualStudio.TextManager.Interop;

using Microsoft.Samples.VisualStudio.CodeDomCodeModel;
using VSConstants = Microsoft.VisualStudio.VSConstants;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace PHP.VisualStudio.PhalangerLanguageService {
    public partial class PythonContainedLanguage : IVsContainedLanguageCodeSupport {
        private CodeDomFileCodeModel fileCodeModel;
        private Microsoft.Samples.VisualStudio.CodeDomCodeModel.StringMerger stringMerger;

        private class CodeClassInfo {
            private EnvDTE.CodeClass codeClass;
            private uint itemId;
            public CodeClassInfo(EnvDTE.CodeClass codeClass, uint itemId) {
                this.codeClass = codeClass;
                this.itemId = itemId;
            }
            public EnvDTE.CodeClass CodeClass {
                get { return codeClass; }
            }
            public uint ItemId {
                get { return itemId; }
            }
        }

        public int CreateUniqueEventName(string pszClassName, string pszObjectName, string pszNameOfEvent, out string pbstrEventHandlerName) {
            // Get the class that will contain the actual implementation of the event handler.
            CodeClassInfo implInfo = ImplementationClass(pszClassName);
            if ((null == implInfo) || (null == implInfo.CodeClass)) {
                throw new System.ArgumentException("pszClassName");
            }
            EnvDTE.CodeClass implClass = implInfo.CodeClass;

            // Build the default name for the event handler.
            string baseName = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}_{1}", pszObjectName, pszNameOfEvent);
            pbstrEventHandlerName = baseName;

            // Now we have to check if the implementation class contains another member with the same name.
            for (int i = 1; ; ++i) {
                if (null == GetClassMember(implClass, pbstrEventHandlerName)) {
                    // No member with this name.
                    break;
                }
                else {
                    // The name is in use. Change it adding a numeric identifier at the end of it.
                    pbstrEventHandlerName = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}_{1}", baseName, i);
                }
            }

            // Add one underscore at the beginning of the function name because this is what the 
            // file code model will add.
            pbstrEventHandlerName = "_" + pbstrEventHandlerName;

            // All done, return.
            return VSConstants.S_OK;
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.ArgumentException.#ctor(System.String)")]
        [SuppressMessage("Microsoft.Usage", "CA2204:LiteralsShouldBeSpelledCorrectly", MessageId = "psz")]
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
        public int EnsureEventHandler(string pszClassName, string pszObjectTypeName, string pszNameOfEvent, string pszEventHandlerName, uint itemidInsertionPoint, out string pbstrUniqueMemberID, out string pbstrEventBody, TextSpan[] pSpanInsertionPoint) {
            if (string.IsNullOrEmpty(pszClassName)) {
                throw new System.ArgumentNullException("pszClassName");
            }
            if ((null == pSpanInsertionPoint) || (pSpanInsertionPoint.Length == 0)) {
                throw new System.ArgumentNullException("pSpanInsertionPoint");
            }
            if (string.IsNullOrEmpty(pszEventHandlerName)) {
                throw new System.ArgumentNullException("pszEventHandlerName");
            }
            // The file code model will create a function name that starts with '_', so we must make
            // sure that the name of the event handler matches it.
            if (pszEventHandlerName[0] != '_') {
                throw new System.ArgumentException("pszEventHandlerName");
            }
            // Given the fact that the file code model will add _ at the beginning of the name,
            // we have to remove the underscore from the name passed to AddFunction.
            string handlerName = pszEventHandlerName.Substring(1);

            // Initialize the out parameters.
            pbstrUniqueMemberID = null;
            pbstrEventBody = null;

            // Get the implementation class.
            CodeClassInfo implInfo = ImplementationClass(pszClassName);
            if ((null == implInfo) || (null == implInfo.CodeClass)) {
                // According with the documentation this method must return E_FAIL if the class does not
                // exist in this scope.
                return VSConstants.E_FAIL;
            }
            EnvDTE.CodeClass implClass = implInfo.CodeClass;

            // Check if the method exists.
            EnvDTE.CodeElement member = GetClassMember(implClass, handlerName);
            if (null != member) {
                // If the member exists this function must return its unique id and insertion point, but the
                // event body must be null.
                pbstrUniqueMemberID = ClassMemberUniqueId(implClass, pszEventHandlerName);
                pSpanInsertionPoint[0] = CodeElementSpan(member);
                return VSConstants.S_OK;
            }

            // There is no event handler, so we have to create it.
            EnvDTE.CodeFunction eventHandler =
                implClass.AddFunction(handlerName, EnvDTE.vsCMFunction.vsCMFunctionFunction,
                                      EnvDTE.vsCMTypeRef.vsCMTypeRefVoid, -1, EnvDTE.vsCMAccess.vsCMAccessPrivate, null);

            eventHandler.AddParameter("sender", EnvDTE.vsCMTypeRef.vsCMTypeRefObject, 1);
            eventHandler.AddParameter("args", "System.EventArgs", 2);

            // Make sure that the right document is visible.
            if (null != eventHandler.ProjectItem) {
                eventHandler.ProjectItem.Open(System.Guid.Empty.ToString("B"));
                if (null != eventHandler.ProjectItem.Document) {
                    eventHandler.ProjectItem.Document.Activate();
                }
            }

            // Set the navigation point
            pSpanInsertionPoint[0] = CodeElementSpan(eventHandler as EnvDTE.CodeElement);
            pbstrUniqueMemberID = ClassMemberUniqueId(implClass, pszEventHandlerName);

            // The HTML editor will remove the initial spaces on the string returned by this function, but
            // this will break the indentation and the file will not compile. To work around the problem
            // we create a first line that is a comment, so that its indentation is not a problem.
            StringBuilder eventBody = new StringBuilder();
            eventBody.AppendLine("#");
            // Add the actual body of the event handler.
            eventBody.Append(stringMerger.GetTextFromLine(pSpanInsertionPoint[0].iStartLine - 1));
            pbstrEventBody = eventBody.ToString();

            return VSConstants.S_OK;
        }

        public int GetBaseClassName(string pszClassName, out string pbstrBaseClassName) {
            throw new System.NotImplementedException();
        }

        public int GetCompatibleEventHandlers(string pszClassName, string pszObjectTypeName, string pszNameOfEvent, out int pcMembers, out IntPtr ppbstrEventHandlerNames, out IntPtr ppbstrMemberIDs) {
            throw new System.NotImplementedException();
        }

        public int GetEventHandlerMemberID(string pszClassName, string pszObjectTypeName, string pszNameOfEvent, string pszEventHandlerName, out string pbstrUniqueMemberID) {
            // This function must return an unique string for the event handler.
            // Note that if this function returns a not empty string, then it means that the event handler
            // is implemented inside the secondary buffer.
            pbstrUniqueMemberID = null;

            // Get the implementation class.
            EnvDTE.CodeClass aspClass = FindClass(pszClassName, FileCodeModel.CodeElements);
            if (null == aspClass) {
                throw new System.ArgumentException("pszClassName");
            }

            if (string.IsNullOrEmpty(pszEventHandlerName)) {
                throw new System.ArgumentNullException("pszEventHandlerName");
            }

            // Initialize the out parameter.
            pbstrUniqueMemberID = null;

            string handlerName = pszEventHandlerName;
            if (handlerName[0] == '_') {
                handlerName = handlerName.Substring(1);
            }

            // If there is no element with this name there is no id for it.
            if (null == GetClassMember(aspClass, handlerName)) {
                return VSConstants.S_OK;
            }

            // Build the string and return.
            pbstrUniqueMemberID = ClassMemberUniqueId(aspClass, pszEventHandlerName);
            return VSConstants.S_OK;
        }

        public int GetMemberNavigationPoint(string pszClassName, string pszUniqueMemberID, TextSpan[] pSpanNavPoint, out uint pItemID) {
            // Validate the parameters
            if (string.IsNullOrEmpty(pszClassName)) {
                throw new System.ArgumentNullException("pszClassName");
            }
            if (string.IsNullOrEmpty(pszUniqueMemberID)) {
                throw new System.ArgumentNullException("pszUniqueMemberID");
            }
            if ((null == pSpanNavPoint) || (0 == pSpanNavPoint.Length)) {
                throw new ArgumentNullException("pSpanNavPoint");
            }
            // Get the implementation class
            CodeClassInfo implInfo = ImplementationClass(pszClassName);
            if ((null == implInfo) || (null == implInfo.CodeClass)) {
                throw new System.ArgumentException("pszClassName");
            }
            EnvDTE.CodeClass implClass = implInfo.CodeClass;

            // Verify that the unique id is valid.
            string memberName = MemberNameFromUniqueId(pszUniqueMemberID);
            if (string.IsNullOrEmpty(memberName)) {
                throw new System.ArgumentException();
            }
            // Remove the '_' added by the file code model
            if (memberName[0] == '_') {
                memberName = memberName.Substring(1);
            }

            // Get the code element for the member.
            EnvDTE.CodeElement member = GetClassMember(implClass, memberName);
            if (null == member) {
                throw new System.ArgumentException();
            }

            // Set the output variables.
            pSpanNavPoint[0] = CodeElementSpan(member);
            pItemID = implInfo.ItemId;

            return VSConstants.S_OK;
        }

        public int GetMembers(string pszClassName, uint dwFlags, out int pcMembers, out IntPtr ppbstrDisplayNames, out IntPtr ppbstrMemberIDs) {
            throw new System.NotImplementedException();
        }

        public int IsValidID(string bstrID, out bool pfIsValidID) {
            // This function should return the same value as EnvDTE.CodeModel.IsValidID, but at the moment
            // there is no IronPython support for CodeModel (only FileCodeModel), so we assume that any
            // identifier is a valid ID.
            pfIsValidID = true;
            return VSConstants.S_OK;
        }

        public int OnRenamed(ContainedLanguageRenameType clrt, string bstrOldID, string bstrNewID) {
            // This method is called when a control is renamed; we don't want to do anything in this case.
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Finds the class that stores the code for a given class defined inside the secondary buffer.
        /// By (our) definition, the implementation class is the base class, as long as it is defined
        /// inside a code file.
        /// TODO: This function should store the result in some kind of cache to avoid this search
        /// that can be expensive. The problem with the cache is that we need to know when to invalidate
        /// it and this information is available to the intellisense project(s), so we have to implement
        /// a notifiacation system.
        /// </summary>
        private CodeClassInfo ImplementationClass(string aspClassName) {
            if (null == FileCodeModel) {
                throw new System.InvalidOperationException();
            }
            // Search for the definition of the class in the parse tree for the secondary buffer
            // of the container file.
            EnvDTE.CodeClass aspClass = FindClass(aspClassName, FileCodeModel.CodeElements);
            if (null == aspClass) {
                // The class is not defined in this buffer, so we can not find its implementation.
                return null;
            }

            // Now we have to look at the base class. Even if the Bases member looks like a good
            // candidate to find the base class, it actually is not because we are looking for
            // base classes defined outside this file, so the code model is not able to translate
            // the declaration of the class to an actual CodeClass object.
            // What we have to do is get the list of the declarations as System.CodeDom.CodeTypeReference
            // and search for a class with this name in the code files.
            CodeDomCodeClass codeClass = aspClass as CodeDomCodeClass;
            if (null == codeClass) {
                return new CodeClassInfo(aspClass, itemId);
            }
            // Loop on the base types of the class.
            foreach (CodeTypeReference codeRef in codeClass.CodeObject.BaseTypes) {
                // Loop on the code files known by the intellisense project.
                foreach (FileCodeModelInfo codeModelInfo in intellisenseProject.FileCodeModels) {
                    if (null != codeModelInfo.FileCodeModel) {
                        // Search for the implementation class in the file code model.
                        EnvDTE.CodeClass implClass = FindClass(codeRef.BaseType, codeModelInfo.FileCodeModel.CodeElements);
                        if (null != implClass) {
                            // We have found it, return.
                            return new CodeClassInfo(implClass, codeModelInfo.ItemId);
                        }
                    }
                }
            }

            // No base class found, return the one found in the first loop.
            return new CodeClassInfo(aspClass, itemId);
        }

        private EnvDTE.CodeClass FindClass(string className, EnvDTE.CodeElements elements) {
            if (null == elements) {
                return null;
            }
            foreach (EnvDTE.CodeElement element in elements) {
                if (null == element) {
                    continue;
                }
                if (EnvDTE.vsCMElement.vsCMElementClass == element.Kind) {
                    if (element.FullName == className) {
                        return element as EnvDTE.CodeClass;
                    }
                }
                EnvDTE.CodeClass innerClass = FindClass(className, element.Children);
                if (null != innerClass) {
                    return innerClass;
                }
            }
            return null;
        }

        private static EnvDTE.CodeElement GetClassMember(EnvDTE.CodeClass codeClass, string memberName) {
            foreach (EnvDTE.CodeElement member in codeClass.Members) {
                if ((null != member) && (member.FullName == memberName)) {
                    return member;
                }
            }
            return null;
        }

        private const char uniqueIdSeparator = '@';

        private static string ClassMemberUniqueId(EnvDTE.CodeClass codeClass, string memberName) {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}{1}{2}", memberName, uniqueIdSeparator, codeClass.FullName);
        }

        private static string MemberNameFromUniqueId(string uniqueId) {
            if (string.IsNullOrEmpty(uniqueId)) {
                return null;
            }
            int separatorPos = uniqueId.IndexOf(uniqueIdSeparator);
            if (separatorPos <= 0) {
                return null;
            }
            return uniqueId.Substring(0, separatorPos);
        }

        private static TextSpan CodeElementSpan(EnvDTE.CodeElement element) {
            TextSpan span = new TextSpan();
            if (null == element) {
                return span;
            }

            span.iStartLine = element.StartPoint.Line;
            if (span.iStartLine > 1) {
                span.iStartLine -= 1;
            }
            span.iStartIndex = element.StartPoint.DisplayColumn;

            EnvDTE.TextPoint endPoint = element.EndPoint;
            if (null == endPoint) {
                endPoint = element.StartPoint;
            }
            span.iEndLine = endPoint.Line;
            if (span.iEndLine > 1) {
                span.iEndLine -= 1;
            }
            span.iEndIndex = endPoint.DisplayColumn;

            return span;
        }

        private CodeDomFileCodeModel FileCodeModel {
            get {
                if (null != fileCodeModel) {
                    return fileCodeModel;
                }

                EnvDTE.DTE dte = PhalangerPackage.GetGlobalService(typeof(EnvDTE._DTE)) as EnvDTE.DTE;
                IVsTextLines buffer;
                ErrorHandler.ThrowOnFailure(bufferCoordinator.GetSecondaryBuffer(out buffer));
                fileCodeModel = new CodeDomFileCodeModel(dte, buffer, intellisenseProject.CodeDomProvider, "Test");

                // The contained language should not change the secondary buffer, so we have to use a string
                // as target of the merge. We will use this string to get the text generated by the file code model
                // during the creation of the event handlers.

                // Get the initial text from the buffer.
                int lines;
                int columns;
                ErrorHandler.ThrowOnFailure(buffer.GetLastLineIndex(out lines, out columns));
                string text;
                ErrorHandler.ThrowOnFailure(buffer.GetLineText(0, 0, lines, columns, out text));
                stringMerger = new Microsoft.Samples.VisualStudio.CodeDomCodeModel.StringMerger(text);
                fileCodeModel.MergeDestination = stringMerger;

                return fileCodeModel;
            }
        }
    }
}
