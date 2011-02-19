/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

using VSConstants = Microsoft.VisualStudio.VSConstants;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace PHP.VisualStudio.PhalangerLanguageService {
    /// <summary>
    /// This partial class contains the code needed to make the view filter work when
    /// the language service is contained inside another language. The problem in this
    /// case is that there is no access to the actual text buffer, but we have to use
    /// the buffer coordinator that knows how to map the primary buffer to the secondary.
    /// </summary>
    internal partial class PythonViewFilter {
        private IVsTextBufferCoordinator bufferCoordinator;

        internal IVsTextBufferCoordinator BufferCoordinator {
            get { return this.bufferCoordinator; }
            set { this.bufferCoordinator = value; }
        }

        public override ExpansionProvider GetExpansionProvider() {
            if (null == this.Source) {
                return null;
            }
            return base.GetExpansionProvider();
        }

        // Override the IVsTextViewFilter methods.

        public override int GetWordExtent(int line, int index, uint flags, TextSpan[] span) {
            // Check if we have a buffer coordinator set.
            if (null == BufferCoordinator) {
                // No buffer coordinator, so delegate to the base implementation.
                return base.GetWordExtent(line, index, flags, span);
            }

            // Make sure that the span array is not empty.
            if ((null == span) || (span.Length == 0)) {
                throw new ArgumentNullException("span");
            }

            // There is a buffer coordinator, so we have to translate the spans from the
            // primary to the secondary buffer.
            TextSpan originalSpan = new TextSpan();
            originalSpan.iStartLine = line;
            originalSpan.iStartIndex = index;
            originalSpan.iEndLine = line;
            originalSpan.iEndIndex = index;

            TextSpan[] convertedSpan = new TextSpan[1];
            ErrorHandler.ThrowOnFailure(BufferCoordinator.MapPrimaryToSecondarySpan(originalSpan, convertedSpan));
            // Now call the base function using the converted location.
            TextSpan[] secondarySpan = new TextSpan[1];
            int returnCode = base.GetWordExtent(convertedSpan[0].iStartLine, convertedSpan[0].iStartIndex, flags, secondarySpan);
            if (VSConstants.S_OK != returnCode) {
                return returnCode;
            }
            // Convert the returned span to something usable in the primary buffer.
            ErrorHandler.ThrowOnFailure(BufferCoordinator.MapSecondaryToPrimarySpan(secondarySpan[0], span));
            return VSConstants.S_OK;
        }

        public override int GetDataTipText(TextSpan[] aspan, out string textValue) {
            // Make sure that we have a text span.
            if ((null == aspan) || (0 == aspan.Length)) {
                throw new ArgumentNullException("aspan");
            }

            // Check if we have to convert the text span for the secondary buffer.
            TextSpan[] convertedSpan = new TextSpan[1];
            if (null == BufferCoordinator) {
                convertedSpan[0] = aspan[0];
            } else {
                ErrorHandler.ThrowOnFailure(BufferCoordinator.MapPrimaryToSecondarySpan(aspan[0], convertedSpan));
            }

            // Call the base implementation on the new span.
            return base.GetDataTipText(convertedSpan, out textValue);
        }

        public override int GetPairExtents(int line, int index, TextSpan[] span) {
            // If the buffer coordinator is null, then this is the standard case and we can
            // delegate to the base implementation.
            if (null == BufferCoordinator) {
                return base.GetPairExtents(line, index, span);
            }

            // Verify that the array with the text span is usable.
            if ((null == span) || (0 == span.Length)) {
                throw new ArgumentNullException("span");
            }

            // Now we have to translate the position from the primary to the secondary buffer.
            TextSpan originalSpan = new TextSpan();
            originalSpan.iStartLine = line;
            originalSpan.iStartIndex = index;
            originalSpan.iEndLine = line;
            originalSpan.iEndIndex = index;

            TextSpan[] convertedSpan = new TextSpan[1];
            ErrorHandler.ThrowOnFailure(BufferCoordinator.MapPrimaryToSecondarySpan(originalSpan, convertedSpan));

            // Now we can call the base implementation saving the result in a temporary span that
            // is relative to the secondary buffer.
            TextSpan[] secondarySpan = new TextSpan[1];
            int returnCode = base.GetPairExtents(convertedSpan[0].iStartLine, convertedSpan[0].iStartIndex, secondarySpan);
            if (VSConstants.S_OK != returnCode) {
                return returnCode;
            }

            // Translate the span for the primary buffer.
            ErrorHandler.ThrowOnFailure(BufferCoordinator.MapSecondaryToPrimarySpan(secondarySpan[0], span));
            return VSConstants.S_OK;
        }
    }
}
