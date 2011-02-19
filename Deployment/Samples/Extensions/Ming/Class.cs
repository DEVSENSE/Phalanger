using System;
using PHP.Core;
using PHP.Library;

namespace ExtensionSamples
{
	/// <summary>
	/// Uses the php_ming extension to create a Flash movie with rotating red square.
	/// The movie is saved as "ming_test.swf" in current directory.
	/// </summary>
	class MingSample
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			SWFShape s = new SWFShape();
			PhpObject obj = s.addFill(0xff, 0, 0);
			s.setRightFill(obj);
			s.movePenTo(-50,-50);
			s.drawLineTo(50,-50);
			s.drawLineTo(50,50);
			s.drawLineTo(-50,50);
			s.drawLineTo(-50,-50);

			SWFSprite p = new SWFSprite();
			SWFDisplayItem i = p.add(s) as SWFDisplayItem;

			for(int j = 0; j < 17; j++)
			{
				p.nextFrame();
				i.rotate(5);
			}
			p.nextFrame();

			SWFMovie m = new SWFMovie();
			i = m.add(p) as SWFDisplayItem;
			i.moveTo(160,120);
			i.setName(new PhpBytes("item"));

			m.setBackground(0xff, 0xff, 0xff);
			m.setDimension(320,240);

			m.save(new PhpBytes("ming_test.swf"));
		}
	}
}
