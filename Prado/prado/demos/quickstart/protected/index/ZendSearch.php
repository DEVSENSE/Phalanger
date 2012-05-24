<?php
/*
 * Created on 7/05/2006
 */

class ZendSearch extends TModule
{
	private $_data;
	private $_ZF;
	private $_search;
	
	public function setIndexDataDirectory($path)
	{
		$this->_data = Prado::getPathOfNamespace($path);
	}
	
	public function getIndexDataDirectory()
	{
		return $this->_data;
	}
	
	public function setZendFramework($path)
	{
		$this->_ZF = Prado::getPathOfNamespace($path);
	}
	
	protected function importZendNamespace()
	{
		if(is_null(Prado::getPathOfAlias('Zend')))
		{
			$zendBase = !is_null($this->_ZF) ? $this->_ZF.'.*' : 'Application.index.*';
			$path = !is_null($this->_ZF) ? $this->_ZF.'.Zend.*' : 'Application.index.Zend.*';
			Prado::using($zendBase);
			Prado::setPathOfAlias('Zend', Prado::getPathOfNamespace($path));
		}
	}
	
	protected function getZendSearch()
	{
		if(is_null($this->_search))
		{
			$this->importZendNamespace();
			Prado::using('Zend.Search.Lucene');
		 	$this->_search = new Zend_Search_Lucene($this->_data);
		}
		return $this->_search;
	}
	
	public function find($query)
	{
		return $this->getZendSearch()->find(strtolower($query));
	}
} 

?>