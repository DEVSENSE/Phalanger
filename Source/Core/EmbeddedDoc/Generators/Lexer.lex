/*

 Copyright (c) 2008 Daniel Balas.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using PHP.Core;

using System.Collections.Generic;

%%

%namespace PHP.Core.EmbeddedDoc
%type Tokens
%class DocLexer
%eofval Tokens.EOF
%errorval Tokens.ERROR
%attributes public partial
%function GetNextToken
%ignorecase
%charmap Map
%char
%line
%column

%x INITIAL
%x LINE
%x LINE_BEGIN

%%

<INITIAL>[' '\t]*"/**"[' '\t]*[\r\n]+ {
	BEGIN(LexicalStates.LINE_BEGIN);
	return Tokens.T_BEGIN;
}

<INITIAL>[' '\t]*"/**" {
	BEGIN(LexicalStates.LINE);
	return Tokens.T_BEGIN;
}

<LINE>"{@link" {
	return Tokens.T_INLINE_LINK;
}

<LINE>"array" {
	return Tokens.T_ARRAY;
}

<LINE>"public" {
	return Tokens.T_PUBLIC;
}

<LINE>"private" {
	return Tokens.T_PRIVATE;
}

<LINE>"protected" {
	return Tokens.T_PROTECTED;
}

<LINE>[a-zA-Z'_'][a-zA-Z0-9'_']* { 
	return Tokens.T_IDENTIFIER; 
}

<LINE>[0-9]* { 
	return Tokens.T_INTEGER; 
}

<LINE>"|" {
	return Tokens.T_BAR;
}

<LINE>"$" {
	return Tokens.T_DOLLAR;
}

<LINE>"[" {
	return Tokens.T_LBRA;
}

<LINE>"]" {
	return Tokens.T_RBRA;
}

<LINE>"}" {
	return Tokens.T_RCURLY;
}

<LINE>"*/".* {
	return Tokens.T_END;
}

<LINE>[\r\n]+ {
	BEGIN(LexicalStates.LINE_BEGIN);
	return Tokens.T_NEWLINE;
}

<LINE>[' '\t]+ {	
	return Tokens.T_WHITESPACE;
}

<LINE>[^' '\t\n\r] {
	return Tokens.T_SYMBOL;
}

<LINE_BEGIN>[' '\t]*"*"[' '\t]*"@param" {
	BEGIN(LexicalStates.LINE);
	return Tokens.T_ELEMENT_PARAM;
}

<LINE_BEGIN>[' '\t]*"*"[' '\t]*"@return" {
	BEGIN(LexicalStates.LINE);
	return Tokens.T_ELEMENT_RETURN;
}

<LINE_BEGIN>[' '\t]*"*"[' '\t]*"@var" {
	BEGIN(LexicalStates.LINE);
	return Tokens.T_ELEMENT_VAR;
}

<LINE_BEGIN>[' '\t]*"*"[' '\t]*"@access" {
	BEGIN(LexicalStates.LINE);
	return Tokens.T_ELEMENT_ACCESS;
}

<LINE_BEGIN>[' '\t]*"*/".* {
	return Tokens.T_END;
}

<LINE_BEGIN>[' '\t]*"*"[' ']? {
	BEGIN(LexicalStates.LINE);
	return Tokens.T_LINE_BEGIN;
}

<LINE_BEGIN>. {
	yyless(1);
	BEGIN(LexicalStates.LINE);
	break;
}