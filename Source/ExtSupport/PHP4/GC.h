//
// ExtSupport.PHP4 - substitute for php5ts.dll
//
// GC.h 
// - need to be implemented when ZEND GC will be added
//

#pragma once

#include "stdafx.h"
#include "ExtSupport.h"

BEGIN_EXTERN_C()

ZEND_API void gc_remove_zval_from_buffer(zval *zv TSRMLS_DC);

END_EXTERN_C()