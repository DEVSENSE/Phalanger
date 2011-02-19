//
// ExtSupport.PHP5 - substitute for php5ts.dll
//
// Unsupported.h
//

#define UNSUPPORTED_MSG(func)	zend_error(E_ERROR, func " Zend API call is not supported in Phalanger")

#define TMP_UNSUPPORTED_MSG(func)	zend_error(E_ERROR, func " Zend API call is not temporary supported in Phalanger")


#define UNSUPPORTED(func)       \
	ZEND_API void func()        \
	{                           \
		UNSUPPORTED_MSG(#func); \
	}

#define UNSUPPORTED_QUIETLY(func) \
	ZEND_API void func()          \
	{ }
