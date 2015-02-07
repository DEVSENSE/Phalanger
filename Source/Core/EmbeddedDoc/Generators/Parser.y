/*

 Copyright (c) 2012 DEVSENSE

 Parser was generated using The Gardens Point Parser Generator (GPPG).

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.
 
*/

using PHP.Core;
using PHP.Core.EmbeddedDoc;
using PHP.Core.Parsers.GPPG;

%%

%namespace PHP.Core.EmbeddedDoc
%valuetype SemanticValueType
%positiontype Position
%tokentype Tokens
%visibility public

%union
{
	public string String;
	public object Object;
}

/* terminals declaration */

%token<String> T_WHITESPACE

%token T_NEWLINE
%token T_LINE_BEGIN
%token T_BEGIN
%token T_END

%token<String> T_DOLLAR
%token<String> T_BAR
%token<String> T_LBRA
%token<String> T_RBRA
%token<String> T_RCURLY

%token<String> T_IDENTIFIER
%token<String> T_SYMBOL
%token<String> T_INTEGER

%token<String> T_COMPOUND

%token<String> T_ARRAY
%token<String> T_PRIVATE
%token<String> T_PROTECTED
%token<String> T_PUBLIC

%token T_ELEMENT_PARAM
%token T_ELEMENT_RETURN
%token T_ELEMENT_VAR
%token T_ELEMENT_ACCESS
/*
%token T_ELEMENT_INTERNAL
%token T_ELEMENT_PROPERTY
%token T_ELEMENT_PROPERTYREAD
%token T_ELEMENT_PROPERTYWRITE
%token T_ELEMENT_METHOD
*/

%token T_INLINE_LINK
/*
%token T_INLINE_INTERNAL
*/
  
/* Nonterminals */

%type<Object> start
%type<Object> element_block
%type<Object> element
%type<Object> identifier_attribute
%type<Object> access_modifier
%type<Object> compound_attribute
%type<Object> expression_list
%type<Object> expression
%type<Object> type_attribute
%type<Object> type_list
%type<Object> type
%type<String> variable

%% /* Productions */

start:
	compound_attribute element_block
		{ 
			($2 as List<Tuple<DocElementType,DocElement>>).Add(new Tuple<DocElementType,DocElement>(DocElementType.Summary, new DocSummaryElement($1 as DocCompoundAttribute))); 
			$$ = $2; 
			elements = ($2 as List<Tuple<DocElementType,DocElement>>).ToArray(); 
		}
	;
	
element_block:
	element_block element whitespace_option
		{ ($1 as List<Tuple<DocElementType,DocElement>>).Add($2 as Tuple<DocElementType,DocElement>); $$ = $1; }
	|
	/* EMPTY */
		{ $$ = new List<Tuple<DocElementType,DocElement>>(); }
	;
	
