using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.IO;

namespace TestApp
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
#if DEBUG
            string dynPath = this.Server.MapPath("~/Dynamic");
            string[] dynAsm = Directory.GetFiles(dynPath, "*.dll");
            foreach (string asm in dynAsm)
            {
                try
                {
                    File.Delete(Path.Combine(dynPath, asm));
                }
                catch
                {

                }
            }
#endif
        }

        protected void Session_Start(object sender, EventArgs e)
        {
            HttpContext.Current.Session["test"] = "toto";
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}