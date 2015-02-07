/*

 Copyright (c) 2006- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region TypeRef

        abstract class TypeRefCompiler<T> : ITypeRefCompiler, INodeCompiler where T : TypeRef
        {
            public abstract DType ResolvedType { get; }

            /// <summary>
            /// Resolves generic arguments.
            /// </summary>
            /// <returns><B>true</B> iff all arguments are resolvable to types or constructed types (none is variable).</returns>
            internal virtual bool Analyze(T/*!*/node, Analyzer/*!*/ analyzer)
            {
                bool result = true;

                foreach (TypeRef arg in node.GenericParams)
                    result &= TypeRefHelper.Analyze(arg, analyzer);

                return result;
            }

            internal abstract void EmitLoadTypeDesc(T node, CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags);

            /// <summary>
            /// Emits code that loads type descriptors for all generic arguments and a call to 
            /// <see cref="Operators.MakeGenericTypeInstantiation"/>.
            /// </summary>
            internal void EmitMakeGenericInstantiation(TypeRef node, CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
            {
                if (node.GenericParams == null || node.GenericParams.Count == 0)
                    return;

                ILEmitter il = codeGenerator.IL;

                il.EmitOverloadedArgs(Types.DTypeDesc[0], node.GenericParams.Count, Methods.Operators.MakeGenericTypeInstantiation.ExplicitOverloads, delegate(ILEmitter eil, int i)
                {
                    TypeRefHelper.EmitLoadTypeDesc(node.GenericParams[i], codeGenerator, flags);
                });

                if (node.GenericParams.Count > 0)
                    il.Emit(OpCodes.Call, Methods.Operators.MakeGenericTypeInstantiation.Overload(node.GenericParams.Count));
            }

            #region ITypeRefCompiler

            DType ITypeRefCompiler.ResolvedType
            {
                get { return this.ResolvedType; }
            }

            bool ITypeRefCompiler.Analyze(TypeRef node, Analyzer analyzer)
            {
                return this.Analyze((T)node, analyzer);
            }

            void ITypeRefCompiler.EmitLoadTypeDesc(TypeRef node, CodeGenerator codeGenerator, ResolveTypeFlags flags)
            {
                this.EmitLoadTypeDesc((T)node, codeGenerator, flags);
            }

            #endregion
        }

        #endregion

        #region PrimitiveTypeRef

        [NodeCompiler(typeof(PrimitiveTypeRef))]
        sealed class PrimitiveTypeRefCompiler : TypeRefCompiler<PrimitiveTypeRef>
        {
            public override DType/*!*/ ResolvedType { get { return type; } }
            private PrimitiveType/*!A*/ type;
            public PrimitiveType/*!*/ Type { get { return type; } }

            internal override bool Analyze(PrimitiveTypeRef node, Analyzer analyzer)
            {
                Debug.Assert(node.QualifiedName.IsPrimitiveTypeName);

                type = PrimitiveType.GetByName(node.QualifiedName);

                if (type == null)
                    throw new InvalidOperationException();
                
                //
                return true;
            }

            internal override void EmitLoadTypeDesc(PrimitiveTypeRef node, CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
            {
                type.EmitLoadTypeDesc(codeGenerator, ResolveTypeFlags.SkipGenericNameParsing);
            }
        }

        #endregion

        #region DirectTypeRef

        [NodeCompiler(typeof(DirectTypeRef))]
        sealed class DirectTypeRefCompiler : TypeRefCompiler<DirectTypeRef>
        {
            public override DType ResolvedType { get { return resolvedType; } }
            private DType/*!A*/ resolvedType;

            #region Analysis

            internal override bool Analyze(DirectTypeRef node, Analyzer analyzer)
            {
                resolvedType = analyzer.ResolveTypeName(node.ClassName, analyzer.CurrentType, analyzer.CurrentRoutine, node.Position, false);

                // base call must follow the class name resolution:
                bool args_static = base.Analyze(node, analyzer);

                if (args_static)
                {
                    DTypeDesc[] resolved_arguments = DTypeDesc.EmptyArray;
                    var genericParams = node.GenericParams;

                    if (genericParams != null && genericParams.Count > 0)
                    {
                        resolved_arguments = new DTypeDesc[genericParams.Count];
                        for (int i = 0; i < genericParams.Count; i++)
                            resolved_arguments[i] = TypeRefHelper.ResolvedType(genericParams[i]).TypeDesc;
                    }

                    resolvedType = resolvedType.MakeConstructedType(analyzer, resolved_arguments, node.Position);
                }

                return args_static;
            }

            #endregion

            #region Emission

            internal override void EmitLoadTypeDesc(DirectTypeRef node, CodeGenerator codeGenerator, ResolveTypeFlags flags)
            {
                Debug.Assert(resolvedType != null);

                // disallow generic parameters on generic type which already has generic arguments:
                resolvedType.EmitLoadTypeDesc(codeGenerator, flags |
                          ((node.GenericParams.Count > 0) ? ResolveTypeFlags.SkipGenericNameParsing : 0));

                // constructed type already emited its generic parameters:
                if (!(resolvedType is ConstructedType))
                    EmitMakeGenericInstantiation(node, codeGenerator, flags);
            }

            #endregion
        }

        #endregion

        #region IndirectTypeRef

        [NodeCompiler(typeof(IndirectTypeRef))]
        sealed class IndirectTypeRefCompiler : TypeRefCompiler<IndirectTypeRef>
        {
            public override DType ResolvedType { get { return null; } }

            #region Analysis

            internal override bool Analyze(IndirectTypeRef node, Analyzer analyzer)
            {
                node.ClassNameVar.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

                // base call must follow the class name resolve:
                base.Analyze(node, analyzer);

                // indirect:
                return false;
            }

            #endregion

            #region Emission

            internal override void EmitLoadTypeDesc(IndirectTypeRef node, CodeGenerator codeGenerator, ResolveTypeFlags flags)
            {
                // disallow generic parameters on generic type which already has generic arguments:
                codeGenerator.EmitLoadTypeDescOperator(null, node.ClassNameVar, flags |
                    ((node.GenericParams.Count > 0) ? ResolveTypeFlags.SkipGenericNameParsing : 0));

                EmitMakeGenericInstantiation(node, codeGenerator, flags);
            }

            #endregion
        }

        #endregion
    }

    #region ITypeRefCompiler

    internal static class TypeRefHelper
    {
        public static DType ResolvedType(this TypeRef/*!*/node)
        {
            var nodecompiler = node.NodeCompiler<ITypeRefCompiler>();
            return nodecompiler.ResolvedType;
        }

        /// <summary>
        /// <see cref="ResolvedType"/> or new instance of <see cref="UnknownType"/> if the type was not resolved.
        /// </summary>
        public static DType/*!A*/ResolvedTypeOrUnknown(this TypeRef/*!*/node)
        {
            return ResolvedType(node) ?? new UnknownType(string.Empty, node);
        }

        public static bool Analyze(this TypeRef/*!*/node, Analyzer/*!*/ analyzer)
        {
            var nodecompiler = node.NodeCompiler<ITypeRefCompiler>();
            return nodecompiler.Analyze(node, analyzer);
        }

        public static void EmitLoadTypeDesc(this TypeRef/*!*/node, CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
        {
            var nodecompiler = node.NodeCompiler<ITypeRefCompiler>();
            nodecompiler.EmitLoadTypeDesc(node, codeGenerator, flags);
        }
    }

    internal interface ITypeRefCompiler
    {
        DType ResolvedType { get; }
        bool Analyze(TypeRef/*!*/node, Analyzer/*!*/ analyzer);
        void EmitLoadTypeDesc(TypeRef/*!*/node, CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags);
    }

    #endregion
}
