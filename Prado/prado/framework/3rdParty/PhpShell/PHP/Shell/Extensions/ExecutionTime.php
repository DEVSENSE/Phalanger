<?php

class PHP_Shell_Extensions_ExecutionTime implements PHP_Shell_Extension {
    protected $show_exectime = false;

    protected $parse_time;
    protected $exec_time;
    protected $end_time;

    public function register() {
        $opt = PHP_Shell_Options::getInstance();

        $opt->registerOption("exectime", $this, "optSetExecTime");
    }

    public function optSetExecTime($key, $val) {
        switch ($val) {
        case "enable":
        case "1":
        case "on":
            $this->show_exectime = true;
            break;
        case "disable":
        case "0":
        case "off":
            $this->show_exectime = false;
            break;
        default:
            printf(":set %s failed, unknown value. Use :set %s = (on|off)", $key, $key);
            break;
        }
    }

    public function startParseTime() {
        $this->parse_time = microtime(1);
        $this->exec_time = 0.0;
    }
    public function startExecTime() {
        $this->exec_time = microtime(1);
    }
    public function stopTime() {
        $this->end_time = microtime(1);
    }

    public function getParseTime() {
        return ($this->exec_time == 0.0 ? $this->end_time : $this->exec_time) - $this->parse_time;
    }
 
    public function getExecTime() {
        return ($this->exec_time == 0.0 ? 0.0 : $this->end_time - $this->exec_time);
    }
   
    public function isShow() {
        return $this->show_exectime;
    }
}
