<?php

/**
 * Prado command line developer tools.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: prado-cli.php 2498 2008-08-18 00:28:11Z knut $
 */

if(!isset($_SERVER['argv']) || php_sapi_name()!=='cli')
	die('Must be run from the command line');

require_once(dirname(__FILE__).'/prado.php');

//stub application class
class PradoShellApplication extends TApplication
{
	public function run()
	{
		$this->initApplication();
	}
}

restore_exception_handler();

//config PHP shell
if(count($_SERVER['argv']) > 1 && strtolower($_SERVER['argv'][1])==='shell')
{
	function __shell_print_var($shell,$var)
	{
		if(!$shell->has_semicolon) echo Prado::varDump($var);
	}
	include_once(dirname(__FILE__).'/3rdParty/PhpShell/php-shell-init.php');
}


//register action classes
PradoCommandLineInterpreter::getInstance()->addActionClass('PradoCommandLineCreateProject');
PradoCommandLineInterpreter::getInstance()->addActionClass('PradoCommandLineCreateTests');
PradoCommandLineInterpreter::getInstance()->addActionClass('PradoCommandLinePhpShell');
PradoCommandLineInterpreter::getInstance()->addActionClass('PradoCommandLineUnitTest');
PradoCommandLineInterpreter::getInstance()->addActionClass('PradoCommandLineActiveRecordGen');

//run it;
PradoCommandLineInterpreter::getInstance()->run($_SERVER['argv']);

/**************** END CONFIGURATION **********************/

/**
 * PradoCommandLineInterpreter Class
 *
 * Command line interface, configures the action classes and dispatches the command actions.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: prado-cli.php 2498 2008-08-18 00:28:11Z knut $
 * @since 3.0.5
 */
class PradoCommandLineInterpreter
{
	/**
	 * @var array command action classes
	 */
	protected $_actions=array();

	/**
	 * @param string action class name
	 */
	public function addActionClass($class)
	{
		$this->_actions[$class] = new $class;
	}

	/**
	 * @return PradoCommandLineInterpreter static instance
	 */
	public static function getInstance()
	{
		static $instance;
		if($instance === null)
			$instance = new self;
		return $instance;
	}

	/**
	 * Dispatch the command line actions.
	 * @param array command line arguments
	 */
	public function run($args)
	{
		echo "Command line tools for Prado ".Prado::getVersion().".\n";

		if(count($args) > 1)
			array_shift($args);
		$valid = false;
		foreach($this->_actions as $class => $action)
		{
			if($action->isValidAction($args))
			{
				$valid |= $action->performAction($args);
				break;
			}
			else
			{
				$valid = false;
			}
		}
		if(!$valid)
			$this->printHelp();
	}

	/**
	 * Print command line help, default action.
	 */
	public function printHelp()
	{
		echo "usage: php prado-cli.php action <parameter> [optional]\n";
		echo "example: php prado-cli.php -c mysite\n\n";
		echo "actions:\n";
		foreach($this->_actions as $action)
			echo $action->renderHelp();
	}
}

/**
 * Base class for command line actions.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: prado-cli.php 2498 2008-08-18 00:28:11Z knut $
 * @since 3.0.5
 */
abstract class PradoCommandLineAction
{
	/**
	 * Execute the action.
	 * @param array command line parameters
	 * @return boolean true if action was handled
	 */
	public abstract function performAction($args);

	protected function createDirectory($dir, $mask)
	{
		if(!is_dir($dir))
		{
			mkdir($dir);
			echo "creating $dir\n";
		}
		if(is_dir($dir))
			chmod($dir, $mask);
	}

	protected function createFile($filename, $content)
	{
		if(!is_file($filename))
		{
			file_put_contents($filename, $content);
			echo "creating $filename\n";
		}
	}

	public function isValidAction($args)
	{
		return strtolower($args[0]) === $this->action &&
				count($args)-1 >= count($this->parameters);
	}

	public function renderHelp()
	{
		$params = array();
		foreach($this->parameters as $v)
			$params[] = '<'.$v.'>';
		$parameters = join($params, ' ');
		$options = array();
		foreach($this->optional as $v)
			$options[] = '['.$v.']';
		$optional = (strlen($parameters) ? ' ' : ''). join($options, ' ');
		$description='';
		foreach(explode("\n", wordwrap($this->description,65)) as $line)
			$description .= '    '.$line."\n";
		return <<<EOD
  {$this->action} {$parameters}{$optional}
{$description}

EOD;
	}

