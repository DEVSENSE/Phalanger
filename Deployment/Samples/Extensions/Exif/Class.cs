using System;
using System.Collections.Generic;
using PHP.Core;
using PHP.Library;

namespace ExtensionSamples
{
    /// <summary>
	/// Uses the php_exif extension to extract EXIF headers from an image file.
	/// </summary>
	class ExifSample
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Console.WriteLine("Enter path to a JPEG or TIFF file:");
			
			PhpArray exif = Exif.read_exif_data(new PhpBytes(Console.ReadLine()));

			if (exif != null)
			{
				foreach (KeyValuePair<PHP.Core.IntStringKey, object> section in exif)
				{
					PhpArray array = section.Value as PhpArray;
					if (array != null)
					{
						foreach (KeyValuePair<PHP.Core.IntStringKey, object> entry in array)
						{
							Console.WriteLine("{0}.{1}: {2}", section.Key.Object, entry.Key.Object, entry.Value);
						}
					}
					else Console.WriteLine("{0}: {1}", section.Key.Object, section.Value);
				}
			}
			else Console.WriteLine("Error reading EXIF headers from the file.");

			Console.ReadLine();
		}
	}
}
