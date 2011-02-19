//
// ExtSupport.PHP4 - substitute for php5ts.dll
//
// GC.cpp 
// - need to be implemented when ZEND GC will be added
//

#include "stdafx.h"

#ifdef PHP5TS

#include "GC.h"

ZEND_API void gc_remove_zval_from_buffer(zval *zv TSRMLS_DC)
{
	//TODO: Garbage collection, this function can be empty now.
}

#endif