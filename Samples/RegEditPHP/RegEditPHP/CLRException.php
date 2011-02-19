<? //© Ðonny 2009 - Part of Phalanger project
    namespace RegEditPHP{
        //PHPBUG:
        ///<summary>Wraps any <see cref="System:::Exception"/> as Phalanger <see cref="Exception"/></summary>
        ///<remarks>Purpose of having such class is actual inability of Phalanger to throw Clr exceptions (though they can be caught)</remarks>
        final class CLRException extends Exception{
            ///<summary><see cref="System:::Exception"/> being encapsulated</summary>
            private $ex;
            ///<summary>Creates new instance of the <see cref="CLRExceprion"/> class</summary>
            ///<param name="ex">The <see cref="System:::Exception"/> to wrap</param>
	        function __construct(System:::Exception $ex){
	            parent::__construct();
	            $this->ex = $ex;
                $this->message = $ex->Message;
	        }
	        ///<summary>Gets exception wrapped by this instance</summary>
	        ///<returns type="System:::Exception">Exception wrapped by this instance</returns>
	        public function GetException(){
	            return $this->ex;
	        }
	        ///<summary>PHP magic method to get dynamic property</summary>
	        ///<remarks>Defined properties<list type="bullet"><item><b>Exception</b> (<see cref="System:::Exception"/>)</item></list></remarks>
	        public function __get(string $name){
	            //Note this implementation is not very correct because there is no __set (and even __isset and __unset) method.
	            //So, you are allowed set/unset $Exception byt it does not get set/unset.
	            //isset return false (unless you've set $Exception) but __get returns non-null.
	            if($name=="Exception") return $this->ex;
	            return null;
	        }
        }
    }
?>