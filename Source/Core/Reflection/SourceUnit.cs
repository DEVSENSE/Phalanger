/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using PHP.Core.Parsers;
using PHP.Core.Emit;
using System.Reflection.Emit;
using System.Reflection;
using System.Diagnostics.SymbolStore;
using System.Diagnostics;

namespace PHP.Core.Reflection
{
	#region SourceUnit

	public abstract class SourceUnit
	{
		/// <summary>
		/// Containing compilation unit.
		/// </summary>
		public CompilationUnitBase/*!*/ CompilationUnit { get { return compilationUnit; } }
		protected readonly CompilationUnitBase/*!*/ compilationUnit;

		/// <summary>
		/// Source file containing the unit. For evals, it can be even a non-php source file.
		/// Used for emitting debug information and error reporting.
		/// </summary>
		public PhpSourceFile/*!*/ SourceFile { get { return sourceFile; } }
		protected readonly PhpSourceFile/*!*/ sourceFile;

		public AST.GlobalCode Ast { get { return ast; } }
		protected AST.GlobalCode ast;

        /// <summary>
        /// Dictionary of PHP aliases.
        /// </summary>
        public Dictionary<string, QualifiedName>/*!*/ Aliases { get { return aliases; } }
        private readonly Dictionary<string, QualifiedName>/*!*/ aliases = new Dictionary<string, QualifiedName>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Current namespace (in case we are compiling through eval from within namespace).
        /// </summary>
        public QualifiedName? CurrentNamespace { get { return currentNamespace; } }
        private QualifiedName? currentNamespace = null;


        //public Dictionary<Name, QualifiedName> TypeAliases { get { return typeAliases; } }
        //private Dictionary<Name, QualifiedName> typeAliases = null;

        //public Dictionary<Name, QualifiedName> FunctionAliases { get { return functionAliases; } }
        //private Dictionary<Name, QualifiedName> functionAliases = null;

        //public Dictionary<Name, QualifiedName> ConstantAliases { get { return constantAliases; } }
        //private Dictionary<Name, QualifiedName> constantAliases = null;

        public List<QualifiedName>/*!*/ImportedNamespaces { get { return importedNamespaces; } }
        private readonly List<QualifiedName>/*!*/importedNamespaces = new List<QualifiedName>();
        public bool HasImportedNamespaces { get { return this.importedNamespaces != null && this.importedNamespaces.Count > 0; } }

		/// <summary>
		/// Place where this unit's <see cref="NamingContext"/> is stored (<B>null</B> if there are no imports).
		/// Not an <see cref="IPlace"/> as we need to encode it to the <see cref="DTypeSpec"/>.
		/// </summary>
		public FieldBuilder NamingContextFieldBuilder { get { return namingContextFieldBuilder; } }
		private FieldBuilder namingContextFieldBuilder;

		/// <summary>
		/// Symbol document writer associated with the unit.
		/// </summary>
		public ISymbolDocumentWriter SymbolDocumentWriter { get { return symbolDocumentWriter; } }
		private ISymbolDocumentWriter symbolDocumentWriter;

		/// <summary>
		/// Encoding of the file or the containing file.
		/// </summary>
		public Encoding/*!*/ Encoding { get { return encoding; } }
		protected readonly Encoding/*!*/ encoding;

		#region Construction

		public SourceUnit(CompilationUnitBase/*!*/ compilationUnit, PhpSourceFile/*!*/ sourceFile, Encoding/*!*/ encoding)
		{
			Debug.Assert(compilationUnit != null && sourceFile != null && encoding != null);

			this.compilationUnit = compilationUnit;
			this.sourceFile = sourceFile;
			this.encoding = encoding;
			this.namingContextFieldBuilder = null;   // to be filled during compilation just before the unit gets emitted
			this.symbolDocumentWriter = null; // to be filled during compilation just before the unit gets emitted
		}

		#endregion

		/// <summary>
		/// Gets a piece of source code.
		/// </summary>
		/// <param name="position">Position of the piece to get.</param>
		/// <returns>Source code.</returns>
		public abstract string GetSourceCode(Position position);

