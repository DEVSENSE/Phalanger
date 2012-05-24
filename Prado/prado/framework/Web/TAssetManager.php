<?php
/**
 * TAssetManager class
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TAssetManager.php 2674 2009-06-07 07:38:09Z godzilla80@gmx.net $
 * @package System.Web
 */

/**
 * TAssetManager class
 *
 * TAssetManager provides a scheme to allow web clients visiting
 * private files that are normally web-inaccessible.
 *
 * TAssetManager will copy the file to be published into a web-accessible
 * directory. The default base directory for storing the file is "assets", which
 * should be under the application directory. This can be changed by setting
 * the {@link setBasePath BasePath} property together with the
 * {@link setBaseUrl BaseUrl} property that refers to the URL for accessing the base path.
 *
 * By default, TAssetManager will not publish a file or directory if it already
 * exists in the publishing directory and has an older modification time.
 * If the application mode is set as 'Performance', the modification time check
 * will be skipped. You can explicitly require a modification time check
 * with the function {@link publishFilePath}. This is usually
 * very useful during development.
 *
 * TAssetManager may be configured in application configuration file as follows,
 * <code>
 * <module id="asset" BasePath="Application.assets" BaseUrl="/assets" />
 * </code>
 * where {@link getBasePath BasePath} and {@link getBaseUrl BaseUrl} are
 * configurable properties of TAssetManager. Make sure that BasePath is a namespace
 * pointing to a valid directory writable by the Web server process.
 *
 * @author Qiang Xue <qiang.xue@gmail.com>
 * @version $Id: TAssetManager.php 2674 2009-06-07 07:38:09Z godzilla80@gmx.net $
 * @package System.Web
 * @since 3.0
 */
class TAssetManager extends TModule
{
	/**
	 * Default web accessible base path for storing private files
	 */
	const DEFAULT_BASEPATH='assets';
	/**
	 * @var string base web accessible path for storing private files
	 */
	private $_basePath=null;
	/**
	 * @var string base URL for accessing the publishing directory.
	 */
	private $_baseUrl=null;
	/**
	 * @var boolean whether to use timestamp checking to ensure files are published with up-to-date versions.
	 */
	private $_checkTimestamp=false;
	/**
	 * @var TApplication application instance
	 */
	private $_application;
	/**
	 * @var array published assets
	 */
	private $_published=array();
	/**
	 * @var boolean whether the module is initialized
	 */
	private $_initialized=false;

	/**
	 * Initializes the module.
	 * This method is required by IModule and is invoked by application.
	 * @param TXmlElement module configuration
	 */
	public function init($config)
	{
		$application=$this->getApplication();
		if($this->_basePath===null)
			$this->_basePath=dirname($application->getRequest()->getApplicationFilePath()).DIRECTORY_SEPARATOR.self::DEFAULT_BASEPATH;
		if(!is_writable($this->_basePath) || !is_dir($this->_basePath))
			throw new TConfigurationException('assetmanager_basepath_invalid',$this->_basePath);
		if($this->_baseUrl===null)
			$this->_baseUrl=rtrim(dirname($application->getRequest()->getApplicationUrl()),'/\\').'/'.self::DEFAULT_BASEPATH;
		$application->setAssetManager($this);
		$this->_initialized=true;
	}

	/**
	 * @return string the root directory storing published asset files
	 */
	public function getBasePath()
	{
		return $this->_basePath;
	}

	/**
	 * Sets the root directory storing published asset files.
	 * The directory must be in namespace format.
	 * @param string the root directory storing published asset files
	 * @throws TInvalidOperationException if the module is initialized already
	 */
	public function setBasePath($value)
	{
		if($this->_initialized)
			throw new TInvalidOperationException('assetmanager_basepath_unchangeable');
		else
		{
			$this->_basePath=Prado::getPathOfNamespace($value);
			if($this->_basePath===null || !is_dir($this->_basePath) || !is_writable($this->_basePath))
				throw new TInvalidDataValueException('assetmanager_basepath_invalid',$value);
		}
	}

	/**
	 * @return string the base url that the published asset files can be accessed
	 */
	public function getBaseUrl()
	{
		return $this->_baseUrl;
	}

