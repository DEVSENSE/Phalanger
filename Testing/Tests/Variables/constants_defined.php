[expect php]
[file]
<?  
eval('class A
{

}');
	var_dump(constant('KONSTANTA')); // Warning: constant(): Couldn't find constant KONSTANTA
	var_dump(KONSTANTA);				// Notice: Use of undefined constant KONSTANTA - assumed 'KONSTANTA'
	//echo constant('A::X'); 	// Fatal error: Undefined class constant 'A::X'
	//echo A::X;				// Fatal error: Undefined class constant 'X'
	//echo constant('X::A');	// Fatal error: Class 'X' not found
	//echo X::A; 				// Fatal error: Class 'X' not found
?>