		public abstract void Parse(ErrorSink/*!*/ errors, IReductionsSink/*!*/ reductionsSink,
			Parsers.Position initialPosition, LanguageFeatures features);

		public abstract void Close();

		#region Source Position Mapping (#pragma line/file)

		public const int DefaultLine = Int32.MinValue;
		public const string DefaultFile = null;
		
		private List<int> mappedPathsAnchors;
		private List<string> mappedPaths;
		private List<int> mappedLinesAnchors;
		private List<int> mappedLines;

		internal void AddSourceFileMapping(int realLine, string mappedFullPath)
		{
			if (mappedPathsAnchors == null)
			{
				mappedPathsAnchors = new List<int>();
				mappedPaths = new List<string>();
			}

			mappedPathsAnchors.Add(realLine);
			mappedPaths.Add(mappedFullPath);
		}

		internal void AddSourceLineMapping(int realLine, int mappedLine)
		{
			if (mappedLinesAnchors == null)
			{
				mappedLinesAnchors = new List<int>();
				mappedLines = new List<int>();
			}

			mappedLinesAnchors.Add(realLine);
			mappedLines.Add(mappedLine);
		}

		public ISymbolDocumentWriter GetMappedSymbolDocumentWriter(int realLine)
		{
			return compilationUnit.GetSymbolDocumentWriter(GetMappedFullSourcePath(realLine));
		}

		public string/*!*/ GetMappedFullSourcePath(int realLine)
		{
			if (mappedPathsAnchors == null) return sourceFile.FullPath;
			Debug.Assert(mappedPaths != null);

			int index = mappedPathsAnchors.BinarySearch(realLine);

			// the line containing the pragma:
			string result;
			if (index >= 0)
			{
				result = mappedPaths[index];
			}
			else
			{
				index = ~index - 1;
				result = (index < 0) ? sourceFile.FullPath : mappedPaths[index];
			}

			return (result != DefaultFile) ? result : sourceFile.FullPath;
		}

		public int GetMappedLine(int realLine)
		{
			if (mappedLinesAnchors == null) return realLine;
			Debug.Assert(mappedLines != null);

			int index = mappedLinesAnchors.BinarySearch(realLine);

			// the line containing the pragma:
			if (index >= 0)
				return (mappedLines[index] != 0) ? mappedLines[index] : realLine;

			index = ~index - 1;

			return (index < 0 || mappedLines[index] == DefaultLine) ? realLine :
				mappedLines[index] + realLine - mappedLinesAnchors[index];
		}

		#endregion

        //#region Aliases and Imported Namespaces

        //public bool AddTypeAlias(QualifiedName typeName, Name alias)
        //{
        //    if (typeAliases == null)
        //        typeAliases = new Dictionary<Name, QualifiedName>();
        //    else if (typeAliases.ContainsKey(alias))
        //        return false;

        //    typeAliases.Add(alias, typeName);
        //    return true;
        //}

        //public bool AddFunctionAlias(QualifiedName functionName, Name alias)
        //{
        //    if (functionAliases == null)
        //        functionAliases = new Dictionary<Name, QualifiedName>();
        //    else if (functionAliases.ContainsKey(alias))
        //        return false;

        //    functionAliases.Add(alias, functionName);
        //    return true;
        //}

        //public bool AddConstantAlias(QualifiedName constantName, Name alias)
        //{
        //    if (constantAliases == null)
        //        constantAliases = new Dictionary<Name, QualifiedName>();
        //    else if (constantAliases.ContainsKey(alias))
        //        return false;

        //    constantAliases.Add(alias, constantName);
        //    return true;
        //}

        //public void AddImportedNamespace(QualifiedName namespaceName)
        //{
        //    if (importedNamespaces == null)
        //        importedNamespaces = new List<QualifiedName>();

        //    importedNamespaces.Add(namespaceName);
        //}

