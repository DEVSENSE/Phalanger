<? //© Ðonny 2009 - Part of Phalanger project
import namespace System;
import namespace System:::Windows:::Forms;
import namespace System:::Reflection;

import namespace RegEditPHP;

namespace RegEditPHP{
    //Apply some attributes onto assembly to have some extended info
    [assembly: AssemblyCompanyAttribute("The Phalanger Project Team")]
    [assembly: AssemblyProduct("Phalanger")]
    [assembly: AssemblyTitle("RegEditPHP")]
    [assembly: AssemblyDescription("Sample Windows Forms application written in Phalanger - A registry editor")]
    [assembly: AssemblyVersion("2.0.0.1")]
    [assembly: AssemblyCopyright("© Ðonny 2009")]
    [assembly: AssemblyCulture("en")]
    ///<summary>Main class of program</summary>
    class Program{
        ///<summary>Settings</summary>
        static $Settings;
        ///<summary>Version info</summary>
        public static $Version;
        ///<summary>Localized resources</summary>
        public static $Resources;
        ///<summary>Runs program</summary>
	    static function Main(){
	        $Type=CLRTypeOf Program;
	        //get version
	        self::$Version=new Version($Type->Assembly->GetCustomAttributes(CLRTypeOf AssemblyVersionAttribute,false)->GetValue(0)->Version->ToString());
	        //Init resources
	        self::$Resources = new ResourceAccessor("RegEditPHP.Resources");
	        //Init settings
	        self::$Settings = new SettingsProvider();
	        self::$Settings->Load();//Load settings from file
	        //Set language
	        if(self::$Settings->Culture)
	            System:::Threading:::Thread::$CurrentThread->CurrentUICulture = new System:::Globalization:::CultureInfo(self::$Settings->Culture);
		    Application::EnableVisualStyles();//To be nice
		    Application::Run(new frmMain());//Show and wait to close
	    }
    }
}
?>