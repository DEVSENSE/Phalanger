<?php
/**
 * TLogRouter, TLogRoute, TFileLogRoute, TEmailLogRoute class file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TLogRouter.php 2938 2011-06-01 07:46:44Z ctrlaltca@gmail.com $
 * @package System.Util
 */

Prado::using('System.Data.TDbConnection');

/**
 * TLogRouter class.
 *
 * TLogRouter manages routes that record log messages in different media different ways.
 * For example, a file log route {@link TFileLogRoute} records log messages
 * in log files. An email log route {@link TEmailLogRoute} sends log messages
 * to email addresses.
 *
 * Log routes may be configured in application or page folder configuration files
 * or an external configuration file specified by {@link setConfigFile ConfigFile}.
 * The format is as follows,
 * <code>
 *   <route class="TFileLogRoute" Categories="System.Web.UI" Levels="Warning" />
 *   <route class="TEmailLogRoute" Categories="Application" Levels="Fatal" Emails="admin@pradosoft.com" />
 * </code>
 * You can specify multiple routes with different filtering conditions and different
 * targets, even if the routes are of the same type.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TLogRouter.php 2938 2011-06-01 07:46:44Z ctrlaltca@gmail.com $
 * @package System.Util
 * @since 3.0
 */
class TLogRouter extends TModule
{
	/**
	 * File extension of external configuration file
	 */
	const CONFIG_FILE_EXT='.xml';
	/**
	 * @var array list of routes available
	 */
	private $_routes=array();
	/**
	 * @var string external configuration file
	 */
	private $_configFile=null;

	/**
	 * Initializes this module.
	 * This method is required by the IModule interface.
	 * @param TXmlElement configuration for this module, can be null
	 * @throws TConfigurationException if {@link getConfigFile ConfigFile} is invalid.
	 */
	public function init($config)
	{
		if($this->_configFile!==null)
		{
 			if(is_file($this->_configFile))
 			{
				$dom=new TXmlDocument;
				$dom->loadFromFile($this->_configFile);
				$this->loadConfig($dom);
			}
			else
				throw new TConfigurationException('logrouter_configfile_invalid',$this->_configFile);
		}
		$this->loadConfig($config);
		$this->getApplication()->attachEventHandler('OnEndRequest',array($this,'collectLogs'));
	}

	/**
	 * Loads configuration from an XML element
	 * @param TXmlElement configuration node
	 * @throws TConfigurationException if log route class or type is not specified
	 */
	private function loadConfig($xml)
	{
		foreach($xml->getElementsByTagName('route') as $routeConfig)
		{
			$properties=$routeConfig->getAttributes();
			if(($class=$properties->remove('class'))===null)
				throw new TConfigurationException('logrouter_routeclass_required');
			$route=Prado::createComponent($class);
			if(!($route instanceof TLogRoute))
				throw new TConfigurationException('logrouter_routetype_invalid');
			foreach($properties as $name=>$value)
				$route->setSubproperty($name,$value);
			$this->_routes[]=$route;
			$route->init($routeConfig);
		}
	}

	/**
	 * Adds a TLogRoute instance to the log router.
	 *
	 * @param TLogRoute $route
	 * @throws TInvalidDataTypeException if the route object is invalid
	 */
	public function addRoute($route)
	{
		if(!($route instanceof TLogRoute))
			throw new TInvalidDataTypeException('logrouter_routetype_invalid');
		$this->_routes[]=$route;
		$route->init(null);
	}

	/**
	 * @return string external configuration file. Defaults to null.
	 */
	public function getConfigFile()
	{
		return $this->_configFile;
	}

	/**
	 * @param string external configuration file in namespace format. The file
	 * must be suffixed with '.xml'.
	 * @throws TConfigurationException if the file is invalid.
	 */
	public function setConfigFile($value)
	{
		if(($this->_configFile=Prado::getPathOfNamespace($value,self::CONFIG_FILE_EXT))===null)
			throw new TConfigurationException('logrouter_configfile_invalid',$value);
	}

	/**
	 * Collects log messages from a logger.
	 * This method is an event handler to application's EndRequest event.
	 * @param mixed event parameter
	 */
	public function collectLogs($param)
	{
		$logger=Prado::getLogger();
		foreach($this->_routes as $route)
			$route->collectLogs($logger);
	}
}

