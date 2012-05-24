<?php
/**
 * Exception classes file
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 */

/**
 * TException class
 *
 * TException is the base class for all PRADO exceptions.
 *
 * TException provides the functionality of translating an error code
 * into a descriptive error message in a language that is preferred
 * by user browser. Additional parameters may be passed together with
 * the error code so that the translated message contains more detailed
 * information.
 *
 * By default, TException looks for a message file by calling
 * {@link getErrorMessageFile()} method, which uses the "message-xx.txt"
 * file located under "System.Exceptions" folder, where "xx" is the
 * code of the user preferred language. If such a file is not found,
 * "message.txt" will be used instead.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 * @since 3.0
 */
class TException extends Exception
{
	private $_errorCode='';
	static $_messageCache=array();

	/**
	 * Constructor.
	 * @param string error message. This can be a string that is listed
	 * in the message file. If so, the message in the preferred language
	 * will be used as the error message. Any rest parameters will be used
	 * to replace placeholders ({0}, {1}, {2}, etc.) in the message.
	 */
	public function __construct($errorMessage)
	{
		$this->_errorCode=$errorMessage;
		$errorMessage=$this->translateErrorMessage($errorMessage);
		$args=func_get_args();
		array_shift($args);
		$n=count($args);
		$tokens=array();
		for($i=0;$i<$n;++$i)
			$tokens['{'.$i.'}']=TPropertyValue::ensureString($args[$i]);
		parent::__construct(strtr($errorMessage,$tokens));
	}

	/**
	 * Translates an error code into an error message.
	 * @param string error code that is passed in the exception constructor.
	 * @return string the translated error message
	 */
	protected function translateErrorMessage($key)
	{
		$msgFile=$this->getErrorMessageFile();

		// Cache messages
		if (!isset(self::$_messageCache[$msgFile])) 
		{
			if(($entries=@file($msgFile))!==false)
			{
				foreach($entries as $entry)
				{
					@list($code,$message)=explode('=',$entry,2);
					self::$_messageCache[$msgFile][trim($code)]=trim($message);
				}
			}
		}
		return isset(self::$_messageCache[$msgFile][$key]) ? self::$_messageCache[$msgFile][$key] : $key;
	}

	/**
	 * @return string path to the error message file
	 */
	protected function getErrorMessageFile()
	{
		$lang=Prado::getPreferredLanguage();
		$msgFile=Prado::getFrameworkPath().'/Exceptions/messages/messages-'.$lang.'.txt';
		if(!is_file($msgFile))
			$msgFile=Prado::getFrameworkPath().'/Exceptions/messages/messages.txt';
		return $msgFile;
	}

	/**
	 * @return string error code
	 */
	public function getErrorCode()
	{
		return $this->_errorCode;
	}

	/**
	 * @param string error code
	 */
	public function setErrorCode($code)
	{
		$this->_errorCode=$code;
	}

	/**
	 * @return string error message
	 */
	public function getErrorMessage()
	{
		return $this->getMessage();
	}

	/**
	 * @param string error message
	 */
	protected function setErrorMessage($message)
	{
		$this->message=$message;
	}
}

/**
 * TSystemException class
 *
 * TSystemException is the base class for all framework-level exceptions.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 * @since 3.0
 */
class TSystemException extends TException
{
}

/**
 * TApplicationException class
 *
 * TApplicationException is the base class for all user application-level exceptions.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 * @since 3.0
 */
class TApplicationException extends TException
{
}

/**
 * TInvalidOperationException class
 *
 * TInvalidOperationException represents an exception caused by invalid operations.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 * @since 3.0
 */
class TInvalidOperationException extends TSystemException
{
}

/**
 * TInvalidDataTypeException class
 *
 * TInvalidDataTypeException represents an exception caused by invalid data type.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 * @since 3.0
 */
class TInvalidDataTypeException extends TSystemException
{
}

/**
 * TInvalidDataValueException class
 *
 * TInvalidDataValueException represents an exception caused by invalid data value.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 * @since 3.0
 */
class TInvalidDataValueException extends TSystemException
{
}

/**
 * TConfigurationException class
 *
 * TConfigurationException represents an exception caused by invalid configurations,
 * such as error in an application configuration file or control template file.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 * @since 3.0
 */
class TConfigurationException extends TSystemException
{
}

/**
 * TTemplateException class
 *
 * TTemplateException represents an exception caused by invalid template syntax.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 * @since 3.1
 */
class TTemplateException extends TConfigurationException
{
	private $_template='';
	private $_lineNumber=0;
	private $_fileName='';

