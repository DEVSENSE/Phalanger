<?php
class Home extends TPage
{
	public function onLoad($param)
	{
		if (!$this->IsPostBack)
			$this->GameMultiView->ActiveView=$this->IntroView;
	}

	public function selectLevel($sender,$param)
	{
		if(($selection=$this->LevelSelection->SelectedValue)==='')
		{
			$this->LevelError->Visible=true;
			return;
		}
		else
			$this->Level=TPropertyValue::ensureInteger($selection);
		$this->Word=$this->generateWord();
		$this->GuessWord=str_repeat('_',strlen($this->Word));
		$this->Misses=0;
		$this->GameMultiView->ActiveView=$this->GuessView;
	}

	public function guessWord($sender,$param)
	{
		$sender->Enabled=false;
		$letter=$sender->Text;
		$word=$this->Word;
		$guessWord=$this->GuessWord;
		$pos=0;
		$success=false;
		while(($pos=strpos($word,$letter,$pos))!==false)
		{
			$guessWord[$pos]=$letter;
			$success=true;
			$pos++;
		}
		if($success)
		{
			$this->GuessWord=$guessWord;
			if($guessWord===$word)
				$this->GameMultiView->ActiveView=$this->WinView;
		}
		else
		{
			$this->Misses++;
			if($this->Misses>=$this->Level)
				$this->giveUp(null,null);
		}
	}

	public function giveUp($sender,$param)
	{
		$this->GameMultiView->ActiveView=$this->LoseView;
	}

	public function startAgain($sender,$param)
	{
		$this->GameMultiView->ActiveView=$this->IntroView;
		$this->LevelError->Visible=false;
		for($letter=65;$letter<=90;++$letter)
		{
			$guessLetter='Guess'.chr($letter);
			$this->$guessLetter->Enabled=true;
		}
	}

	protected function generateWord()
	{
		$wordFile=dirname(__FILE__).'/words.txt';
		$words=preg_split("/[\s,]+/",file_get_contents($wordFile));
		do
		{
			$i=rand(0,count($words)-1);
			$word=$words[$i];
		} while(strlen($word)<5 || !preg_match('/^[a-z]*$/i',$word));
		return strtoupper($word);
	}

	public function setLevel($value)
	{
		$this->setViewState('Level',$value,0);
	}

	public function getLevel()
	{
		return $this->getViewState('Level',0);
	}

	public function setWord($value)
	{
		$this->setViewState('Word',$value,'');
	}

	public function getWord()
	{
		return $this->getViewState('Word','');
	}

	public function getGuessWord()
	{
		return $this->getViewState('GuessWord','');
	}

	public function setGuessWord($value)
	{
		$this->setViewState('GuessWord',$value,'');
	}

	public function setMisses($value)
	{
		$this->setViewState('Misses',$value,0);
	}

	public function getMisses()
	{
		return $this->getViewState('Misses',0);
	}
}
?>