using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Markup;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library
{
    public static class Utils
    {

        [ImplementsFunction("loadfile")]
        public static void LoadFile(string path, PhpCallback function)
        {
            string file = string.Empty;

            if (function == null)
            {
                PhpException.ArgumentNull("function");
                return;
            }
            if (function.IsInvalid) return;

            WebClient webclient = new WebClient();
            webclient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(
                delegate(object sender, DownloadStringCompletedEventArgs downEventArgs)
                {
                    var canvas = ((ClrObject)ScriptContext.CurrentContext.AutoGlobals.Canvas.Value).RealObject as System.Windows.Controls.Canvas;

                    canvas.Dispatcher.BeginInvoke(() =>
                        {
                            function.Invoke(downEventArgs.Result);

                        });
                }
                );

            var source_root = ((ClrObject)ScriptContext.CurrentContext.AutoGlobals.Addr.Value).RealObject as string;

            Uri baseUri = new Uri(source_root + "/", UriKind.Absolute);
            Uri uriFile = new Uri(path, UriKind.RelativeOrAbsolute);
            Uri uri = new Uri(baseUri, uriFile);


            webclient.DownloadStringAsync(uri);

            //downloadFinished.WaitOne();

            //return XamlReader.Load(file);

        }



    }
}
