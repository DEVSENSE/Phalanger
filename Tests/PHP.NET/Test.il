.assembly TestIL
{ 
  .ver 2:0:0:0
}

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) 
  .ver 2:0:0:0
}

.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) 
  .ver 2:0:0:0
}

.namespace IL
{

	.class interface public abstract I
	{
		.method public newslot abstract virtual instance void f1() { }
		.method public newslot abstract virtual instance void f2() { }
		.method public newslot abstract virtual instance void g() { }
	}

	.class abstract public A implements IL.I
	{
		.method public specialname rtspecialname instance void .ctor()
		{
			ldarg.0
			call instance void [mscorlib]System.Object::.ctor()
			ret
		}
		
		.method private virtual final instance void '_f'()
		{
			.override IL.I::f1
			.override IL.I::f2
			ret
		}
	}

	.class abstract public B
	{
		.method public specialname rtspecialname instance void .ctor()
		{
			ldarg.0
			call instance void [mscorlib]System.Object::.ctor()
			ret
		}
		
		.method public newslot abstract virtual instance void f() { }
		
		.method public newslot abstract virtual instance void g() { }
	}
	
	.class abstract public C extends IL.B
	{
		.method public specialname rtspecialname instance void .ctor()
		{
			ldarg.0
			call instance void IL.B::.ctor()
			
			ret
		}
		
		.method private virtual final instance void h()
		{
			.override IL.B::f
			.override IL.B::g
			
			ret
		}
		
		.method public newslot abstract virtual instance void i() { }
		
	}
}