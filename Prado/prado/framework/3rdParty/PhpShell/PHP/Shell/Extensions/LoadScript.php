<?php

class PHP_Shell_Extensions_LoadScript implements PHP_Shell_Extension {
    public function register() {
        $cmd = PHP_Shell_Commands::getInstance();

        $cmd->registerCommand('#^r #', $this, 'cmdLoadScript', 'r <filename>', 
            'load a php-script and execute each line');

    }

    public function cmdLoadScript($l) {
        $l = substr($l, 2);

        if (file_exists($l)) {
            $content = file($l);

            $source = array();

            foreach ($content as $line) {
                $line = chop($line);

                if (preg_match('#^<\?php#', $line)) continue;

                $source[] = $line;
            }

            return $source;
        }
        return "";
    }
}