/**
 * TLogRoute class.
 *
 * TLogRoute is the base class for all log route classes.
 * A log route object retrieves log messages from a logger and sends it
 * somewhere, such as files, emails.
 * The messages being retrieved may be filtered first before being sent
 * to the destination. The filters include log level filter and log category filter.
 *
 * To specify level filter, set {@link setLevels Levels} property,
 * which takes a string of comma-separated desired level names (e.g. 'Error, Debug').
 * To specify category filter, set {@link setCategories Categories} property,
 * which takes a string of comma-separated desired category names (e.g. 'System.Web, System.IO').
 *
 * Level filter and category filter are combinational, i.e., only messages
 * satisfying both filter conditions will they be returned.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TLogRouter.php 2938 2011-06-01 07:46:44Z ctrlaltca@gmail.com $
 * @package System.Util
 * @since 3.0
 */
abstract class TLogRoute extends TApplicationComponent
{
	/**
	 * @var array lookup table for level names
	 */
	protected static $_levelNames=array(
		TLogger::DEBUG=>'Debug',
		TLogger::INFO=>'Info',
		TLogger::NOTICE=>'Notice',
		TLogger::WARNING=>'Warning',
		TLogger::ERROR=>'Error',
		TLogger::ALERT=>'Alert',
		TLogger::FATAL=>'Fatal'
	);
	/**
	 * @var array lookup table for level values
	 */
	protected static $_levelValues=array(
		'debug'=>TLogger::DEBUG,
		'info'=>TLogger::INFO,
		'notice'=>TLogger::NOTICE,
		'warning'=>TLogger::WARNING,
		'error'=>TLogger::ERROR,
		'alert'=>TLogger::ALERT,
		'fatal'=>TLogger::FATAL
	);
	/**
	 * @var integer log level filter (bits)
	 */
	private $_levels=null;
	/**
	 * @var array log category filter
	 */
	private $_categories=null;

	/**
	 * Initializes the route.
	 * @param TXmlElement configurations specified in {@link TLogRouter}.
	 */
	public function init($config)
	{
	}

	/**
	 * @return integer log level filter
	 */
	public function getLevels()
	{
		return $this->_levels;
	}

	/**
	 * @param integer|string integer log level filter (in bits). If the value is
	 * a string, it is assumed to be comma-separated level names. Valid level names
	 * include 'Debug', 'Info', 'Notice', 'Warning', 'Error', 'Alert' and 'Fatal'.
	 */
	public function setLevels($levels)
	{
		if(is_integer($levels))
			$this->_levels=$levels;
		else
		{
			$this->_levels=null;
			$levels=strtolower($levels);
			foreach(explode(',',$levels) as $level)
			{
				$level=trim($level);
				if(isset(self::$_levelValues[$level]))
					$this->_levels|=self::$_levelValues[$level];
			}
		}
	}

	/**
	 * @return array list of categories to be looked for
	 */
	public function getCategories()
	{
		return $this->_categories;
	}

	/**
	 * @param array|string list of categories to be looked for. If the value is a string,
	 * it is assumed to be comma-separated category names.
	 */
	public function setCategories($categories)
	{
		if(is_array($categories))
			$this->_categories=$categories;
		else
		{
			$this->_categories=null;
			foreach(explode(',',$categories) as $category)
			{
				if(($category=trim($category))!=='')
					$this->_categories[]=$category;
			}
		}
	}

	/**
	 * @param integer level value
	 * @return string level name
	 */
	protected function getLevelName($level)
	{
		return isset(self::$_levelNames[$level])?self::$_levelNames[$level]:'Unknown';
	}

	/**
	 * @param string level name
	 * @return integer level value
	 */
	protected function getLevelValue($level)
	{
		return isset(self::$_levelValues[$level])?self::$_levelValues[$level]:0;
	}

	/**
	 * Formats a log message given different fields.
	 * @param string message content
	 * @param integer message level
	 * @param string message category
	 * @param integer timestamp
	 * @return string formatted message
	 */
	protected function formatLogMessage($message,$level,$category,$time)
	{
		return @gmdate('M d H:i:s',$time).' ['.$this->getLevelName($level).'] ['.$category.'] '.$message."\n";
	}

