/*

 Copyright (c) 2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

.namespace PHP.Library.SPL
{
	.class public interface abstract Reflector
	{
		.custom instance void [PhpNetCore]PHP.Core.ImplementsTypeAttribute::.ctor()

		.method public newslot abstract virtual instance object __toString(class [PhpNetCore]PHP.Core.ScriptContext context)
		{ }

		.method public static object export(class [PhpNetCore]PHP.Core.ScriptContext context)
		{
			.custom instance void [PhpNetCore]PHP.Core.PhpAbstractAttribute::.ctor()
			.custom instance void [PhpNetCore]PHP.Core.AllowParametersOverrideAttribute::.ctor()
			
			ldnull
			ret
		}
	}
}