	protected function initializePradoApplication($directory)
	{
		$app_dir = realpath($directory.'/protected/');
		if($app_dir !== false && is_dir($app_dir))
		{
			if(Prado::getApplication()===null)
			{
				$app = new PradoShellApplication($app_dir);
				$app->run();
				$dir = substr(str_replace(realpath('./'),'',$app_dir),1);
				$initialized=true;
				echo '** Loaded PRADO appplication in directory "'.$dir."\".\n";
			}

			return Prado::getApplication();
		}
		else
		{
			echo '+'.str_repeat('-',77)."+\n";
			echo '** Unable to load PRADO application in directory "'.$directory."\".\n";
			echo '+'.str_repeat('-',77)."+\n";
		}
		return false;
	}

}

/**
 * Create a Prado project skeleton, including directories and files.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: prado-cli.php 2498 2008-08-18 00:28:11Z knut $
 * @since 3.0.5
 */
class PradoCommandLineCreateProject extends PradoCommandLineAction
{
	protected $action = '-c';
	protected $parameters = array('directory');
	protected $optional = array();
	protected $description = 'Creates a Prado project skeleton for the given <directory>.';

	public function performAction($args)
	{
		$this->createNewPradoProject($args[1]);
		return true;
	}

	/**
	 * Functions to create new prado project.
	 */
	protected function createNewPradoProject($dir)
	{
		if(strlen(trim($dir)) == 0)
			return;

		$rootPath = realpath(dirname(trim($dir)));

		if(basename($dir)!=='.')
			$basePath = $rootPath.DIRECTORY_SEPARATOR.basename($dir);
		else
			$basePath = $rootPath;
		$appName = basename($basePath);
		$assetPath = $basePath.DIRECTORY_SEPARATOR.'assets';
		$protectedPath  = $basePath.DIRECTORY_SEPARATOR.'protected';
		$runtimePath = $basePath.DIRECTORY_SEPARATOR.'protected'.DIRECTORY_SEPARATOR.'runtime';
		$pagesPath = $protectedPath.DIRECTORY_SEPARATOR.'pages';

		$indexFile = $basePath.DIRECTORY_SEPARATOR.'index.php';
		$htaccessFile = $protectedPath.DIRECTORY_SEPARATOR.'.htaccess';
		$configFile = $protectedPath.DIRECTORY_SEPARATOR.'application.xml';
		$defaultPageFile = $pagesPath.DIRECTORY_SEPARATOR.'Home.page';

		$this->createDirectory($basePath, 0755);
		$this->createDirectory($assetPath,0777);
		$this->createDirectory($protectedPath,0755);
		$this->createDirectory($runtimePath,0777);
		$this->createDirectory($pagesPath,0755);

		$this->createFile($indexFile, $this->renderIndexFile());
		$this->createFile($configFile, $this->renderConfigFile($appName));
		$this->createFile($htaccessFile, $this->renderHtaccessFile());
		$this->createFile($defaultPageFile, $this->renderDefaultPage());
	}

	protected function renderIndexFile()
	{
		$framework = realpath(dirname(__FILE__)).DIRECTORY_SEPARATOR.'prado.php';
return '<?php

$frameworkPath=\''.$framework.'\';

// The following directory checks may be removed if performance is required
$basePath=dirname(__FILE__);
$assetsPath=$basePath.\'/assets\';
$runtimePath=$basePath.\'/protected/runtime\';

if(!is_file($frameworkPath))
	die("Unable to find prado framework path $frameworkPath.");
if(!is_writable($assetsPath))
	die("Please make sure that the directory $assetsPath is writable by Web server process.");
if(!is_writable($runtimePath))
	die("Please make sure that the directory $runtimePath is writable by Web server process.");


require_once($frameworkPath);

$application=new TApplication;
$application->run();

?>';
	}

	protected function renderConfigFile($appName)
	{
		return <<<EOD
<?xml version="1.0" encoding="utf-8"?>

<application id="$appName" mode="Debug">
  <!-- alias definitions and namespace usings
  <paths>
    <alias id="myalias" path="./lib" />
    <using namespace="Application.common.*" />
  </paths>
  -->

  <!-- configurations for modules -->
  <modules>
    <!-- Remove this comment mark to enable caching
    <module id="cache" class="System.Caching.TDbCache" />
    -->

    <!-- Remove this comment mark to enable PATH url format
    <module id="request" class="THttpRequest" UrlFormat="Path" />
    -->

    <!-- Remove this comment mark to enable logging
    <module id="log" class="System.Util.TLogRouter">
      <route class="TBrowserLogRoute" Categories="System" />
    </module>
    -->
  </modules>

  <!-- configuration for available services -->
  <services>
    <service id="page" class="TPageService" DefaultPage="Home" />
  </services>

  <!-- application parameters
  <parameters>
    <parameter id="param1" value="value1" />
    <parameter id="param2" value="value2" />
  </parameters>
  -->
</application>
EOD;
	}