        /// <summary>
        /// Used to merge namespaces included by the caller of 'eval' function.
        /// </summary>
        /// <param name="namingContext">Naming context of the caller</param>
        public void AddImportedNamespaces(NamingContext namingContext)
        {
            if (namingContext == null) return;

            this.currentNamespace = namingContext.CurrentNamespace;
            if (namingContext.Aliases != null)
                foreach (var alias in namingContext.Aliases)
                    this.Aliases.Add(alias.Key, alias.Value);
            
            //foreach (string s in namingContext.Prefixes)
            //{
            //    string nsn = s.EndsWith(QualifiedName.Separator.ToString()) ? s.Substring(0, s.Length - QualifiedName.Separator.ToString().Length) : s;
            //    AddImportedNamespace(new QualifiedName(nsn, false));
            //}
        }

        //#endregion

		#region Name Resolving

		/// <summary>
		/// Resolves a function or type name using aliases and imported namespaces of the source unit.
		/// </summary>
		/// <param name="qualifiedName">Function qualified name to resolve. Doesn't resolve special names ("self", "parent").</param>
		/// <param name="kind">Declaration kind.</param>
		/// <param name="currentScope">Current scope.</param>
		/// <param name="alias">
		/// <B>null</B>, if the function name is resolved immediately.
		/// Otherwise, if the <paramref name="qualifiedName"/> is simple and an alias exists, contains its qualified target.
		/// </param>
		/// <param name="errors">Error sink or <B>null</B> if errors shouldn't be reported.</param>
		/// <param name="position">Position where to report an error.</param>
		/// <param name="mustResolve">Whether name must be resolved if possible.</param>
		/// <returns>
		/// Resolved member, the unknown member, or <B>null</B> if error reporting is disabled (errors == null).
		/// </returns>
		/// <remarks>
		/// If the name is simple, is not resolved and has an alias then the run-time resolve should be run on the alias.
		/// If the name is simple, is not resolved and hasn't an alias, the run-time resolve should be run on the name
		///		within the naming context of the source unit (i.e. imported namespaces should be considered).
		/// If the name is fully qualified and is not resolved then then the run-time resolve should be run on the name itself.
		/// </remarks>
		private DMember ResolveName(QualifiedName qualifiedName, DeclarationKind kind, Scope currentScope,
			out QualifiedName? alias, ErrorSink errors, Position position, bool mustResolve)
		{
			string full_name = null;
			DMember result;
			alias = null;

			// try exact match:
			result = ResolveExactName(qualifiedName, ref full_name, kind, currentScope, mustResolve);
			if (result != null)
				return result;

            /*  // aliases are resolved in parse-time
			// try explicit aliases:
			if (qualifiedName.IsSimpleName)
			{
				QualifiedName alias_qualified_name;

				Dictionary<Name, QualifiedName> aliases = null;
				switch (kind)
				{
					case DeclarationKind.Type: aliases = typeAliases; break;
					case DeclarationKind.Function: aliases = functionAliases; break;
					case DeclarationKind.Constant: aliases = constantAliases; break;
				}

				// try alias:
				if (aliases != null && aliases.TryGetValue(qualifiedName.Name, out alias_qualified_name))
				{
					// alias exists //

					full_name = null;
					result = ResolveExactName(alias_qualified_name, ref full_name, kind, currentScope, mustResolve);
					if (result != null)
						return result;

					alias = alias_qualified_name;

					switch (kind)
					{
						case DeclarationKind.Type: result = new UnknownType(full_name); break;
						case DeclarationKind.Function: result = new UnknownFunction(full_name); break;
						case DeclarationKind.Constant: result = new UnknownGlobalConstant(full_name); break;
					}

					return result;
				}
			}
            */

			// try imported namespaces:
            if (!qualifiedName.IsFullyQualifiedName && HasImportedNamespaces)
			{
				result = null;

				foreach (QualifiedName imported_ns in importedNamespaces)
				{
					QualifiedName combined_qualified_name = new QualifiedName(qualifiedName, imported_ns);
					full_name = null;

					DMember candidate = ResolveExactName(combined_qualified_name, ref full_name, kind, currentScope, mustResolve);
					if (candidate != null)
					{
						if (result != null)
						{
							if (errors != null)
							{
								ErrorInfo error;
								switch (kind)
								{
									case DeclarationKind.Type: error = Errors.AmbiguousTypeMatch; break;
									case DeclarationKind.Function: error = Errors.AmbiguousFunctionMatch; break;
									case DeclarationKind.Constant: error = Errors.AmbiguousConstantMatch; break;
									default: throw null;
								}

								errors.Add(error, this, position, result.FullName, candidate.FullName, qualifiedName.Name);
							}
						}
						else
						{
							result = candidate;
						}
					}
				}

				if (result != null)
					return result;
			}
            
			// unknown qualified name:
			if (errors != null)
			{
				switch (kind)
				{
					case DeclarationKind.Type: result = new UnknownType(qualifiedName.ToString()); break;
					case DeclarationKind.Function: result = new UnknownFunction(qualifiedName.ToString()); break;
					case DeclarationKind.Constant: result = new UnknownGlobalConstant(qualifiedName.ToString()); break;
				}

				return result;
			}

			return null;
		}