	/**
	 * @param string the base url that the published asset files can be accessed
	 * @throws TInvalidOperationException if the module is initialized already
	 */
	public function setBaseUrl($value)
	{
		if($this->_initialized)
			throw new TInvalidOperationException('assetmanager_baseurl_unchangeable');
		else
			$this->_baseUrl=rtrim($value,'/');
	}

	/**
	 * Publishes a file or a directory (recursively).
	 * This method will copy the content in a directory (recursively) to
	 * a web accessible directory and returns the URL for the directory.
	 * If the application is not in performance mode, the file modification
	 * time will be used to make sure the published file is latest or not.
	 * If not, a file copy will be performed.
	 * @param string the path to be published
	 * @param boolean If true, file modification time will be checked even if the application
	 * is in performance mode.
	 * @return string an absolute URL to the published directory
	 * @throws TInvalidDataValueException if the file path to be published is
	 * invalid
	 */
	public function publishFilePath($path,$checkTimestamp=false)
	{
		if(isset($this->_published[$path]))
			return $this->_published[$path];
		else if(empty($path) || ($fullpath=realpath($path))===false)
			throw new TInvalidDataValueException('assetmanager_filepath_invalid',$path);
		else if(is_file($fullpath))
		{
			$dir=$this->hash(dirname($fullpath));
			$fileName=basename($fullpath);
			$dst=$this->_basePath.DIRECTORY_SEPARATOR.$dir;
			if(!is_file($dst.DIRECTORY_SEPARATOR.$fileName) || $checkTimestamp || $this->getApplication()->getMode()!==TApplicationMode::Performance)
				$this->copyFile($fullpath,$dst);
			return $this->_published[$path]=$this->_baseUrl.'/'.$dir.'/'.$fileName;
		}
		else
		{
			$dir=$this->hash($fullpath);
			if(!is_dir($this->_basePath.DIRECTORY_SEPARATOR.$dir) || $checkTimestamp || $this->getApplication()->getMode()!==TApplicationMode::Performance)
			{
				Prado::trace("Publishing directory $fullpath",'System.Web.UI.TAssetManager');
				$this->copyDirectory($fullpath,$this->_basePath.DIRECTORY_SEPARATOR.$dir);
			}
			return $this->_published[$path]=$this->_baseUrl.'/'.$dir;
		}
	}

	/**
	 * @return array List of published assets
	 * @since 3.1.6
	 */
	protected function getPublished()
	{
		return $this->_published;
	}

	/**
	 * @param $values List of published assets
	 * @since 3.1.6
	 */
	protected function setPublished($values=array())
	{
		$this->_published = $values;
	}

	/**
	 * Returns the published path of a file path.
	 * This method does not perform any publishing. It merely tells you
	 * if the file path is published, where it will go.
	 * @param string directory or file path being published
	 * @return string the published file path
	 */
	public function getPublishedPath($path)
	{
		$path=realpath($path);
		if(is_file($path))
			return $this->_basePath.DIRECTORY_SEPARATOR.$this->hash(dirname($path)).DIRECTORY_SEPARATOR.basename($path);
		else
			return $this->_basePath.DIRECTORY_SEPARATOR.$this->hash($path);
	}

	/**
	 * Returns the URL of a published file path.
	 * This method does not perform any publishing. It merely tells you
	 * if the file path is published, what the URL will be to access it.
	 * @param string directory or file path being published
	 * @return string the published URL for the file path
	 */
	public function getPublishedUrl($path)
	{
		$path=realpath($path);
		if(is_file($path))
			return $this->_baseUrl.'/'.$this->hash(dirname($path)).'/'.basename($path);
		else
			return $this->_baseUrl.'/'.$this->hash($path);
	}

	/**
	 * Generate a CRC32 hash for the directory path. Collisions are higher
	 * than MD5 but generates a much smaller hash string.
	 * @param string string to be hashed.
	 * @return string hashed string.
	 */
	protected function hash($dir)
	{
		return sprintf('%x',crc32($dir.Prado::getVersion()));
	}

