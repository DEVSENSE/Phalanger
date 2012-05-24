<?php

class PHP_Shell_Extensions_Colour implements PHP_Shell_Extension {
    static protected $instance;
    # shell colours
    const C_RESET = "\033[0m";

    const C_BLACK = "\033[0;30m";
    const C_RED = "\033[0;31m";
    const C_GREEN = "\033[0;32m";
    const C_BROWN = "\033[0;33m";
    const C_BLUE = "\033[0;34m";
    const C_PURPLE = "\033[0;35m";
    const C_CYAN = "\033[0;36m";
    const C_LIGHT_GRAY = "\033[0;37m";

    const C_GRAY = "\033[1;30m";
    const C_LIGHT_RED = "\033[1;31m";
    const C_LIGHT_GREEN = "\033[1;32m";
    const C_YELLOW = "\033[1;33m";
    const C_LIGHT_BLUE = "\033[1;34m";
    const C_LIGHT_PURPLE = "\033[1;35m";
    const C_LIGHT_CYAN = "\033[1;36m";
    const C_WHITE = "\033[1;37m";

    /**
    * shell colours
    *
    * @var array
    * @see applyColourScheme
    */
    protected $colours;

    /**
    * shell colour schemes
    *
    * @var array
    * @see registerColourScheme
    */
    protected $colour_scheme;

    public function register() {
        $opt = PHP_Shell_Options::getInstance();

        $opt->registerOption("background", $this, "optSetBackground");
        $opt->registerOptionAlias("bg", "background");

        $this->registerColourScheme(
            "plain", array( 
                "default"   => "", "value"     => "",
                "exception" => "", "reset"     => ""));

        $this->registerColourScheme(
            "dark", array( 
                "default"   => self::C_YELLOW, 
                "value"     => self::C_WHITE,
                "exception" => self::C_PURPLE));

        $this->registerColourScheme(
            "light", array( 
                "default"   => self::C_BLACK, 
                "value"     => self::C_BLUE,
                "exception" => self::C_RED));

    }

    /**
    * background colours
    */
    public function optSetBackground($key, $value) {
        if (is_null($value)) {
            print(':set '.$key.' needs a colour-scheme, e.g. :set '.$key.'=dark');
            return;
        }
        if (false == $this->applyColourScheme($value)) {
            print('setting colourscheme failed: colourscheme '.$value.' is unknown');
            return;
        }
    }

    /**
    * get a colour for the shell
    *
    * @param string $type one of (value|exception|reset|default)
    * @return string a colour string or a empty string
    */
    public function getColour($type) {
        return isset($this->colour[$type]) ? $this->colour[$type] : '';
    }

    /**
    * apply a colour scheme to the current shell
    *
    * @param string $scheme name of the scheme
    * @return false if colourscheme is not known, otherwise true
    */
    public function applyColourScheme($scheme) {
        if (!isset($this->colour_scheme[$scheme])) return false;

        $this->colour = $this->colour_scheme[$scheme];

        return true;
    }

    /**
    * registers a colour scheme
    *
    * @param string $scheme name of the colour scheme
    * @param array a array of colours
    */
    public function registerColourScheme($scheme, $colours) {
        if (!is_array($colours)) return;

        /* set a reset colour if it is not supplied from the outside */
        if (!isset($colours["reset"])) $colours["reset"] = self::C_RESET;

        $this->colour_scheme[$scheme] = $colours;
    }
}

