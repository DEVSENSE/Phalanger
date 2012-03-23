/*

 Copyright (c) 2004-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using PHP.Core;

namespace PHP.Library
{
	/// <summary>
	/// Implementation of constants handling functions.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class PhpConstants
	{
		/// <summary>
		/// Defines a constant.
		/// </summary>
		/// <param name="name">The name of the constant. Can be arbitrary string.</param>
		/// <param name="value">The value of the constant. Can be <B>null</B> reference or a scalar <see cref="PhpVariables.IsScalar"/>.</param>
		/// <returns>Whether the new constant has been defined.</returns>
		[ImplementsFunction("define")]
		public static bool Define(string name, object value)
		{
			return ScriptContext.CurrentContext.DefineConstant(name, value, false);
		}

		/// <summary>
		/// Defines a constant.
		/// </summary>
		/// <param name="name">The name of the constant. Can be arbitrary string.</param>
		/// <param name="value">The value of the constant. Can be <B>null</B> reference or a scalar <see cref="PhpVariables.IsScalar"/>.</param>
		/// <param name="caseInsensitive">Whether the name is case insensitive.</param>
		/// <returns>Whether the new constant has been defined.</returns>
		[ImplementsFunction("define")]
		public static bool Define(string name, object value, bool caseInsensitive)
		{
			return ScriptContext.CurrentContext.DefineConstant(name, value, caseInsensitive);
		}

		/// <summary>
		/// Determines whether a constant is defined.
		/// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
		/// <param name="name">The name of the constant.</param>
		/// <returns>Whether the constant is defined.</returns>
		[ImplementsFunction("defined")]
        [PureFunction(typeof(PhpConstants), "Defined_Analyze")]
		public static bool Defined(ScriptContext context, string name)
		{
            return context.IsConstantDefined(name);
        }

        #region analyzer of: defined(), constant()

        /// <summary>
        /// Try to find constant by given <paramref name="name"/> in compile time.
        /// </summary>
        /// <param name="analyzer">Actual <see cref="Analyzer"/>.</param>
        /// <param name="name">Constant name, including class constants.</param>
        /// <param name="exists">Outputs <c>true</c> or <c>false</c> if the existance of the constant was determined.</param>
        /// <returns>Constant descriptor.</returns>
        private static PHP.Core.Reflection.DConstant EvaluateConstant(Analyzer analyzer, string name, out bool? exists)
        {
            if (name == null)
                name = string.Empty;

            // try global constant:
            QualifiedName? alias;
            var constant = analyzer.SourceUnit.ResolveConstantName(new QualifiedName(new Name(name)), analyzer.CurrentScope, out alias, null, PHP.Core.Parsers.Position.Invalid, false);

            if (constant != null)
            {
                exists = true;  // we surely know, the constant is defined.
                return constant;
            }
            
            // try class constant:
            string typename, constname;
            if (Name.IsClassMemberSyntax(name, out typename, out constname))
            {
                var type = analyzer.SourceUnit.ResolveTypeName(new QualifiedName(new Name(typename)), analyzer.CurrentScope, out alias, null, PHP.Core.Parsers.Position.Invalid, false);

                if (type != null && !type.IsUnknown)
                {
                    Core.Reflection.ClassConstant classconst;
                    type.GetConstant(new VariableName(constname), null /* class constants are global only */, out classconst);
                    Debug.Assert(classconst == null || classconst.IsPublic, "Class constant are expected to be public only.");
                    exists = (classconst != null);  // we surely know, wheter the constant is or is not defined.
                    return classconst;
                }
            }

            // special constants defined in runtime, but definitely defined:
            if (name == "PHALANGER")
            {
                exists = true;
                return null;
            }

            // do not evaluate in compile time
            exists = null;   // we are not sure about existance of this constant.
            return null;
        }

        public static PHP.Core.AST.DirectFcnCall.EvaluateInfo Defined_Analyze(Analyzer analyzer, string name)
        {
            bool? exists;
            var constant = EvaluateConstant(analyzer, name, out exists);

            if (exists != null)
                return new Core.AST.DirectFcnCall.EvaluateInfo()
                {
                    value = exists.Value    // constant existance is known in compile time
                };

            // check in run time:
            return null;
        }

        public static PHP.Core.AST.DirectFcnCall.EvaluateInfo Constant_Analyze(Analyzer analyzer, string name)
        {
            bool? exists;
            var constant = EvaluateConstant(analyzer, name, out exists);

            if (constant != null && constant.HasValue)
                return new Core.AST.DirectFcnCall.EvaluateInfo()
                {
                    value = constant.Value  // evaluated value in compile time
                };

            // check in run time:
            return null;
        }

        #endregion

        /// <summary>
		/// Retrieves a value of a constant.
		/// </summary>
		/// <param name="name">The name of the constant.</param>
		/// <returns>The value.</returns>
		[ImplementsFunction("constant")]
        [PureFunction(typeof(PhpConstants), "Constant_Analyze")]
        public static object Constant(string name)
		{
			return ScriptContext.CurrentContext.GetConstantValue(name, false, false);
		}

        /// <summary>
		/// Retrieves defined constants.
		/// </summary>
		/// <returns>The array which contains pairs (constant name,constant value).</returns>
		[ImplementsFunction("get_defined_constants")]
		public static PhpArray GetDefinedConstants()
		{
			ScriptContext context = ScriptContext.CurrentContext;
			PhpArray result = new PhpArray(0, context.GetDefinedConstantCount());
			context.GetDefinedConstants(result);
			return result;
		}

        /// <summary>
        /// Retrieves defined constants.
        /// </summary>
        /// <param name="categorize">Returns a multi-dimensional array with categories in the keys of the first dimension and constants and their values in the second dimension. </param>
        /// <returns>Retrives the names and values of all the constants currently defined.</returns>
        [ImplementsFunction("get_defined_constants")]
        public static PhpArray GetDefinedConstants(bool categorize)
        {
            if (categorize == false)
                return GetDefinedConstants();

            ScriptContext context = ScriptContext.CurrentContext;
            PhpArray resultArray = new PhpArray();
            PhpArray internalArray = null;

            //Core constants first
            internalArray = new PhpArray();
            context.GetDefinedExtensionConstants(internalArray, "Core");
            if (internalArray.Count > 0)
                resultArray.Add("Core", internalArray);

            // Loaded extensions constants
            foreach (string extensionName in ScriptContext.CurrentContext.ApplicationContext.GetLoadedExtensions())//TODO: sort alphabeticaly
            {
                if (extensionName == "Core")
                    continue; // skip core, it's already defined ( Class library has few core classes )

                internalArray = new PhpArray();
                context.GetDefinedExtensionConstants(internalArray, extensionName);

                if (internalArray.Count > 0)
                    resultArray.Add(extensionName, internalArray);
            }

			//User constants
			internalArray = new PhpArray(0, context.GetDefinedUserConstantCount());
			context.GetDefinedUserConstants(internalArray);
			resultArray.Add("user", internalArray);

            return resultArray;
        }

	}
}
