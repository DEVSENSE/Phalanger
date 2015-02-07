<?
	namespace Phalanger
	{
        use System\Random;
        use System\Drawing\Color;

		// Computes the Mandelbrot set
		class MandelbrotSet
		{
			const PRECISION = 64;
			const TRESHOLD = 4;
		
			private $width;
			private $height;
		
			// initial values:;
			private $cx = -0.6;
			private $cy = 0;
			private $zoom = 0.0075;
			
			private $colorTable;
			
			public function __construct($width, $height)
			{
				$this->width = $width;
				$this->height = $height;
				
				// initialize color table with random values
				$random = new Random;
				
				for ($i = 0; $i < self::PRECISION; $i++)
				{
					$this->colorTable[$i][0] = $random->Next(256);
					$this->colorTable[$i][1] = $random->Next(256);
					$this->colorTable[$i][2] = $random->Next(256);
				}
			}

			public function SetSize($width, $height)
			{
				$this->width = $width;
				$this->height = $height;
			}
			
			public function SetCenter($x, $y)
			{
				$this->cx = $this->cx + ($x - $this->width / 2) * $this->zoom;
				$this->cy = $this->cy + ($y - $this->height / 2) * $this->zoom;
			}
			
			public function ZoomIn()
			{
				$this->zoom /= 1.5;
			}
			
			public function ZoomOut()
			{
				$this->zoom *= 1.5;
			}
			
			// Returns the color that should be drawn at the specified pixel coordinates
			public function GetColor($x, $y)
			{
				$mx = $this->cx + ($x - $this->width / 2) * $this->zoom;
				$my = $this->cy + ($y - $this->height / 2) * $this->zoom;
				
				$zx = 0;
				$zy = 0;
				for ($i = 0; $i < self::PRECISION; $i++)
				{
					$zx2 = $zx * $zx;
					$zy2 = $zy * $zy;

					$zy = 2 * $zx * $zy + $my;
					$zx = $zx2 - $zy2 + $mx;
					
					// how many iteretations did it take to reach the treshold?
					if ($zx2 + $zy2 > self::TRESHOLD)
					{
						return Color::FromArgb(
							$this->colorTable[$i][0],
							$this->colorTable[$i][1],
							$this->colorTable[$i][2]);
					}
   				}
				
				return Color::$Blue;
			}
		}
	}
?>
