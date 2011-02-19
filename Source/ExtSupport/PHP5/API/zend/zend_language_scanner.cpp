//
// ExtSupport.PHP5 - substitute for php5ts.dll
//
// zend_language_scanner.cpp
// - this is slightly modified zend_language_scanner.c, originally PHP 5.3.3 source files
//

#include "../../Unsupported.h"

//#include "zend_language_scanner_defs.h"

#include <errno.h>
#include "zend.h"
#include "zend_alloc.h"
#include "zend_language_parser.h"
#include "zend_compile.h"
#include "zend_language_scanner.h"
#include "zend_highlight.h"
#include "zend_constants.h"
#include "zend_variables.h"
#include "zend_operators.h"
#include "zend_API.h"
#include "zend_strtod.h"
#include "zend_exceptions.h"
//#include "../TSRM/tsrm_virtual_cwd.h"//causing some bug in VC++( Error	2	error C2466: cannot allocate an array of constant size 0	c:\Program Files (x86)\Microsoft Visual Studio 9.0\VC\include\sys\stat.inl	44	ExtSupport.PHP5)
//#include "../TSRM/tsrm_config_common.h"

//BEGIN_EXTERN_C()

ZEND_API void zend_save_lexical_state(zend_lex_state *lex_state TSRMLS_DC)
{
	UNSUPPORTED_MSG("zend_save_lexical_state");
}

ZEND_API void zend_restore_lexical_state(zend_lex_state *lex_state TSRMLS_DC)
{
	UNSUPPORTED_MSG("zend_restore_lexical_state");
}

ZEND_API void zend_destroy_file_handle(zend_file_handle *file_handle TSRMLS_DC)
{
	UNSUPPORTED_MSG("zend_destroy_file_handle");
}


ZEND_API int open_file_for_scanning(zend_file_handle *file_handle TSRMLS_DC)
{
	UNSUPPORTED_MSG("open_file_for_scanning");
	return FAILURE;
}
//END_EXTERN_C()


ZEND_API zend_op_array *compile_file(zend_file_handle *file_handle, int type TSRMLS_DC)
{
	UNSUPPORTED_MSG("compile_file");
	return NULL;

}


ZEND_API int zend_prepare_string_for_scanning(zval *str, char *filename TSRMLS_DC)
{
	UNSUPPORTED_MSG("zend_prepare_string_for_scanning");
	return FAILURE;

}


ZEND_API size_t zend_get_scanned_file_offset(TSRMLS_D)
{
	UNSUPPORTED_MSG("zend_get_scanned_file_offset");
	return 0;

}


#ifdef ZEND_MULTIBYTE

//BEGIN_EXTERN_C()
ZEND_API void zend_multibyte_yyinput_again(zend_encoding_filter old_input_filter, zend_encoding *old_encoding TSRMLS_DC)//UNSUPPORTED
{
	UNSUPPORTED_MSG("zend_multibyte_yyinput_again");

}


ZEND_API int zend_multibyte_yyinput(zend_file_handle *file_handle, char *buf, size_t len TSRMLS_DC)//UNSUPPORTED
{
	UNSUPPORTED_MSG("zend_multibyte_yyinput");
	return FAILURE;

}


ZEND_API int zend_multibyte_read_script(unsigned char *buf, size_t n TSRMLS_DC)//UNSUPPORTED
{
	UNSUPPORTED_MSG("zend_multibyte_read_script");
	return FAILURE;
}
//END_EXTERN_C()

#endif