		private DMember ResolveExactName(QualifiedName qualifiedName, ref string/*!*/ fullName,
			DeclarationKind kind, Scope currentScope, bool mustResolve)
		{
			switch (kind)
			{
				case DeclarationKind.Type:
					return compilationUnit.GetVisibleType(qualifiedName, ref fullName, currentScope, mustResolve);

				case DeclarationKind.Function:
					return compilationUnit.GetVisibleFunction(qualifiedName, ref fullName, currentScope);

				case DeclarationKind.Constant:
					return compilationUnit.GetVisibleConstant(qualifiedName, ref fullName, currentScope);

				default:
					Debug.Fail();
					throw null;
			}
		}

		public DRoutine ResolveFunctionName(QualifiedName qualifiedName, Scope currentScope, out QualifiedName? alias,
			ErrorSink errors, Position position, bool mustResolve)
		{
			return (DRoutine)ResolveName(qualifiedName, DeclarationKind.Function, currentScope, out alias, errors, position, mustResolve);
		}

		public DType ResolveTypeName(QualifiedName qualifiedName, Scope currentScope, out QualifiedName? alias,
			ErrorSink errors, Position position, bool mustResolve)
		{
			return (DType)ResolveName(qualifiedName, DeclarationKind.Type, currentScope, out alias, errors, position, mustResolve);
		}

		public DConstant ResolveConstantName(QualifiedName qualifiedName, Scope currentScope, out QualifiedName? alias,
			ErrorSink errors, Position position, bool mustResolve)
		{
			return (DConstant)ResolveName(qualifiedName, DeclarationKind.Constant, currentScope, out alias, errors, position, mustResolve);
		}

		internal ClassConstant TryResolveClassConstantGlobally(GenericQualifiedName typeName, VariableName constantName)
		{
			if (typeName.GenericParams.Length != 0) return null;

			QualifiedName? alias;
			DType type = ResolveTypeName(typeName.QualifiedName, Scope.Global, out alias, null, Position.Invalid, false);

			ClassConstant constant;
			if (type != null && type.IsDefinite && type.GetConstant(constantName, null, out constant) == GetMemberResult.OK)
				return constant;
			else
				return null;
		}

		internal DConstant TryResolveGlobalConstantGlobally(QualifiedName qualifiedName)
		{
			QualifiedName? alias;
			return ResolveConstantName(qualifiedName, Scope.Global, out alias, null, Position.Invalid, false);
		}



		#endregion

		#region Emission

		internal void Emit(CodeGenerator/*!*/ codeGen)
		{
			if (!compilationUnit.IsTransient || ((TransientCompilationUnit)compilationUnit).EvalKind == EvalKinds.SyntheticEval)
				this.symbolDocumentWriter = compilationUnit.GetSymbolDocumentWriter(sourceFile.FullPath);

			// emit naming context:
			this.namingContextFieldBuilder = EmitInitNamingContext();

			// emit AST content:
			ast.Emit(codeGen);
		}