	protected function renderHtaccessFile()
	{
		return 'deny from all';
	}


	protected function renderDefaultPage()
	{
return <<<EOD
<html>
<head>
  <title>Welcome to PRADO</title>
</head>
<body>
<h1>Welcome to PRADO!</h1>
</body>
</html>
EOD;
	}
}

/**
 * Creates test fixtures for a Prado application.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: prado-cli.php 2498 2008-08-18 00:28:11Z knut $
 * @since 3.0.5
 */
class PradoCommandLineCreateTests extends PradoCommandLineAction
{
	protected $action = '-t';
	protected $parameters = array('directory');
	protected $optional = array();
	protected $description = 'Create test fixtures in the given <directory>.';

	public function performAction($args)
	{
		$this->createTestFixtures($args[1]);
		return true;
	}

	protected function createTestFixtures($dir)
	{
		if(strlen(trim($dir)) == 0)
			return;

		$rootPath = realpath(dirname(trim($dir)));
		$basePath = $rootPath.'/'.basename($dir);

		$tests = $basePath.'/tests';
		$unit_tests = $tests.'/unit';
		$functional_tests = $tests.'/functional';

		$this->createDirectory($tests,0755);
		$this->createDirectory($unit_tests,0755);
		$this->createDirectory($functional_tests,0755);

		$unit_test_index = $tests.'/unit.php';
		$functional_test_index = $tests.'/functional.php';

		$this->createFile($unit_test_index, $this->renderUnitTestFixture());
		$this->createFile($functional_test_index, $this->renderFunctionalTestFixture());
	}

	protected function renderUnitTestFixture()
	{
		$tester = realpath(dirname(__FILE__).'/../tests/test_tools/unit_tests.php');
return '<?php

include_once \''.$tester.'\';

$app_directory = "../protected";
$test_cases = dirname(__FILE__)."/unit";

$tester = new PradoUnitTester($test_cases, $app_directory);
$tester->run(new HtmlReporter());

?>';
	}

	protected function renderFunctionalTestFixture()
	{
		$tester = realpath(dirname(__FILE__).'/../tests/test_tools/functional_tests.php');
return '<?php

include_once \''.$tester.'\';

$test_cases = dirname(__FILE__)."/functional";

$tester=new PradoFunctionalTester($test_cases);
$tester->run(new SimpleReporter());

?>';
	}

}

/**
 * Creates and run a Prado application in a PHP Shell.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: prado-cli.php 2498 2008-08-18 00:28:11Z knut $
 * @since 3.0.5
 */
class PradoCommandLinePhpShell extends PradoCommandLineAction
{
	protected $action = 'shell';
	protected $parameters = array();
	protected $optional = array('directory');
	protected $description = 'Runs a PHP interactive interpreter. Initializes the Prado application in the given [directory].';

	public function performAction($args)
	{
		if(count($args) > 1)
			$app = $this->initializePradoApplication($args[1]);
		return true;
	}
}

/**
 * Runs unit test cases.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: prado-cli.php 2498 2008-08-18 00:28:11Z knut $
 * @since 3.0.5
 */
class PradoCommandLineUnitTest extends PradoCommandLineAction
{
	protected $action = 'test';
	protected $parameters = array('directory');
	protected $optional = array('testcase ...');
	protected $description = 'Runs all unit test cases in the given <directory>. Use [testcase] option to run specific test cases.';

	protected $matches = array();

	public function performAction($args)
	{
		$dir = realpath($args[1]);
		if($dir !== false && is_dir($dir))
			$this->runUnitTests($dir,$args);
		else
			echo '** Unable to find directory "'.$args[1]."\".\n";
		return true;
	}

	protected function initializeTestRunner()
	{
		$TEST_TOOLS = realpath(dirname(__FILE__).'/../tests/test_tools/');

		require_once($TEST_TOOLS.'/simpletest/unit_tester.php');
		require_once($TEST_TOOLS.'/simpletest/web_tester.php');
		require_once($TEST_TOOLS.'/simpletest/mock_objects.php');
		require_once($TEST_TOOLS.'/simpletest/reporter.php');
	}

