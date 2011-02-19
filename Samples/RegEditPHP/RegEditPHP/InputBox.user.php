<? //© Ðonny 2009 - Part of Phalanger project
import namespace System:::Windows:::Forms;
import namespace RegEditPHP;

namespace RegEditPHP{
    ///<summary>This is simple string input box</summary>
	partial class InputBox extends System:::Windows:::Forms:::Form{
            //Called by __construct (I've written the call there)
            private function Init(){
                $this->DialogResult=DialogResult::Cancel;
            }
            //OK
            private function cmdOK_Click(System:::Object $sender, System:::EventArgs $e) {
                $this->DialogResult = DialogResult::OK;
            }
            //Cancel
            private function cmdCancel_Click(System:::Object $sender, System:::EventArgs $e) {
                $this->DialogResult = DialogResult::Cancel;
            }
            public static function GetInput(string $Promtp, string $Title, string $DefaultValue=""){
                $form=new InputBox();//Create instance
                //Initialize it
                $form->Text = $Title;
                $form->lblPrompt->Text=$Promtp;
                $form->txtText->Text=$DefaultValue;
                $form->txtText->SelectAll();
                $form->ShowDialog();//Show it
                //Depending on result
                if($form->DialogResult == DialogResult::OK)
                    return $form->Value;//Return value
                else
                    return null;//Or null
            }
            //PHP magic method for properties follows:
            public function __get(string $name){
                switch($name):
                    case "Prompt": return $this->lblPrompt->Text;
                    case "Value": return $this->txtText->Text;
                    default: return $vars[$name];
                endswitch;
            } 
            public function __set(string $name,$value){
                switch($name):
                    case "Prompt": return $this->lblPrompt->Text=$value;
                    case "Value": return $this->txtText->Text=$value;
                    default: return $vars[$name]=$value;
                endswitch;
            }
            public function __isset(string $name){
                switch($name):
                    case "Prompt":
                    case "Value": return true;
                    default: isset($vars[$name]);
                endswitch;
            }
            public function __unset(string $name){
                switch($name):
                    case "Prompt":
                    case "Value": throw new CLRException(new System:::InvalidOperationException(Program::$Resources->e_Unset($name)));
                    default: unset($vars[$name]);
                endswitch;
            }
            private $vars;
	}    
}
?>