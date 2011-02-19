<?
//import namespace ;

$nl = new CSharpNullableLib:::NullableTests();

$nl->IntNull = 10;
$nl->DoubleNull = 3.14159265;
$nl->BoolNull = true;
$nl->Print();

$nl->IntNull = $nl->DoubleNull;
$nl->Print();

$nl->DoubleNull = null;
$nl->IntNull = $nl->DoubleNull;
$nl->Print();

fgets(STDIN);

?>