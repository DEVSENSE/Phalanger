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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

using System.Collections;
using System.Reflection;

using PHP.Core;
using PHP.Core.AST;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

using PHP.VisualStudio.PhalangerLanguageService.Scopes;
using PHP.VisualStudio.PhalangerLanguageService.Analyzer;
using PHP.VisualStudio.PhalangerLanguageService.Declarations;

namespace PHP.VisualStudio.PhalangerLanguageService.Parsing
{
    /// <summary>
    /// Select declarations specified by the part of AST.
    /// </summary>
    public class GetDeclarations : TreeVisitor
    {
        #region Initialization


        /// <summary>
        /// Project declarations object.
        /// </summary>
        private readonly ProjectDeclarations projectdeclarations;

        /// <summary>
        /// Local scope.
        /// </summary>
        private readonly ScopeInfo localscope;

        /// <summary>
        /// Create the declaration if not exists yet.
        /// </summary>
        private readonly bool CreateIfNotYet;

        /// <summary>
        /// The element which is just typed by the user.
        /// </summary>
        private readonly LangElement NotCompletedElement;

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="projectdeclarations"></param>
        /// <param name="localscope"></param>
        public GetDeclarations(ProjectDeclarations/*!*/projectdeclarations, ScopeInfo/*!*/localscope, bool CreateIfNotYet, LangElement NotCompletedElement)
        {
            this.projectdeclarations = projectdeclarations;
            this.localscope = localscope;
            this.CreateIfNotYet = CreateIfNotYet;
            this.NotCompletedElement = NotCompletedElement;
        }

        #endregion

        #region Start

        /// <summary>
        /// Selected declarations stack.
        /// </summary>
        private Stack<DeclarationList> stack = new Stack<DeclarationList>();

        /// <summary>
        /// Process the declaration chain and select declarations.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="elementCompleted"></param>
        /// <returns></returns>
        protected DeclarationList GetDeclarationsByName(LangElement element)
        {
            try
            {
                VisitElement(element);
            }
            catch(InterruptProcessing)
            {

            }
            catch(Exception)
            {

            }            

            return (stack.Count > 0) ? stack.Pop() : new DeclarationList();
        }

        /// <summary>
        /// Process the declaration chain and select declarations.
        /// </summary>
        /// <param name="projectdeclarations"></param>
        /// <param name="localscope"></param>
        /// <param name="CreateIfNotYet"></param>
        /// <param name="element"></param>
        /// <param name="elementCompleted"></param>
        /// <returns>List of declarations.</returns>
        public static DeclarationList GetDeclarationsByName(ProjectDeclarations/*!*/projectdeclarations, ScopeInfo/*!*/localscope, bool CreateIfNotYet, LangElement element, LangElement NotCompletedElement)
        {
            GetDeclarations getdeclarations = new GetDeclarations(projectdeclarations, localscope, CreateIfNotYet, NotCompletedElement);
            return getdeclarations.GetDeclarationsByName(element);
        }

        #endregion

        #region Selecting declaration

        /// <summary>
        /// Interrupt the tree processing and select the list on the top of the stack.
        /// </summary>
        class InterruptProcessing:Exception
        {

        }

        /// <summary>
        /// Put required declarations on the stack.
        /// </summary>
        /// <param name="match">Declaration match criteria.</param>
        /// <param name="newDeclName">In the case of creating new declaration, the name of it.</param>
        /// <param name="isMemberOf">Required declarations are members of this expression.</param>
        /// <param name="isMemberOfType">If required declarations are members of some expression, they are accessible throw this modifier.</param>
        /// <param name="decltypes">The mask of requested declarations type.</param>
        /// <param name="position">In the case of creating new declaration, the element position.</param>
        protected void ProcessElement(DeclarationMatches/*!*/match, string newDeclName, Expression isMemberOf, MemberType isMemberOfType, DeclarationInfo.DeclarationTypes decltypes, PHP.Core.Parsers.Position position)
        {
            ProcessElement(new QualifiedName(), match, newDeclName, isMemberOf, isMemberOfType, decltypes, position);
        }

