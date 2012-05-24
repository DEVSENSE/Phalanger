<?php

class ViewSource extends TPage
{
	private $_path=null;
	private $_fullPath=null;
	private $_fileType=null;

	public function onLoad($param)
	{
		parent::onLoad($param);
		$path=$this->Request['path'];
		$fullPath=realpath($this->Service->BasePath.'/'.$path);
		$fileExt=$this->getFileExtension($fullPath);
		if($fullPath!==false && is_file($fullPath) && strpos($fullPath,$this->Service->BasePath)!==false)
		{
 			if($this->isFileTypeAllowed($fileExt))
 			{
				$this->_fullPath=strtr($fullPath,'\\','/');
				$this->_path=strtr(substr($fullPath,strlen($this->Service->BasePath)),'\\','/');
 			}
		}
		if($this->_fullPath===null)
			throw new THttpException(500,'File Not Found: %s',$path);

		$this->SourceList->DataSource=$this->SourceFiles;
		$this->SourceList->dataBind();

		$this->Highlighter->Language=$this->getFileLanguage($fileExt);
		if($this->Request['lines']==='false')
			$this->Highlighter->ShowLineNumbers=false;
		$this->SourceView->Text=file_get_contents($this->_fullPath);
	}

	public function getFilePath()
	{
		return $this->_path;
	}

	protected function getSourceFiles()
	{
		$list=array();
		$basePath=dirname($this->_fullPath);
		if($dh=opendir($basePath))
		{
			while(($file=readdir($dh))!==false)
			{
				if(is_file($basePath.'/'.$file))
				{
					$extension=$this->getFileExtension($basePath.'/'.$file);
					if($this->isFileTypeAllowed($extension))
					{
						$fileType=$this->getFileType($extension);
						$list[]=array(
							'name'=>$file,
							'type'=>$fileType,
							'active'=>basename($this->_fullPath)===$file,
							'url'=>'?page=ViewSource&amp;path=/'.ltrim(strtr(dirname($this->_path),'\\','/').'/'.$file,'/')
						);
					}
				}

			}
			closedir($dh);
		}
		foreach($list as $item)
			$aux[]=$item['name'];
		array_multisort($aux, SORT_ASC, $list);
		return $list;
	}

	protected function isFileTypeAllowed($extension)
	{
		return in_array($extension,array('tpl','page','php','html'));
	}

	protected function getFileExtension($fileName)
	{
		if(($pos=strrpos($fileName,'.'))===false)
			return '';
		else
			return substr($fileName,$pos+1);
	}

	protected function getFileType($extension)
	{
		if($extension==='tpl' || $extension==='page')
			return 'Template file';
		else
			return 'Class file';
	}

	protected function getFileLanguage($extension)
	{
		switch($extension)
		{
			case 'page' :
			case 'tpl' :
				return 'prado';
			case 'php' :
				return 'php';
				break;
			case 'xml' :
				return 'xml';
				break;
			default :
				return 'html';
		}
	}
}

?>