	protected function runUnitTests($dir, $args)
	{
		$app_dir = $this->getAppDir($dir);
		if($app_dir !== false)
			$this->initializePradoApplication($app_dir.'/../');

		$this->initializeTestRunner();
		$test_dir = $this->getTestDir($dir);
		if($test_dir !== false)
		{
			$test =$this->getUnitTestCases($test_dir,$args);
			$running_dir = substr(str_replace(realpath('./'),'',$test_dir),1);
			echo 'Running unit tests in directory "'.$running_dir."\":\n";
			$test->run(new TextReporter());
		}
		else
		{
			$running_dir = substr(str_replace(realpath('./'),'',$dir),1);
			echo '** Unable to find test directory "'.$running_dir.'/unit" or "'.$running_dir.'/tests/unit".'."\n";
		}
	}

	protected function getAppDir($dir)
	{
		$app_dir = realpath($dir.'/protected');
		if($app_dir !== false && is_dir($app_dir))
			return $app_dir;
		return realpath($dir.'/../protected');
	}

	protected function getTestDir($dir)
	{
		$test_dir = realpath($dir.'/unit');
		if($test_dir !== false && is_dir($test_dir))
			return $test_dir;
		return realpath($dir.'/tests/unit/');
	}

	protected function getUnitTestCases($dir,$args)
	{
		$matches = null;
		if(count($args) > 2)
			$matches = array_slice($args,2);
		$test=new GroupTest(' ');
		$this->addTests($test,$dir,true,$matches);
		$test->setLabel(implode(' ',$this->matches));
		return $test;
	}

	protected function addTests($test,$path,$recursive=true,$match=null)
	{
		$dir=opendir($path);
		while(($entry=readdir($dir))!==false)
		{
			if(is_file($path.'/'.$entry) && (preg_match('/[^\s]*test[^\s]*\.php/', strtolower($entry))))
			{
				if($match==null||($match!=null && $this->hasMatch($match,$entry)))
					$test->addTestFile($path.'/'.$entry);
			}
			if($entry!=='.' && $entry!=='..' && $entry!=='.svn' && is_dir($path.'/'.$entry) && $recursive)
				$this->addTests($test,$path.'/'.$entry,$recursive,$match);
		}
		closedir($dir);
	}

	protected function hasMatch($match,$entry)
	{
		$file = strtolower(substr($entry,0,strrpos($entry,'.')));
		foreach($match as $m)
		{
			if(strtolower($m) === $file)
			{
				$this->matches[] = $m;
				return true;
			}
		}
		return false;
	}
}

/**
 * Create active record skeleton
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: prado-cli.php 2498 2008-08-18 00:28:11Z knut $
 * @since 3.1
 */
class PradoCommandLineActiveRecordGen extends PradoCommandLineAction
{
	protected $action = 'generate';
	protected $parameters = array('table', 'output');
	protected $optional = array('directory', 'soap');
	protected $description = 'Generate Active Record skeleton for <table> to <output> file using application.xml in [directory]. May also generate [soap] properties.';
	private $_soap=false;

	public function performAction($args)
	{
		$app_dir = count($args) > 3 ? $this->getAppDir($args[3]) : $this->getAppDir();
		$this->_soap = count($args) > 4;
		if($app_dir !== false)
		{
			$config = $this->getActiveRecordConfig($app_dir);
			$output = $this->getOutputFile($app_dir, $args[2]);
			if(is_file($output))
				echo "** File $output already exists, skiping. \n";
			else if($config !== false && $output !== false)
				$this->generateActiveRecord($config, $args[1], $output);
		}
		return true;
	}

	protected function getAppDir($dir=".")
	{
		if(is_dir($dir))
			return realpath($dir);
		if(false !== ($app_dir = realpath($dir.'/protected/')) && is_dir($app_dir))
			return $app_dir;
		echo '** Unable to find directory "'.$dir."\".\n";
		return false;
	}

	protected function getXmlFile($app_dir)
	{
		if(false !== ($xml = realpath($app_dir.'/application.xml')) && is_file($xml))
			return $xml;
		if(false !== ($xml = realpath($app_dir.'/protected/application.xml')) && is_file($xml))
			return $xml;
		echo '** Unable to find application.xml in '.$app_dir."\n";
		return false;
	}

