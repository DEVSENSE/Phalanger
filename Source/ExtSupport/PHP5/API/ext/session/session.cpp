//
// ExtSupport.PHP5 - substitute for php5ts.dll
//
// session.cpp
// - this is heavily session.c, originally PHP 5.3.3 source files
// - everything here is unsupported
//

#include "../../main/php.h"
#include "../../../Unsupported.h"
#include "php_session.h"


PHPAPI void session_adapt_url(const char *, size_t, char **, size_t * TSRMLS_DC)
{
	UNSUPPORTED_MSG("session_adapt_url");
}

PHPAPI void php_add_session_var(char *name, size_t namelen TSRMLS_DC)
{
	UNSUPPORTED_MSG("php_add_session_var");
}

PHPAPI void php_set_session_var(char *name, size_t namelen, zval *state_val, /*php_unserialize_data_t* */ void* var_hash TSRMLS_DC)
{
	UNSUPPORTED_MSG("php_set_session_var");

}

PHPAPI int php_get_session_var(char *name, size_t namelen, zval ***state_var TSRMLS_DC)
{
	UNSUPPORTED_MSG("php_get_session_var");
	return FAILURE;
}

PHPAPI /*ps_module* */ void* _php_find_ps_module(char *name TSRMLS_DC)
{
	UNSUPPORTED_MSG("_php_find_ps_module");
	return NULL;
}

PHPAPI /*const ps_serializer* */ void* _php_find_ps_serializer(char *name TSRMLS_DC)
{
	UNSUPPORTED_MSG("_php_find_ps_serializer");
	return NULL;
}

ZEND_API int php_session_register_module(/*ps_module*/void *)
{
	UNSUPPORTED_MSG("php_session_register_module");
	return FAILURE;
}

ZEND_API int php_session_register_serializer(const char *name, int (*encode)(/*PS_SERIALIZER_ENCODE_ARGS*/),
											 int (*decode)(/*PS_SERIALIZER_DECODE_ARGS*/))
{
	UNSUPPORTED_MSG("php_session_register_serializer");
	return FAILURE;
}

ZEND_API void php_session_set_id(char *id TSRMLS_DC)
{
	UNSUPPORTED_MSG("php_session_set_id");
}

ZEND_API void php_session_start(TSRMLS_D)
{
	UNSUPPORTED_MSG("php_session_start");
}
