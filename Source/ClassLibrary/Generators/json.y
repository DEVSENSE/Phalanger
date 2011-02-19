using PHP.Core;
using PHP.Core.Parsers.GPPG;
using System.Diagnostics;

%%

%namespace PHP.Library.Json
%valuetype SemanticValueType
%positiontype Position
%tokentype Tokens
%visibility public

%union
{
	public object obj; 
}

%token ARRAY_OPEN
%token ARRAY_CLOSE
%token ITEMS_SEPARATOR
%token NAMEVALUE_SEPARATOR
%token OBJECT_OPEN
%token OBJECT_CLOSE
%token TRUE
%token FALSE
%token NULL
%token INTEGER
%token DOUBLE
%token STRING

%token STRING_BEGIN
%token CHARS
%token UNICODECHAR
%token ESCAPEDCHAR
%token STRING_END
	   
%% /* Productions */

start:
	  value	{ Result = $1.obj; }
;

object:
		OBJECT_OPEN members OBJECT_CLOSE
		{
			var elements = (List<KeyValuePair<string, object>>)$2.obj;
				
			if (decodeOptions.Assoc)
			{
				var arr = new PhpArray( elements.Count );
				
				foreach (var item in elements)
					arr.Add( PHP.Core.Convert.StringToArrayKey(item.Key), item.Value );
					
				$$.obj = arr;
			}
			else
			{
				var std = new stdClass(context, true);
				std.AddRange( elements );
				$$.obj = std;
			}
		}
	|	OBJECT_OPEN OBJECT_CLOSE	{ $$.obj = new stdClass(context, true); }
	;
	
members:
		pair ITEMS_SEPARATOR members
		{
			var elements = (List<KeyValuePair<string, object>>)$3.obj;
			var result = new List<KeyValuePair<string, object>>( elements.Count + 1 ){ (KeyValuePair<string,object>)$1.obj };
			result.AddRange(elements);			
			$$.obj = result;
		}
	|	pair	{ $$.obj = new List<KeyValuePair<string, object>>(){ (KeyValuePair<string,object>)$1.obj }; }
	;
	
pair:
		STRING NAMEVALUE_SEPARATOR value	{ $$.obj = new KeyValuePair<string,object>((string)$1.obj, $3.obj); }
	;
	
array:
		ARRAY_OPEN elements ARRAY_CLOSE
		{
			var elements = (List<object>)$2.obj;
			var arr = new PhpArray( elements.Count );
			
			foreach (var item in elements)
				arr.Add( item );
				
			$$.obj = arr;
		}
	|	ARRAY_OPEN ARRAY_CLOSE	{ $$.obj = new PhpArray(); }
	;
	
elements:
		value ITEMS_SEPARATOR elements
		{
			var elements = (List<object>)$3.obj;
			var result = new List<object>( elements.Count + 1 ){ $1.obj };
			result.AddRange(elements);			
			$$.obj = result;
		}
	|	value { $$.obj = new List<object>(){ $1.obj }; }
	;
	
value:
		STRING	{$$.obj = $1.obj;}
	|	INTEGER	{$$.obj = $1.obj;}
	|	DOUBLE	{$$.obj = $1.obj;}
	|	object	{$$.obj = $1.obj;}
	|	array	{$$.obj = $1.obj;}
	|	TRUE	{$$.obj = true;}
	|	FALSE	{$$.obj = false;}
	|	NULL	{$$.obj = null;}
	;

%%

protected override int EofToken { get { return (int)Tokens.EOF; } }
protected override int ErrorToken { get { return (int)Tokens.ERROR; } }

private readonly ScriptContext/*!*/context;
private readonly PHP.Library.JsonFormatter.DecodeOptions/*!*/decodeOptions;

public Parser(ScriptContext/*!*/context, PHP.Library.JsonFormatter.DecodeOptions/*!*/decodeOptions)
{
	System.Diagnostics.Debug.Assert(context != null && decodeOptions != null);
	
	this.context = context;
	this.decodeOptions = decodeOptions;
}

public object Result{get;private set;}