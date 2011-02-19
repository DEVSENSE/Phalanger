[expect php]
[file]
<?
class FooBar { static function error() { echo "error"; exit(); } }
set_error_handler(array('FooBar', 'error'));
include('foobar.php');
?>