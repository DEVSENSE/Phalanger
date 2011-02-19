using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace WebManager
{
	class App
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			string iis;


			System.ServiceProcess.ServiceController s = new System.ServiceProcess.ServiceController("W3SVC");
			try
			{
				iis = s.ServiceName;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "IIS *must* be installed for this to work!");
				return;
			}

			//TODO: Application.ThreadException += new ThreadExceptionEventHandler(form.GlobalExceptionHandler);
			Application.EnableVisualStyles();
			ConfigForm form = new ConfigForm();
			Application.Run(form);
		}
	}
}
