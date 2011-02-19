/*

 Copyright (c) 2006 Tomas Matousek.

 Copyright (c) 2008 Jakub Misek


 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Collections;
using PHP.Core.Reflection;
using PHP.Core.Parsers;
using PHP.Core.AST;
using Microsoft.VisualStudio;

using System.Runtime.InteropServices;

using PHP.VisualStudio.PhalangerLanguageService.Scopes;
using PHP.VisualStudio.PhalangerLanguageService.Declarations;

namespace PHP.VisualStudio.PhalangerLanguageService.OrcasLists
{
    /// <summary>
    /// Two drop-down lists on the top of the text editor.
    /// Manages types list and their members list in the current source code.
    /// </summary>
	class PhpTypesAndMembersDropdowns : TypeAndMemberDropdownBars
	{
		// TP: not used..?
		// private static readonly ArrayList/*!*/ EmptyArrayList = new ArrayList(0);

        private IVsTextView _textview = null;
        private ArrayList _dropDownTypes = null;

        /// <summary>
        /// Init the object.
        /// </summary>
        /// <param name="languageService">Language service.</param>
		public PhpTypesAndMembersDropdowns(LanguageService/*!*/ languageService)
			: base(languageService)
		{

		}

		/// <summary>
		/// The method has to be overridden since its base implementation is buggy 
		/// Actually, the DropDownMember.op_Equality is buggy as it doesn't handle comparison with null.
		/// Apparently, this comparison is used in the base method.
		/// </summary>
		public override int GetEntryImage(int combo, int entry, out int imgIndex)
		{
			DropDownMember member = this.GetMember(combo, entry);
			imgIndex = !ReferenceEquals(member,null) ? member.Glyph : -1;
			return VSConstants.S_OK;
		}

		/// <summary>
		/// The method has to be overridden since its base implementation is buggy 
		/// Actually, the DropDownMember.op_Equality is buggy as it doesn't handle comparison with null.
		/// Apparently, this comparison is used in the base method.
		/// </summary>
		public override int GetEntryAttributes(int combo, int entry, out uint fontAttrs)
		{
			DropDownMember member = this.GetMember(combo, entry);
            fontAttrs = !ReferenceEquals(member, null) ? (uint)member.FontAttr : 0;
			return VSConstants.S_OK;
		}

		/// <summary>
		/// The method has to be overridden since its base implementation is buggy 
		/// Actually, the DropDownMember.op_Equality is buggy as it doesn't handle comparison with null.
		/// Apparently, this comparison is used in the base method.
		/// </summary>
		public override int GetEntryText(int combo, int entry, out string text)
		{
			DropDownMember member = this.GetMember(combo, entry);
            text = !ReferenceEquals(member, null) ? member.Label : null;
			return VSConstants.S_OK;
		}

        /// <summary>
        /// Set focus to the given window, Win32 function.
        /// </summary>
        /// <param name="hWnd">Window handle.</param>
        /// <returns>Window handle.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetFocus(IntPtr hWnd);

        /// <summary>
        /// The method has to be overridden since its base implementation is buggy.
        /// </summary>
        public override int OnItemChosen(int combo, int entry)
        {
            DropDownMember member = this.GetMember(combo, entry);
            if (!ReferenceEquals(member, null) && _textview != null)
            {
                TextSpan span = member.Span;
                _textview.CenterLines(span.iStartLine, span.iEndLine - span.iStartLine + 1);
                _textview.SetCaretPos(span.iStartLine, span.iStartIndex);
                SetFocus(_textview.GetWindowHandle());                
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Fill the lists with types and current type members.
        /// </summary>
        /// <param name="languageService">Language service.</param>
        /// <param name="textView">Text view.</param>
        /// <param name="line">Cursor line position.</param>
        /// <param name="col">Cursor column position.</param>
        /// <param name="dropDownTypes">Result list of the types in the current source code.</param>
        /// <param name="dropDownMembers">Result list of the type members of the type under the cursor.</param>
        /// <param name="selectedType">Will be set to the index of the current type.</param>
        /// <param name="selectedMember">Will be set to the index of the current type member.</param>
        /// <returns>Returns true if function succedeed, otherwise false.</returns>
        public override bool OnSynchronizeDropdowns(
			LanguageService/*!*/ languageService,
			IVsTextView/*!*/ textView,
			int line, int col,
			ArrayList dropDownTypes,
			ArrayList dropDownMembers,
			ref int selectedType,
			ref int selectedMember)
		{
            _textview = textView;
            _dropDownTypes = dropDownTypes;

			PhpLanguage service = (PhpLanguage)languageService;
            PhpSource source = (PhpSource)service.GetSource(textView);
            
            if (source == null)
			{
				if (dropDownTypes.Count != 0 || dropDownMembers.Count != 0)
				{
					dropDownTypes.Clear();
					dropDownMembers.Clear();
					return true;
				}

				return false;
			}

            if (source.Scope == null)
				return false;

			//if (selectedType >= 0 && selectedType < dropDownTypes.Count)
			//  ((DropDownMember)dropDownTypes[selectedType]).FontAttr = DROPDOWNFONTATTR.FONTATTR_PLAIN;

			//if (selectedMember >= 0 && selectedMember < dropDownMembers.Count)
			//  ((DropDownMember)dropDownMembers[selectedMember]).FontAttr = DROPDOWNFONTATTR.FONTATTR_PLAIN;

			dropDownTypes.Clear();
            dropDownMembers.Clear();

            selectedType = selectedMember = 0;

            //
            // Declarations
            //
            dropDownTypes.Add(new DropDownMember("(global code)", new TextSpan(), 0, DROPDOWNFONTATTR.FONTATTR_GRAY));

            if (source.Scope.Scope != null)
                AddDeclarations(source.Scope.Scope.Declarations, ref selectedType, ref selectedMember, line, col, dropDownTypes, dropDownMembers);

			return true;
		}

        /// <summary>
        /// Fill the types list and the type members list.
        /// </summary>
        /// <param name="decls">List of types declarations.</param>
        /// <param name="curdecl">Will be set to the current type index.</param>
        /// <param name="curmember">Will be set to the current type member index.</param>
        /// <param name="line">Cursor line position.</param>
        /// <param name="col">Cursor column position.</param>
        /// <param name="dropDownTypes">Result list of the types.</param>
        /// <param name="dropDownMembers">Result list of the current type members.</param>
        private void AddDeclarations(
            List<DeclarationInfo> decls,
            ref int curdecl, ref int curmember,
            int line, int col,
			ArrayList dropDownTypes,
			ArrayList dropDownMembers)
        {
            if (decls == null)
                return;

            foreach (DeclarationInfo decl in decls)
            {
                switch (decl.DeclarationType)
                {
                    case DeclarationInfo.DeclarationTypes.Class:
                    case DeclarationInfo.DeclarationTypes.Function:

                        if (Utils.IsInSpan(line, col, decl.Span))
                        {
                            curdecl = dropDownTypes.Count;
                            AddDeclarationMembers(decl.DeclarationScope, ref curmember, line, col, dropDownMembers);
                        }
                        
                        dropDownTypes.Add(decl);

                        break;
                    case DeclarationInfo.DeclarationTypes.Namespace:
                        {
                            DeclarationList members = new DeclarationList();
                            decl.GetTypeMembers(null,members,new DeclarationLabelEqual(null));

                            AddDeclarations(members, ref curdecl, ref curmember, line, col, dropDownTypes, dropDownMembers);
                        }
                        
                        break;
                }
            }
        }

        /// <summary>
        /// Fill the type members list.
        /// </summary>
        /// <param name="declscope">Current type declaration scope.</param>
        /// <param name="curmember">Will be set to the current member index.</param>
        /// <param name="line">Cursor line position.</param>
        /// <param name="col">Cursor collumn position.</param>
        /// <param name="dropDownMembers">Result list of the members.</param>
        private void AddDeclarationMembers(
            ScopeInfo declscope, ref int curmember,
            int line, int col,
            ArrayList dropDownMembers)
        {
            dropDownMembers.Clear();

            dropDownMembers.Add(new DropDownMember("(definition)", declscope.BodySpan, 0, DROPDOWNFONTATTR.FONTATTR_GRAY));
            curmember = 0;

            if (declscope != null)
            {
                List<DeclarationInfo> members = declscope.Declarations;

                if (members == null)
                    return;

                foreach (DeclarationInfo decl in members)
                    if (Utils.IsInSpan(decl.Span.iStartLine, decl.Span.iStartIndex, declscope.BodySpan) &&
                        decl.ParentScope == declscope)
                    {
                        if (Utils.IsInSpan(line, col, decl.Span))
                            curmember = dropDownMembers.Count;
                        
                        dropDownMembers.Add(decl);
                    }
            }
        }
	}
}
