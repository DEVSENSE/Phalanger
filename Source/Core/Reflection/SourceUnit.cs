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
using System.Reflection.Emit;
using System.Reflection;
using System.Diagnostics.SymbolStore;
using System.Diagnostics;

using PHP.Core.AST;
using PHP.Core.Parsers;
using PHP.Core.Emit;
using PHP.Core.Compiler.AST;
using PHP.Core.Text;

namespace PHP.Core.Reflection
{
    #region CompilationSourceUnit

    public abstract class CompilationSourceUnit : SourceUnit
	{
        public override bool IsPure { get { return this.CompilationUnit.IsPure; } }

        public override bool IsTransient { get { return this.CompilationUnit.IsTransient; } }

		/// <summary>
		/// Containing compilation unit.
		/// </summary>
		public CompilationUnitBase/*!*/ CompilationUnit { get { return compilationUnit; } }
		protected readonly CompilationUnitBase/*!*/ compilationUnit;

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

		#region Construction

        public CompilationSourceUnit(CompilationUnitBase/*!*/ compilationUnit, PhpSourceFile/*!*/ sourceFile, Encoding/*!*/ encoding, ILineBreaks/*!*/lineBreaks)
            : base(sourceFile, encoding, lineBreaks)
		{
			Debug.Assert(compilationUnit != null);

			this.compilationUnit = compilationUnit;
			this.namingContextFieldBuilder = null;   // to be filled during compilation just before the unit gets emitted
			this.symbolDocumentWriter = null; // to be filled during compilation just before the unit gets emitted
		}

		#endregion

		public ISymbolDocumentWriter GetMappedSymbolDocumentWriter(int realLine)
		{
			return compilationUnit.GetSymbolDocumentWriter(GetMappedFullSourcePath(realLine));
		}

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
            out QualifiedName? alias, ErrorSink errors, Text.Span position, bool mustResolve)
		{
			string full_name = null;
			DMember result;
			alias = null;

			// try exact match:
			result = ResolveExactName(qualifiedName, ref full_name, kind, currentScope, mustResolve);
			if (result != null)
				return result;

            // try imported namespaces:
            if (!qualifiedName.IsFullyQualifiedName && HasImportedNamespaces)
			{
				result = null;

				foreach (QualifiedName imported_ns in this.ImportedNamespaces)
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
					Debug.Fail(null);
					throw null;
			}
		}

		public DRoutine ResolveFunctionName(QualifiedName qualifiedName, Scope currentScope, out QualifiedName? alias,
            ErrorSink errors, Text.Span position, bool mustResolve)
		{
			return (DRoutine)ResolveName(qualifiedName, DeclarationKind.Function, currentScope, out alias, errors, position, mustResolve);
		}

		public DType ResolveTypeName(QualifiedName qualifiedName, Scope currentScope, out QualifiedName? alias,
			ErrorSink errors, Text.Span position, bool mustResolve)
		{
			return (DType)ResolveName(qualifiedName, DeclarationKind.Type, currentScope, out alias, errors, position, mustResolve);
		}

		public DConstant ResolveConstantName(QualifiedName qualifiedName, Scope currentScope, out QualifiedName? alias,
            ErrorSink errors, Text.Span position, bool mustResolve)
		{
			return (DConstant)ResolveName(qualifiedName, DeclarationKind.Constant, currentScope, out alias, errors, position, mustResolve);
		}

		internal ClassConstant TryResolveClassConstantGlobally(GenericQualifiedName typeName, VariableName constantName)
		{
			if (typeName.IsGeneric) return null;

			QualifiedName? alias;
            DType type = ResolveTypeName(typeName.QualifiedName, Scope.Global, out alias, null, Text.Span.Invalid, false);

			ClassConstant constant;
			if (type != null && type.IsDefinite && type.GetConstant(constantName, null, out constant) == GetMemberResult.OK)
				return constant;
			else
				return null;
		}

		internal DConstant TryResolveGlobalConstantGlobally(QualifiedName qualifiedName)
		{
			QualifiedName? alias;
            return ResolveConstantName(qualifiedName, Scope.Global, out alias, null, Text.Span.Invalid, false);
		}



		#endregion

		#region Emission