	/**
	 * @return string the template source code that causes the exception. This is empty if {@link getTemplateFile TemplateFile} is not empty.
	 */
	public function getTemplateSource()
	{
		return $this->_template;
	}

	/**
	 * @param string the template source code that causes the exception
	 */
	public function setTemplateSource($value)
	{
		$this->_template=$value;
	}

	/**
	 * @return string the template file that causes the exception. This could be empty if the template is an embedded template. In this case, use {@link getTemplateSource TemplateSource} to obtain the actual template content.
	 */
	public function getTemplateFile()
	{
		return $this->_fileName;
	}

	/**
	 * @param string the template file that causes the exception
	 */
	public function setTemplateFile($value)
	{
		$this->_fileName=$value;
	}

	/**
	 * @return integer the line number at which the template has error
	 */
	public function getLineNumber()
	{
		return $this->_lineNumber;
	}

	/**
	 * @param integer the line number at which the template has error
	 */
	public function setLineNumber($value)
	{
		$this->_lineNumber=TPropertyValue::ensureInteger($value);
	}
}

/**
 * TIOException class
 *
 * TIOException represents an exception related with improper IO operations.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 * @since 3.0
 */
class TIOException extends TSystemException
{
}

/**
 * TDbException class
 *
 * TDbException represents an exception related with DB operations.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 * @since 3.0
 */
class TDbException extends TSystemException
{
}

/**
 * TDbConnectionException class
 *
 * TDbConnectionException represents an exception caused by DB connection failure.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 * @since 3.0
 */
class TDbConnectionException extends TDbException
{
}

/**
 * TNotSupportedException class
 *
 * TNotSupportedException represents an exception caused by using an unsupported PRADO feature.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 * @since 3.0
 */
class TNotSupportedException extends TSystemException
{
}

/**
 * TPhpErrorException class
 *
 * TPhpErrorException represents an exception caused by a PHP error.
 * This exception is mainly thrown within a PHP error handler.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 * @since 3.0
 */
class TPhpErrorException extends TSystemException
{
	/**
	 * Constructor.
	 * @param integer error number
	 * @param string error string
	 * @param string error file
	 * @param integer error line number
	 */
	public function __construct($errno,$errstr,$errfile,$errline)
	{
		static $errorTypes=array(
			E_ERROR           => "Error",
			E_WARNING         => "Warning",
			E_PARSE           => "Parsing Error",
			E_NOTICE          => "Notice",
			E_CORE_ERROR      => "Core Error",
			E_CORE_WARNING    => "Core Warning",
			E_COMPILE_ERROR   => "Compile Error",
			E_COMPILE_WARNING => "Compile Warning",
			E_USER_ERROR      => "User Error",
			E_USER_WARNING    => "User Warning",
			E_USER_NOTICE     => "User Notice",
			E_STRICT          => "Runtime Notice"
		);
		$errorType=isset($errorTypes[$errno])?$errorTypes[$errno]:'Unknown Error';
		parent::__construct("[$errorType] $errstr (@line $errline in file $errfile).");
	}
}


/**
 * THttpException class
 *
 * THttpException represents an exception that is caused by invalid operations
 * of end-users. The {@link getStatusCode StatusCode} gives the type of HTTP error.
 * It is used by {@link TErrorHandler} to provide different error output to users.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TException.php 2606 2009-02-11 15:24:27Z haertl.mike $
 * @package System.Exceptions
 * @since 3.0
 */
class THttpException extends TSystemException
{
	private $_statusCode;

	/**
	 * Constructor.
	 * @param integer HTTP status code, such as 404, 500, etc.
	 * @param string error message. This can be a string that is listed
	 * in the message file. If so, the message in the preferred language
	 * will be used as the error message. Any rest parameters will be used
	 * to replace placeholders ({0}, {1}, {2}, etc.) in the message.
	 */
	public function __construct($statusCode,$errorMessage)
	{
		$this->_statusCode=$statusCode;
		$this->setErrorCode($errorMessage);
		$errorMessage=$this->translateErrorMessage($errorMessage);
		$args=func_get_args();
		array_shift($args);
		array_shift($args);
		$n=count($args);
		$tokens=array();
		for($i=0;$i<$n;++$i)
			$tokens['{'.$i.'}']=TPropertyValue::ensureString($args[$i]);
		parent::__construct(strtr($errorMessage,$tokens));
	}

	/**
	 * @return integer HTTP status code, such as 404, 500, etc.
	 */
	public function getStatusCode()
	{
		return $this->_statusCode;
	}
}