	/**
	 * Retrieves log messages from logger to log route specific destination.
	 * @param TLogger logger instance
	 */
	public function collectLogs(TLogger $logger)
	{
		$logs=$logger->getLogs($this->getLevels(),$this->getCategories());
		if(!empty($logs))
			$this->processLogs($logs);
	}

	/**
	 * Processes log messages and sends them to specific destination.
	 * Derived child classes must implement this method.
	 * @param array list of messages.  Each array elements represents one message
	 * with the following structure:
	 * array(
	 *   [0] => message
	 *   [1] => level
	 *   [2] => category
	 *   [3] => timestamp);
	 */
	abstract protected function processLogs($logs);
}

/**
 * TFileLogRoute class.
 *
 * TFileLogRoute records log messages in files.
 * The log files are stored under {@link setLogPath LogPath} and the file name
 * is specified by {@link setLogFile LogFile}. If the size of the log file is
 * greater than {@link setMaxFileSize MaxFileSize} (in kilo-bytes), a rotation
 * is performed, which renames the current log file by suffixing the file name
 * with '.1'. All existing log files are moved backwards one place, i.e., '.2'
 * to '.3', '.1' to '.2'. The property {@link setMaxLogFiles MaxLogFiles}
 * specifies how many files to be kept.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TLogRouter.php 2938 2011-06-01 07:46:44Z ctrlaltca@gmail.com $
 * @package System.Util
 * @since 3.0
 */
class TFileLogRoute extends TLogRoute
{
	/**
	 * @var integer maximum log file size
	 */
	private $_maxFileSize=512; // in KB
	/**
	 * @var integer number of log files used for rotation
	 */
	private $_maxLogFiles=2;
	/**
	 * @var string directory storing log files
	 */
	private $_logPath=null;
	/**
	 * @var string log file name
	 */
	private $_logFile='prado.log';

	/**
	 * @return string directory storing log files. Defaults to application runtime path.
	 */
	public function getLogPath()
	{
		if($this->_logPath===null)
			$this->_logPath=$this->getApplication()->getRuntimePath();
		return $this->_logPath;
	}

	/**
	 * @param string directory (in namespace format) storing log files.
	 * @throws TConfigurationException if log path is invalid
	 */
	public function setLogPath($value)
	{
		if(($this->_logPath=Prado::getPathOfNamespace($value))===null || !is_dir($this->_logPath) || !is_writable($this->_logPath))
			throw new TConfigurationException('filelogroute_logpath_invalid',$value);
	}

	/**
	 * @return string log file name. Defaults to 'prado.log'.
	 */
	public function getLogFile()
	{
		return $this->_logFile;
	}

	/**
	 * @param string log file name
	 */
	public function setLogFile($value)
	{
		$this->_logFile=$value;
	}

	/**
	 * @return integer maximum log file size in kilo-bytes (KB). Defaults to 1024 (1MB).
	 */
	public function getMaxFileSize()
	{
		return $this->_maxFileSize;
	}

	/**
	 * @param integer maximum log file size in kilo-bytes (KB).
	 * @throws TInvalidDataValueException if the value is smaller than 1.
	 */
	public function setMaxFileSize($value)
	{
		$this->_maxFileSize=TPropertyValue::ensureInteger($value);
		if($this->_maxFileSize<=0)
			throw new TInvalidDataValueException('filelogroute_maxfilesize_invalid');
	}

	/**
	 * @return integer number of files used for rotation. Defaults to 2.
	 */
	public function getMaxLogFiles()
	{
		return $this->_maxLogFiles;
	}

	/**
	 * @param integer number of files used for rotation.
	 */
	public function setMaxLogFiles($value)
	{
		$this->_maxLogFiles=TPropertyValue::ensureInteger($value);
		if($this->_maxLogFiles<1)
			throw new TInvalidDataValueException('filelogroute_maxlogfiles_invalid');
	}

	/**
	 * Saves log messages in files.
	 * @param array list of log messages
	 */
	protected function processLogs($logs)
	{
		$logFile=$this->getLogPath().DIRECTORY_SEPARATOR.$this->getLogFile();
		if(@filesize($logFile)>$this->_maxFileSize*1024)
			$this->rotateFiles();
		foreach($logs as $log)
			error_log($this->formatLogMessage($log[0],$log[1],$log[2],$log[3]),3,$logFile);
	}

