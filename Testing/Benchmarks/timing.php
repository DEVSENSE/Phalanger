<?php

	class Timing
	{
		private static $start;
		private static $descr;
        private static $results = array();
		
        private static $configurationName;
        private static function ConfigurationName()
        {
            if (empty(self::$configurationName)) {
                if (defined("PHALANGER")) {
                    @date_default_timezone_set('UTC');
                    self::$configurationName = "Phalanger/" . (System\Environment::$Is64BitProcess ? "x64" : "x86");
                    self::$configurationName .= "/" . date("Ymd");    // TODO: changeset number (.rev.build)
                } else {
                    self::$configurationName = "PHP " . PHP_VERSION;
                }
                //self::$configurationName .= strtoupper(php_uname('n'));
            }
            return self::$configurationName;
        }
        

		private static function GetTicks()
		{
			return microtime(true);
		}

        static function Start($descr)
		{
			self::$descr = $descr;
			self::$start = self::GetTicks();

            echo $descr . ";";
		}
		
		static function Stop()
		{
			$time = self::GetTicks() - self::$start;

            // update results for this test
            $testresult = @self::$results[self::$descr] ?: array();
            if (empty($testresult))
            {
			    $testresult['descr'] = self::$descr;
                $testresult['time'] = $time;
            }
            else
            {
                $testresult['time'] = min($time, $testresult['time']);
            }

            //
			echo " " . $time . "\n";

            // update results
            self::$results[self::$descr] = $testresult;	
		}

        static function OutputResults()
        {
            $argv = $GLOBALS['argv'];
            if (count($argv) > 1)
            {
                $fname = $argv[1];

                // Test;        configuration1; configuration2; ...
                // <descr>;     <time1>;        <time2>;        ...
                // <descr>;     <time1>;        <time2>;        ...
                // <descr>;     <time1>;        <time2>;        ...
                
                // merge with results already in the CSV file:
                $header = array("Test");
                $table = array();
                
                $first = true;
                if (($f = @fopen($fname, "r")) !== FALSE) {
                    while (($data = fgetcsv($f, 0, ";")) !== FALSE) {
                        if ($first) { // this is header
                            $header = $data;
                            $first = false;
                        }
                        else
                            $table[] = $data;
                    }
                    fclose($f);
                }
                
                // find current configuration within $header, or add it
                $column = array_search(self::ConfigurationName(), $header);
                if (!$column) {
                    // add column since current configuration is not listed yet
                    $column = count($header);
                    $header[] = self::ConfigurationName();
                }

                // update $table:
                foreach (self::$results as $result) {
                    // find test line:
                    for ($line = 0; $line < count($table); $line ++)
                        if ($table[$line][0] == $result['descr'])
                            break;
                    if ($line == count($table))
                        $table[] = array($result['descr']);

                    // update test time for current configuration:
                    $oldtime = (double)@$table[$line][$column];
                    $time = (double)$result['time'];
                    if ($oldtime <= 0.0 || $oldtime > $time)
                        $table[$line][$column] = $time;
                }

                // output results
                $f = fopen($fname, "w");
                // write header
                fwrite($f, implode(";", $header) . "\n");
                // write table
                foreach ($table as $row)
                {
                    //fwrite($f, implode(";", $row));
                    for ($i = 0; $i < count($header); $i++) {
                        if ($i > 0) fwrite( $f, ";");
                        fwrite($f, @$row[$i]);
                    }
                    fwrite($f, "\n");
                }

                fclose($f);
            }
        }
	}

?>
