using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using PHP.Core;
using System.Windows.Browser;

namespace SilverlightLoader
{
    public partial class Page : UserControl
    {
        public string Source
        {
            get { return HtmlPage.Document.GetElementById("phpsource").GetAttribute("value"); }
        }

        public Page()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(PhalangerLoader_Loaded);
        }


        void PhalangerLoader_Loaded(object sender, EventArgs e)
        {
            try
            {
                EventHandler loaded = ScriptContext.RunSilverlightApplication(LayoutRoot, Source);
                loaded(sender, e);
            }
            catch (Exception er)
            {

                TextBlock tb = new TextBlock()
                {
                    FontSize = 12,
                    Text = er.ToString(),
                    Width = 600,
                    Height = 200,
                    Foreground = new SolidColorBrush(Colors.Red)
                };


                LayoutRoot.Children.Add(tb);
            }
        }
    }
}
