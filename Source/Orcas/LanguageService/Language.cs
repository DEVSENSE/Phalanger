/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Configuration;
using System.Xml;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell;

using PHP.Core.Reflection;
using PHP.Core.Parsers;
using PHP.Core;
using PHP.Core.AST;

using PHP.VisualStudio.PhalangerLanguageService.Scopes;
using PHP.VisualStudio.PhalangerLanguageService.Declarations;
using PHP.VisualStudio.PhalangerLanguageService.Parsing;
using PHP.VisualStudio.PhalangerLanguageService.OrcasLists;

namespace PHP.VisualStudio.PhalangerLanguageService 
{
    /// <summary>
    /// PHP language integration for Microsoft Visual Studio.
    /// This is the base class for a language service that supplies language features including syntax highlighting, brace matching, auto-completion, IntelliSense support, and code snippet expansion. 
    /// </summary>
	[Guid(PhalangerConstants.languageServiceGuidString)]
	public partial class PhpLanguage : LanguageService
	{
		/// <summary>
        /// LanguagePreferences object for this language service
        /// </summary>
		private LanguagePreferences preferences;

        /// <summary>
        /// Tokens scanner.
        /// Shared for all opened source files,
        /// using mechanism of saving scanner state
        /// </summary>
        private PhpScanner scanner;

		#region Colorable Items

        /// <summary>
        /// Text color styles.
        /// </summary>
		private static PhpColorableItem[] colorableItems = 
		{
			// The first 6 items in this list MUST be these default items.
			new PhpColorableItem("Keyword", COLORINDEX.CI_BLUE, COLORINDEX.CI_USERTEXT_BK, FONTFLAGS.FF_DEFAULT),
			new PhpColorableItem("Comment", COLORINDEX.CI_DARKGREEN, COLORINDEX.CI_USERTEXT_BK, FONTFLAGS.FF_DEFAULT),
			new PhpColorableItem("Identifier", COLORINDEX.CI_SYSPLAINTEXT_FG, COLORINDEX.CI_USERTEXT_BK, FONTFLAGS.FF_DEFAULT),
			new PhpColorableItem("String", COLORINDEX.CI_MAROON, COLORINDEX.CI_USERTEXT_BK, FONTFLAGS.FF_DEFAULT),
			new PhpColorableItem("Number", COLORINDEX.CI_SYSPLAINTEXT_FG, COLORINDEX.CI_USERTEXT_BK, FONTFLAGS.FF_DEFAULT),
			new PhpColorableItem("Text", COLORINDEX.CI_SYSPLAINTEXT_FG, COLORINDEX.CI_USERTEXT_BK, FONTFLAGS.FF_DEFAULT),
						
			// PHP specific:
			new PhpColorableItem("PHP - Script Tags", COLORINDEX.CI_RED, COLORINDEX.CI_USERTEXT_BK, FONTFLAGS.FF_DEFAULT), // #7
			new PhpColorableItem("PHP - Encapsulated Variable", COLORINDEX.CI_MAROON, COLORINDEX.CI_USERTEXT_BK, FONTFLAGS.FF_BOLD), // #8
			new PhpColorableItem("PHP - Outer HTML", COLORINDEX.CI_DARKGRAY, COLORINDEX.CI_USERTEXT_BK, FONTFLAGS.FF_DEFAULT), // #9
		};

		internal const TokenColor ScriptTagsColor = (TokenColor)7;
		internal const TokenColor EncapsulatedVariableColor = (TokenColor)8;
		internal const TokenColor OuterHtmlColor = (TokenColor)9;


