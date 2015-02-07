<?
	namespace Phalanger
	{
	    use System;
	    
        use System\Windows\Forms;
        use System\Windows\Forms\Timer;
        use System\Windows\Forms\Button;
        use System\Windows\Forms\Cursor;
        
        use System\Windows\Forms\MessageBox;
        use System\Windows\Forms\MessageBoxButtons;
        use System\Windows\Forms\MessageBoxIcon;
        
        use System\Drawing\Point;

    	// A button that performs sophisticated evasive maneuvers ;) 
		class TrickyButton extends Button
		{
			const IDLE_TICK_COUNT = 10;
		
			private $timer;
			private $counter;
		
			public function __construct()
			{
				$timer = new Timer;
				$timer->Tick->Add(new System\EventHandler(array($this, "Timer_OnTick")));
				$timer->Interval = 50;
				$timer->Enabled = true;
			}
			
			protected function OnClick($eventArgs)
			{
				MessageBox::Show(
					$this,
					"Hello from Phalanger 2.0 !",
					$this->Parent->Text,
					MessageBoxButtons::OK,
					MessageBoxIcon::Information);
			}
			
			protected function OnMouseMove($e)
			{
				static $last_pos;
			
				// do not evade if this method was not called as a result of cursor movement
				$cursor = Cursor::$Position;
				if (isset($last_pos) && $last_pos->X == $cursor->X && $last_pos->Y == $cursor->Y)
				{
					return;
				}
				else
				{
					$last_pos = $cursor;
				}

				$width = $this->Size->Width;
				$height = $this->Size->Height;
				
				$dx = ($e->X > ($width / 2) ? $e->X - $width - 2 : $e->X + 2);
				$dy = ($e->Y > ($height / 2) ? $e->Y - $height - 2 : $e->Y + 2);
				
				if (($dy < 0 && $dx < $dy) || ($dy >= 0 && $dx > $dy))
				{
					$dx = ($width / 2) - $e->X;
				}
				else $dy = ($height / 2) - $e->Y;
				
				$this->EvasiveLocation = new Point(
					$this->Location->X + $dx,
					$this->Location->Y + $dy);
					
				$this->counter = 0;
			}
			
			public function Timer_OnTick()
			{
				$this->counter++;

				if ($this->counter < self::IDLE_TICK_COUNT && isset($this->EvasiveLocation))
				{
					// the cursor seems to be still hunting us
					$this->MoveTo($this->EvasiveLocation);
				}
				else
				{
					// the cursor probably gave it up
					if (isset($this->CenterLocation))
					{
						// check whether the center location is safe for us
						$cursor = Cursor::$Position;
						$cursor = $this->Parent->PointToClient($cursor);
						
     					if (!$this->IsLocationSafe($this->CenterLocation, $cursor)) return;
					
						$this->MoveTo($this->CenterLocation);
					}
				}
			}
			
			// Smoothly moves to the given location
			private function MoveTo($location)
			{
				$dx = $location->X - $this->Location->X;
				$dy = $location->Y - $this->Location->Y;
				
				if ($dx < -1 || $dx > 1) $dx /= 3;
				if ($dy < -1 || $dy > 1) $dy /= 3;
				
				$location = new Point(
					$this->Location->X + $dx,
					$this->Location->Y + $dy);

				$this->Location = $location;
			}
			
			private function IsLocationSafe($offset, $point)
			{
				return
					($point->X < $offset->X || $point->X > $offset->X + $this->Size->Width ||
					$point->Y < $offset->Y || $point->Y > $offset->Y + $this->Size->Height);
			}
		}
	}
?>
