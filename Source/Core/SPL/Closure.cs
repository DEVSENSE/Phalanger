/*

 Copyright (c) 2011 Jakub Misek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/


using System;
using System.Collections;
using System.Collections.Generic;
using PHP.Core;
using PHP.Core.Reflection;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

namespace PHP.Library.SPL
{
    /// <summary>
    /// Prototype class for PHP closure.
    /// </summary>
    [ImplementsType]
    public sealed class Closure : PhpObject
    {
        private readonly RoutineDelegate/*!*/lambda;
        private readonly PhpArray parameter;
        private readonly PhpArray @static;

        /// <summary>
        /// Constructor of PHP closure.
        /// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/></param>
        /// <param name="lambda">Delegate to lambda function itself.</param>
        /// <param name="parameter"><see cref="PhpArray"/> of closure <c>parameter</c> field. Can be <c>null</c> if there are no parameters.</param>
        /// <param name="static"><see cref="PhpArray"/> of closure <c>parameter</c> field. Can be <c>null</c> if there is no <c>use</c> of scope variables.</param>
        public Closure(ScriptContext/*!*/context, RoutineDelegate/*!*/lambda, PhpArray parameter, PhpArray @static)
            :this(context, true)
        {
            Debug.Assert(context != null);
            Debug.Assert(lambda != null);

            this.lambda = lambda;
            this.parameter = parameter;
            this.@static = @static;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((Closure)instance).__construct(stack.Context);
        }

        [ImplementsMethod]
        public object __construct(ScriptContext context)
        {
            PhpException.Throw(PhpError.Error, string.Format(CoreResources.instantiation_not_allowed, "Closure"));
            return null;
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        private Closure(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only. Invokes constructor dynamically.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        private Closure(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __invoke(object/*!*/instance, PhpStack/*!*/stack)
        {
            var closure = (Closure)instance;

            stack.ExpandFrame(closure.@static);

            return closure.lambda(instance, stack);
        }

        [ImplementsMethod]
        [NeedsArgless]
        public object __invoke(ScriptContext context)
        {
            // this method should not be called, its argless should.
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Alters special behaviour of var_dump, export and print_r.
        /// </summary>
        protected override IEnumerable<KeyValuePair<VariableName, DObject.AttributedValue>> PropertyIterator()
        {
            if (this.@static != null)
                yield return new KeyValuePair<VariableName, DObject.AttributedValue>(new VariableName("static"), new AttributedValue(this.@static));

            if (this.parameter != null)
                yield return new KeyValuePair<VariableName, DObject.AttributedValue>(new VariableName("parameter"), new AttributedValue(this.parameter));
        }
    }
}