	/**
	 * Rotates log files.
	 */
	protected function rotateFiles()
	{
		$file=$this->getLogPath().DIRECTORY_SEPARATOR.$this->getLogFile();
		for($i=$this->_maxLogFiles;$i>0;--$i)
		{
			$rotateFile=$file.'.'.$i;
			if(is_file($rotateFile))
			{
				if($i===$this->_maxLogFiles)
					unlink($rotateFile);
				else
					rename($rotateFile,$file.'.'.($i+1));
			}
		}
		if(is_file($file))
			rename($file,$file.'.1');
	}
}

/**
 * TEmailLogRoute class.
 *
 * TEmailLogRoute sends selected log messages to email addresses.
 * The target email addresses may be specified via {@link setEmails Emails} property.
 * Optionally, you may set the email {@link setSubject Subject} and the
 * {@link setSentFrom SentFrom} address.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TLogRouter.php 2938 2011-06-01 07:46:44Z ctrlaltca@gmail.com $
 * @package System.Util
 * @since 3.0
 */
class TEmailLogRoute extends TLogRoute
{
	/**
	 * Regex pattern for email address.
	 */
	const EMAIL_PATTERN='/^([0-9a-zA-Z]+[-._+&])*[0-9a-zA-Z]+@([-0-9a-zA-Z]+[.])+[a-zA-Z]{2,6}$/';
	/**
	 * Default email subject.
	 */
	const DEFAULT_SUBJECT='Prado Application Log';
	/**
	 * @var array list of destination email addresses.
	 */
	private $_emails=array();
	/**
	 * @var string email subject
	 */
	private $_subject='';
	/**
	 * @var string email sent from address
	 */
	private $_from='';

	/**
	 * Initializes the route.
	 * @param TXmlElement configurations specified in {@link TLogRouter}.
	 * @throws TConfigurationException if {@link getSentFrom SentFrom} is empty and
	 * 'sendmail_from' in php.ini is also empty.
	 */
	public function init($config)
	{
		if($this->_from==='')
			$this->_from=ini_get('sendmail_from');
		if($this->_from==='')
			throw new TConfigurationException('emaillogroute_sentfrom_required');
	}

	/**
	 * Sends log messages to specified email addresses.
	 * @param array list of log messages
	 */
	protected function processLogs($logs)
	{
		$message='';
		foreach($logs as $log)
			$message.=$this->formatLogMessage($log[0],$log[1],$log[2],$log[3]);
		$message=wordwrap($message,70);
		$returnPath = ini_get('sendmail_path') ? "Return-Path:{$this->_from}\r\n" : '';
		foreach($this->_emails as $email)
			mail($email,$this->getSubject(),$message,"From:{$this->_from}\r\n{$returnPath}");

	}

	/**
	 * @return array list of destination email addresses
	 */
	public function getEmails()
	{
		return $this->_emails;
	}

	/**
	 * @return array|string list of destination email addresses. If the value is
	 * a string, it is assumed to be comma-separated email addresses.
	 */
	public function setEmails($emails)
	{
		if(is_array($emails))
			$this->_emails=$emails;
		else
		{
			$this->_emails=array();
			foreach(explode(',',$emails) as $email)
			{
				$email=trim($email);
				if(preg_match(self::EMAIL_PATTERN,$email))
					$this->_emails[]=$email;
			}
		}
	}

	/**
	 * @return string email subject. Defaults to TEmailLogRoute::DEFAULT_SUBJECT
	 */
	public function getSubject()
	{
		if($this->_subject===null)
			$this->_subject=self::DEFAULT_SUBJECT;
		return $this->_subject;
	}

	/**
	 * @param string email subject.
	 */
	public function setSubject($value)
	{
		$this->_subject=$value;
	}

	/**
	 * @return string send from address of the email
	 */
	public function getSentFrom()
	{
		return $this->_from;
	}

	/**
	 * @param string send from address of the email
	 */
	public function setSentFrom($value)
	{
		$this->_from=$value;
	}
}

