<? //© Ðonny 2009 - Part of Phalanger project
  import namespace System:::Windows:::Forms;

    import namespace RegEditPHP;
    import namespace Microsoft:::Win32;
   
    namespace RegEditPHP{
        ///<summary>Generic value edeitor</summary>
	    partial class ValueEditor extends System:::Windows:::Forms:::Form{
            //Actual editor control
            private $editor;
            //Type of value being edited
            private $type;
            //Caled by __construct
            private function Init(){$this->DialogResult=DialogResult::Cancel;}
            //Initializes editro
            public function SetValue($value, int $type ){
                $this->type=$type;
                switch($type)://Byt type
                    case RegistryValueKind::Binary://Use text box
                        $this->editor=new TextBox();
                        $this->editor->Multiline=true;
                        $this->editor->WordWrap=true;
                        $this->editor->ScrollBars=ScrollBars::Both;
                        $sb = new System:::Text:::StringBuilder(count($value)*2);
                        foreach($value as $b):
                            if($sb->Length > 0) $sb->Append(" ");
                            $sb->Append($b->ToString("X2"));
                        endforeach;
                        $this->editor->Text = $sb->ToString();
                        $this->lblType->Text=REG_BINARY;
                        $this->lblHelp->Text=Program::$Resources->Binary_d;
                    break;
                    case RegistryValueKind::DWord://Use special control
                        $this->lblType->Text=REG_DWORD;
                    case RegistryValueKind::QWord:
                        $this->editor=new NumericEditor();
                        $this->editor->Type= $type==RegistryValueKind::DWord ? 32 : 64;
                        if(!is_null($value))
                            $this->editor->Value = $value;
                        if($this->lblType<>REG_)
                            $this->lblType->Text=REG_QWORD;
                        $this->editor->Value = $value;
                        $this->lblHelp->Text=Program::$Resources->Number_d;
                    break;
                    case RegistryValueKind::ExpandString://Textbox (single line)
                        $this->lblType->Text=REG_EXPAND_SZ;
                    case RegistryValueKind::i'String':
                        $this->editor=new TextBox();
                        $this->editor->Text = $value;
                        if($this->lblType<>REG_)
                            $this->lblType->Text=REG_SZ;
                        $this->lblHelp->Text=Program::$Resources->String_d;
                    break;
                    case RegistryValueKind::MultiString://Textbox (multi line)
                        $this->editor=new TextBox();
                        $this->editor->Multiline=true;
                        $this->editor->WordWrap=false;
                        $this->editor->ScrollBars=ScrollBars::Both;
                        $this->editor->AcceptsReturn=true;
                        if(!is_null($value))
                            $this->editor->Text=System:::String::Join("\r\n",$value);
                        $this->lblType->Text=REG_MULTI_SZ;
                        $this->lblHelp->Text=Program::$Resources->Multistring_d;
                    break;
                    default: throw new CLRException(new System:::ComponentModel:::InvalidEnumArgumentException("type",(int)$type,CLRTypeOf RegistryValueKind));
                endswitch;
                $this->editor->Dock=DockStyle::Fill;
                $this->panValue->Controls->Add($this->editor);
            }
            //Magic properties:
            public function __set(string $name,$value){
                switch($name):
                    case "Value": case "Type": throw new CLRException(new System:::InvalidOperationException(Program::$Resources->e_Set($name)));
                    case "NameReadOnly": return $this->txtName->ReadOnly = $value;
                    case "ValueName":return $this->txtName->Text=$value;
                    default: return $this->$values[$name]=$value;
                endswitch;
            }
            private $values;     
            public function __get(string $name){
                switch($name):
                    case "Value": 
                        switch($this->type):
                            case RegistryValueKind::Binary:
                                $value = $this->editor->Text->Replace("\r","")->Replace("\n","")->Replace("\t","")->Replace(" ","");
                                if(strlen($value) % 2 <> 0) throw new CLRException(new System:::InvalidOperationException(Program::$Resources->e_BinaryNotEven)); 
                                $ret = new System:::IO:::MemoryStream();
                                for($i=0;$i<strlen($value);$i+=2):
                                    $bytestring = $value{$i}.$value{$i+1};
                                    $ret->WriteByte(
                                        System:::Byte::Parse($bytestring,System:::Globalization:::NumberStyles::HexNumber));
                                endfor;
                                $ret2=System:::Array::CreateInstance(CLRTypeOf System:::Byte,$ret->Length);
                                System:::Array::ConstrainedCopy($ret->GetBuffer(),0,$ret2,0,$ret->Length);
                                return $ret2;
                            case RegistryValueKind::DWord:
                            case RegistryValueKind::QWord:
                                return $this->editor->Value;
                            case RegistryValueKind::ExpandString:
                            case RegistryValueKind::i'String':
                                return $this->editor->Text;
                            case RegistryValueKind::MultiString:
                                return $this->editor->Text->Split(array("\r\n"),System:::StringSplitOptions::None);
                        endswitch;
                    case "Type": return $this->type;
                    case "NameReadOnly": return $this->txtName->ReadOnly;
                    case "ValueName": return $this->txtName->Text;
                    default: return $this->values[$name];
                endswitch;
            }
            public function __isset(string $name){
                switch($name):
                    case "Value": case "NameReadOnly": case "ValueName": case "Type": return true;
                    default: return isset($this->values[$name]);
                endswitch;
            }
            public function __unset(string $name){
                switch($name):
                    case "Value": case "NameReadOnly": case "ValueName": case "Type": throw new CLRException(new System:::InvalidOperationException(Program::$Resources->e_Unset($name)));
                    default: unset($this->values[$name]);
                endswitch;
            }
            
            private function cmdCancel_Click(System:::Object $sender, System:::EventArgs $e) {
                $this->DialogResult=DialogResult::Cancel;
                $this->Close();
            }
            
            private function cmdOK_Click(System:::Object $sender, System:::EventArgs $e) {
                unset($errmsg);
                try{
                    $value = $this->Value;
                }catch(System:::Exception $ex){
                    $errmsg=$ex->Message;
                }catch(Exception $ex){
                    $errmsg=(string)$ex;
                }
                if(isset($errmsg)):
                    frmMain::OnError($errmsg);
                else:
                    $this->DialogResult=DialogResult::OK;
                    $this->Close();
                endif;    
            }	    
	}    
}
?>