[expect php]
[file]
<?php
	class MyClass {
        public $pub;
        protected $prot;
        private $priv;
        
        function MyClass()
        {
            $this->pub = "pub value";
            $this->prot = "prot value";
            $this->priv = "priv value";
        }
        
        function PrintMe()
        {
            echo "\$pub = ".$this->pub."<br />";
            echo "\$prot = ".$this->prot."<br />";
            echo "\$priv = ".$this->priv."<br />";
        }
	}
	
    class MyDerivedClass extends MyClass {
        public $pub_new;
        protected $prot_new;
        private $priv_new;
        
        function MyDerivedClass()
        {
            parent::__construct();
            $this->pub_new = "pub_new value";
            $this->prot_new = "prot_new value";
            $this->priv_new = "priv_new value";
        }
        
        function PrintMe()
        {
            parent::PrintMe();
            echo "\$pub_new = ".$this->pub_new."<br />";
            echo "\$prot_new = ".$this->prot_new."<br />";
            echo "\$priv_new = ".$this->priv_new."<br />";
        }
	}
	
	$a = new MyDerivedClass();

    $a->PrintMe();
	$data = serialize($a);
	
	echo "<br />";
	
	$b = unserialize($data);
	$b->PrintMe();
?>