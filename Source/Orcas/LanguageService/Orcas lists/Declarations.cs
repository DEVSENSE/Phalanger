/*

 Copyright (c) 2008 Jakub Misek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;

using Microsoft.VisualStudio.Package;
using System.Diagnostics.CodeAnalysis;

using PHP.VisualStudio.PhalangerLanguageService.Declarations;

namespace PHP.VisualStudio.PhalangerLanguageService.OrcasLists 
{
	/// <summary>
	/// Manages a list of PHP declarations to be shown in the IntelliSense drop-down list.
	/// </summary>
	public class PhpDeclarations : Microsoft.VisualStudio.Package.Declarations
	{
        /// <summary>
        /// Declarations list.
        /// </summary>
        private readonly List<DeclarationInfo> declarations;

        private readonly bool AddVariableDollar, DeclarationsAreLocal;
        private readonly bool CompleteFullNamespaceName;

		private LanguageService langService;
		private TextSpan commitSpan;

        /// <summary>
        /// Full name of snippets starts with this string.
        /// Snipped shortcut follows.
        /// </summary>
        public const string SnippetsFullNameStartsWith = "#snippet-shortcut#";

        /// <summary>
        /// For special declarations which represent a snippet it extracts the snippet name from the declaration full name.
        /// </summary>
        /// <param name="decl">Declaration which could represent a snippet.</param>
        /// <returns>Null or a snippet name.</returns>
        private string GetSnippetShortcut(DeclarationInfo decl)
        {
            if (decl != null)
            {
                string fullname = decl.FullName;

                if (fullname != null && fullname.StartsWith(SnippetsFullNameStartsWith))
                {
                    return fullname.Substring(SnippetsFullNameStartsWith.Length);
                }
            }

            return null;
        }

        /// <summary>
        /// Init the list of declarations.
        /// </summary>
        /// <param name="langService">Language service.</param>
        /// <param name="declarations">List of declarations to be used.</param>
        /// <param name="VariablesStartsWithDollar">Add $ before the variables name.</param>
        /// <param name="CompleteFullNamespaceName">Add full namespace before the type name.</param>
        public PhpDeclarations(LanguageService langService, List<DeclarationInfo> declarations, bool declarationsAreLocal, bool addVariableDollar, bool completeFullNamespaceName)
		{
			this.langService = langService;
            this.declarations = declarations;
            this.AddVariableDollar = addVariableDollar;
            this.DeclarationsAreLocal = declarationsAreLocal;
            this.CompleteFullNamespaceName = completeFullNamespaceName;

            RemoveDuplicities();
		}

        private static int DeclarationsComparer(DeclarationInfo x, DeclarationInfo y)
        {
            return string.Compare(x.Label, y.Label);// ignore $ at start
        }

        /// <summary>
        /// Sort declarations and removes duplicities.
        /// </summary>
        /// <remarks>Declarations are sorted alphabetically by their name.
        /// Declarations with the same name and same properties are unified. Declarations are same when the DeclarationInfo.IsSame method returns true. For the two same declarations the one without object parameters is dropped.</remarks>
        private void RemoveDuplicities()
        {
            // sort first
            declarations.Sort(DeclarationsComparer);

            // find duplicities
            int index = 0;
            DeclarationInfo lastdecl = null;
            while (index < declarations.Count)
            {
                DeclarationInfo curdecl = declarations[index];
                if ( lastdecl != null && lastdecl.IsSameAs(curdecl) )
                {   // remove curdecl or lastdecl (that one with no parameters, so we are sure if function has any parameters we will know it)
                    if (curdecl.ObjectParameters == null || curdecl.ObjectParameters.Count == 0)
                    {
                        declarations.RemoveAt(index);
                    }
                    else
                    {
                        declarations.RemoveAt(index - 1);
                        lastdecl = curdecl;
                    }
                }
                else
                {
                    // next decl
                    lastdecl = curdecl;
                    ++index;
                }
            }
        }

        /// <summary>
        /// Get declaration at specified position in the list.
        /// Should not return null, 'cause IntelliSense only asks for items in range given by GetCount().
        /// </summary>
        /// <param name="index">Index in the list.</param>
        /// <returns>Declaration at specified position.</returns>
        private DeclarationInfo Get(int index)
		{
			return index >= 0 && index < declarations.Count ? declarations[index] : null;
		}

        /// <summary>
        /// Get declarations count.
        /// </summary>
        /// <returns>Declarations count.</returns>
		public override int GetCount()
		{
			return declarations.Count;
		}

        /// <summary>
        /// Get declaration label
        /// </summary>
        /// <param name="decl">The declaration.</param>
        /// <param name="forceDollar">If declaration can start with $, add it.</param>
        /// <returns></returns>
        private string GetDeclText(DeclarationInfo decl, bool forceDollar)
        {
            bool addDollar = forceDollar && (
                (DeclarationsAreLocal && decl.DeclarationType == DeclarationInfo.DeclarationTypes.Variable) ||  // local variable
                (!DeclarationsAreLocal && decl.DeclarationType == DeclarationInfo.DeclarationTypes.Variable && decl.IsStatic)   // object static property
                );

            if (addDollar)
            {
                return "$" + decl.Label;
            }
            else
            {
                if (decl.DeclarationType == DeclarationInfo.DeclarationTypes.Function && decl.ObjectParameters == null)
                    return decl.Label + "()";

                return decl.Label;
            }
        }
        
        /// <summary>
        /// Get text to be displayed in the given index
        /// </summary>
        /// <param name="index">index of the declaration</param>
        /// <returns>Text for the declaration on the given index</returns>
		public override string GetDisplayText(int index)
		{
            DeclarationInfo decl = Get(index);

            return GetDeclText(decl, AddVariableDollar);
		}

        /// <summary>
        /// Name of the declaration on given index
        /// </summary>
        /// <param name="index">index of the declaration</param>
        /// <returns>declaration name</returns>
		public override string GetName(int index)
		{
            DeclarationInfo decl = Get(index);

            string shortcut = GetSnippetShortcut(decl);

            if (shortcut != null)
            {
                return shortcut;
            }
            else
            {
                if (CompleteFullNamespaceName)
                        return decl.FullName;

                return decl.Label;
            }
		}

        /// <summary>
        /// Description of the declaration with given index
        /// </summary>
        /// <param name="index">index of the declaration</param>
        /// <returns>Description of the declaration with given index</returns>
		public override string GetDescription(int index)
		{
            DeclarationInfo decl = Get(index);
            //string fullname = decl.FullName;
            return decl.GetDescription();// +((fullname != null) ? "\n\n" + fullname : null);
		}

        /// <summary>
        /// Image number of the declaration with given index
        /// </summary>
        /// <param name="index">declaration index</param>
        /// <returns>Image number of the declaration with given index</returns>
        /// <remarks>
        /// return (-1) if there is no image
        /// </remarks>
		public override int GetGlyph(int index)
		{
			// return right image next the declaration with given index)
            DeclarationInfo decl = Get(index);

            return decl.Glyph;
		}

        /*/// <summary>
        /// Get best item to be matched.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="index"></param>
        /// <param name="uniqueMatch"></param>
        public override void GetBestMatch(string value, out int index, out bool uniqueMatch)
        {
            string valuel = value.ToLower();

            uniqueMatch = (declarations.Count == 1);

            int matchPriority = 0;
            index = 0;

            for (int i = 0; i < declarations.Count; ++i )
            {
                DeclarationInfo decl = declarations[i];

                int priority = 0;

                if (decl.Label.StartsWith(value)) priority += 10;
                else if (decl.Label.ToLower().StartsWith(valuel)) priority += 5;
                if (decl.DeclarationType == DeclarationInfo.DeclarationTypes.Variable) priority += 3;

                if (priority > matchPriority)
                {
                    index = i;
                    matchPriority = priority;
                }
            }

            if (matchPriority > 0)
                return;

            index = 0;
            base.GetBestMatch(value, out index, out uniqueMatch);
        }*/

        /*public override bool  IsMatch(string textSoFar, int index)
        {
 	         DeclarationInfo decl = Get(index);

            return decl.Label.StartsWith(textSoFar);
        }*/

        /// <summary>
		/// This method is called to get the string to commit to the source buffer.
		///	Note that the initial extent is only what the user has typed so far.
		///	Disable the "ParameterNamesShouldMatchBaseDeclaration" warning.
		/// </summary>
        /// <remarks>If the selected declaration represents a snippet, the snippet is inserted. Otherwise the declaration Label is used.
        /// For variables the $ is inserted before is the VariablesStartsWithDollar is enabled.
        /// For functions without parameters, the starting end ending brackets are added.
        /// If the CompleteFullNamespaceName is specified, the declaration FullName is used.</remarks>
		[SuppressMessage("Microsoft.Naming", "CA1725")]
		public override string OnCommit(IVsTextView textView, string textSoFar, char commitCharacter, int index, ref TextSpan initialExtent)
        {
            DeclarationInfo decl = Get(index);

            string shortcut = GetSnippetShortcut(decl);
            commitSpan = initialExtent;

            if (shortcut != null)
            {
                return base.OnCommit(textView, textSoFar, commitCharacter, index, ref initialExtent);
            }
            else
            {
                return GetDeclText(decl, AddVariableDollar);
                /*// this text will be inserted on the cursor position
                
                if (VariablesStartsWithDollar &&
                    decl.DeclarationType == DeclarationInfo.DeclarationTypes.Variable)
                    return "$" + decl.Label;    // variables starts with $

                if (decl.DeclarationType == DeclarationInfo.DeclarationTypes.Function &&
                    (decl.ObjectParameters == null || decl.ObjectParameters.Count == 0))
                    return decl.Label + "()";   // add () for methods/functions with no parameters

                if (CompleteFullNamespaceName)
                    return decl.FullName;

                return decl.Label;*/
            }
		}

		/// <summary>
		/// This method is called after the string has been committed to the source buffer.
		/// </summary>
		public override char OnAutoComplete(IVsTextView textView, string committedText, char commitCharacter, int index)
		{
			const char defaultReturnValue = '\0';
            DeclarationInfo item = Get(index);
			if (item == null)
				return defaultReturnValue;

            if (GetSnippetShortcut(item) == null /*item.Type != Declaration.DeclarationType.Snippet*/)
            {
                return defaultReturnValue;
            }
			
			// it is a snippet - insert the snippet code
			Source src = this.langService.GetSource(textView);
			if (src == null)
				return defaultReturnValue;
			
			ExpansionProvider ep = src.GetExpansionProvider();
			if (ep == null)
				return defaultReturnValue;
			
			// now we have everything to insert the snippet
			string title;
			string path;
			int commitLength = commitSpan.iEndIndex - commitSpan.iStartIndex;
			if (commitLength < committedText.Length)
			{
				// Replace everything that was inserted so calculate the span of the full
				// insertion, taking into account what was inserted when the commitSpan
				// was obtained in the first place.
				commitSpan.iEndIndex += (committedText.Length - commitLength);
			}
            // replace the commitCharacter too
            switch (commitCharacter)
            {
                case '\0':
                case '\r':
                case '\n':
                    break;
                default:
                    commitSpan.iEndIndex += 1;
                    break;
            }

			if (ep.FindExpansionByShortcut(textView, committedText, commitSpan, true, out title, out path) >= 0)
				ep.InsertNamedExpansion(textView, title, path, commitSpan, false);
            
			return defaultReturnValue;
		}
	}
}