/**
 * TBrowserLogRoute class.
 *
 * TBrowserLogRoute prints selected log messages in the response.
 *
 * @author Xiang Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @version $Id: TLogRouter.php 2938 2011-06-01 07:46:44Z ctrlaltca@gmail.com $
 * @package System.Util
 * @since 3.0
 */
class TBrowserLogRoute extends TLogRoute
{
	/**
	 * @var string css class for indentifying the table structure in the dom tree
	 */
	private $_cssClass=null;
	
	public function processLogs($logs)
	{
		if(empty($logs) || $this->getApplication()->getMode()==='Performance') return;
		$first = $logs[0][3];
		$even = true;
		$response = $this->getApplication()->getResponse();
		$response->write($this->renderHeader());
		for($i=0,$n=count($logs);$i<$n;++$i)
		{
			if ($i<$n-1)
			{
				$timing['delta'] = $logs[$i+1][3] - $logs[$i][3];
				$timing['total'] = $logs[$i+1][3] - $first;
			}
			else
			{
				$timing['delta'] = '?';
				$timing['total'] = $logs[$i][3] - $first;
			}
			$timing['even'] = !($even = !$even);
			$response->write($this->renderMessage($logs[$i],$timing));
		}
		$response->write($this->renderFooter());
	}
	
	/**
	 * @param string the css class of the control
	 */
	public function setCssClass($value)
	{
		$this->_cssClass = TPropertyValue::ensureString($value);
	}

	/**
	 * @return string the css class of the control
	 */
	public function getCssClass()
	{
		return TPropertyValue::ensureString($this->_cssClass);
	}

	protected function renderHeader()
	{
		$string = '';
		if($className=$this->getCssClass())
		{
			$string = <<<EOD
<table class="$className">
	<tr class="header">
		<th colspan="5">
			Application Log
		</th>
	</tr><tr class="description">
	    <th>&nbsp;</th>
		<th>Category</th><th>Message</th><th>Time Spent (s)</th><th>Cumulated Time Spent (s)</th>
	</tr>
EOD;
		}
		else
		{
			$string = <<<EOD
<table cellspacing="0" cellpadding="2" border="0" width="100%" style="table-layout:auto">
	<tr>
		<th style="background-color: black; color:white;" colspan="5">
			Application Log
		</th>
	</tr><tr style="background-color: #ccc; color:black">
	    <th style="width: 15px">&nbsp;</th>
		<th style="width: auto">Category</th><th style="width: auto">Message</th><th style="width: 120px">Time Spent (s)</th><th style="width: 150px">Cumulated Time Spent (s)</th>
	</tr>
EOD;
		}
		return $string;
	}

	protected function renderMessage($log, $info)
	{
		$string = '';
		$total = sprintf('%0.6f', $info['total']);
		$delta = sprintf('%0.6f', $info['delta']);
		$msg = preg_replace('/\(line[^\)]+\)$/','',$log[0]); //remove line number info
		$msg = THttpUtility::htmlEncode($msg);
		if($this->getCssClass())
		{
			$colorCssClass = $log[1];
			$messageCssClass = $info['even'] ? 'even' : 'odd';
			$string = <<<EOD
	<tr class="message level$colorCssClass $messageCssClass">
		<td class="code">&nbsp;</td>
		<td class="category">{$log[2]}</td>
		<td class="message">{$msg}</td>
		<td class="time">{$delta}</td>
		<td class="cumulatedtime">{$total}</td>
	</tr>
EOD;
		}
		else
		{
			$bgcolor = $info['even'] ? "#fff" : "#eee";
			$color = $this->getColorLevel($log[1]);
			$string = <<<EOD
	<tr style="background-color: {$bgcolor}; color:#000">
		<td style="border:1px solid silver;background-color: $color;">&nbsp;</td>
		<td>{$log[2]}</td>
		<td>{$msg}</td>
		<td style="text-align:center">{$delta}</td>
		<td style="text-align:center">{$total}</td>
	</tr>
EOD;
		}
		return $string;
	}

	protected function getColorLevel($level)
	{
		switch($level)
		{
			case TLogger::DEBUG: return 'green';
			case TLogger::INFO: return 'black';
			case TLogger::NOTICE: return '#3333FF';
			case TLogger::WARNING: return '#33FFFF';
			case TLogger::ERROR: return '#ff9933';
			case TLogger::ALERT: return '#ff00ff';
			case TLogger::FATAL: return 'red';
		}
		return '';
	}