        /// <summary>
        /// Put required declarations on the stack.
        /// </summary>
        /// <param name="qualifiedName">The declaration placing qualified name.</param>
        /// <param name="match">Declaration match criteria.</param>
        /// <param name="newDeclName">In the case of creating new declaration, the name of it.</param>
        /// <param name="isMemberOf">Required declarations are members of this expression.</param>
        /// <param name="isMemberOfType">If required declarations are members of some expression, they are accessible throw this modifier.</param>
        /// <param name="decltypes">The mask of requested declarations type.</param>
        /// <param name="position">In the case of creating new declaration, the element position.</param>
        protected void ProcessElement(QualifiedName qualifiedName, DeclarationMatches/*!*/match, string newDeclName, Expression isMemberOf, MemberType isMemberOfType, DeclarationInfo.DeclarationTypes decltypes, Position position)
        {
            // qualified name (the type where the desired declarations are placed)
            DeclarationList namespcdecls;
            if (qualifiedName.Name.Value != null && qualifiedName.Name.Value.Length > 0)
                namespcdecls = ScopeInfo.GetDeclarationsByName(qualifiedName, projectdeclarations, localscope);
            else
                namespcdecls = null;


            // select declarations
            if (isMemberOf == null)
            {
                // from local scope
                DeclarationList result;

                if (namespcdecls == null)
                {
                    result = new DeclarationList();
                    localscope.GetLocalDeclarations(result, decltypes, match, CreateIfNotYet ? null : projectdeclarations);

                    // create variable if doesn't exist
                    if (result.Count == 0 &&
                        CreateIfNotYet &&
                        newDeclName != null &&
                        (decltypes == 0 || (decltypes & DeclarationInfo.DeclarationTypes.Variable) != 0))   // not specified or variable type
                    {
                        // create variable declaration
                        DeclarationInfo newdecl = new VariableDeclaration(newDeclName, DeclarationInfo.DeclarationVisibilities.Public, position, null, null, localscope);
                        localscope.AddDeclaration(newdecl);
                        result.Add(newdecl);
                    }
                }
                else
                {
                    result = SelectMembers(namespcdecls, match, newDeclName, MemberType.StaticMember, decltypes, position);
                }

                

                stack.Push(result);
            }
            else
            {
                // from parent object
                VisitElement(isMemberOf); // process parent node

                DeclarationList parents = stack.Pop();
                DeclarationList members = SelectMembers(parents, match, newDeclName, isMemberOfType, decltypes, position);

                //
                stack.Push(members);
            }
        }

        /// <summary>
        /// Select desired members of the given parent declarations.
        /// </summary>
        /// <param name="parentdecls">Parent declaration(s).</param>
        /// <param name="match">The match.</param>
        /// <param name="newDeclName">n case of creating new variable, the name of the new variable.</param>
        /// <param name="isMemberOfType">If required declarations are members of some expression, they are accessible throw this modifier.</param>
        /// <param name="decltypes">The mask of requested declarations type.</param>
        /// <param name="position">In the case of creating new declaration, the element position.</param>
        /// <returns>Member declarations.</returns>
        protected DeclarationList   SelectMembers(DeclarationList parentdecls, DeclarationMatches/*!*/match, string newDeclName, MemberType isMemberOfType, DeclarationInfo.DeclarationTypes decltypes, Position position)
        {
            DeclarationList members = new DeclarationList();

            foreach (DeclarationInfo decl in parentdecls)
            {
                switch (isMemberOfType)
                {
                    case MemberType.ObjectMember:
                        decl.GetObjectMembers(projectdeclarations, members, match);
                        break;
                    case MemberType.StaticMember:
                        decl.GetStaticMembers(projectdeclarations, members, match);
                        break;
                    case MemberType.TypeMember:
                        decl.GetTypeMembers(projectdeclarations, members, match);
                        break;
                }

            }

            // create member if not yet
            if (members.Count == 0 && parentdecls.Count > 0 &&  // parents exists but no matched object members
                CreateIfNotYet &&   // allow to create is not exist
                newDeclName != null // new declaration name is known
                )
            {
                // member does not exist yet, create it
                DeclarationInfo lowestparent = parentdecls[parentdecls.Count - 1];
                CustomAnalyzer analyzer = new CustomAnalyzer();
                DeclarationInfo newdecl = null;
                
                if (decltypes == 0 || (decltypes& DeclarationInfo.DeclarationTypes.Variable) != 0)
                    newdecl = new VariableDeclaration(newDeclName, DeclarationInfo.DeclarationVisibilities.Public, position, null, null, localscope);
                
                if (newdecl != null)
                {
                    DeclarationInfo[] newdecls = new DeclarationInfo[] { newdecl };

                    switch (isMemberOfType)
                    {
                        case MemberType.ObjectMember:
                            analyzer.ObjectMembers = newdecls;
                            break;
                        case MemberType.StaticMember:
                            analyzer.StaticMembers = newdecls;
                            break;
                        default:
                            return members;
                    }

                    lowestparent.AddAnalyzer(analyzer);
                    members.Add(newdecl);
                }
            }

            // remove declarations with different type
            members.FilterType(decltypes);

            // done
            return members;
        }

