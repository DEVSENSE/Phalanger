<? //© Ðonny 2009 - Part of Phalanger project
    import namespace System:::Windows:::Forms;
    import namespace System;
    import namespace RegEditPHP;

    namespace RegEditPHP{
        ///<summary>Gives easy access to localized resources</summary>
	    class ResourceAccessor{
	        //Resource manager
	        private $manager;
	        //Culture (not used, null) when null; CurrentUICulture is used by manager
	        public $Culture;
	        ///<summary>CTor</summary>
	        ///<param name="ResourceName">Name of resx file under which it is embdeded</param>
            public function __construct(string $ResourceName){
                $MyType = CLRTypeOf ResourceAccessor;
                $this->manager = new System:::Resources:::ResourceManager($ResourceName,$MyType->Assembly);
             }
             //gets resource
             public function __get(string $name){
                $rs = $this->manager->GetObject($name,$this->Culture);
                return $rs;
             }
             public function __set(string $name, $value){
                throw new CLRException(new InvalidOperationException(Program::$Resources->e_SetResourceValue));
             }
             public function __call(string $name, array $arguments  ){
                return System:::String::Format($this->$name,$arguments);
             }
	    }    
    }
?>