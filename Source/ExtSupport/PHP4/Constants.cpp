//
// ExtSupport.PHP4 - substitute for php4ts.dll
//
// Constants.cpp 
// - contains definitions of constants related functions
//

#include "stdafx.h"
#include "Constants.h"
#include "Module.h"
#include "PhpMarshaler.h"
#include "Request.h"

using namespace PHP::ExtManager;

void free_zend_constant(zend_constant *c)
{
	if (!(c->flags & CONST_PERSISTENT)) {
		zval_dtor(&c->value);
	}
	free(c->name);
}


ZEND_API void zend_register_long_constant(char *name, uint name_len, long lval, int flags, int module_number TSRMLS_DC)
{
	if ((flags & CONST_PERSISTENT) != 0)
	{
		zend_constant c;
		c.flags = flags;
		c.name = name;
		c.name_len = name_len;
		c.module_number = module_number;

		INIT_ZVAL(c.value);
		ZVAL_LONG(&c.value, lval);
		unmng_register_constant(&c);
	}

	Constant ^constant = gcnew Constant(Module::GetCurrentModule(), gcnew String(name, 0, name_len - 1), lval,
		!(flags & CONST_CS));
	constant->Register((flags & CONST_PERSISTENT) != 0);
}

ZEND_API void zend_register_double_constant(char *name, uint name_len, double dval, int flags, int module_number TSRMLS_DC)
{
	if ((flags & CONST_PERSISTENT) != 0)
	{
		zend_constant c;
		c.flags = flags;
		c.name = name;
		c.name_len = name_len;
		c.module_number = module_number;

		INIT_ZVAL(c.value);
		ZVAL_DOUBLE(&c.value, dval);
		unmng_register_constant(&c);
	}

	Constant ^constant = gcnew Constant(Module::GetCurrentModule(), gcnew String(name, 0, name_len - 1), dval,
		!(flags & CONST_CS));
	constant->Register((flags & CONST_PERSISTENT) != 0);
}

ZEND_API void zend_register_stringl_constant(char *name, uint name_len, char *strval, uint strlen, int flags, 
											 int module_number TSRMLS_DC)
{
	if ((flags & CONST_PERSISTENT) != 0)
	{
		zend_constant c;
		c.flags = flags;
		c.name = name;
		c.name_len = name_len;
		c.module_number = module_number;

		INIT_ZVAL(c.value);
		ZVAL_STRINGL(&c.value, strval, strlen, false);
		unmng_register_constant(&c);
	}

	Constant ^constant = gcnew Constant(Module::GetCurrentModule(), gcnew String(name, 0, name_len - 1),
		gcnew String(strval, 0, strlen), !(flags & CONST_CS));
	constant->Register((flags & CONST_PERSISTENT) != 0);
}

ZEND_API void zend_register_string_constant(char *name, uint name_len, char *strval, int flags, int module_number TSRMLS_DC)
{
	if ((flags & CONST_PERSISTENT) != 0)
	{
		zend_constant c;
		c.flags = flags;
		c.name = name;
		c.name_len = name_len;
		c.module_number = module_number;

		INIT_ZVAL(c.value);
		ZVAL_STRING(&c.value, strval, false);
		unmng_register_constant(&c);
	}

	Constant ^constant = gcnew Constant(Module::GetCurrentModule(), gcnew String(name, 0, name_len - 1),
		gcnew String(strval), !(flags & CONST_CS));
	constant->Register((flags & CONST_PERSISTENT) != 0);
}

ZEND_API int zend_get_constant(char *name, uint name_len, zval *result TSRMLS_DC)
{
	String ^nameStr = gcnew String(name, 0, name_len);
	
#ifdef DEBUG
	Debug::WriteLine("PHP4TS", nameStr);
#endif

	Constant ^constant = Constant::Lookup(nameStr);
	if (constant == nullptr) return FAILURE;
	
	zval *value = (zval *)(PhpMarshaler::GetInstance(nullptr)->MarshalManagedToNative(constant->GetValue()).ToPointer());
	memcpy(result, value, sizeof(zval));
	efree(value);

	return SUCCESS;
}

ZEND_API int zend_register_constant(zend_constant *c TSRMLS_DC)
{
	if ((c->flags & CONST_PERSISTENT) != 0) unmng_register_constant(c);

	// get constant name
	String ^name = gcnew String(c->name, 0, c->name_len - 1);

	// get constant value
	PhpMarshaler ^marshaler = PhpMarshaler::GetInstance(nullptr);
	Object ^value = marshaler->MarshalNativeToManaged(IntPtr(&c->value));

	if (!(c->flags & CONST_PERSISTENT)) marshaler->CleanUpNativeData(IntPtr(&c->value));
	//free(c->name); // suspicious, isn't it?

	Constant ^constant = gcnew Constant(Module::GetCurrentModule(), name, value, !(c->flags & CONST_CS));
	return (constant->Register((c->flags & CONST_PERSISTENT) != 0) ? SUCCESS : FAILURE);
}


#ifdef PHP5TS
//copied from zend_constants.c
int zend_startup_constants(TSRMLS_D)
{
	EG(zend_constants) = (HashTable *) malloc(sizeof(HashTable));

	if (zend_hash_init(EG(zend_constants), 20, NULL, ZEND_CONSTANT_DTOR, 1)==FAILURE) {
		return FAILURE;
	}
	return SUCCESS;
}
#endif