<? //© Ðonny 2009 - Part of Phalanger project
//Place your code in this file.
//In case you rename class or namespace it must be renamed in dlgAbout.php as well.
import namespace System:::Windows:::Forms;
import namespace System:::ComponentModel;
import namespace System:::Drawing;
import namespace RegEditPHP;
import namespace System:::Reflection;

namespace RegEditPHP{
	///<summary>The "About" dialog</summary>
	partial class dlgAbout extends System:::Windows:::Forms:::Form{
            //Called when dialog loads
            private function dlgAbout_Load(System:::Object $sender, System:::EventArgs $e) {
                //We need some info from our assembly (set in Program.php)
                //The easiest way to get assembly is get come type from it - i.e. dlgAbout
                $Type=CLRTypeOf dlgAbout;
                //And then query the type for assembly
                $Asm = $Type->Assembly;//So, this is our assembly
                //Now query the assembly from info stored in attributes and show the info in labels
                //In each step we get all the attributes of given type,
                //PHPBUG:
                // select 1st of them (using GetValue because [0] does not work well for non-Php arrays sometimes, currently
                // we are sure that there is one (otherwisa exception will occur)
                // and then get value of the information and show it
                //Product name
                $attrs=$Asm->GetCustomAttributes(CLRTypeOf AssemblyProductAttribute,false);
                $this->lblProduct->Text = $attrs->GetValue(0)->Product;
                //Assembly title (aka name)
                $attrs=$Asm->GetCustomAttributes(CLRTypeOf AssemblyTitleAttribute,false);
                $this->lblTitle->Text = $attrs->GetValue(0)->Title;
                //Version
                $attrs=$Asm->GetCustomAttributes(CLRTypeOf AssemblyVersionAttribute,false);
                $this->lblVersion->Text = $attrs->GetValue(0)->Version->ToString();
                //Now we can set titlebar of window
                $this->Text .= " " . $this->lblTitle->Text . " " .$this->lblVersion->Text;
                //Description (note: it is not localized)
                $attrs=$Asm->GetCustomAttributes(CLRTypeOf AssemblyDescriptionAttribute,false);
                $this->lblDescription->Text = $attrs->GetValue(0)->Description;
                //Company name
                $attrs=$Asm->GetCustomAttributes(CLRTypeOf AssemblyCompanyAttribute,false);
                $this->lblCompany->Text = $attrs->GetValue(0)->Company;
                //Copyright
                $attrs=$Asm->GetCustomAttributes(CLRTypeOf AssemblyCopyrightAttribute,false);
                $this->lblCopyright->Text = $attrs->GetValue(0)->Copyright;
                //Finally load readme from resources
                $this->rtbReadMe->Rtf=Program::$Resources->readme;
            }
            //Handles click on link
            private function llbPhalanger_LinkClicked(System:::Object $sender, System:::Windows:::Forms:::LinkLabelLinkClickedEventArgs $e) {
                self::GoPhalanger();
                $e->Link->Visited=true;
            }
            //Handles click on powered-by image
            private function picPhalanger_Click(System:::Object $sender, System:::EventArgs $e) {
                self::GoPhalanger();
            }
            ///<summary>Opens Phalanger URL in browser</summary>
            private static function GoPhalanger(){
                //Just start process with url
                try{System:::Diagnostics:::Process::Start("http://codeplex.com/Phalanger");}
                catch(System:::Exception $ex){}//And ignore any failure
            }
            //Handles click on OK button
            private function cmdOK_Click(System:::Object $sender, System:::EventArgs $e) {
                $this->DialogResult = DialogResult::OK;//This is not necessary since nothing depends on dialog result of this dialog
                $this->Close();//Close form
            }
	    
	}    
}
?>