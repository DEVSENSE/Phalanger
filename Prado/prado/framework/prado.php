<?php
/**
 * Prado bootstrap file.
 *
 * This file is intended to be included in the entry script of Prado applications.
 * It defines Prado class by extending PradoBase, a static class providing globally
 * available functionalities that enable PRADO component model and error handling mechanism.
 *
 * By including this file, the PHP error and exception handlers are set as
 * PRADO handlers, and an __autoload function is provided that automatically
 * loads a class file if the class is not defined.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: prado.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 */

/**
 * Includes the PradoBase class file
 */
require_once(dirname(__FILE__).'/PradoBase.php');

/**
 * Defines Prado class if not defined.
 */
if(!class_exists('Prado',false))
{
	class Prado extends PradoBase
	{
	}
}

/**
 * Registers the autoload function.
 * Since Prado::autoload will report a fatal error if the class file
 * cannot be found, if you have multiple autoloaders, Prado::autoload
 * should be registered in the last.
 */
spl_autoload_register(array('Prado','autoload'));

/**
 * Initializes error and exception handlers
 */
Prado::initErrorHandlers();

/**
 * Includes TApplication class file
 */
require_once(dirname(__FILE__).'/TApplication.php');

/**
 * Includes TShellApplication class file
 */
require_once(dirname(__FILE__).'/TShellApplication.php');