	protected function renderFooter()
	{
		$string = '';
		if($this->getCssClass())
		{
			$string .= '<tr class="footer"><td colspan="5">';
			foreach(self::$_levelValues as $name => $level)
			{
				$string .= '<span class="level'.$level.'">'.strtoupper($name)."</span>";
			}
		}
		else
		{
			$string .= "<tr><td colspan=\"5\" style=\"text-align:center; background-color:black; border-top: 1px solid #ccc; padding:0.2em;\">";
			foreach(self::$_levelValues as $name => $level)
			{
				$string .= "<span style=\"color:white; border:1px solid white; background-color:".$this->getColorLevel($level);
				$string .= ";margin: 0.5em; padding:0.01em;\">".strtoupper($name)."</span>";
			}
		}
		$string .= '</td></tr></table>';
		return $string;
	}
}


/**
 * TDbLogRoute class
 *
 * TDbLogRoute stores log messages in a database table.
 * To specify the database table, set {@link setConnectionID ConnectionID} to be
 * the ID of a {@link TDataSourceConfig} module and {@link setLogTableName LogTableName}.
 * If they are not setting, an SQLite3 database named 'sqlite3.log' will be created and used
 * under the runtime directory.
 *
 * By default, the database table name is 'pradolog'. It has the following structure:
 * <code>
 *	CREATE TABLE pradolog
 *  (
 *		log_id INTEGER NOT NULL PRIMARY KEY,
 *		level INTEGER,
 *		category VARCHAR(128),
 *		logtime VARCHAR(20),
 *		message VARCHAR(255)
 *   );
 * </code>
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TLogRouter.php 2938 2011-06-01 07:46:44Z ctrlaltca@gmail.com $
 * @package System.Util
 * @since 3.1.2
 */
class TDbLogRoute extends TLogRoute
{
	/**
	 * @var string the ID of TDataSourceConfig module
	 */
	private $_connID='';
	/**
	 * @var TDbConnection the DB connection instance
	 */
	private $_db;
	/**
	 * @var string name of the DB log table
	 */
	private $_logTable='pradolog';
	/**
	 * @var boolean whether the log DB table should be created automatically
	 */
	private $_autoCreate=true;

	/**
	 * Destructor.
	 * Disconnect the db connection.
	 */
	public function __destruct()
	{
		if($this->_db!==null)
			$this->_db->setActive(false);
	}

	/**
	 * Initializes this module.
	 * This method is required by the IModule interface.
	 * It initializes the database for logging purpose.
	 * @param TXmlElement configuration for this module, can be null
	 * @throws TConfigurationException if the DB table does not exist.
	 */
	public function init($config)
	{
		$db=$this->getDbConnection();
		$db->setActive(true);

		$sql='SELECT * FROM '.$this->_logTable.' WHERE 0=1';
		try
		{
			$db->createCommand($sql)->query()->close();
		}
		catch(Exception $e)
		{
			// DB table not exists
			if($this->_autoCreate)
				$this->createDbTable();
			else
				throw new TConfigurationException('db_logtable_inexistent',$this->_logTable);
		}

		parent::init($config);
	}

	/**
	 * Stores log messages into database.
	 * @param array list of log messages
	 */
	protected function processLogs($logs)
	{
		$sql='INSERT INTO '.$this->_logTable.'(level, category, logtime, message) VALUES (:level, :category, :logtime, :message)';
		$command=$this->getDbConnection()->createCommand($sql);
		foreach($logs as $log)
		{
			$command->bindValue(':message',$log[0]);
			$command->bindValue(':level',$log[1]);
			$command->bindValue(':category',$log[2]);
			$command->bindValue(':logtime',$log[3]);
			$command->execute();
		}
	}

