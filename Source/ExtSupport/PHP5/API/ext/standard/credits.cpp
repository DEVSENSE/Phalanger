//
// ExtSupport.PHP5 - substitute for php5ts.dll
//
// credits.cpp
// - this is modified credits.c, originally PHP 5.3.3 source files
//

#include "../../main/php.h"
#include "credits.h"

#pragma managed

// rewritten
PHPAPI void php_print_credits(int flag)
{
	PHP::Core::PhpNetInfo::Write(PHP::Core::PhpNetInfo::Sections::Credits,
		PHP::Core::ScriptContext::CurrentContext->Output);
}