		private FieldBuilder EmitInitNamingContext()
		{
            /*
			if (importedNamespaces != null && importedNamespaces.Count > 0)
			{
				ILEmitter il = ((PhpAssemblyBuilderBase)compilationUnit.ModuleBuilder.AssemblyBuilder).GlobalTypeEmitter;

				// naming the field by a number makes it possible to store it efficiently in the d-type-spec:
				string field_name = il.GetNextUniqueIndex().ToString();

				FieldBuilder result = il.TypeBuilder.DefineField(field_name, typeof(NamingContext),
					FieldAttributes.Assembly | FieldAttributes.Static | FieldAttributes.InitOnly);

				// tmp = new string[<importedNamespaces.Count>] { ... };
				//LocalBuilder tmp = 
                il.EmitInitializedArray(Types.String[0], importedNamespaces.Count, (_il, i) =>
				{
					_il.Emit(OpCodes.Ldstr, importedNamespaces[i].ToString());
				});

				// instantiate NamingContext
				//il.Ldloc(tmp);
				il.Emit(OpCodes.Newobj, Constructors.NamingContext);
				il.Emit(OpCodes.Stsfld, result);

				return result;
			}
			else
            */
			{
				return null;
			}
		}

		#endregion

	}

	#endregion

	#region SourceFileUnit

	public sealed class SourceFileUnit : SourceUnit
	{
		public FileStream Stream { get { return stream; } }
		private FileStream stream;

		public SourceFileUnit(CompilationUnitBase/*!*/ compilationUnit, PhpSourceFile/*!*/ sourceFile, Encoding/*!*/ encoding)
			: base(compilationUnit, sourceFile, encoding)
		{
			Debug.Assert(!(compilationUnit is TransientCompilationUnit) && encoding != null && sourceFile != null);
		}

		public override string GetSourceCode(Position position)
		{
			int length = position.LastOffset - position.FirstOffset + 1;

			byte[] buf = new byte[length];
			stream.Seek(position.FirstOffset, SeekOrigin.Begin);
			int real_length = stream.Read(buf, 0, length);

			Debug.Assert(real_length == length);

			return Encoding.GetString(buf, 0, real_length);
		}
		
		/// <summary>
		/// Keeps stream open.
		/// </summary>
		/// <exception cref="InvalidSourceException">Source file cannot be opened for reading.</exception>
		public override void Parse(ErrorSink/*!*/ errors, IReductionsSink/*!*/ reductionsSink,
			Parsers.Position initialPosition, LanguageFeatures features)
		{
			Parser parser = new Parser();
			StreamReader source_reader;
			
			try
			{
				stream = new FileStream(sourceFile.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
				source_reader = new StreamReader(stream, encoding, true);

				// if the file contains a byte-order-mark, we must advance the offset in order to 
				// get correct token positions in lexer (unfortunately, StreamReader skips BOM without
				// a possibility to detect it from outside, so we have to do that manually):

				// TODO:

				// initialPosition.FirstOffset += FileSystemUtils.GetByteOrderMarkLength(stream);
			}
			catch (Exception e)
			{
				throw new InvalidSourceException(sourceFile.FullPath, e);
			}	

			ast = parser.Parse(this, source_reader, errors, reductionsSink, initialPosition,
				Lexer.LexicalStates.INITIAL, features);
		}

		public override void Close()
		{
			if (stream != null)
			{
				stream.Close();
				stream = null;
			}
		}
	}

	#endregion

	#region SourceCodeUnit

	public class SourceCodeUnit : SourceUnit
	{
		public string/*!*/ Code { get { return code; } }
		private string/*!*/ code;

		/// <summary>
		/// Line position in the immediately containing source code.
		/// </summary>
		public int Line { get { return line; } }
		private int line;

		/// <summary>
		/// Column position in the immediately containing source code.
		/// </summary>
		public int Column { get { return column; } }
		private int column;


		/// <summary>
		/// Initial state of the lexer
		/// </summary>
		protected Lexer.LexicalStates initialState;

		public SourceCodeUnit(CompilationUnitBase/*!*/ compilationUnit, string/*!*/ code, PhpSourceFile/*!*/ sourceFile,
			Encoding/*!*/ encoding, int line, int column)
			: base(compilationUnit, sourceFile, encoding)
		{
			this.code = code;
			this.line = line;
			this.column = column;

			this.initialState = Lexer.LexicalStates.ST_IN_SCRIPTING;
		}

		public override void Parse(ErrorSink/*!*/ errors, IReductionsSink/*!*/ reductionsSink, Position initialPosition,
			LanguageFeatures features)
		{
			Parser parser = new Parser();

			using (StringReader source_reader = new StringReader(code))
			{
				ast = parser.Parse(this, source_reader, errors, reductionsSink, initialPosition,
					initialState, features);
			}
		}

