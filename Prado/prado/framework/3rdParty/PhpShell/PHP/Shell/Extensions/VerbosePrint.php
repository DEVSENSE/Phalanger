<?php
class PHP_Shell_Extensions_VerbosePrint implements PHP_Shell_Extension {
    protected $opt_verbose = false;
    protected $oneshot_verbose = false;

    public function register() {
/*        $cmd = PHP_Shell_Commands::getInstance();
        $cmd->registerCommand('#^p #', $this, 'cmdPrint', 'p <var>', 'print the variable verbosly');

        $opt = PHP_Shell_Options::getInstance();
        $opt->registerOption('verboseprint', $this, 'optSetVerbose');
*/
    }

    /**
    * handle the 'p ' command
    *
    * set the verbose flag
    *
    * @return string the pure command-string without the 'p ' command
    */
    public function cmdPrint($l) {
        $this->oneshot_verbose = true;

        $cmd = substr($l, 2);

        return $cmd;
    }

    public function optSetVerbose($key, $val) {
        switch($val) {
        case "false":
        case "on":
        case "1":
            $this->opt_verbose = true;
        default:
            $this->opt_verbose = false;
            break;
        }
    }

    /**
    * check if we have a verbose print-out
    *
    * @return bool 1 if verbose, 0 otherwise
    */
    public function isVerbose() {
        $v = $this->opt_verbose || $this->oneshot_verbose;

        $this->oneshot_verbose = false;

        return $v;
    }
}


