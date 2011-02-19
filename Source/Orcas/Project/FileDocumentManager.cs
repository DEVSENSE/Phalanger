using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections;
using System.Text;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using IServiceProvider = System.IServiceProvider;
using ShellConstants = Microsoft.VisualStudio.Shell.Interop.Constants;
using OleConstants = Microsoft.VisualStudio.OLE.Interop.Constants;
using Microsoft.VisualStudio.Project;

namespace PHP.VisualStudio.PhalangerProject {
    /// <summary>
    /// This class handles opening, saving of file items in the hierarchy.
    /// </summary>
    [CLSCompliant(false)]
    public class PhalangerFileDocumentManager:FileDocumentManager {
        public PhalangerFileDocumentManager(FileNode node): base(node){}
        public override int Open(bool newFile, bool openWith, ref Guid logicalView, IntPtr docDataExisting, out IVsWindowFrame windowFrame, WindowFrameShowAction windowFrameAction) {
            PhalangerFileNode currNode = this.Node as PhalangerFileNode;
            if(currNode != null && currNode.SubType == "Form" && currNode.FirstChild != null) {
                        
            }
            return base.Open(newFile, openWith, ref logicalView, docDataExisting, out windowFrame, windowFrameAction);
        }
        public override int OpenWithSpecific(uint editorFlags, ref Guid editorType, string physicalView, ref Guid logicalView, IntPtr docDataExisting, out IVsWindowFrame windowFrame, WindowFrameShowAction windowFrameAction) {
            return base.OpenWithSpecific(editorFlags, ref editorType, physicalView, ref logicalView, docDataExisting, out windowFrame, windowFrameAction);
        }
        public override int Open(ref Guid logicalView, IntPtr docDataExisting, out IVsWindowFrame windowFrame, WindowFrameShowAction windowFrameAction) {
            return base.Open(ref logicalView, docDataExisting, out windowFrame, windowFrameAction);
        }
    }
}
