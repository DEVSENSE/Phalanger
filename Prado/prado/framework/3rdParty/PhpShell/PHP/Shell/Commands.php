<?php

/**
* Commands for the PHP_Shell
*
* Extensions can register their own commands for the shell like the
* InlineHelp Extension which provides inline help for all functions
*
* It uses the pattern '? <string>' to catch the cmdline strings.
*
* registerCommand() should be called by the extensions in the register()
* method. Its parameters are
* - the regex which matches the command
* - the object and the method to call if the command is matched
* - the human readable command string and the description for the help
*/
class PHP_Shell_Commands {
    /*
    * instance of the current class
    *
    * @var PHP_Shell_Commands
    */
    static protected $instance;

    /**
    * registered commands
    *
    * array('quit' => ... )
    *
    * @var array
    * @see registerCommand
    */
    protected $commands = array();

    /**
    * register your own command for the shell
    *
    * @param string $regex a regex to match against the input line
    * @param string $obj a Object
    * @param string $method a method in the object to call of the regex matches
    * @param string $cmd the command string for the help
    * @param string $help the full help description for this command
    */
    public function registerCommand($regex, $obj, $method, $cmd, $help) {
        $this->commands[] = array(
            'regex' => $regex,
            'obj' => $obj,
            'method' => $method,
            'command' => $cmd,
            'description' => $help
        );
    }

    /**
    * return a copy of the commands array
    *
    * @return all commands
    */
    public function getCommands() {
        return $this->commands;
    }

    static function getInstance() {
        if (is_null(self::$instance)) {
            $class = __CLASS__;
            self::$instance = new $class();
        }
        return self::$instance;
    }
}


