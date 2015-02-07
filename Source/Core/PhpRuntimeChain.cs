/*

 Copyright (c) 2004-2006 Ladislav Prosek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Diagnostics;
using PHP.Core.Reflection;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	#region RuntimeChainElement

	/// <summary>
	/// An abstract element of <see cref="PhpRuntimeChain"/>.
	/// </summary>
	[Serializable]
	public abstract class RuntimeChainElement
	{
		/// <summary>
		/// Applies the element on a given variable (<B>Read</B> semantics).
		/// </summary>
		/// <param name="var">The variable.</param>
		/// <param name="context">Script context.</param>
		/// <param name="caller">Class context.</param>
		/// <returns>The resulting value.</returns>
		public abstract object Get(object var, ScriptContext context, DTypeDesc caller);

		/// <summary>
		/// Applies the element on a given variable (<B>ReadRef</B> semantics).
		/// </summary>
		/// <param name="var">The variable.</param>
		/// <param name="context">Script context.</param>
		/// <param name="caller">Class context.</param>
		/// <returns>The resulting value.</returns>
		public abstract PhpReference GetRef(ref object var, ScriptContext context, DTypeDesc caller);

		/// <summary>
		/// Performs the ensure operation on an variable to make it suitable for current element application.
		/// </summary>
		/// <param name="var">The variable.</param>
		/// <param name="context">Script context.</param>
		/// <param name="caller">Class context.</param>
		/// <returns>The new variable value.</returns>
		public abstract object EnsureVariable(ref object var, ScriptContext context, DTypeDesc caller);

		/// <summary>
        /// Performs the ensure operation on this element to make it suitable for <see cref="Next"/> element
		/// application.
		/// </summary>
		/// <param name="var">The variable.</param>
		/// <param name="context">Script context.</param>
		/// <param name="caller">Class context.</param>
		/// <returns>The new element value.</returns>
		public abstract object Ensure(object var, ScriptContext context, DTypeDesc caller);

		/// <summary>
		/// Applies the element on a given variable (<B>ReadRef</B> semantics) that has already been ensured to have
		/// the suitable type.
		/// </summary>
		/// <param name="var">The variable.</param>
		/// <param name="context">Script context.</param>
		/// <param name="caller">Class context.</param>
		/// <returns>The resulting value.</returns>
		public abstract PhpReference GetEnsuredRef(object var, ScriptContext context, DTypeDesc caller);

		/// <summary>
		/// Returns the name of the chain element.
		/// </summary>
		public abstract object Name
		{
			get;
		}

		/// <summary>
		/// Next <see cref="RuntimeChainElement"/> in the chain.
		/// </summary>
		public RuntimeChainElement Next;
	}

	#endregion

	#region RuntimeChainProperty

	/// <summary>
	/// Represents an object property access (<B>-&gt;</B>).
	/// </summary>
	[Serializable]
	public class RuntimeChainProperty : RuntimeChainElement
	{
		/// <summary>
		/// Creates a new <see cref="RuntimeChainProperty"/> with a given name.
		/// </summary>
		/// <param name="name">The field name.</param>
		public RuntimeChainProperty(string name)
		{
			this.name = name;
		}

		/// <summary>
		/// Applies the field access on a given variable (<B>Read</B> semantics).
		/// </summary>
		public override object Get(object var, ScriptContext context, DTypeDesc caller)
		{
			return Operators.GetProperty(var, name, caller, false);
		}

		/// <summary>
		/// Applies the field access on a given variable (<B>ReadRef</B> semantics).
		/// </summary>
		public override PhpReference GetRef(ref object var, ScriptContext context, DTypeDesc caller)
		{
			return Operators.GetPropertyRef(ref var, name, caller, context);
		}

		/// <summary>
		/// Performs the ensure operation on an variable to make it a <see cref="DObject"/>.
		/// </summary>
		public override object EnsureVariable(ref object var, ScriptContext context, DTypeDesc caller)
		{
			return Operators.EnsureVariableIsObject(ref var, context);
		}

		/// <summary>
        /// Performs the ensure operation on this field to make it suitable for <see cref="RuntimeChainElement.Next"/> element
		/// application.
		/// </summary>
		public override object Ensure(object var, ScriptContext context, DTypeDesc caller)
		{
			Debug.Assert(Next is RuntimeChainProperty || Next is RuntimeChainItem || Next is RuntimeChainNewItem);

			if (Next is RuntimeChainProperty) return Operators.EnsurePropertyIsObject((DObject)var, name, caller, context);
			else return Operators.EnsurePropertyIsArray((DObject)var, name, caller);
		}

		/// <summary>
		/// Applies the field access on a given variable (<B>ReadRef</B> semantics) that has already been ensured to
		/// be <see cref="DObject"/>.
		/// </summary>
		public override PhpReference GetEnsuredRef(object var, ScriptContext context, DTypeDesc caller)
		{
			return Operators.GetObjectPropertyRef((DObject)var, name, caller);
		}

		/// <summary>The name of the property.</summary>
		private string name;

		/// <summary>Returns the name of the property.</summary>
		public override object Name
		{
			get
			{ return name; }
		}
	}

	#endregion

	#region RuntimeChainItem

	/// <summary>
	/// Represents an array item access (<B>[x]</B>).
	/// </summary>
	[Serializable]
	public class RuntimeChainItem : RuntimeChainElement
	{
		/// <summary>
		/// Returns the name of the item (index).
		/// </summary>
		public override object Name 
		{ 
			get { return key.Object; } 
		}
		
		/// <summary>
		/// The key.
		/// </summary>
		private IntStringKey key;

		/// <summary>
		/// Creates a new <see cref="RuntimeChainItem"/> with a given key.
		/// </summary>
		public RuntimeChainItem(IntStringKey key)
		{
			this.key = key;
		}

		/// <summary>
		/// Applies the item access on a given variable (<B>Read</B> semantics).
		/// </summary>
		public override object Get(object var, ScriptContext context, DTypeDesc caller)
		{
			if (key.IsString)
				return Operators.GetItem(var, key.String, Operators.GetItemKinds.Get);
			else
				return Operators.GetItem(var, key.Integer, Operators.GetItemKinds.Get);
		}

		/// <summary>
		/// Applies the item access on a given variable (<B>ReadRef</B> semantics).
		/// </summary>
		public override PhpReference GetRef(ref object var, ScriptContext context, DTypeDesc caller)
		{
			return Operators.GetItemRef(Name, ref var);
		}

		/// <summary>
		/// Performs the ensure operation on an variable to make it a <see cref="PhpArray"/>.
		/// </summary>
		public override object EnsureVariable(ref object var, ScriptContext context, DTypeDesc caller)
		{
			return Operators.EnsureVariableIsArray(ref var);
		}

		/// <summary>
        /// Performs the ensure operation on this item to make it suitable for <see cref="RuntimeChainElement.Next"/> element
		/// application.
		/// </summary>
		public override object Ensure(object var, ScriptContext context, DTypeDesc caller)
		{
			Debug.Assert(Next is RuntimeChainProperty || Next is RuntimeChainItem || Next is RuntimeChainNewItem);

			if (Next is RuntimeChainProperty) 
				return ((PhpArray)var).EnsureItemIsObject(Name, context);
			else 
				return ((PhpArray)var).EnsureItemIsArray(Name);
		}

		/// <summary>
		/// Applies the item access on a given variable (<B>ReadRef</B> semantics) that has already been ensured to
		/// be <see cref="PhpArray"/>.
		/// </summary>
		public override PhpReference GetEnsuredRef(object var, ScriptContext context, DTypeDesc caller)
		{
			return ((PhpArray)var).GetArrayItemRef(Name);
		}
	}

	#endregion

	#region RuntimeChainNewItem

	/// <summary>
	/// Represents an array item access (<B>[]</B>).
	/// </summary>
	[Serializable]
	public class RuntimeChainNewItem : RuntimeChainElement
	{
		/// <summary>
		/// Creates a new <see cref="RuntimeChainNewItem"/>.
		/// </summary>
		public RuntimeChainNewItem()
		{ }

		/// <summary>
		/// Throws an error because <B>[]</B> cannot be used in read context.
		/// </summary>
		public override object Get(object var, ScriptContext context, DTypeDesc caller)
		{
			PhpException.Throw(PhpError.Error, CoreResources.operator_array_access_used_for_reading);
			return null;
		}

		/// <summary>
		/// Applies the new item access on a given variable (<B>ReadRef</B> semantics).
		/// </summary>
		public override PhpReference GetRef(ref object var, ScriptContext context, DTypeDesc caller)
		{
			PhpReference reference = new PhpReference();
			Operators.SetItem(reference, ref var);
			return reference;
		}

		/// <summary>
		/// Performs the ensure operation on an variable to make it a <see cref="PhpArray"/>.
		/// </summary>
		public override object EnsureVariable(ref object var, ScriptContext context, DTypeDesc caller)
		{
			return Operators.EnsureVariableIsArray(ref var);
		}

		/// <summary>
        /// Performs the ensure operation on the new item to make it suitable for <see cref="RuntimeChainElement.Next"/> element
		/// application.
		/// </summary>
		public override object Ensure(object var, ScriptContext context, DTypeDesc caller)
		{
			Debug.Assert(Next is RuntimeChainProperty || Next is RuntimeChainItem || Next is RuntimeChainNewItem);

			if (Next is RuntimeChainProperty)
			{
				DObject std = Library.stdClass.CreateDefaultObject(context);
				((PhpArray)var).Add(std);
				return std;
			}
			else
			{
				PhpArray arr = new PhpArray();
				((PhpArray)var).Add(arr);
				return arr;
			}
		}

		/// <summary>
		/// Applies the new item access on a given variable (<B>ReadRef</B> semantics) that has already been ensured to
		/// be <see cref="PhpArray"/>.
		/// </summary>
		public override PhpReference GetEnsuredRef(object var, ScriptContext context, DTypeDesc caller)
		{
			PhpReference reference = new PhpReference();
			((PhpArray)var).Add(reference);
			return reference;
		}

		/// <summary>Returns <B>null</B> as new item (<B>[]</B>) has no name.</summary>
		public override object Name
		{
			get
			{ return null; }
		}
	}

	#endregion

	#region PhpRuntimeChain
	
	/// <summary>
	/// Represents an operator chain at runtime.
	/// </summary>
	/// <remarks>
	/// When a compile-time unknown function invocation is encountered and it has a complex parameter consisting of
	/// an operator chain, evaluation of the chain must be postponed to run-time. Only at run-time it becomes
	/// clear whether the chain's outcome should be an object or a <see cref="PhpReference"/> (formal parameter
	/// equipped with the <B>&amp;</B>) which leads to somewhat different evaluation procedure.
	/// </remarks>
	[Serializable]
	public class PhpRuntimeChain
	{
		/// <summary>
		/// Creates a new <see cref="PhpRuntimeChain"/> operating on a given variable.
		/// </summary>
		/// <param name="var">The variable.</param>
		public PhpRuntimeChain(object var)
		{
			this.Variable = var;
		}

		/// <summary>
		/// Creates a new <see cref="PhpRuntimeChain"/> operating on a given variable in a given class context.
		/// </summary>
		/// <param name="var">The variable.</param>
		/// <param name="caller">The class context.</param>
		public PhpRuntimeChain(object var, DTypeDesc caller)
		{
			this.Variable = var;
			this.Caller = caller;
		}

		/// <summary>
		/// Extends this chain with an object field access.
		/// </summary>
		/// <param name="name">The name of the field.</param>
		[Emitted]
		public void AddField(string name)
		{
			if (lastElement != null)
			{
				RuntimeChainElement element = new RuntimeChainProperty(name);
				lastElement.Next = element;
				lastElement = element;
			}
			else Chain = lastElement = new RuntimeChainProperty(name);
		}

		/// <summary>
		/// Extends this chain with an array item access.
		/// </summary>
		/// <param name="name">The name of the item (index).</param>
		[Emitted]
		public void AddItem(object name)
		{
			IntStringKey key;
			if (Convert.ObjectToArrayKey(name, out key))
			{
				if (lastElement != null)
				{
					RuntimeChainElement element = new RuntimeChainItem(key);
					lastElement.Next = element;
					lastElement = element;
				}
				else
					Chain = lastElement = new RuntimeChainItem(key);
			}
			else
				PhpException.IllegalOffsetType();
		}

		/// <summary>
		/// Extends this chain with a &quot;new field item&quot; (<B>[]</B>) access.
		/// </summary>
		[Emitted]
		public void AddItem()
		{
			if (lastElement != null)
			{
				RuntimeChainElement element = new RuntimeChainNewItem();
				lastElement.Next = element;
				lastElement = element;
			}
			else Chain = lastElement = new RuntimeChainNewItem();
		}

		/// <summary>
		/// Extends this chain with a <see cref="RuntimeChainElement"/>.
		/// </summary>
		/// <param name="element">The element.</param>
		public void Add(RuntimeChainElement element)
		{
			if (lastElement != null)
			{
				lastElement.Next = element;
				lastElement = element;
			}
			else Chain = lastElement = element;
		}

		/// <summary>
		/// Evaluates this chain as if it had the <see cref="PHP.Core.AST.AccessType.Read"/> access type.
		/// </summary>
		/// <param name="context">Current script context.</param>
		/// <returns>The result of chain evaluation.</returns>
		public object GetValue(ScriptContext context)
		{
			// dereference the PhpReference
			object var = PhpVariable.Dereference(Variable);

			RuntimeChainElement element = Chain;
			while (element != null)
			{
				// GetProperty/GetItem
				var = element.Get(var, context, Caller);
				element = element.Next;
			}
			return var;
		}

		/// <summary>
		/// Evaluates this chain as if it had the <see cref="PHP.Core.AST.AccessType.ReadRef"/> access type.
		/// </summary>
		/// <param name="context">Current script context.</param>
		/// <returns>The result of chain evaluation.</returns>
		public PhpReference GetReference(ScriptContext context)
		{
			PhpReference reference;

			RuntimeChainElement element = Chain;
			if (element == null)
			{
				// if we are just wrapping the variable with a PhpReference, make a copy
				reference = Variable as PhpReference;
				if (reference == null) reference = new PhpReference(PhpVariable.Copy(Variable, CopyReason.Unknown));

				return reference;
			}

			// make sure that we have a PhpReference
			reference = PhpVariable.MakeReference(Variable);

			if (element == lastElement)
			{
				// GetPropertyRef/GetItemRef
				return element.GetRef(ref reference.value, context, Caller);
			}

			// EnsureVariableIsObject/EnsureVariableIsArray
			object var = element.EnsureVariable(ref reference.value, context, Caller);
			if (var == null) return new PhpReference();

			while (element.Next != null)
			{
				// Ensure{Field,Item}Is{Object,Array}
				var = element.Ensure(var, context, Caller);
				if (var == null) return new PhpReference();

				element = element.Next;
			}

			// GetObjectPropertyRef/GetArrayItemRef
			return element.GetEnsuredRef(var, context, Caller);
		}

		/// <summary>
		/// The variable on which the chain is applied.
		/// </summary>
		internal object Variable;

		/// <summary>
		/// The class context in which the chain should be evaluated.
		/// </summary>
		internal DTypeDesc Caller;

		/// <summary>
		/// Head of the linked list of <see cref="RuntimeChainElement"/>s representing field and item names
		/// applied to the <see cref="Variable"/> in the lexical order.
		/// </summary>
		internal RuntimeChainElement Chain;

		/// <summary>
		/// The lastly added element or <B>null</B> if there are no elements yet.
		/// </summary>
		private RuntimeChainElement lastElement;
	}

	#endregion
	
	#region PhpSetterChainArray Proxy
	
	[Serializable]
	internal sealed class PhpSetterChainArray : PhpArray
	{
		internal PhpSetterChainArray()
		{
		}

		#region Operators

        protected override object GetArrayItemOverride(object key, bool quiet)
		{
			Debug.Fail("N/A");
			throw null;
		}
		
		protected override void SetArrayItemOverride(object key, object value)
		{
			IntStringKey array_key;
			ScriptContext context = ScriptContext.CurrentContext; // TODO: context -> field 
			
			// extend and finish the setter chain if one already exists
			if (!Convert.ObjectToArrayKey(key, out array_key))
			{
				PhpException.IllegalOffsetType();
				context.AbortSetterChain(true);
				return;
			}

			context.ExtendSetterChain(new RuntimeChainItem(array_key));
			context.FinishSetterChain(value);
			return;
		}

        protected override PhpReference GetArrayItemRefOverride()
		{
			ScriptContext.CurrentContext.AbortSetterChain(false);
			return new PhpReference();
		}

        protected override PhpReference GetArrayItemRefOverride(object key)
		{
			ScriptContext.CurrentContext.AbortSetterChain(false);
			return new PhpReference();
		}

        protected override PhpReference GetArrayItemRefOverride(int key)
		{
			ScriptContext.CurrentContext.AbortSetterChain(false);
			return new PhpReference();
		}

        protected override PhpReference GetArrayItemRefOverride(string key)
		{
			ScriptContext.CurrentContext.AbortSetterChain(false);
			return new PhpReference();
		}

        protected override void SetArrayItemOverride(object value)
		{
			ScriptContext.CurrentContext.AbortSetterChain(false);
		}

        protected override void SetArrayItemOverride(int key, object value)
		{
			ScriptContext.CurrentContext.AbortSetterChain(false);
		}

        protected override void SetArrayItemOverride(string key, object value)
		{
			ScriptContext.CurrentContext.AbortSetterChain(false);
		}

        protected override void SetArrayItemRefOverride(object key, PhpReference value)
		{
			ScriptContext.CurrentContext.AbortSetterChain(false);
		}

        protected override PhpArray EnsureItemIsArrayOverride()
		{
			ScriptContext.CurrentContext.AbortSetterChain(false);
			return null;
		}

        protected override DObject EnsureItemIsObjectOverride(ScriptContext context)
		{
			// setter chain:
			ScriptContext.CurrentContext.AbortSetterChain(false);
			return null;
		}
		
		protected override PhpArray EnsureItemIsArrayOverride(object key)
		{
			ScriptContext context = ScriptContext.CurrentContext;

			IntStringKey array_key;
			
			if (!Convert.ObjectToArrayKey(key, out array_key))
			{
				PhpException.IllegalOffsetType();
				context.AbortSetterChain(true);
				return null;
			}
			
			// extend the setter chain if one already exists:
			context.ExtendSetterChain(new RuntimeChainItem(array_key));

			return this;
		}

        protected override DObject EnsureItemIsObjectOverride(object key, ScriptContext/*!*/ context)
		{
			IntStringKey array_key;

			if (!Convert.ObjectToArrayKey(key, out array_key))
			{
				PhpException.IllegalOffsetType();
				context.AbortSetterChain(true);
				return null;
			}
			
			// extend the setter chain if one already exists
			context.ExtendSetterChain(new RuntimeChainItem(array_key));

			return ScriptContext.SetterChainSingletonObject;

		}
		
		#endregion
	}
	
	#endregion
}