        #endregion

        #region Catched AST nodes

        /// <summary>
        /// Member types
        /// </summary>
        public enum MemberType
        {
            ObjectMember,   // after ->
            StaticMember,   // after ::
            TypeMember      // after :::
        }

        /// <summary>
        /// ?->$a
        /// </summary>
        /// <param name="x"></param>
        public override void VisitDirectVarUse(DirectVarUse x)
        {
            if (x != NotCompletedElement)
            {
                ProcessElement(new DeclarationLabelEqual(x.VarName.Value), x.VarName.Value, x.IsMemberOf, MemberType.ObjectMember, DeclarationInfo.DeclarationTypes.Variable, x.Position);
            }
            else
            {
                ProcessElement(new DeclarationLabelContains(x.VarName.Value), null, x.IsMemberOf, MemberType.ObjectMember, DeclarationInfo.DeclarationTypes.Variable | DeclarationInfo.DeclarationTypes.Function, x.Position);

                // throw Selecting end - the list on the top of the stack is what you want
                throw new InterruptProcessing();
            }
        }

        /// <summary>
        /// ?->foo(...)
        /// </summary>
        /// <param name="x"></param>
        public override void VisitDirectFcnCall(DirectFcnCall x)
        {
            // TODO: is this right ? QualifiedName as identifier of the function call ?
            string fncName = x.QualifiedName.Name.Value;
            List<Name> qnames = new List<Name>();

            if (x.QualifiedName.Namespaces != null)
                foreach (Name n in x.QualifiedName.Namespaces)
                    qnames.Add(n);

            QualifiedName qname;
            if (qnames.Count > 0)
            {
                Name lastone = qnames[qnames.Count-1];
                qnames.Remove(lastone);
                qname = new QualifiedName(lastone, qnames.ToArray());
            }
            else
            {
                qname = new QualifiedName();
            }
            //
            

            if (x != NotCompletedElement)
            {
                ProcessElement(qname, new DeclarationLabelEqual(fncName), fncName, x.IsMemberOf, MemberType.ObjectMember, DeclarationInfo.DeclarationTypes.Function, x.Position);
            }
            else
            {
                ProcessElement(qname, new DeclarationLabelContains(fncName), null, x.IsMemberOf, MemberType.ObjectMember, DeclarationInfo.DeclarationTypes.Function, x.Position);

                // throw Selecting end - the list on the top of the stack is what you want
                throw new InterruptProcessing();
            }
        }

