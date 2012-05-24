<?php

require_once(dirname(__FILE__)."/Extensions.php"); /* for the PHP_Shell_Interface */

/**
*
*/
class PHP_Shell_Options implements PHP_Shell_Extension {
    /*
    * instance of the current class
    *
    * @var PHP_Shell_Options
    */
    static protected $instance;

    /**
    * known options and their setors
    *
    * @var array
    * @see registerOption
    */
    protected $options = array();

    /**
    * known options and their setors
    *
    * @var array
    * @see registerOptionAlias
    */
    protected $option_aliases = array();

    public function register() {
//        $cmd = PHP_Shell_Commands::getInstance();
 //       $cmd->registerCommand('#^:set #', $this, 'cmdSet', ':set <var>', 'set a shell variable');
    }

    /**
    * register a option
    *
    * @param string name of the option
    * @param object a object handle
    * @param string method-name of the setor in the object
    * @param string (unused)
    */
    public function registerOption($option, $obj, $setor, $getor = null) {
        if (!method_exists($obj, $setor)) {
            throw new Exception(sprintf("setor %s doesn't exist on class %s", $setor, get_class($obj)));
        }

        $this->options[trim($option)] = array("obj" => $obj, "setor" => $setor);
    }

    /**
    * set a shell-var
    *
    * :set al to enable autoload
    * :set bg=dark to enable highlighting with a dark backgroud
    */
    public function cmdSet($l) {
        if (!preg_match('#:set\s+([a-z]+)\s*(?:=\s*([a-z0-9]+)\s*)?$#i', $l, $a)) {
            print(':set failed: either :set <option> or :set <option> = <value>');
            return;
        }

        $this->execute($a[1], isset($a[2]) ? $a[2] : null);
    }

    /**
    * get all the option names
    *
    * @return array names of all options
    */
    public function getOptions() {
        return array_keys($this->options);
    }

    /**
    * map a option to another option
    *
    * e.g.: bg maps to background
    *
    * @param string alias
    * @param string option
    */
    public function registerOptionAlias($alias, $option) {
        if (!isset($this->options[$option])) {
            throw new Exception(sprintf("Option %s is not known", $option));
        }

        $this->option_aliases[trim($alias)] = trim($option);

    }

    /**
    * execute a :set command
    *
    * calls the setor for the :set <option>
    *
    *
    */
    private function execute($key, $value) {
        /* did we hit a alias (bg for backgroud) ? */
        if (isset($this->option_aliases[$key])) {
            $opt_key = $this->option_aliases[$key];
        } else {
            $opt_key = $key;
        }

        if (!isset($this->options[$opt_key])) {
            print (':set '.$key.' failed: unknown key');
            return;
        }

        if (!isset($this->options[$opt_key]["setor"])) {
            return;
        }

        $setor = $this->options[$opt_key]["setor"];
        $obj = $this->options[$opt_key]["obj"];
        $obj->$setor($key, $value);
    }

    static function getInstance() {
        if (is_null(self::$instance)) {
            $class = __CLASS__;
            self::$instance = new $class();
        }
        return self::$instance;
    }
}


