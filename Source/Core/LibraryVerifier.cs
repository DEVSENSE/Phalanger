/*

 Copyright (c) 2004-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection;
using System.Collections;
using System.Diagnostics;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Core
{
	// TODO:
	///// <summary>
	///// Summary description for ClassLibraryVerifier.
	///// </summary>
	//public class PhpLibraryVerifier
	//{
	//  private PhpLibraryVerifier() {}

	//  /// <summary>
	//  /// Checks whether a type is acceptable for type of Class Library function argument.
	//  /// </summary>
	//  /// <param name="type">The type to be checked.</param>
	//  /// <param name="params">Whether <see cref="ParamArrayAttribute"/> is applied to the argument.</param>
	//  /// <param name="deepCopy">Whether the argument is marked by <see cref="PhpDeepCopyAttribute"/>.</param>
	//  /// <returns>Whether <paramref name="type"/> is allowed to be a type of an argument of a Class Library function.</returns>
	//  internal static bool VerifyArgumentType(Type type,bool @params,bool deepCopy)
	//  {
	//    // out/ref arguments:
	//    if (type.IsByRef)
	//    {
	//      // dereferences type:
	//      type = type.GetElementType();
	//    }

	//    // CLR arrays:
	//    if (type.IsArray)
	//    {
	//      // allowed only as vararg params:
	//      if (!@params) return false;

	//      // gets element type:
	//      type = type.GetElementType();
	//    }  

	//    // object is allowed:
	//    if (type==typeof(object)) return true;

	//    // boolean, integer, double, string, char are allowed, shouldn't be deep copied: 
	//    if (PhpVariable.IsLiteralPrimitiveType(type) || type == typeof(char)) return !deepCopy;

	//    // any implementor of IPhpVariable is allowed:
	//    if (typeof(IPhpVariable).IsAssignableFrom(type)) return true;

	//    // can be PhpArray assigned to the argument?
	//    if (type.IsAssignableFrom(typeof(PhpArray))) return true;

	//    // PhpCallback:
	//    if (type==typeof(PhpCallback)) return !deepCopy;

	//    // others:
	//    return false;    
	//  }    

	//  /// <summary>
	//  /// Checks whether a type is acceptable for type of Class Library function return value.
	//  /// </summary>
	//  /// <param name="type">The type to be checked.</param>
	//  /// <param name="deepCopy">Whether the return value is marked by <see cref="PhpDeepCopyAttribute"/>.</param>
	//  /// <returns>Whether <paramref name="type"/> is allowed to be a type of a return value of a Class Library function.</returns>
	//  internal static bool VerifyReturnType(Type type,bool deepCopy)
	//  {
	//    bool cast = type.IsDefined(typeof(CastToFalseAttribute),false);

	//    // cast to false on value type which is not integer:
	//    if (cast && type.IsValueType && type!=typeof(int)) return false;

	//    // void, booleans, integers, doubles, strings are allowed:
	//    if (type == Types.Void || PhpVariable.IsLiteralPrimitiveType(type)) return !deepCopy;

	//    // object is ok:
	//    if (type == typeof(object)) return true;

	//    // any implementor of IPhpVariable is allowed (including PhpReference):
	//    if (typeof(IPhpVariable).IsAssignableFrom(type)) return true;

	//    // PhpCallback and others are not allowed:
	//    return false;
	//  }

	//  /// <summary>
	//  /// Checks whether all library functions has well-declared overloads.
	//  /// </summary>
	//  /// <param name="errors">The array where to report errors.</param>
	//  /// <param name="functions">The table of declared functions.</param>
	//  internal static void VerifyOverloadsLists(ArrayList errors, PhpFunctionsTable functions)
	//  {
	//    IDictionaryEnumerator iterator = functions.GetEnumerator();
	//    while (iterator.MoveNext())
	//    {
	//      OverloadInfo[] overloads = (OverloadInfo[])iterator.Value;

	//      Debug.Assert(overloads.Length>0);

	//      FunctionImplOptions options = overloads[0].Options;
	//      for(int i=0;i<overloads.Length;i++)
	//      {
	//        MethodInfo method = overloads[i].GetUserEntryPoint;

	//        // options should be same in all overloads:
	//        if (overloads[i].Options!=options)
	//        {
	//          errors.Add(CoreResources.GetString("overload_has_different_impl_options",method.DeclaringType.FullName,method.Name,iterator.Key)); 
	//        }

	//        // vararg should be set only on the last overload:
	//        if (i<overloads.Length-1 && (overloads[i].Flags & OverloadFlags.IsVararg)!=0)
	//        {
	//          errors.Add(CoreResources.GetString("overload_has_lesser_param_count",method.DeclaringType.FullName,method.Name,iterator.Key)); 
	//        }
	//      }
	//    }
	//  }

	//  /// <summary>
	//  /// Verifies an assembly against demands on Phalanger class library.
	//  /// </summary>
	//  /// <param name="assembly">The assembly to be verified.</param>
	//  /// <param name="errors">An <see cref="ArrayList"/> of error messages</param>
	//  /// <param name="warnings">An <see cref="ArrayList"/> of warning messages</param>
	//  public static void VerifyLibrary(Assembly assembly,out ArrayList errors,out ArrayList warnings)
	//  {
	//    errors = new ArrayList();
	//    warnings = new ArrayList();
	//    object[] attrs;
	//    bool vararg,deep_copy;
	//    Name? name;
	//    bool contains_implementation;    
	//    PhpConstantsTable implemented_constants = new PhpConstantsTable();
	//    PhpFunctionsTableBuilder ft_builder = new PhpFunctionsTableBuilder(PhpFunctionUtils.AssumedMaxOverloadCount);

	//    foreach (Type type in assembly.GetTypes())
	//    {
	//      // whethe the type contains implementation of function or method:
	//      contains_implementation = false;

	//      // checks methods:
	//      foreach (MethodInfo method in type.GetMethods())
	//      {
	//        attrs = method.GetCustomAttributes(Emit.Types.ImplementsFunctionAttribute,false);
	//        if (attrs.Length>0)
	//        {
	//          ImplementsFunctionAttribute ifa = (ImplementsFunctionAttribute)attrs[0];

	//          // skips not supported functions:
	//          if ((ifa.Options & FunctionImplOptions.NotSupported)!=0) continue;

	//          contains_implementation = true;

	//          name = new Name(ifa.Name);

	//          // checks the name:
	//          if (!PhpFunctionUtils.IsValidName(name.ToString()))
	//          {
	//            errors.Add(CoreResources.GetString("invalid_function_name", type.FullName, method.Name, ifa.Name)); 

	//            name = null;  
	//          }

	//          // checks "public static":
	//          if (!method.IsStatic || !method.IsPublic)
	//          {
	//            errors.Add(CoreResources.GetString("invalid_method_modifiers",type.FullName,method.Name));
	//          }

	//          ParameterInfo[] ps = method.GetParameters();

	//          // checks NeedsVariable option:
	//          if ((ifa.Options & FunctionImplOptions.NeedsVariables)!=0)
	//          {
	//            if (ps.Length==0 || ps[0].ParameterType!=typeof(IDictionary))
	//              errors.Add(CoreResources.GetString("first_param_not_dictionary",type.FullName,method.Name));
	//          }

	//          // parameters:
	//          foreach (ParameterInfo param in ps)
	//          {
	//            vararg = param.IsDefined(typeof(ParamArrayAttribute),false);
	//            deep_copy = param.IsDefined(typeof(PhpDeepCopyAttribute),false);

	//            if (!VerifyArgumentType(param.ParameterType,vararg,deep_copy))
	//            {
	//              errors.Add(CoreResources.GetString("invalid_parameter_type",type.FullName,method.Name,param.Name,param.ParameterType.FullName));
	//            }    
	//          }

	//          // return value:
	//          deep_copy = method.ReturnType.IsDefined(typeof(PhpDeepCopyAttribute),false);

	//          if (!VerifyReturnType(method.ReturnType,deep_copy))
	//          {
	//            errors.Add(CoreResources.GetString("invalid_return_type",type.FullName,method.Name,method.ReturnType.FullName));
	//          }

	//          // checks overloads:
	//          if (name.HasValue)
	//          {
	//            MethodInfo prev_method = ft_builder.AddInternal(name.Value, method, ps, ifa.Options);
	//            if (prev_method!=null)
	//            {
	//              errors.Add(CoreResources.GetString("function_reimplemented",
	//                type.FullName,method.Name,
	//                ifa.Name,
	//                prev_method.DeclaringType.FullName,prev_method.Name)); 
	//            }
	//            if (ft_builder.Modified)
	//            {
	//              warnings.Add(CoreResources.GetString("overloads_not_contiguous",
	//                type.FullName,method.Name,
	//                ifa.Name));
	//            }
	//          }  
	//        }  
	//      }

	//      // checks constant fields:
	//      foreach (FieldInfo field in type.GetFields())
	//      {
	//        attrs = field.GetCustomAttributes(typeof(ImplementsConstantAttribute),false);
	//        if (attrs.Length>0)
	//        {
	//          contains_implementation = true;

	//          ImplementsConstantAttribute ica = (ImplementsConstantAttribute)attrs[0];

	//          // checks the name:
	//          if (!ClassConstant.IsValidName(ica.Name))
	//          {
	//            errors.Add(CoreResources.GetString("invalid_constant_name",
	//              type.FullName,field.Name,ica.Name)); 
	//          }

	//          // checks field attributes:
	//          if (!field.IsLiteral || !field.IsPublic)
	//          {
	//            errors.Add(CoreResources.GetString("invalid_constant_field",
	//              type.FullName,field.Name)); 
	//          }

	//          // checks field type:
	//          if (!PhpVariable.IsLiteralPrimitiveType(field.FieldType))
	//          {
	//            errors.Add(CoreResources.GetString("invalid_constant_type",
	//              type.FullName,field.Name,field.FieldType.FullName));
	//          } 

	//          // checks existence:
	//          if (!implemented_constants.Add(ica.Name,field,ica.CaseInsensitive))
	//          {
	//            bool exists;
	//            FieldInfo prev_field = (FieldInfo)implemented_constants.Get(ica.Name,out exists);
	//            Debug.Assert(exists);

	//            errors.Add(CoreResources.GetString("constant_reimplemented",
	//              type.FullName,field.Name,
	//              ica.Name,
	//              prev_field.DeclaringType.FullName,prev_field.Name)); 
	//          }
	//        }  
	//      }

	//      // ckecks type itself:
	//      if (contains_implementation)
	//      {
	//        if (!type.IsPublic && !(type.IsNestedPublic && type.IsEnum))
	//          errors.Add(CoreResources.GetString("invalid_type_visibility",type.FullName));

	//        if (!type.Namespace.StartsWith(Namespaces.Library))
	//          errors.Add(CoreResources.GetString("invalid_type_namespace",type.FullName,Namespaces.Library));
	//      }
	//    }

	//    PhpFunctionsTable implemented_functions = ft_builder.ToFunctionsTable();
	//    VerifyOverloadsLists(errors,implemented_functions);
	//  }

	//}
}