	/**
	 * Creates the DB table for storing log messages.
	 * @todo create sequence for PostgreSQL
	 */
	protected function createDbTable()
	{
		$db = $this->getDbConnection();
		$driver=$db->getDriverName();
		$autoidAttributes = '';
		if($driver==='mysql')
			$autoidAttributes = 'AUTO_INCREMENT';

		$sql='CREATE TABLE '.$this->_logTable.' (
			log_id INTEGER NOT NULL PRIMARY KEY ' . $autoidAttributes . ',
			level INTEGER,
			category VARCHAR(128),
			logtime VARCHAR(20),
			message VARCHAR(255))';
		$db->createCommand($sql)->execute();
	}

	/**
	 * Creates the DB connection.
	 * @param string the module ID for TDataSourceConfig
	 * @return TDbConnection the created DB connection
	 * @throws TConfigurationException if module ID is invalid or empty
	 */
	protected function createDbConnection()
	{
		if($this->_connID!=='')
		{
			$config=$this->getApplication()->getModule($this->_connID);
			if($config instanceof TDataSourceConfig)
				return $config->getDbConnection();
			else
				throw new TConfigurationException('dblogroute_connectionid_invalid',$this->_connID);
		}
		else
		{
			$db=new TDbConnection;
			// default to SQLite3 database
			$dbFile=$this->getApplication()->getRuntimePath().'/sqlite3.log';
			$db->setConnectionString('sqlite:'.$dbFile);
			return $db;
		}
	}

	/**
	 * @return TDbConnection the DB connection instance
	 */
	public function getDbConnection()
	{
		if($this->_db===null)
			$this->_db=$this->createDbConnection();
		return $this->_db;
	}

	/**
	 * @return string the ID of a {@link TDataSourceConfig} module. Defaults to empty string, meaning not set.
	 */
	public function getConnectionID()
	{
		return $this->_connID;
	}

	/**
	 * Sets the ID of a TDataSourceConfig module.
	 * The datasource module will be used to establish the DB connection for this log route.
	 * @param string ID of the {@link TDataSourceConfig} module
	 */
	public function setConnectionID($value)
	{
		$this->_connID=$value;
	}

	/**
	 * @return string the name of the DB table to store log content. Defaults to 'pradolog'.
	 * @see setAutoCreateLogTable
	 */
	public function getLogTableName()
	{
		return $this->_logTable;
	}

	/**
	 * Sets the name of the DB table to store log content.
	 * Note, if {@link setAutoCreateLogTable AutoCreateLogTable} is false
	 * and you want to create the DB table manually by yourself,
	 * you need to make sure the DB table is of the following structure:
	 * (key CHAR(128) PRIMARY KEY, value BLOB, expire INT)
	 * @param string the name of the DB table to store log content
	 * @see setAutoCreateLogTable
	 */
	public function setLogTableName($value)
	{
		$this->_logTable=$value;
	}

	/**
	 * @return boolean whether the log DB table should be automatically created if not exists. Defaults to true.
	 * @see setAutoCreateLogTable
	 */
	public function getAutoCreateLogTable()
	{
		return $this->_autoCreate;
	}

	/**
	 * @param boolean whether the log DB table should be automatically created if not exists.
	 * @see setLogTableName
	 */
	public function setAutoCreateLogTable($value)
	{
		$this->_autoCreate=TPropertyValue::ensureBoolean($value);
	}

}

/**
 * TFirebugLogRoute class.
 *
 * TFirebugLogRoute prints selected log messages in the firebug log console.
 *
 * {@link http://www.getfirebug.com/ FireBug Website}
 *
 * @author Enrico Stahn <mail@enricostahn.com>, Christophe Boulain <Christophe.Boulain@gmail.com>
 * @version $Id: TLogRouter.php 2938 2011-06-01 07:46:44Z ctrlaltca@gmail.com $
 * @package System.Util
 * @since 3.1.2
 */
class TFirebugLogRoute extends TBrowserLogRoute
{
	protected function renderHeader ()
	{
		$string = <<<EOD

<script type="text/javascript">
/*<![CDATA[*/
if (typeof(console) == 'object')
{
	console.log ("[Cumulated Time] [Time] [Level] [Category] [Message]");

EOD;

		return $string;
	}

	protected function renderMessage ($log, $info)
	{
		$logfunc = $this->getFirebugLoggingFunction($log[1]);
		$total = sprintf('%0.6f', $info['total']);
		$delta = sprintf('%0.6f', $info['delta']);
		$msg = trim($this->formatLogMessage($log[0],$log[1],$log[2],''));
		$msg = preg_replace('/\(line[^\)]+\)$/','',$msg); //remove line number info
		$msg = "[{$total}] [{$delta}] ".$msg; // Add time spent and cumulated time spent
		$string = $logfunc . '(\'' . addslashes($msg) . '\');' . "\n";

		return $string;
	}


	protected function renderFooter ()
	{
		$string = <<<EOD

}
</script>

EOD;

		return $string;
	}

