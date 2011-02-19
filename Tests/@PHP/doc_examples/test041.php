[expect] hello world!
[expect] call_user_func():
[expect] should not be called statically
[expect] Hello World!Hello World!

[file]
<?php 

// simple callback example
function my_callback_function() {
    echo 'hello world!';
}
call_user_func('my_callback_function'); 

// method callback examples
class MyClass {
    function myCallbackMethod() {
        echo 'Hello World!';
    }
}

// static class method call without instantiating an object
call_user_func(array('MyClass', 'myCallbackMethod')); 

// object method call
$obj = new MyClass();
call_user_func(array(&$obj, 'myCallbackMethod'));
?>