		public override string GetSourceCode(Position position)
		{
			int length = position.LastOffset - position.FirstOffset + 1;
			return code.Substring(position.FirstOffset, length);
		}

		public override void Close()
		{

		}
	}

	#endregion

	#region

	/// <summary>
	/// Represents a source code that is stored in a string, but contains
	/// a complete PHP script file including the initial marks
	/// </summary>
	public sealed class PhpScriptSourceUnit : SourceCodeUnit
	{
		public PhpScriptSourceUnit(CompilationUnitBase/*!*/ compilationUnit, string/*!*/ code, PhpSourceFile/*!*/ sourceFile,
			Encoding/*!*/ encoding, int line, int column)
			: base(compilationUnit, code, sourceFile, encoding, line, column)
		{
			this.initialState = Lexer.LexicalStates.INITIAL;
		}
	}

	#endregion

	#region VirtualSourceFileUnit

	public sealed class VirtualSourceFileUnit : SourceUnit
	{
		public string/*!*/ Code { get { return code; } }
		private string/*!*/ code;

		public bool AllowGlobalCode { get { return allowGlobalCode; } set { allowGlobalCode = value; } }
		private bool allowGlobalCode = true;

		public VirtualSourceFileUnit(CompilationUnitBase/*!*/ compilationUnit, string/*!*/ code,
			PhpSourceFile/*!*/ sourceFile, Encoding/*!*/ encoding)
			: base(compilationUnit, sourceFile, encoding)
		{
			this.code = code;
		}

		public override void Parse(ErrorSink/*!*/ errors, IReductionsSink/*!*/ reductionsSink, Position initialPosition,
			LanguageFeatures features)
		{
			Parser parser = new Parser();
			parser.AllowGlobalCode = this.allowGlobalCode;

			using (StringReader reader = new StringReader(code))
			{
				ast = parser.Parse(this, reader, errors, reductionsSink, initialPosition,
					Lexer.LexicalStates.INITIAL, features);
			}
		}

		public override string GetSourceCode(Position position)
		{
			int length = position.LastOffset - position.FirstOffset + 1;
			return code.Substring(position.FirstOffset, length);
		}

		public override void Close()
		{

		}
	}

	#endregion

	#region SourceCodeDescriptor

	/// <summary>
	/// Uniquely identifies a piece of compiled source code.
	/// </summary>
	[DebuggerNonUserCode]
	public struct SourceCodeDescriptor : IEquatable<SourceCodeDescriptor>
	{
		/// <summary>
		/// Relative path to the containing source file.
		/// </summary>
		public string/*!*/ ContainingSourcePath { get { return containingSourcePath; } }
		private readonly string/*!*/ containingSourcePath;

		/// <summary>
		/// Column where the code is positioned relatively to the containing code.
		/// </summary>
		public int ContainingTransientModuleId { get { return containerId; } }
		private readonly int containerId;

		/// <summary>
		/// Line where the code located relatively to the containing code.
		/// </summary>
		public int Line { get { return line; } }
		private readonly int line;

		/// <summary>
		/// Column where the code is positioned relatively to the containing code.
		/// </summary>
		public int Column { get { return column; } }
		private readonly int column;

		internal SourceCodeDescriptor(string/*!*/ containingSourcePath, int containerId, int line, int column)
		{
			this.containingSourcePath = containingSourcePath;
			this.containerId = containerId;
			this.line = line;
			this.column = column;
		}

		#region IEquatable<SourceCodeDescriptor> Members

		public bool Equals(SourceCodeDescriptor other)
		{
			return this.line == other.line && this.column == other.column && this.containerId == other.containerId &&
				 this.containingSourcePath == other.containingSourcePath;
		}

		#endregion

		public override int GetHashCode()
		{
			return unchecked(containingSourcePath.GetHashCode() ^ (containerId << 24) ^ (line << 16) ^ column);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is SourceCodeDescriptor)) return false;
			return Equals((SourceCodeDescriptor)obj);
		}
	}

	#endregion

}
