<?

namespace WinForms{
    
    use System;
    use System\Windows\Forms;
    use System\Windows\Forms\MouseButtons;
    use System\Windows\Forms\FormWindowState;

    use System\Drawing\Bitmap;
    use System\Drawing\Point;
    use System\Drawing\Imaging\PixelFormat;

    use System\IDisposable;
    use System\Environment;

    use Phalanger\MandelbrotSet;
	
    partial class Form1 extends Forms\Form
	{
        private $bitmap;
		private $mandelbrot;

        private $row;
		private $resolution;
		
		private function CreateBitmap()
		{
			$width = $this->ClientSize->Width;
			$height = $this->ClientSize->Height;

			$bitmap = new Bitmap($width, $height, PixelFormat::Format24bppRgb);

			$old_bitmap = $this->BackgroundImage;
			$this->BackgroundImage = $bitmap;

			if ($old_bitmap instanceof IDisposable) $old_bitmap->Dispose();
			
			$this->timer->Enabled = true;
			
			$this->row = 0;
			$this->resolution = 8;
		}
		
		private function Form1_Load(System\Object $sender, System\EventArgs $e)
		{
            $this->mandelbrot = new MandelbrotSet($this->ClientSize->Width, $this->ClientSize->Height);
            
            $this->Resize();
        }
        
        private function Resize()
        {
            // update the button's center location
			$this->button->CenterLocation = new Point(
				($this->ClientSize->Width - $this->button->Size->Width) / 2,
				($this->ClientSize->Height - $this->button->Size->Height) / 2);

			// recreate background image and initiate computation of a new Mandelbrot set
			$this->CreateBitmap();
			$this->mandelbrot->SetSize($this->ClientSize->Width, $this->ClientSize->Height);
        }
        
        private function Form1_Resize(System\Object $sender, System\EventArgs $e)
        {
            if ($this->WindowState != FormWindowState::Minimized)
			{
				$this->Resize();
			}
        }
        
        private function Form1_Move(System\Object $sender, System\EventArgs $e)
        {
            static $last_x, $last_y;
				
			$dx = $last_x - $this->Location->X;
			$dy = $last_y - $this->Location->Y;
			
			// update the button's position to give a "inertia" feel
			$this->button->Location = new Point(
				$this->button->Location->X + $dx,
				$this->button->Location->Y + $dy);
				
			$last_x = $this->Location->X;
			$last_y = $this->Location->Y;
        }
        
         private function Form1_Click(System\Object $sender, System\EventArgs $e)
         {
        	// set a new center point of the Mandelbrot set and zoom in/out
			$this->mandelbrot->SetCenter($e->X, $e->Y);
			
			if ($e->Button == MouseButtons::Left) $this->mandelbrot->ZoomIn();
			else $this->mandelbrot->ZoomOut();
			
			$this->CreateBitmap();
         }
         
         private function timer_Tick(System\Object $sender, System\EventArgs $e)
         {
            $bitmap = $this->BackgroundImage;
			
			$ticks = Environment::$TickCount + 2;
			do
			{
				$y = $this->row;
				$res = $this->resolution;
				
				if ($y >= $bitmap->Height)
				{
					if ($this->resolution == 1)
					{
						// this Mandelbrot set is fully rendered
						if (isset($this->timer))
						{
							$this->timer->Enabled = 0;
							unset($this->timer);
						}
					}
					else
					{
						// improve the resolution
						$this->resolution = ($res >>= 1);
						$this->row = $y = 0;
					}
					break;
				}
				
				$y1 = $y + $res - 1;
				if ($y1 >= $bitmap->Height) $y1 = $bitmap->Height - 1;
				
				// draw a single line
				for ($x = 0; $x < $bitmap->Width; $x++)
				{
					if (($x % $res) == 0) $color = $this->mandelbrot->GetColor($x, $y);
					
					for ($ry = $y; $ry <= $y1; $ry++)
					{
						$bitmap->SetPixel($x, $ry, $color);
					}
				}
				
				$this->row += $res;
			}
			while (Environment::$TickCount < $ticks);
			
			$this->Refresh();
         }
	}    
}
?>