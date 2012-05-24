<?php

class NorthwindCrud extends TPage
{
	function onInit($param)
	{
		$classes = $this->getRecordClassList(Prado::getPathOfNamespace('Application.database.*'));
		$this->class_list->dataSource = $classes;
		$this->class_list->dataBind();
	}

	protected function getRecordClassList($directory)
	{
		$list=array();
		$folder=@opendir($directory);
		while($entry=@readdir($folder))
		{
			if($entry[0]==='.')
				continue;
			else if(is_file($directory.'/'.$entry) && strpos($entry,'.php'))
				$list[] = str_replace('.php', '', $entry);
		}
		closedir($folder);
		return $list;
	}
}

?>