element:
	T_ELEMENT_PARAM T_WHITESPACE type_attribute T_WHITESPACE identifier_attribute whitespace_nl compound_attribute
		{ $$ = new Tuple<DocElementType,DocElement>(DocElementType.Param, new DocParamElement($3 as DocTypeAttribute, $5 as DocIdentifierAttribute, $7 as DocCompoundAttribute)); }
	|
	T_ELEMENT_PARAM T_WHITESPACE identifier_attribute whitespace_nl compound_attribute
		{ $$ = new Tuple<DocElementType,DocElement>(DocElementType.Param, new DocParamElement(null, $3 as DocIdentifierAttribute, $5 as DocCompoundAttribute)); }
	|
	T_ELEMENT_RETURN T_WHITESPACE type_attribute whitespace_nl compound_attribute
		{ $$ = new Tuple<DocElementType,DocElement>(DocElementType.Return, new DocReturnElement($3 as DocTypeAttribute, $5 as DocCompoundAttribute)); }
	|
	T_ELEMENT_RETURN whitespace_nl compound_attribute
		{ $$ = new Tuple<DocElementType,DocElement>(DocElementType.Return, new DocReturnElement(null, $3 as DocCompoundAttribute)); }
	|
	T_ELEMENT_VAR T_WHITESPACE type_attribute whitespace_nl compound_attribute
		{ $$ = new Tuple<DocElementType,DocElement>(DocElementType.Var, new DocVarElement($3 as DocTypeAttribute, $5 as DocCompoundAttribute)); }
	|
	T_ELEMENT_ACCESS T_WHITESPACE access_modifier compound_attribute
		{ $$ = new Tuple<DocElementType,DocElement>(DocElementType.Access, new DocAccessElement(new DocAccessModifierAttribute((DocAccessModifier)($3)))); }
	/*
	|
	T_ELEMENT_PROPERTY T_WHITESPACE type_attribute T_WHITESPACE identifier_attribute T_WHITESPACE compound_attribute 
		{ $$ = new Tuple<DocElementType,DocElement>(DocElementType.Property, new DocPropertyElement($3 as DocTypeAttribute, $5 as DocIdentifierAttribute, $7 as DocCompoundAttribute, DocPropertyType.ReadWrite)); }
	|
	T_ELEMENT_PROPERTYREAD T_WHITESPACE type_attribute T_WHITESPACE identifier_attribute T_WHITESPACE compound_attribute 
		{ $$ = new Tuple<DocElementType,DocElement>(DocElementType.Property, new DocPropertyElement($3 as DocTypeAttribute, $5 as DocIdentifierAttribute, $7 as DocCompoundAttribute, DocPropertyType.Read)); }
	|
	T_ELEMENT_PROPERTYWRITE T_WHITESPACE type_attribute T_WHITESPACE identifier_attribute T_WHITESPACE compound_attribute 
		{ $$ = new Tuple<DocElementType,DocElement>(DocElementType.Property, new DocPropertyElement($3 as DocTypeAttribute, $5 as DocIdentifierAttribute, $7 as DocCompoundAttribute, DocPropertyType.Write)); }
	*/
	;
	
whitespace_option:
		whitespace_option T_WHITESPACE
	|	whitespace_option T_NEWLINE
	|	/* EMPTY */
	;

whitespace_nl:
		T_WHITESPACE
	|	T_NEWLINE
	;
	
access_modifier:
	T_PUBLIC
		{ $$ = DocAccessModifier.Public; }
	|
	T_PRIVATE
		{ $$ = DocAccessModifier.Private; }
	|
	T_PROTECTED
		{ $$ = DocAccessModifier.Protected; } 
	;
	
identifier_attribute:
	variable
		{ $$ = new DocIdentifierAttribute($1); }
	;
	
compound_attribute:
	{ CompoundTokens = true; } expression_list { CompoundTokens = false; }
		{ $$ = new DocCompoundAttribute($2 as List<DocExpression>); }
	;
	
expression_list:
	expression_list expression
		{ ($1 as List<DocExpression>).Add($2 as DocExpression); $$ = $1; }
	|
	/* EMPTY */
		{ $$ = new List<DocExpression>(); }
	;
	
expression:
	T_COMPOUND
		{ $$ = new DocTextExpr($1.ToString()); }
	|
	T_INLINE_LINK { CompoundTokens = false; } T_WHITESPACE T_IDENTIFIER { CompoundTokens = true; } T_COMPOUND T_RCURLY
		{ $$ = new DocLinkExpr($3.ToString(), $4.ToString()); }
	;
	
type_attribute:
	type_list
		{ $$ = new DocTypeAttribute($1 as List<DocRawType>); }
	;
	
type_list:
	type_list T_BAR type 
		{ ($1 as List<DocRawType>).Add($3 as DocRawType); $$ = $1; }
	|
	type
		{ $$ = new List<DocRawType>(); ($$ as List<DocRawType>).Add($1 as DocRawType); }
	;
	
type:
	T_IDENTIFIER
		{ $$ = new DocRawTypeIdentifier($1.ToString()); }
	|
	T_ARRAY
		{ $$ = new DocRawTypeArray(null, null); }
	|
	T_ARRAY T_LBRA T_RBRA
		{ $$ = new DocRawTypeArray(null, null); }
	|
	T_ARRAY T_LBRA type T_RBRA
		{ $$ = new DocRawTypeArray($3 as DocRawType, null); }
	|
	T_ARRAY T_LBRA T_RBRA type
		{ $$ = new DocRawTypeArray(null, $4 as DocRawType); }
	|
	T_ARRAY T_LBRA type T_RBRA type
		{ $$ = new DocRawTypeArray($3 as DocRawType, $5 as DocRawType); }
	;
	
variable:
	T_DOLLAR T_IDENTIFIER
		{ $$ = $2; }
	;

%%
