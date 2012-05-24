<?php

class Home extends TPage
{
	public function fileUploaded($sender,$param)
	{
		if($sender->HasFile)
		{
			$this->Result->Text="
				You just uploaded a file:
				<br/>
				Name: {$sender->FileName}
				<br/>
				Size: {$sender->FileSize}
				<br/>
				Type: {$sender->FileType}";
		}
	}
}

?>