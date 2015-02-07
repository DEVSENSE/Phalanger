/*

 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Diagnostics;
using System.Reflection.Emit;
using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region StaticFieldUse

        [NodeCompiler(typeof(StaticFieldUse))]
        abstract class StaticFieldUseCompiler<T> : VariableUseCompiler<T> where T : StaticFieldUse
        {
            protected DType/*!*/ type;

            /// <summary>
            /// Points to a method that emits code to be placed after the new static field value has
            /// been loaded on the evaluation stack.
            /// </summary>
            internal AssignmentCallback assignmentCallback;

            public override Evaluation Analyze(T node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                TypeRefHelper.Analyze(node.TypeRef, analyzer);
                this.type = TypeRefHelper.ResolvedTypeOrUnknown(node.TypeRef);

                analyzer.AnalyzeConstructedType(type);

                return new Evaluation(node);
            }

            #region Emit, EmitAssign, EmitIsset, EmitRead, EmitWrite, EmitEnsure

            public override PhpTypeCode Emit(T/*!*/node, CodeGenerator/*!*/codeGenerator)
            {
                Statistics.AST.AddNode("FieldUse.Static");
                ChainBuilder chain = codeGenerator.ChainBuilder;
                PhpTypeCode result = PhpTypeCode.Invalid;

                switch (codeGenerator.SelectAccess(access))
                {
                    case AccessType.Read:
                        result = EmitRead(node, codeGenerator, false);
                        if (chain.IsMember) chain.Lengthen();
                        break;

                    case AccessType.ReadUnknown:
                        result = EmitRead(node, codeGenerator, true);
                        if (chain.IsMember) chain.Lengthen();
                        break;

                    case AccessType.ReadRef:
                        if (chain.IsMember)
                        {
                            chain.Lengthen();
                            result = EmitRead(node, codeGenerator, false);
                        }
                        else
                        {
                            result = EmitRead(node, codeGenerator, true);
                        }
                        break;

                    case AccessType.Write:
                        if (chain.IsMember)
                        {
                            result = EmitEnsure(node, codeGenerator, chain);
                            chain.Lengthen();
                        }
                        else
                        {
                            assignmentCallback = EmitWrite(node, codeGenerator, false);
                            result = PhpTypeCode.Unknown;
                        }
                        break;

                    case AccessType.WriteRef:
                        if (chain.IsMember)
                        {
                            result = EmitEnsure(node, codeGenerator, chain);
                            chain.Lengthen();
                        }
                        else
                        {
                            assignmentCallback = EmitWrite(node, codeGenerator, true);
                            result = PhpTypeCode.Unknown;
                        }
                        break;

                    case AccessType.None:
                        result = PhpTypeCode.Void;
                        break;
                }

                return result;
            }

            internal override PhpTypeCode EmitAssign(T/*!*/node, CodeGenerator codeGenerator)
            {
                switch (access)
                {
                    case AccessType.Write:
                    case AccessType.WriteRef:
                    case AccessType.WriteAndReadRef:
                    case AccessType.WriteAndReadUnknown:
                    case AccessType.ReadAndWrite:
                    case AccessType.ReadAndWriteAndReadRef:
                    case AccessType.ReadAndWriteAndReadUnknown:
                        // finish the assignment by invoking the callback obtained in Emit
                        assignmentCallback(codeGenerator, PhpTypeCode.Object);
                        break;

                    default:
                        Debug.Fail(null);
                        break;
                }

                return PhpTypeCode.Void;
            }

            internal override PhpTypeCode EmitIsset(T/*!*/node, CodeGenerator codeGenerator, bool empty)
            {
                Debug.Assert(access == AccessType.Read);

                // Do not report error messages
                codeGenerator.ChainBuilder.QuietRead = true;

                // Emit as if the node is read
                return this.Emit(node, codeGenerator);
            }

            internal abstract PhpTypeCode EmitRead(T/*!*/node, CodeGenerator/*!*/ codeGenerator, bool wantRef);
            internal abstract AssignmentCallback EmitWrite(T/*!*/node, CodeGenerator/*!*/ codeGenerator, bool writeRef);
            internal abstract PhpTypeCode EmitEnsure(T/*!*/node, CodeGenerator/*!*/ codeGenerator, ChainBuilder/*!*/ chain);

            #endregion

        }

        #endregion

        #region DirectStFldUse

        [NodeCompiler(typeof(DirectStFldUse))]
        sealed class DirectStFldUseCompiler : StaticFieldUseCompiler<DirectStFldUse>
        {
            private DProperty property;
            private bool runtimeVisibilityCheck;

            public override Evaluation Analyze(DirectStFldUse node, Analyzer analyzer, ExInfoFromParent info)
            {
                base.Analyze(node, analyzer, info);

<<<<<<< HEAD
                property = analyzer.ResolveProperty(type, node.PropertyName, node.Position, true, analyzer.CurrentType, analyzer.CurrentRoutine, out runtimeVisibilityCheck);
=======
                property = analyzer.ResolveProperty(type, node.PropertyName, node.Span, true, analyzer.CurrentType, analyzer.CurrentRoutine, out runtimeVisibilityCheck);
>>>>>>> refs/remotes/tfs/default

                return new Evaluation(node);
            }

            #region EmitRead, EmitWrite, EmitEnsure, EmitUnset
            
            /// <summary>
            /// Emits IL instructions that read the value of a static field.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
            /// <param name="wantRef">If <B>false</B> the field value should be left on the evaluation stack,
            /// if <B>true</B> the <see cref="PhpReference"/> should be left on the evaluation stack.</param>
            /// <remarks>
            /// Nothing is expected on the evaluation stack. A <see cref="PhpReference"/> (if <paramref name="wantRef"/>
            /// is <B>true</B>) or the field value itself (if <paramref name="wantRef"/> is <B>false</B>) is left on the
            /// evaluation stack (all PHP static fields are <see cref="PhpReference"/>s).
            /// </remarks>
            internal override PhpTypeCode EmitRead(DirectStFldUse/*!*/node, CodeGenerator/*!*/ codeGenerator, bool wantRef)
            {
                return property.EmitGet(codeGenerator, null, wantRef, type as ConstructedType, runtimeVisibilityCheck);
            }

            /// <summary>
            /// Emits IL instructions that write a value to a static field.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
            /// <param name="writeRef">If <B>true</B> the value being written is a <see cref="PhpReference"/>
            /// instance, if <B>false</B> it is an <see cref="Object"/> instance.</param>
            /// <returns>Delegate to a method that emits code to be executed when the actual value has been
            /// loaded on the evaluation stack (see <see cref="StaticFieldUseCompiler{T}.EmitAssign"/>).</returns>
            internal override AssignmentCallback EmitWrite(DirectStFldUse/*!*/node, CodeGenerator/*!*/ codeGenerator, bool writeRef)
            {
                return property.EmitSet(codeGenerator, null, writeRef, type as ConstructedType, runtimeVisibilityCheck);
            }

            internal override PhpTypeCode EmitEnsure(DirectStFldUse/*!*/node, CodeGenerator/*!*/ codeGenerator, ChainBuilder/*!*/ chain)
            {
                // unknown property of a known type reported as an error during analysis
                Debug.Assert(!property.IsUnknown ||
                    property.DeclaringType.IsUnknown ||
                    !property.DeclaringType.IsDefinite);

                // we're only interested in a directly accessible property
                return chain.EmitEnsureStaticProperty((runtimeVisibilityCheck) ? null : property, node.TypeRef, node.PropertyName, chain.IsArrayItem);
            }

            /// <summary>
            /// Emits IL instructions that &quot;unset&quot; a static field.
            /// </summary>
            /// <remarks>
            /// <para>
            /// Nothing is expected on the evaluation stack. Nothing is left on the evaluation stack.
            /// </para>
            /// <para>
            /// An error throwing code is always emitted because static fields cannot be unset.
            /// </para>
            /// </remarks>
            internal override void EmitUnset(DirectStFldUse/*!*/node, CodeGenerator/*!*/ codeGenerator)
            {
                property.EmitUnset(codeGenerator, null, type as ConstructedType, runtimeVisibilityCheck);
            }

            #endregion
        }

        #endregion

        #region IndirectStFldUse

        [NodeCompiler(typeof(IndirectStFldUse))]
        sealed class IndirectStFldUseCompiler : StaticFieldUseCompiler<IndirectStFldUse>
        {
            public override Evaluation Analyze(IndirectStFldUse/*!*/node, Analyzer analyzer, ExInfoFromParent info)
            {
                base.Analyze(node, analyzer, info);

                node.FieldNameExpr = node.FieldNameExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

                return new Evaluation(node);
            }

            #region EmitRead, EmitWrite, EmitEnsure, EmitUnset

            /// <summary>
            /// Emits IL instructions that read the value of a static field.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
            /// <param name="wantRef">If <B>false</B> the field value should be left on the evaluation stack,
            /// if <B>true</B> the <see cref="PhpReference"/> should be left on the evaluation stack.</param>
            /// <remarks>
            /// Nothing is expected on the evaluation stack. A <see cref="PhpReference"/> (if <paramref name="wantRef"/>
            /// is <B>true</B>) or the field value itself (if <paramref name="wantRef"/> is <B>false</B>) is left on the
            /// evaluation stack (all PHP static fields are <see cref="PhpReference"/>s).
            /// </remarks>
            internal override PhpTypeCode EmitRead(IndirectStFldUse/*!*/node, CodeGenerator codeGenerator, bool wantRef)
            {
                return codeGenerator.EmitGetStaticPropertyOperator(type, null, node.FieldNameExpr, wantRef);
            }

            /// <summary>
            /// Emits IL instructions that write the value to a static field.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">The current <see cref="CodeGenerator"/>.</param>
            /// <param name="writeRef">If <B>true</B> the value being written is a <see cref="PhpReference"/>
            /// instance, if <B>false</B> it is an <see cref="Object"/> instance.</param>
            /// <returns>Delegate to a method that emits code to be executed when the actual value has been
            /// loaded on the evaluation stack (see <see cref="StaticFieldUseCompiler{T}.EmitAssign"/>).</returns>
            internal override AssignmentCallback EmitWrite(IndirectStFldUse/*!*/node, CodeGenerator codeGenerator, bool writeRef)
            {
                return codeGenerator.EmitSetStaticPropertyOperator(type, null, node.FieldNameExpr, writeRef);

                // obsolete:
                //codeGenerator.IL.Emit(OpCodes.Ldstr, className.QualifiedName.ToString());
                //codeGenerator.EmitBoxing(fieldNameExpr.Emit(codeGenerator));

                //return delegate(CodeGenerator codeGen)
                //{
                //  codeGen.EmitLoadClassContext();
                //  codeGen.EmitLoadScriptContext();
                //  codeGen.EmitLoadNamingContext();

                //  // invoke the operator
                //  codeGen.IL.EmitCall(OpCodes.Call, Methods.Operators.SetStaticProperty, null);
                //};
            }

            internal override PhpTypeCode EmitEnsure(IndirectStFldUse/*!*/node, CodeGenerator/*!*/ codeGenerator, ChainBuilder chain)
            {
                return chain.EmitEnsureStaticProperty(node.TypeRef, null, node.FieldNameExpr, chain.IsArrayItem);
            }

            /// <summary>
            /// Emits IL instructions that &quot;unset&quot; a static field.
            /// </summary>
            /// <remarks>
            /// <para>
            /// Nothing is expected on the evaluation stack. Nothing is left on the evaluation stack.
            /// </para>
            /// <para>
            /// Call to the <see cref="Operators.UnsetStaticProperty"/> error throwing operator is always emitted because static
            /// fields cannot be unset.
            /// </para>
            /// </remarks>
            internal override void EmitUnset(IndirectStFldUse/*!*/node, CodeGenerator codeGenerator)
            {
                codeGenerator.EmitUnsetStaticPropertyOperator(type, null, node.FieldNameExpr);
            }

            #endregion
        }

        #endregion
    }
}