        /// <summary>
        /// A::B::$c
        /// </summary>
        /// <param name="x"></param>
        public override void VisitDirectStFldUse(DirectStFldUse x)
        {
            if (x != NotCompletedElement)
            {
                ProcessElement(x.TypeName.QualifiedName, new DeclarationLabelEqual(x.PropertyName.Value), x.PropertyName.Value, x.IsMemberOf, MemberType.StaticMember, DeclarationInfo.DeclarationTypes.Variable, x.Position);
            }
            else
            {
                ProcessElement(
                    x.TypeName.QualifiedName,
                    new DeclarationLabelContains(x.PropertyName.Value), null,
                    x.IsMemberOf, MemberType.StaticMember,
                    DeclarationInfo.DeclarationTypes.Variable,
                    x.Position);

                // throw Selecting end - the list on the top of the stack is what you want
                throw new InterruptProcessing();
            }
        }

        /// <summary>
        /// XXX::constant
        /// </summary>
        /// <param name="x"></param>
        public override void VisitClassConstUse(ClassConstUse x)
        {
            if (x != NotCompletedElement)
            {
                ProcessElement(x.ClassName.QualifiedName, new DeclarationLabelEqual(x.Name.Value), x.Name.Value, null, MemberType.StaticMember, DeclarationInfo.DeclarationTypes.Constant, x.Position);
            }
            else
            {
                ProcessElement(
                    x.ClassName.QualifiedName,
                    new DeclarationLabelContains(x.Name.Value), null,
                    null, MemberType.StaticMember,
                    DeclarationInfo.DeclarationTypes.Variable | DeclarationInfo.DeclarationTypes.Constant | DeclarationInfo.DeclarationTypes.Function,  // variable or constant or function should follow double colon
                    x.Position);

                // throw Selecting end - the list on the top of the stack is what you want
                throw new InterruptProcessing();
            }
        }

        /// <summary>
        /// XXX::staticFnc
        /// </summary>
        /// <param name="x"></param>
        public override void  VisitDirectStMtdCall(DirectStMtdCall x)
        {
            if (x != NotCompletedElement)
            {
                ProcessElement(x.ClassName.QualifiedName, new DeclarationLabelEqual(x.MethodName.Value), x.MethodName.Value, null, MemberType.StaticMember, DeclarationInfo.DeclarationTypes.Function, x.Position);
            }
            else
            {
                ProcessElement(
                    x.ClassName.QualifiedName,
                    new DeclarationLabelContains(x.MethodName.Value), null,
                    null, MemberType.StaticMember,
                    DeclarationInfo.DeclarationTypes.Function,
                    x.Position);

                // throw Selecting end - the list on the top of the stack is what you want
                throw new InterruptProcessing();
            }
        }

        /// <summary>
        /// A:::B:::C (or just A)
        /// </summary>
        /// <param name="x"></param>
        public override void VisitGlobalConstUse(GlobalConstUse x)
        {
            if (x != NotCompletedElement)
            {
                // get list of specified declarations
                stack.Push(ScopeInfo.GetDeclarationsByName(x.Name, projectdeclarations, localscope));
            }
            else
            {
                // the x.Name.Name is incomplete (user just typing it)
                
                if (x.Name.Namespaces == null || x.Name.Namespaces.Length == 0)
                {   // no namespace, the x.Name.Name is visible on the local scope
                    // just "?", push the result onto the stack
                    ProcessElement(new DeclarationLabelContains(x.Name.Name.Value), null, null, MemberType.ObjectMember, 0, x.Position);
                }
                else
                {
                    // the namespace part
                    QualifiedName name = new QualifiedName(new Name(), x.Name.Namespaces);

                    DeclarationList typedecls = ScopeInfo.GetDeclarationsByName(name, projectdeclarations, localscope);
                    typedecls.FilterType(DeclarationInfo.DeclarationTypes.Namespace);

                    // required list of namespace members
                    stack.Push(SelectMembers(
                        typedecls,
                        new DeclarationLabelContains(x.Name.Name.Value), null,
                        MemberType.TypeMember,
                        DeclarationInfo.DeclarationTypes.Class | DeclarationInfo.DeclarationTypes.Namespace,
                        x.Position));
                }
                
            }            
        }