	protected function getActiveRecordConfig($app_dir)
	{
		if(false === ($xml=$this->getXmlFile($app_dir)))
			return false;
		if(false !== ($app=$this->initializePradoApplication($app_dir)))
		{
			Prado::using('System.Data.ActiveRecord.TActiveRecordConfig');
			foreach($app->getModules() as $module)
				if($module instanceof TActiveRecordConfig)
					return $module;
			echo '** Unable to find TActiveRecordConfig module in '.$xml."\n";
		}
		return false;
	}

	protected function getOutputFile($app_dir, $namespace)
	{
		if(is_file($namespace) && strpos($namespace, $app_dir)===0)
				return $namespace;
		$file = Prado::getPathOfNamespace($namespace, ".php");
		if($file !== null && false !== ($path = realpath(dirname($file))) && is_dir($path))
		{
			if(strpos($path, $app_dir)===0)
				return $file;
		}
		echo '** Output file '.$file.' must be within directory '.$app_dir."\n";
		return false;
	}

	protected function generateActiveRecord($config, $tablename, $output)
	{
		$manager = TActiveRecordManager::getInstance();
		if($connection = $manager->getDbConnection()) {
			$gateway = $manager->getRecordGateway();
			$tableInfo = $gateway->getTableInfo($manager->getDbConnection(), $tablename);
			if(count($tableInfo->getColumns()) === 0)
			{
				echo '** Unable to find table or view "'.$tablename.'" in "'.$manager->getDbConnection()->getConnectionString()."\".\n";
				return false;
			}
			else
			{
				$properties = array();
				foreach($tableInfo->getColumns() as $field=>$column)
					$properties[] = $this->generateProperty($field,$column);
			}

			$classname = basename($output, '.php');
			$class = $this->generateClass($properties, $tablename, $classname);
			echo "  Writing class $classname to file $output\n";
			file_put_contents($output, $class);
		} else {
			echo '** Unable to connect to database with ConnectionID=\''.$config->getConnectionID()."'. Please check your settings in application.xml and ensure your database connection is set up first.\n";
		}
	}

	protected function generateProperty($field,$column)
	{
		$prop = '';
		$name = '$'.$field;
		$type = $column->getPHPType();
		if($this->_soap)
		{
			$prop .= <<<EOD

	/**
	 * @var $type $name
	 * @soapproperty
	 */

EOD;
		}
		$prop .= "\tpublic $name;";
		return $prop;
	}

	protected function generateClass($properties, $tablename, $class)
	{
		$props = implode("\n", $properties);
		$date = date('Y-m-d h:i:s');
return <<<EOD
<?php
/**
 * Auto generated by prado-cli.php on $date.
 */
class $class extends TActiveRecord
{
	const TABLE='$tablename';

$props

	public static function finder(\$className=__CLASS__)
	{
		return parent::finder(\$className);
	}
}
?>
EOD;
	}
}

//run PHP shell
if(class_exists('PHP_Shell_Commands', false))
{
	class PHP_Shell_Extensions_ActiveRecord implements PHP_Shell_Extension {
	    public function register()
	    {

	        $cmd = PHP_Shell_Commands::getInstance();
			if(Prado::getApplication() !== null)
			{
		        $cmd->registerCommand('#^generate#', $this, 'generate', 'generate <table> <output> [soap]',
		            'Generate Active Record skeleton for <table> to <output> file using'.PHP_EOL.
		            '    application.xml in [directory]. May also generate [soap] properties.');
			}
	    }

	    public function generate($l)
	    {
			$input = explode(" ", trim($l));
			if(count($input) > 2)
			{
				$app_dir = '.';
				if(Prado::getApplication()!==null)
					$app_dir = dirname(Prado::getApplication()->getBasePath());
				$args = array($input[0],$input[1], $input[2],$app_dir);
				if(count($input)>3)
					$args = array($input[0],$input[1], $input[2],$app_dir,'soap');
				$cmd = new PradoCommandLineActiveRecordGen;
				$cmd->performAction($args);
			}
			else
			{
				echo "\n    Usage: generate table_name Application.pages.RecordClassName\n";
			}
	    }
	}

	$__shell_exts = PHP_Shell_Extensions::getInstance();
	$__shell_exts->registerExtensions(array(
		    "active-record"        => new PHP_Shell_Extensions_ActiveRecord)); /* the :set command */

	include_once(dirname(__FILE__).'/3rdParty/PhpShell/php-shell-cmd.php');
}

?>
