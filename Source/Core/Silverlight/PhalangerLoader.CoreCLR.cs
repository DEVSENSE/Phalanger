using System;
using System.Windows.Controls;

using PHP.Core;
using System.Windows.Media;
//using System.Windows.Browser.Net;
using System.Net;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace PHP.Silverlight
{	
	public class PhalangerLoader : Panel
	{
        private string _source;

        public string Source
        {
            get { return _source; }
            set { _source = value; }
        }


		public PhalangerLoader():
            base()

		{
            //DefaultStyleKey = typeof(PhalangerLoader);

			this.Loaded += new RoutedEventHandler(PhalangerLoader_Loaded);
		}

		void PhalangerLoader_Loaded(object sender, EventArgs e)
		{
			var c = Parent as Canvas;
			try
			{
				EventHandler loaded = ScriptContext.RunSilverlightApplication(c, Source);
				loaded(sender, e);
			}
			catch (Exception er)
			{
				TextBlock tb = new TextBlock() 
					{ FontSize = 9, Text = Source, Width = 600, Height = 200,
						Foreground = new SolidColorBrush(Colors.Black) };
				c.Children.Add(tb);
				tb.Text = er.ToString();
			}
		}
	}
}