        /// <summary>
        /// ?->a[]
        /// </summary>
        /// <param name="x"></param>
        public override void VisitItemUse(ItemUse x)
        {
            DirectVarUse directvaruse = x.Array as DirectVarUse;// TODO: other var use
            if (directvaruse != null)
            {   // array is direct var use
                ProcessElement(
                    new DeclarationLabelEqual(directvaruse.VarName.Value), directvaruse.VarName.Value,
                    x.IsMemberOf, MemberType.ObjectMember,
                    DeclarationInfo.DeclarationTypes.Variable, directvaruse.Position);
            }
            else
            {
                // array is of unknown type, put empty list onto the stack
                stack.Push(new DeclarationList());
            }
            

            // x.Array is just beeing typed by the user ?
            if (x.Array == NotCompletedElement || x == NotCompletedElement)
                throw new InterruptProcessing();

            // select array variable
            DeclarationList arraydecls = stack.Pop();

            DeclarationList result = new DeclarationList();
            foreach (DeclarationInfo decl in arraydecls)
            {
                decl.GetArrayDeclarations(projectdeclarations, result);
            }

            // create array if doesn't exist yet
            if (CreateIfNotYet && 
                result.Count == 0 && arraydecls.Count > 0)
            {
                // variableName member does not exist yet, create it
                DeclarationInfo lowestvar = arraydecls[arraydecls.Count - 1];

                DeclarationInfo newdecl = new VariableDeclaration(lowestvar.Label, DeclarationInfo.DeclarationVisibilities.Private, x.Position, null, null, localscope);

                CustomAnalyzer analyzer = new CustomAnalyzer();
                analyzer.ArrayDeclarations = new DeclarationInfo[] { newdecl };
                lowestvar.AddAnalyzer(analyzer);

                result.Add(newdecl);
            }

            // put the result onto the stack
            stack.Push(result);
        }

        /// <summary>
        /// ?->$$c
        /// </summary>
        /// <param name="x"></param>
        public override void VisitIndirectVarUse(IndirectVarUse x)
        {
            // select indirected variables
            VisitElement(x.VarNameEx);  // process indirect variable, put result on the stack
            DeclarationList indirectvars = stack.Pop();

            // get possible values of indirected variables
            List<string> names = new List<string>();

            foreach (DeclarationInfo decl in indirectvars)
            {
                decl.GetIndirectIdentifiers(projectdeclarations, localscope, names);
            }
            
            // process variables specified by their names
            ProcessElement(new DeclarationLabelEqualMultiple(names), null, x.IsMemberOf, MemberType.ObjectMember, DeclarationInfo.DeclarationTypes.Variable, x.Position);
        }

        /*/// <summary>
        /// ?->(not completed var name)
        /// </summary>
        /// <param name="x"></param>
        public virtual void VisitNotCompletedVarLikeConstructUse(NotCompletedVarLikeConstructUse x)
        {
            ProcessElement(new DeclarationLabelContains(x.notCompletedName), null, x.isMemberOf, x.isMemberOfType, 0, x.Position);

            // throw Selecting end - the list on the top of the stack is what you want
            throw new InterruptProcessing();
        }*/

        #endregion
    }

    /*/// <summary>
    /// AST node with not fully typed declaration name.
    /// </summary>
    public class MyNotCompletedVarLikeConstructUse:NotCompletedVarLikeConstructUse
    {
        /// <summary>
        /// Init element.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="notCompletedName"></param>
        /// <param name="isMemberOfType"></param>
        public MyNotCompletedVarLikeConstructUse(Position position, string notCompletedName, MemberType isMemberOfType)
            : base(position,notCompletedName,isMemberOfType)
        {
            
        }

        /// <summary>
        /// My tree visitor implementation
        /// </summary>
        /// <param name="visitor"></param>
        public override void VisitMe(TreeVisitor visitor)
        {
            GetDeclarations myvisitor = visitor as GetDeclarations;

            if (myvisitor != null)
            {
                myvisitor.VisitNotCompletedVarLikeConstructUse(this);
            }
            else
            {
                //Debug.Assert(myvisitor != null, "not supported tree visitor");
            }
        }
    }*/
}