		internal void Emit(CodeGenerator/*!*/codeGenerator)
		{
			if (!compilationUnit.IsTransient || ((TransientCompilationUnit)compilationUnit).EvalKind == EvalKinds.SyntheticEval)
				this.symbolDocumentWriter = compilationUnit.GetSymbolDocumentWriter(sourceFile.FullPath);

			// emit naming context:
			this.namingContextFieldBuilder = EmitInitNamingContext();

			// emit AST content:
            ast.Emit(codeGenerator);
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

	public sealed class SourceFileUnit : CompilationSourceUnit, IScannerHandler
	{
		public FileStream Stream { get { return stream; } }
		private FileStream stream;

        /// <summary>
        /// Gets inner line breaks as ExpandableLineBreaks.
        /// </summary>
        private ExpandableLineBreaks ExpandableLineBreaks { get { return (ExpandableLineBreaks)innerLineBreaks; } }

        public SourceFileUnit(CompilationUnitBase/*!*/ compilationUnit, PhpSourceFile/*!*/ sourceFile, Encoding/*!*/ encoding)
			: base(compilationUnit, sourceFile, encoding, new ExpandableLineBreaks())
		{
			Debug.Assert(!(compilationUnit is TransientCompilationUnit) && encoding != null && sourceFile != null);
		}

        /// <summary>
		/// Keeps stream open.
		/// </summary>
		/// <exception cref="InvalidSourceException">Source file cannot be opened for reading.</exception>
        public override void Parse(ErrorSink errors, IReductionsSink reductionsSink, LanguageFeatures features)
		{
			Parser parser = new Parser();
			StreamReader source_reader;

            try
            {
                stream = new FileStream(sourceFile.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                source_reader = new StreamReader(stream, encoding, true);
            }
            catch (Exception e)
            {
                throw new InvalidSourceException(sourceFile.FullPath, e);
            }

			ast = parser.Parse(this, source_reader, errors, reductionsSink, Lexer.LexicalStates.INITIAL, features);
		}

        public override void Close()
		{
			if (stream != null)
			{
				stream.Close();
				stream = null;
			}

            if (innerLineBreaks.GetType() == typeof(ExpandableLineBreaks))
                innerLineBreaks = this.ExpandableLineBreaks.Finalize();
		}

        public override string GetSourceCode(Text.Span span)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var source_reader = new StreamReader(stream, encoding, true);
            var buffer = new char[1024];
            
            // seek to span.Start
            for (int toread = span.Start; toread > 0; toread -= buffer.Length)
                source_reader.Read(buffer, 0, Math.Min(toread, buffer.Length));

            // read desired span
            buffer = new char[span.Length];
            source_reader.Read(buffer, 0, buffer.Length);

            return new string(buffer);

            //byte[] buf = new byte[length];
            //stream.Seek(position.FirstOffset, SeekOrigin.Begin);
            //int real_length = stream.Read(buf, 0, length);

            //Debug.Assert(real_length == length);

            //return Encoding.GetString(buf, 0, real_length);
        }

        #region IScannerHandler Members

        void IScannerHandler.OnNextToken(Tokens token, char[] buffer, int tokenStart, int tokenLength)
        {
            // update internal ILineBreaks
            Debug.Assert(innerLineBreaks is ExpandableLineBreaks);
            this.ExpandableLineBreaks.Expand(buffer, tokenStart, tokenLength);
        }

        #endregion
    }

	#endregion

	#region SourceCodeUnit

    /// <summary>
    /// Source unit from string representation of code.
    /// The code is expected to not contain opening and closing script tags.
    /// </summary>
	public class SourceCodeUnit : CompilationSourceUnit
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
			: base(compilationUnit, sourceFile, encoding, Text.LineBreaks.Create(code))
		{
			this.code = code;
			this.line = line;
			this.column = column;

            // opening and closing script tags are not present
			this.initialState = Lexer.LexicalStates.ST_IN_SCRIPTING;
		}

        public override void Parse(ErrorSink/*!*/ errors, IReductionsSink/*!*/ reductionsSink, LanguageFeatures features)
		{
            Parser parser = new Parser();

			using (StringReader source_reader = new StringReader(code))
			{
                ast = parser.Parse(this, source_reader, errors, reductionsSink, initialState, features);
			}
		}

        public override string GetSourceCode(Text.Span span)
		{
            return span.GetText(code);
		}

		public override void Close()
		{

		}

        #region ILineBreaks Members

        public override int GetLineFromPosition(int position)
        {
            // shift the position
            return base.GetLineFromPosition(position) + this.Line;
        }

        public override void GetLineColumnFromPosition(int position, out int line, out int column)
        {
            // shift the position
            base.GetLineColumnFromPosition(position, out line, out column);
            if (line == 0)
                column += this.Column;
            line += this.Line;
        }

        #endregion
    }

	#endregion

    #region PhpScriptSourceUnit

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

	public sealed class VirtualSourceFileUnit : CompilationSourceUnit
	{
		public string/*!*/ Code { get { return code; } }
		private string/*!*/ code;

		public bool AllowGlobalCode { get { return allowGlobalCode; } set { allowGlobalCode = value; } }
		private bool allowGlobalCode = true;

		public VirtualSourceFileUnit(CompilationUnitBase/*!*/ compilationUnit, string/*!*/ code,
			PhpSourceFile/*!*/ sourceFile, Encoding/*!*/ encoding)
            : base(compilationUnit, sourceFile, encoding, Text.LineBreaks.Create(code))
		{
			this.code = code;
		}

        public override void Parse(ErrorSink/*!*/ errors, IReductionsSink/*!*/ reductionsSink, LanguageFeatures features)
		{
			Parser parser = new Parser();
			parser.AllowGlobalCode = this.allowGlobalCode;

			using (StringReader reader = new StringReader(code))
			{
				ast = parser.Parse(this, reader, errors, reductionsSink,
					Lexer.LexicalStates.INITIAL, features);
			}
		}

        public override string GetSourceCode(Text.Span span)
		{
            return span.GetText(code);
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