	protected function getFirebugLoggingFunction($level)
	{
		switch ($level)
		{
			case TLogger::DEBUG:
			case TLogger::INFO:
			case TLogger::NOTICE:
				return 'console.log';
			case TLogger::WARNING:
				return 'console.warn';
			case TLogger::ERROR:
			case TLogger::ALERT:
			case TLogger::FATAL:
				return 'console.error';
		}
		return 'console.log';
	}

}

/**
 * TFirePhpLogRoute class.
 *
 * TFirePhpLogRoute prints log messages in the firebug log console via firephp.
 *
 * {@link http://www.getfirebug.com/ FireBug Website}
 * {@link http://www.firephp.org/ FirePHP Website}
 *
 * @author Yves Berkholz <godzilla80[at]gmx[dot]net>
 * @version $Id: TLogRouter.php 2938 2011-06-01 07:46:44Z ctrlaltca@gmail.com $
 * @package System.Util
 * @since 3.1.5
 */
class TFirePhpLogRoute extends TLogRoute
{
	/**
	 * Default group label
	 */
	const DEFAULT_LABEL = 'System.Util.TLogRouter(TFirePhpLogRoute)';

	private $_groupLabel = null;

	public function processLogs($logs)
	{
		if(empty($logs) || $this->getApplication()->getMode()==='Performance') return;

		if( headers_sent() ) {
			echo '
				<div style="width:100%; background-color:darkred; color:#FFF; padding:2px">
					TFirePhpLogRoute.GroupLabel "<i>' . $this -> getGroupLabel() . '</i>" -
					Routing to FirePHP impossible, because headers already sent!
				</div>
			';
			$fallback = new TBrowserLogRoute();
			$fallback->processLogs($logs);
			return;
		}

		require_once Prado::getPathOfNamespace('System.3rdParty.FirePHPCore') . '/FirePHP.class.php';
		$firephp = FirePHP::getInstance(true);
		$firephp -> setOptions(array('useNativeJsonEncode' => false));

		$firephp -> group($this->getGroupLabel(), array('Collapsed' => true));

		$firephp ->log('Time,  Message');

		$first = $logs[0][3];
		$c = count($logs);
		for($i=0,$n=$c;$i<$n;++$i)
		{
			$message	= $logs[$i][0];
			$level		= $logs[$i][1];
			$category	= $logs[$i][2];

			if ($i<$n-1)
			{
				$delta = $logs[$i+1][3] - $logs[$i][3];
				$total = $logs[$i+1][3] - $first;
			}
			else
			{
				$delta = '?';
				$total = $logs[$i][3] - $first;
			}

			$message = sPrintF('+%0.6f: %s', $delta, preg_replace('/\(line[^\)]+\)$/','',$message));
			$firephp ->fb($message, $category, self::translateLogLevel($level));
		}
		$firephp ->log( sPrintF('%0.6f', $total), 'Cumulated Time');
		$firephp -> groupEnd();
	}

	protected static function translateLogLevel($level)
	{
		switch($level)
		{
			case TLogger::INFO:
				return FirePHP::INFO;
			case TLogger::DEBUG:
			case TLogger::NOTICE:
				return FirePHP::LOG;
			case TLogger::WARNING:
				return FirePHP::WARN;
			case TLogger::ERROR:
			case TLogger::ALERT:
			case TLogger::FATAL:
				return FirePHP::ERROR;
		}
		return FirePHP::LOG;
	}

	/**
	 * @return string group label. Defaults to TFirePhpLogRoute::DEFAULT_LABEL
	 */
	public function getGroupLabel()
	{
		if($this->_groupLabel===null)
			$this->_groupLabel=self::DEFAULT_LABEL;
		return $this->_groupLabel;
	}

	/**
	 * @param string group label.
	 */
	public function setGroupLabel($value)
	{
		$this->_groupLabel=$value;
	}
}