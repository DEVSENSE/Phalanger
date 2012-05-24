<?php

Prado::using('System.Data.*');
Prado::using('System.Web.UI.ActiveControls.*');
Prado::using('System.Data.ActiveRecord.TActiveRecordManager');

$db = new TDbConnection('mysql:host=localhost;dbname=xxxx', 'yyyy', 'zzzz');
$manager = TActiveRecordManager::getInstance();
$manager->setDbConnection($db);

class CommentRecord extends TActiveRecord
{
	const TABLE='qs_comments';

	public $id;
	public $username;
	public $date_added;
	public $page;
	public $block_id;
	public $content;

	public static function finder($className=__CLASS__)
	{
		return parent::finder($className);
	}
}

class CommentBlock extends TTemplateControl
{
	private $_page;

	function onLoad($param)
	{
		if(!$this->Page->IsCallBack)
		{
			$count = array();
			$data = $this->getCommentData();
			foreach($data as $r)
			{
				if(!isset($count[$r->block_id]))
					$count[$r->block_id]=0;
				$count[$r->block_id]++;
			}
			$js = "var comment_count = ".TJavascript::encode($count).";\n";
			$this->Page->ClientScript->registerBeginScript('count',$js);
			$this->comments->dataSource = $data;
			$this->comments->dataBind();
		}
	}

	function getCommentData()
	{
		return CommentRecord::finder()->findAllByPage($this->getCurrentPagePath());
	}

	function add_comment($sender, $param)
	{
		if(!$this->Page->IsValid)
			return;
		$record = new CommentRecord;
		$record->username = $this->username->Text;
		$record->date_added = date('Y-m-d h:i:s');
		$record->page = $this->getCurrentPagePath();
		$record->block_id = $this->block_id->Value;
		$record->content = $this->content->Text;
		$record->save();

		$this->content->Text = '';
		$this->password->Text = '';
		$cc = $this->Page->CallbackClient;
		$cc->appendContent('comment-list', $this->format_message($record));
		$cc->callClientFunction('hide_add_comment');
		$cc->callClientFunction('increment_count_tag', $record->block_id);
		if(!$this->Page->IsCallBack)
		{
			$this->comments->dataSource = $this->getCommentData();
			$this->comments->dataBind();
		}
	}

	protected function getCurrentPagePath()
	{
		if(is_null($this->_page))
		{
			$page = str_replace($this->Service->BasePath, '', $this->Page->Template->TemplateFile);
			$this->_page = str_replace('\\', '/', $page);
		}
		return $this->_page;
	}

	function validate_credential($sender, $param)
	{
		$param->IsValid = $this->password->Text == 'Prado';
	}

	protected function format_message($record)
	{
		$username=htmlspecialchars($record->username);
		$content=nl2br(htmlspecialchars($record->content));
		return <<<EOD
	<div class="comment c-{$record->block_id}">
		<span><strong>{$username}</strong> on {$record->date_added}.</span>
		<div>{$content}</div>
	</div>
EOD;
	}
}