	/**
	 * Copies a file to a directory.
	 * Copying is done only when the destination file does not exist
	 * or has an older file modification time.
	 * @param string source file path
	 * @param string destination directory (if not exists, it will be created)
	 */
	protected function copyFile($src,$dst)
	{
		if(!is_dir($dst))
		{
			@mkdir($dst);
			@chmod($dst, PRADO_CHMOD);
		}
		$dstFile=$dst.DIRECTORY_SEPARATOR.basename($src);
		if(@filemtime($dstFile)<@filemtime($src))
		{
			Prado::trace("Publishing file $src to $dstFile",'System.Web.TAssetManager');
			@copy($src,$dstFile);
		}
	}

	/**
	 * Copies a directory recursively as another.
	 * If the destination directory does not exist, it will be created.
	 * File modification time is used to ensure the copied files are latest.
	 * @param string the source directory
	 * @param string the destination directory
	 * @todo a generic solution to ignore certain directories and files
	 */
	public function copyDirectory($src,$dst)
	{
		if(!is_dir($dst))
		{
			@mkdir($dst);
			@chmod($dst, PRADO_CHMOD);
		}
		if($folder=@opendir($src))
		{
			while($file=@readdir($folder))
			{
				if($file==='.' || $file==='..' || $file==='.svn')
					continue;
				else if(is_file($src.DIRECTORY_SEPARATOR.$file))
				{
					if(@filemtime($dst.DIRECTORY_SEPARATOR.$file)<@filemtime($src.DIRECTORY_SEPARATOR.$file))
					{
						@copy($src.DIRECTORY_SEPARATOR.$file,$dst.DIRECTORY_SEPARATOR.$file);
						@chmod($dst.DIRECTORY_SEPARATOR.$file, PRADO_CHMOD);
					}
				}
				else
					$this->copyDirectory($src.DIRECTORY_SEPARATOR.$file,$dst.DIRECTORY_SEPARATOR.$file);
			}
			closedir($folder);
		} else {
			throw new TInvalidDataValueException('assetmanager_source_directory_invalid', $src);
		}
	}

	/**
	 * Publish a tar file by extracting its contents to the assets directory.
	 * Each tar file must be accomplished with its own MD5 check sum file.
	 * The MD5 file is published when the tar contents are successfully
	 * extracted to the assets directory. The presence of the MD5 file
	 * as published asset assumes that the tar file has already been extracted.
	 * @param string tar filename
	 * @param string MD5 checksum for the corresponding tar file.
	 * @param boolean Wether or not to check the time stamp of the file for publishing. Defaults to false.
	 * @return string URL path to the directory where the tar file was extracted.
	 */
	public function publishTarFile($tarfile, $md5sum, $checkTimestamp=false)
	{
		if(isset($this->_published[$md5sum]))
			return $this->_published[$md5sum];
		else if(($fullpath=realpath($md5sum))===false || !is_file($fullpath))
			throw new TInvalidDataValueException('assetmanager_tarchecksum_invalid',$md5sum);
		else
		{
			$dir=$this->hash(dirname($fullpath));
			$fileName=basename($fullpath);
			$dst=$this->_basePath.DIRECTORY_SEPARATOR.$dir;
			if(!is_file($dst.DIRECTORY_SEPARATOR.$fileName) || $checkTimestamp || $this->getApplication()->getMode()!==TApplicationMode::Performance)
			{
				if(@filemtime($dst.DIRECTORY_SEPARATOR.$fileName)<@filemtime($fullpath))
				{
					$this->copyFile($fullpath,$dst);
					$this->deployTarFile($tarfile,$dst);
				}
			}
			return $this->_published[$md5sum]=$this->_baseUrl.'/'.$dir;
		}
	}

	/**
	 * Extracts the tar file to the destination directory.
	 * N.B Tar file must not be compressed.
	 * @param string tar file
	 * @param string path where the contents of tar file are to be extracted
	 * @return boolean true if extract successful, false otherwise.
	 */
	protected function deployTarFile($path,$destination)
	{
		if(($fullpath=realpath($path))===false || !is_file($fullpath))
			throw new TIOException('assetmanager_tarfile_invalid',$path);
		else
		{
			Prado::using('System.IO.TTarFileExtractor');
			$tar = new TTarFileExtractor($fullpath);
			return $tar->extract($destination);
		}
	}

}

?>
