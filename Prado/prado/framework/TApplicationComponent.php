<?php
/**
 * TApplicationComponent class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TApplicationComponent.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 */

/**
 * TApplicationComponent class
 *
 * TApplicationComponent is the base class for all components that are
 * application-related, such as controls, modules, services, etc.
 *
 * TApplicationComponent mainly defines a few properties that are shortcuts
 * to some commonly used methods. The {@link getApplication Application}
 * property gives the application instance that this component belongs to;
 * {@link getService Service} gives the current running service;
 * {@link getRequest Request}, {@link getResponse Response} and {@link getSession Session}
 * return the request and response modules, respectively;
 * And {@link getUser User} gives the current user instance.
 *
 * Besides, TApplicationComponent defines two shortcut methods for
 * publishing private files: {@link publishAsset} and {@link publishFilePath}.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TApplicationComponent.php 2541 2008-10-21 15:05:13Z qiang.xue $
 * @package System
 * @since 3.0
 */
class TApplicationComponent extends TComponent
{
	/**
	 * @return TApplication current application instance
	 */
	public function getApplication()
	{
		return Prado::getApplication();
	}

	/**
	 * @return IService the current service
	 */
	public function getService()
	{
		return Prado::getApplication()->getService();
	}

	/**
	 * @return THttpRequest the current user request
	 */
	public function getRequest()
	{
		return Prado::getApplication()->getRequest();
	}

	/**
	 * @return THttpResponse the response
	 */
	public function getResponse()
	{
		return Prado::getApplication()->getResponse();
	}

	/**
	 * @return THttpSession user session
	 */
	public function getSession()
	{
		return Prado::getApplication()->getSession();
	}

	/**
	 * @return IUser information about the current user
	 */
	public function getUser()
	{
		return Prado::getApplication()->getUser();
	}

	/**
	 * Publishes a private asset and gets its URL.
	 * This method will publish a private asset (file or directory)
	 * and gets the URL to the asset. Note, if the asset refers to
	 * a directory, all contents under that directory will be published.
	 * Also note, it is recommended that you supply a class name as the second
	 * parameter to the method (e.g. publishAsset($assetPath,__CLASS__) ).
	 * By doing so, you avoid the issue that child classes may not work properly
	 * because the asset path will be relative to the directory containing the child class file.
	 *
	 * @param string path of the asset that is relative to the directory containing the specified class file.
	 * @param string name of the class whose containing directory will be prepend to the asset path. If null, it means get_class($this).
	 * @return string URL to the asset path.
	 */
	public function publishAsset($assetPath,$className=null)
	{
		if($className===null)
			$className=get_class($this);
		$class=new ReflectionClass($className);
		$fullPath=dirname($class->getFileName()).DIRECTORY_SEPARATOR.$assetPath;
		return $this->publishFilePath($fullPath);
	}

	/**
	 * Publishes a file or directory and returns its URL.
	 * @param string absolute path of the file or directory to be published
	 * @return string URL to the published file or directory
	 */
	public function publishFilePath($fullPath)
	{
		return Prado::getApplication()->getAssetManager()->publishFilePath($fullPath);
	}
}