		[SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration")]
		public override int GetItemCount(out int count)
		{
			count = colorableItems.Length;
			return Microsoft.VisualStudio.VSConstants.S_OK;
		}

		[SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration")]
		public override int GetColorableItem(int index, out IVsColorableItem item)
		{
			if (index < 1)
			{
				throw new ArgumentOutOfRangeException("index");
			}
			item = colorableItems[index - 1];
			return Microsoft.VisualStudio.VSConstants.S_OK;
		}

		#endregion

        #region Creating objects

        /// <summary>
        /// Create new Type and Member drop down object.
        /// </summary>
        /// <param name="forView"></param>
        /// <returns></returns>
        public override TypeAndMemberDropdownBars CreateDropDownHelper(IVsTextView/*!*/ forView)
		{
			return new PhpTypesAndMembersDropdowns(this);
		}

        /// <summary>
        /// Instantiates a Source class.
        /// </summary>
        /// <param name="buffer">The IVsTextLines buffer that the Source object represents.</param>
        /// <returns>If successful, returns a Source object; otherwise, returns a null value.</returns>
        /// <remarks>A Source object controls various features of the language service including colorization and code snippet expansion, as well as all parsing other than that used for colorization (which is done by the Colorizer object directly). If you need to support additional features such as commands associated with markers, or to override an existing method to provide additional handling, then you must derive a class from the Source class and return an instance of your class from this method.
        /// The base method always returns a new Source object that has been initialized with a Colorizer object that in turn has been initialized with an IScanner object returned from GetScanner.
        /// Note that the Source object returned by this method will not be parsed automatically (by OnIdle) after loading. To accomplish automatic parsing, override this method and set LastParseTime to 0.</remarks>
		public override Source CreateSource(IVsTextLines buffer)
		{
            return new PhpSource(this, buffer, new Colorizer(this, buffer, GetScanner(buffer)));
		}

        /// <summary>
        /// Returns a single instantiation of a parser.
        /// The parser that is returned is used in the colorizer and can be used in all other parsing operations.
        /// </summary>
        /// <param name="buffer">An IVsTextLines representing the lines of source to parse.</param>
        /// <returns>If successful, returns an IScanner object; otherwise, returns a null value.</returns>
		public override IScanner/*!*/ GetScanner(IVsTextLines buffer)
		{
			if (scanner == null)
			{
				scanner = new PhpScanner();
			}

			return scanner;
        }

        #endregion

        #region Source code parsing
        /// <summary>
        /// Parses the source based on the specified ParseRequest object.
        /// </summary>
        /// <param name="request">The ParseRequest describing how to parse the source file.</param>
        /// <returns>If successful, returns an AuthoringScope object; otherwise, returns a null value.
        /// The returned AuthoringScope contains all the information parsed from the source file.</returns>
        /// <remarks>Be aware that the parse operation may operate on the entire source file or a single line or even a single token.
        /// The ParseReason code on the ParseRequest dictates the scope of the parse operation.</remarks>
		public override AuthoringScope/*!*/ ParseSource(ParseRequest/*!*/ request)
		{
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            System.Diagnostics.Debug.Print("ParseSource at ({0}:{1}), reason {2}, {3}", request.Line, request.Col, request.Reason, request.Text);

            // source object
            PhpSource source = (PhpSource)GetSource(request.View);
            PhpScope newscope = null;
            
            // Highlight braces on position
            if (request.Reason == ParseReason.HighlightBraces)
            {
                FindAndHighlightBraces(source, request.Sink, request.Line, request.Col);
            }

            // Display method tip (parameters info)
            if (request.Reason == ParseReason.MethodTip)
            {
                ProcessMethodTip(source, request.Sink, request.Line, request.Col);
            }

            // Quick info
            if (request.Reason == ParseReason.QuickInfo)
            {

            }
            try
            {
                // create authoring scope
                newscope = ParseSource(request.Sink, request.FileName, request.Text, source, request.Reason);
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine(e);
			}

            // done
			return newscope;
		}

        /// <summary>
        /// Parses the given source code.
        /// </summary>
        /// <param name="sink"></param>
        /// <param name="fileName"></param>
        /// <param name="code"></param>
        /// <param name="source"></param>
        /// <param name="reason"></param>
        /// <returns>Code scope for Visual Studio.</returns>
        internal PhpScope ParseSource(AuthoringSink/*!*/ sink, string/*!*/ fileName, string/*!*/ code, PhpSource source, ParseReason reason)
		{
			LanguageFeatures language_features = LanguageFeatures.PhpClr;

			ParserErrorSink errors = new ParserErrorSink(sink);

			// TODO (get language features, encoding):
            FileInfo fiAppConfig;
            DirectoryInfo diProjectDir = GetProjectDir(fileName, out fiAppConfig);
            string project_path = diProjectDir.FullName;
            
            PhpSourceFile source_file = new PhpSourceFile(new FullPath(project_path), new FullPath(fileName));
            
			PureCompilationUnit compilation_unit = new PureCompilationUnit(true, true);

			VirtualSourceFileUnit source_unit = new VirtualSourceFileUnit(compilation_unit, code, source_file,
				Encoding.Default);

			VirtualSourceFileUnit[] parsed_source_units = null;

			PhpScope result;

            ProjectDeclarations projectdeclarations = GetProjectDeclarations(diProjectDir);

            // previous declarations scope
            ScopeInfo cacheddeclarations = null;
            projectdeclarations.Declarations.TryGetValue(fileName, out cacheddeclarations);

            // parse given source code
			try
			{
                if (reason == ParseReason.Check || cacheddeclarations == null)
                {   // new AST should be built
                    parsed_source_units =
                        compilation_unit.ParseSourceFiles(
                            new VirtualSourceFileUnit[] { source_unit },
                            errors,
                            language_features);
                }

                if (parsed_source_units != null)
                {   // we have new AST

                    GlobalCodeDeclScope declarations = new GlobalCodeDeclScope(parsed_source_units[0], projectdeclarations);
                    if (errors.SinkErrorsCount > 0)
                    {
                        // there were errors, use cached declarations tree and merge with the new one (which is incomplete).
                        declarations.MergeDeclarations(cacheddeclarations);
                    }
                    else
                    {
                        // no errors, tree will be reanalyzed so remove old includes information.
                        projectdeclarations.RemoveIncludesInfo(fileName);   // To do: remove previous connections with this scope too
                    }

                    // add into the cache
                    AddParsedScope(projectdeclarations, diProjectDir, fiAppConfig, declarations, fileName);

                    // update cacheddeclarations local value
                    cacheddeclarations = declarations;
                }

                // create new scope
                result = new PhpScope(this, cacheddeclarations, projectdeclarations, source);

                // show hidden regions
                if (sink.HiddenRegions)
                {
                    ProcessHiddenRegions(sink, cacheddeclarations);
                }
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.Print("ParseSource(): Exception caught: {0} {1}", e.Message, e.StackTrace);
#if DEBUG
				sink.AddError("<LanguageService>", e.Message, new TextSpan(), Severity.Fatal);
#endif
                result = new PhpScope(this, cacheddeclarations, projectdeclarations, source);
			}

			return result;
		}

        /// <summary>
        /// Add hidden regions into the sink.
        /// </summary>
        /// <param name="sink">Sink.</param>
        /// <param name="scope">Scope with declarations.</param>
        private void ProcessHiddenRegions(AuthoringSink/*!*/ sink, ScopeInfo/*!*/ scope)
		{
            if (scope == null)
                return;

            if ( scope.Scopes != null)
            {
                foreach (ScopeInfo s in scope.Scopes)
                {
                    // collapsible scopes
                    if (s.ScopeType == ScopeInfo.ScopeTypes.Function ||
                        s.ScopeType == ScopeInfo.ScopeTypes.Class)
                    {
                        sink.AddHiddenRegion(s.BodySpan);

                    }

                    // look-into scopes
                    //if (s.ScopeType == ScopeInfo.ScopeTypes.Class)
                    {
                        ProcessHiddenRegions(sink, s);
                    }
                }
            }

            sink.ProcessHiddenRegions = true;
		}

        /// <summary>
        /// Find root directory of the project, it's the first directory with web.config.
        /// </summary>
        /// <param name="filename">File in the project.</param>
        /// <param name="configFile">output file with app config (web.config or app.config or .phpproj file)</param>
        /// <returns>Directory with web.config.</returns>
        private DirectoryInfo GetProjectDir(string filename, out FileInfo configFile)
        {
            FileInfo fi = new FileInfo(filename);
            for(DirectoryInfo di = fi.Directory;di != null; di = di.Parent)
            {
                // web.config
                FileInfo fiWebConfig = new FileInfo(di.FullName + "\\web.config");

                if (fiWebConfig.Exists)
                {
                    configFile = fiWebConfig;
                    return di;
                }

                // app.config
                FileInfo fiAppConfig = new FileInfo(di.FullName + "\\app.config");

                if (fiAppConfig.Exists)
                {
                    configFile = fiAppConfig;
                    return di;
                }

                // *.phpproj
                FileInfo[] phpprojFiles = di.GetFiles("*" + PhalangerConstants.phpprojFileExtension);
                if (phpprojFiles != null && phpprojFiles.Length > 0)
                {
                    configFile = phpprojFiles[0];
                    return di;
                }
            }

            // no web.config found, return current directory
            configFile = null;
            return fi.Directory;
        }

        /// <summary>
        /// Find brace match on the given position.
        /// Highlight the brace pairs.
        /// </summary>
        /// <param name="source">PHP Source object.</param>
        /// <param name="sink">Sink.</param>
        /// <param name="iLine">Line position.</param>
        /// <param name="iColumn">Column after the first brace is.</param>
        private void FindAndHighlightBraces(PhpSource source, AuthoringSink sink, int iLine, int iColumn)
        {
            const int MaxLinesLookup = 30;  // stop searching in that distance

            TokenInfo[] TokensOnLine = null;    // list of tokens on the current line
            int iCurToken = -1, iCurLine = iLine;   // current position
            int iLevel = 0; // depth (+/-)

            Stack<ShortPosition> braceStack = new Stack<ShortPosition>();// current stack of braces

            do
            {
                int direction = Math.Sign(iLevel);
                iCurToken += direction;

                // out of line (or initial state)
                while (iCurToken < 0 || iCurToken >= TokensOnLine.Length)
                {
                    // next line (no move if not initiated yet)
                    iCurLine += direction;

                    if (iCurLine < 0 || iCurLine >= source.GetLineCount() || Math.Abs(iLine - iCurLine) > MaxLinesLookup)
                        return; // matching brace is not reachable

                    // parse the line, get tokens list
                    TokensOnLine = source.GetColorizer().GetLineInfo(source.GetTextLines(), iCurLine, source.ColorState);

                    if (direction == 0)
                    {   // initialization, find the token on position
                        TokenInfo tmp = new TokenInfo();
                        iCurToken = (TokensOnLine == null) ? (-1) : (source.GetTokenInfoAt(TokensOnLine, iColumn - 1, ref tmp));
                        if (iCurToken < 0) return;

                        // highlight quotes of string
                        switch ((Tokens)tmp.Token)
                        {
                            case Tokens.SingleQuotedString:
                            case Tokens.DoubleQuotedString:
                                {
                                    TextSpan t1 = new TextSpan(),t2 = new TextSpan();

                                    t1.iStartLine = t1.iEndLine = iCurLine;
                                    t1.iStartIndex = tmp.StartIndex;
                                    t1.iEndIndex = tmp.StartIndex + 1;  // = tmp.EndIndex for highliting whole string

                                    t2.iStartLine = t2.iEndLine = iCurLine;
                                    t2.iStartIndex = tmp.EndIndex;
                                    t2.iEndIndex = tmp.EndIndex + 1;

                                    sink.MatchPair(t1, t2, 0);  // highlight quotes of the string
                                }
                                return;
                        }
                    }
                    else
                    {   // first token on new line, depends on direction
                        if (TokensOnLine == null)
                            iCurToken = -1;
                        else
                            iCurToken = (direction < 0) ? (TokensOnLine.Length - 1) : (0);
                    }
                }

                TokenInfo info = TokensOnLine[iCurToken];
                ShortPosition infopos = new ShortPosition(iCurLine, info.StartIndex);

                int leveladd = 0;

                // ignore wrong pairs (parser underlines errors for me)
                // braces, parentheses, brackets:
                switch ((Tokens)info.Token)
                {
                    case Tokens.T_LPAREN:
                    case Tokens.T_LBRACE:
                    case Tokens.T_LBRACKET:
                        leveladd = +1;
                        break;
                    case Tokens.T_RPAREN:
                    case Tokens.T_RBRACE:
                    case Tokens.T_RBRACKET:
                        leveladd = -1;
                        break;
                }

                // update braces stack
                // highlight matching braces
                if (leveladd != 0)
                {
                    if (iLevel * leveladd < 0)  // closing brace, highlight the pair
                    {
                        TextSpan    t1 = new TextSpan(),
                                    t2 = new TextSpan();

                        ShortPosition prevpos = braceStack.Pop();

                        t1.iStartLine = t1.iEndLine = prevpos.Line;
                        t1.iStartIndex = prevpos.Column;
                        t1.iEndIndex = t1.iStartIndex + 1;

                        t2.iStartLine = t2.iEndLine = infopos.Line;
                        t2.iStartIndex = infopos.Column;
                        t2.iEndIndex = t2.iStartIndex + 1;

                        sink.MatchPair(t1, t2, Math.Abs(iLevel));
                    }
                    else
                    {
                        // opening brace, put into the stack
                        braceStack.Push(infopos);
                    }

                    iLevel += leveladd;
                }

            } while (iLevel != 0);  // braces closed
            
        }

        /// <summary>
        /// Scan last token before the cursor
        /// and call StartName, StartParameters, NextParameter*, EndParameters?.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="sink"></param>
        /// <param name="iLine"></param>
        /// <param name="iColumn"></param>
        private void ProcessMethodTip( PhpSource source, AuthoringSink sink, int iLine, int iColumn )
        {
            TokenInfo[] tokens = source.GetColorizer().GetLineInfo(source.GetTextLines(), iLine, source.ColorState);    // tokens on the current line (cached)

            if (tokens == null || tokens.Length == 0)
                return;

            TokenInfo lasttoken = new TokenInfo();
            int itoken = source.GetTokenInfoAt(tokens, iColumn - 1, ref lasttoken);

            if (itoken < 0)
                return;

            int ilevel;

            // init level
            switch ((Tokens)lasttoken.Token)
            {
                case Tokens.T_LPAREN:
                    ilevel = +1;
                    break;
                case Tokens.T_COMMA:
                    ilevel = +1;
                    break;
                case Tokens.T_RPAREN:
                   ilevel = 0;
                    break;
                default:
                    return;
            }

            Stack<ShortPosition> elementsStack = new Stack<ShortPosition>();// stack of (,,,)

            // parse from right to left, 'til braces are closed ( level == 0 )
            do
            {
                while (itoken < 0)
                {
                    iLine--;
                    if (iLine < 0) return;
                    tokens = source.GetColorizer().GetLineInfo(source.GetTextLines(), iLine, source.ColorState);
                    itoken = ((tokens == null)?(0):(tokens.Length)) - 1;
                }

                TokenInfo token = tokens[itoken];

                if((token.Trigger & TokenTriggers.ParameterStart) != 0)
                {
                    ilevel--;
                    if (ilevel == 0)
                        elementsStack.Push(new ShortPosition(iLine, token.StartIndex));
                }
                else if ((token.Trigger & TokenTriggers.ParameterNext)!=0)
                {
                    if (ilevel == 1)
                        elementsStack.Push(new ShortPosition(iLine, token.StartIndex));
                }
                else if ((token.Trigger & TokenTriggers.ParameterEnd)!=0)
                {
                    ilevel++;
                    if (ilevel == 1)
                        elementsStack.Push(new ShortPosition(iLine, token.StartIndex));
                }

                itoken--;

            } while (ilevel > 0);

            // now function name and finish parameter tip
            for (int inametoken = itoken; inametoken >= Math.Max(itoken - 3,0); inametoken--)
            {
                TokenInfo t = tokens[inametoken];
                
                // what should have parameters list
                if (PhpScope.TokenShouldBeFunctionName((Tokens)t.Token))
                {
                    TextSpan fspan = new TextSpan();
                    fspan.iStartLine = fspan.iEndLine = iLine;
                    fspan.iStartIndex = t.StartIndex;
                    fspan.iEndIndex = t.EndIndex + 1;

                    // function name
                    sink.StartName(fspan, source.GetText(fspan));

                    // parameters
                    int ie = 0;
                    while(elementsStack.Count>0)
                    {
                        ShortPosition pos = elementsStack.Pop();

                        TextSpan espan = new TextSpan();
                        espan.iStartLine = espan.iEndLine = pos.Line;
                        espan.iStartIndex = pos.Column;
                        espan.iEndIndex = pos.Column + 1;

                        if (ie == 0)
                        {
                            //(
                            sink.StartParameters(espan);
                        }
                        else
                        {
                            if (elementsStack.Count == 0 &&
                                (Tokens)lasttoken.Token == Tokens.T_RPAREN)
                            {
                                // )
                                sink.EndParameters(espan);
                                return;
                            }

                            // COMMA
                            sink.NextParameter(espan);
                        }

                        ie++;
                    }
                    
                    return;
                }
            }

        }

        #endregion

        #region Parsed project files cache

        /// <summary>
        /// Parsed PHP source files, used for IntelliSense and sometimes as cache.
        /// </summary>
        internal Dictionary<string, ProjectDeclarations> _projectsdeclarations = new Dictionary<string, ProjectDeclarations>(4);

        /// <summary>
        /// Get declarations scopes associated with the given project directory.
        /// </summary>
        /// <param name="projectDirectory">Project directory.</param>
        /// <returns>Project declarations.</returns>
        internal ProjectDeclarations GetProjectDeclarations(DirectoryInfo projectDirectory)
        {
            ProjectDeclarations declarations;

            if (!_projectsdeclarations.TryGetValue(projectDirectory.FullName, out declarations))
            {
                declarations = _projectsdeclarations[projectDirectory.FullName] = new ProjectDeclarations(projectDirectory);
            }

            return declarations;
        }

        /// <summary>
        /// Add parsed scope to the parsed scopes cache.
        /// </summary>
        /// <param name="projectdeclarations">all project declarations</param>
        /// <param name="projectdirectory">project root directory</param>
        /// <param name="appConfigFile">app.config, web.config or .phpproj file</param>
        /// <param name="scope">New parsed scope</param>
        /// <param name="filename">File name of the parsed file.</param>
        internal void AddParsedScope(ProjectDeclarations projectdeclarations, DirectoryInfo projectdirectory, FileInfo appConfigFile, ScopeInfo scope, string filename)
        {
            bool ReparseProjectDirectory = (projectdeclarations.Declarations.Count == 0);   // whole project directory needs to be parsed

            if ((appConfigFile != null && projectdeclarations.AppConfigLastWriteTime != appConfigFile.LastWriteTime) || projectdeclarations.AppConfigLastWriteTime.Year < 1900)
            {   // no references parsed yet or app config file was changed

                // clear all parsed files
                projectdeclarations.Declarations.Clear();
                ReparseProjectDirectory = true;

                // references
                ParseReferences(projectdeclarations, appConfigFile);
            }

            if (ReparseProjectDirectory)
            {   // no files yet, parse whole project directory

                // project .php files (TODO: should be skipped? Files are parsed on-demand - by includes and IDE)
                ParseProjectFiles(filename, projectdirectory, projectdeclarations);

                // special declarations not defined elsewhere
                projectdeclarations.Declarations.Add(".", new SpecialScope(projectdeclarations));
            }

            //
            projectdeclarations.Declarations[filename] = scope;
        }

        /// <summary>
        /// Parse assemblies added in machine.config.
        /// </summary>
        /// <param name="declarations">Save declarations here.</param>
        internal void ParseMachineConfigAssemblyFiles(ProjectDeclarations declarations)
        {
            System.Configuration.Configuration conf = ConfigurationManager.OpenMachineConfiguration();
            ConfigurationSection sect = conf.GetSection("phpNet");
            if (sect != null)
            {
                // load machine config references
                try
                {
                    XmlDocument docmachine = new XmlDocument();
                    docmachine.LoadXml(sect.SectionInformation.GetRawXml());

                    ParseAssemblyFiles(docmachine.SelectNodes("//classLibrary/add[@assembly]"), "assembly", declarations);
                }
                catch (Exception)
                {
                }

            }
        }

        /// <summary>
        /// Parse assemblies added in app.config | web.config
        /// </summary>
        /// <param name="fiAppConfig">File with the application config.</param>
        /// <param name="declarations">Save declarations here.</param>
        internal void ParseAppConfigAssemblies(FileInfo fiAppConfig, ProjectDeclarations declarations)
        {
            if (fiAppConfig != null && fiAppConfig.Exists && fiAppConfig.Extension == ".config")
            {
                // load app config references
                try
                {
                    XmlDocument docapp = new XmlDocument();
                    docapp.Load(fiAppConfig.FullName);

                    ParseAssemblyFiles(docapp.SelectNodes("//phpNet/classLibrary/add[@assembly]"), "assembly", declarations);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Parse assemblies referenced in .phpproj.
        /// </summary>
        /// <param name="fiPhpProj"></param>
        /// <param name="declarations"></param>
        internal void ParsePhpProjReferences(FileInfo fiPhpProj, ProjectDeclarations declarations)
        {
            if (fiPhpProj != null && fiPhpProj.Exists && fiPhpProj.Extension == PhalangerConstants.phpprojFileExtension)
            {
                // load app config references
                try
                {
                    TextReader tr = new StreamReader(fiPhpProj.FullName);
                    string phpprojcode = tr.ReadToEnd();

                    phpprojcode = phpprojcode.Replace("xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\"", "");   // TODO: otherwise won't select any nodes - why ?

                    XmlDocument docapp = new XmlDocument();
                    docapp.LoadXml(phpprojcode);

                    XmlNodeList references = docapp.SelectNodes("//ItemGroup/Reference[@Include]");
                    

                    ParseAssemblyFiles(references, "Include", declarations);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// Parse references.
        /// </summary>
        /// <param name="addedAssembly">List of add nodes with assembly full name.</param>
        /// <param name="IncludeAttributeName">Xml attribute name, contains assembly full name.</param>
        /// <param name="declarations">Save declarations here.</param>
        internal void ParseAssemblyFiles(XmlNodeList addedAssembly, string IncludeAttributeName, ProjectDeclarations declarations)
        {
            foreach (XmlNode x in addedAssembly)
            {
                XmlAttribute attr = x.Attributes[IncludeAttributeName];
                if (attr != null)
                {
                    string AssemblyFullName = attr.Value;

                    ScopeInfo decl = GetAssemblyDeclarationsScope(AssemblyFullName);

                    if (decl != null)
                        declarations.Declarations[AssemblyFullName] = decl;
                }
            }
        }

        /// <summary>
        /// Parse references for given project.
        /// </summary>
        /// <param name="projectdeclarations">Declarations for this project.</param>
        /// <param name="fiAppConfig">Project app.config, web.config or .phpproj file.</param>
        internal void ParseReferences(ProjectDeclarations projectdeclarations, FileInfo fiAppConfig)
        {
            projectdeclarations.RemoveAssemblyDeclarations();

            // machine.config assemblies
            ParseMachineConfigAssemblyFiles(projectdeclarations);

            // app.config | web.config assemblies
            ParseAppConfigAssemblies(fiAppConfig, projectdeclarations);

            // APP project references
            ParsePhpProjReferences(fiAppConfig, projectdeclarations);
            
            //
            // do not parse config file until it will change again
            //
            if (fiAppConfig != null && fiAppConfig.Exists)
            {
                // update last app config update time
                projectdeclarations.AppConfigLastWriteTime = fiAppConfig.LastWriteTime;
            }
            else
            {
                projectdeclarations.AppConfigLastWriteTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Cached assemblies scopes.
        /// </summary>
        internal Dictionary<string, AssemblyDeclScope> _assemblydeclarations = new Dictionary<string, AssemblyDeclScope>();

        /// <summary>
        /// Get/cache assembly declarations scope.
        /// </summary>
        /// <param name="assemblyfullname">Assembly Full Name.</param>
        /// <returns>Declarations scope.</returns>
        internal ScopeInfo GetAssemblyDeclarationsScope(string assemblyfullname)
        {
            AssemblyDeclScope decl = null;

            if (!_assemblydeclarations.TryGetValue(assemblyfullname, out decl))
            {
                try
                {
                    Assembly ass = Assembly.Load(assemblyfullname);

                    if (ass != null)
                    {
                        decl = _assemblydeclarations[assemblyfullname] = new AssemblyDeclScope(ass);

                        // reflect also referenced assemblies
                        //foreach (var reference in ass.GetReferencedAssemblies())
                        //    GetAssemblyDeclarationsScope(reference.FullName);
                    }
                }
                catch (Exception)
                {
                }
            }

            return decl;
        }

        /// <summary>
        /// Parse project files and add them into parsedScopes.
        /// </summary>
        /// <param name="exceptfile">Do not parse this file.</param>
        /// <param name="projectDirectory">Project root directory.</param>
        /// <param name="declarations">Save declarations here.</param>
        internal void ParseProjectFiles(string exceptfile, DirectoryInfo projectDirectory, ProjectDeclarations projectdeclarations)
        {
            // TODO: list files in project files hierarchy
            // TODO: asynchronously
            FileInfo[] files = projectDirectory.GetFiles("*" + PhalangerConstants.phpFileExtension, SearchOption.AllDirectories);
            
            PureCompilationUnit compilation_unit = new PureCompilationUnit(true, true);

            foreach (FileInfo fi in files)
            {
                if (fi.FullName == exceptfile || string.Compare(fi.Extension, PhalangerConstants.phpFileExtension, StringComparison.CurrentCultureIgnoreCase) != 0)
                    continue;

                projectdeclarations.AddPhpFileDeclarations(fi.FullName);
            }
        }

        #endregion

        #region Supporting Stuff

		public override void Dispose()
		{
			try
			{
				// Dispose the preferences.
				if (null != preferences)
				{
					preferences.Dispose();
					preferences = null;
				}
			}
			finally
			{
				base.Dispose();
			}
		}

        /// <summary>
        /// The name of this language.
        /// </summary>
		public override string Name
		{
			get
			{
				return Resources.PHP;
			}
		}

        /// <summary>
        /// Returns a LanguagePreferences object for this language service.
        /// </summary>
        /// <returns>A LanguagePreferences object for this language service</returns>
		public override LanguagePreferences GetLanguagePreferences()
		{
			if (preferences == null)
			{
                // preferences not created yet
				preferences = new LanguagePreferences(this.Site, typeof(PhpLanguage).GUID, this.Name);

                if (preferences != null)
                {
                    preferences.Init(); // this first

                    // For debugging purposes its recommended
                    // temporarily enable/disable the following properties.
                    preferences.EnableCodeSense = true;
                    preferences.EnableMatchBraces = true;
                    preferences.EnableCommenting = true;
                    preferences.EnableShowMatchingBrace = true;
                    preferences.EnableMatchBracesAtCaret = true;
                    preferences.HighlightMatchingBraceFlags = _HighlightMatchingBraceFlags.HMB_USERECTANGLEBRACES;
                    preferences.LineNumbers = true;
                    preferences.MaxErrorMessages = 100;
                    preferences.AutoOutlining = true;
                    preferences.MaxRegionTime = 2000;
                    preferences.ShowNavigationBar = true;
                    
                    preferences.AutoListMembers = true;
                    preferences.EnableQuickInfo = true;
                    preferences.ParameterInformation = true;
                }
			}

			return preferences;
		}

		public override System.Windows.Forms.ImageList GetImageList()
		{
			System.Windows.Forms.ImageList il = base.GetImageList();
            return il;
		}

		public override void OnIdle(bool periodic)
		{
			Source src = GetSource(this.LastActiveTextView);
			if (src != null && src.LastParseTime == Int32.MaxValue)
			{
				src.LastParseTime = 0;
			}
			base.OnIdle(periodic);
		}

		public override string GetFormatFilterList()
		{
			return Resources.PHPFormatFilter;
		}

		#endregion

        #region Code Snippets
        // IntelliSense, Code Snippets?

		private int classNameCounter = 0;

		public override ExpansionFunction CreateExpansionFunction(ExpansionProvider provider, string functionName)
		{
			ExpansionFunction function = null;
			if (functionName == "GetName")
			{
				++classNameCounter;
                function = new GetNameExpansionFunction(provider, classNameCounter);
			}
			return function;
		}

        /// <summary>
        /// Provides support for expansion functions in code snippets for a language service.
        /// </summary>
        internal class GetNameExpansionFunction : ExpansionFunction
        {
            private int nameCount;

            public GetNameExpansionFunction(ExpansionProvider provider, int counter)
                : base(provider)
            {
                nameCount = counter;
            }

            public override string GetCurrentValue()
            {
                string name = "MyClass";
                name += nameCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
                return name;
            }
        }

		private List<VsExpansion> expansionsList;
		private List<VsExpansion> ExpansionsList
		{
			get
			{
				if (null == expansionsList)
				{
                    GetSnippets();
				}

				return expansionsList;
			}
		}

        /// <summary>
        /// Initialize snippets list
        /// </summary>
        [System.Security.Permissions.SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        private void GetSnippets()
        {
            if (null == this.expansionsList)
            {
                this.expansionsList = new List<VsExpansion>();
            }
            else
            {
                this.expansionsList.Clear();
            }
            IVsTextManager2 textManager = Package.GetGlobalService(typeof(SVsTextManager)) as IVsTextManager2;
            if (textManager == null)
            {
                return;
            }
            SnippetsEnumerator enumerator = new SnippetsEnumerator(textManager, GetLanguageServiceGuid());
            foreach (VsExpansion expansion in enumerator)
            {
                if (!string.IsNullOrEmpty(expansion.shortcut))
                {
                    this.expansionsList.Add(expansion);
                }
            }
        }

		// Disable the "DoNotPassTypesByReference" warning.
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045")]
        public void AddSnippets(List<DeclarationInfo> result, string contains)
        {
            if (null == this.ExpansionsList)
            {
                return;
            }
            foreach (VsExpansion expansionInfo in this.ExpansionsList)
            {
                if (contains == null || expansionInfo.shortcut.Contains(contains))
                    result.Add(new SnippedDecl(expansionInfo));
            }
        }

        #endregion

        #region Debugging

        public override int ValidateBreakpointLocation(IVsTextBuffer buffer, int line, int col, TextSpan[] pCodeSpan)
        {
            if (pCodeSpan != null)
            {
                pCodeSpan[0].iStartLine = line;
                pCodeSpan[0].iStartIndex = col;
                pCodeSpan[0].iEndLine = line;
                pCodeSpan[0].iEndIndex = col;
                if (buffer != null)
                {
                    int length;
                    buffer.GetLengthOfLine(line, out length);
                    pCodeSpan[0].iStartIndex = 0;
                    pCodeSpan[0].iEndIndex = length;
                }
                return Microsoft.VisualStudio.VSConstants.S_OK;
            }
            else
            {
                return Microsoft.VisualStudio.VSConstants.S_FALSE;
            }
        }
        /*
		public override ViewFilter CreateViewFilter(CodeWindowManager mgr, IVsTextView newView)
		{
			// This call makes sure debugging events can be received
			// by our view filter.
			base.GetIVsDebugger();
			return new PythonViewFilter(mgr, newView);
		}
		*/

        #endregion

    }
}
