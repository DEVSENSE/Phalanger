<? //© Ðonny 2009 - Part of Phalanger project
    import namespace System:::Windows:::Forms;
    import namespace System:::ComponentModel;
    import namespace System:::Drawing;
    import namespace RegEditPHP;

    namespace RegEditPHP{
        ///<summary>Allows user to select application languiage</summary>
	    partial class LanguageSelector extends System:::Windows:::Forms:::Form{
	        //Called by __construct
	        private function Init(){
	            //To do not have to react on Closing event and detect if it is via X button or command button
	            $this->DialogResult = DialogResult::Cancel;
	            //We must show available languages
	            // Languages are stored in the culture-named subdireactories of directory where assembly resides
	            $MyType = CLRTypeOf LanguageSelector;
	            $MyAsm = $MyType->Assembly;//Get the assembly
	            $AsmPath = $MyAsm->Location;
	            //get assembly directory
	            $AsmDir = System:::IO:::Path::GetDirectoryName($AsmPath);
	            //Then there is defaut laguage chosen by .NET itself depending on available languages and system locale
	            $this->cmbLanguage->Items->Add(Program::$Resources->DefaultLanguage);
	            //And there is language stored in current assebly instead of in satellite one
	            // it is indicated by AssemblyCultureAttribute
                $this->cmbLanguage->Items->Add(new System:::Globalization:::CultureInfo($MyAsm->GetCustomAttributes(CLRTypeOf System:::Reflection:::AssemblyCultureAttribute,false)->GetValue(0)->Culture));
	            $this->cmbLanguage->SelectedIndex=0;
	            $this->cmbLanguage->DisplayMember="NativeName";//Combobox will show NativeName of items to user
	            try{//Languages in subdirs
	                foreach(System:::IO:::Directory::GetDirectories($AsmDir) as $SubDir):
    	                try{
    	                    //Check if it is correct culture name
    	                    $ci=new System:::Globalization:::CultureInfo(System:::IO:::Path::GetFileName($SubDir));
    	                    $sta=$MyAsm->GetSatelliteAssembly($ci);
    	                    $this->cmbLanguage->Items->Add($ci);
    	                    //And if it really is satellite assembly
    	                    if(System:::Globalization:::CultureInfo::$CurrentUICulture->Equals($ci))
    	                        $this->cmbLanguage->SelectedIndex = $this->cmbLanguage->Items->Count - 1;
    	                }catch(System:::Exception $ex){}
	                endforeach;
	             }catch(System:::Exception $ex){}
	        }
	        //OK
            private function cmdOK_Click(System:::Object $sender, System:::EventArgs $e) {
                $this->DialogResult  =DialogResult::OK;
                //Save in setting
                Program::$Settings->Culture = ($this->cmbLanguage->SelectedItem instanceof System:::Globalization:::CultureInfo) ? $this->cmbLanguage->SelectedItem->Name : "";
                $this->Close();
            }
            //Cancel
            private function cmdCancel_Click(System:::Object $sender, System:::EventArgs $e) {
                $this->DialogResult  =DialogResult::Cancel;
                $this->Close();
            }
            //Change of selected language
            private function cmbLanguage_SelectedIndexChanged(System:::Object $sender, System:::EventArgs $e) {
                $item = $sender->SelectedItem;
                //Show some info
                if($item instanceof System:::Globalization:::CultureInfo):
                    $this->lblCulture->Text = "$item->Name: $item->EnglishName, $item->NativeName, $item->DisplayName";
                else:
                    $this->lblCluture->Text = "";
                endif;
            }            
	       
	}    
}
?>