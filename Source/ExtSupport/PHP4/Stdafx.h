// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently,
// but are changed infrequently

#pragma once

#define _CRT_SECURE_NO_DEPRECATE 1
#define _USE_32BIT_TIME_T
#include <cmath>
#include <winsock2.h>
#include <windows.h>
#include <tchar.h>

#pragma unmanaged
#include "Zend.h"
#pragma managed

// copied from http://msdn.microsoft.com/msdnmag/issues/02/02/ManagedC/default.aspx
template <typename T1, typename T2> inline bool istypeof(T2 ^t)
{
   return (dynamic_cast<T1 ^>(t) != nullptr);
}

#define scg System::Collections::Generic

#ifdef _DEBUG
//#define DEBUG
#define Indent() WriteLine("PHP4TS", "{")
#define Unindent() WriteLine("PHP4TS", "}")
#endif
