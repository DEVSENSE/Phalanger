<? //© Ðonny 2009 - Part of Phalanger project
import namespace System:::Windows:::Forms;
import namespace RegEditPHP;

namespace RegEditPHP{
    ///<summary>Custom control for number editing</summary>
	partial class NumericEditor extends System:::Windows:::Forms:::UserControl{
        //CHnage dex/hex view
        private function opt_CheckedChanged(System:::Object $sender, System:::EventArgs $e) {
                $this->nudValue->Hexadecimal = $this->optHex->Checked;
            }
            //Called by __construct
            private function Init(){
                $this->__set("Type",32);
            }
            //Magic method for properties
            public function __set(string $name,$value){
                switch($name):
                    case "Value": return $this->nudValue->Value = $value;
                    case "Type":
                        switch($value):
                            case 32:
                                $this->nudValue->Maximum=System:::UInt32::MaxValue;
                            break;
                            case 64:
                                //PHPBUG:
                                //The problem is that Phalanger cannot handle UInt64 (and decimal) values
                                $this->nudValue->Maximum= 18446744073709551615.0;//this will be rounded to double
                            break;
                            default: throw new CLRException(new System:::ArgumentNullException(Program::$Resources->e_3264($name)));
                        endswitch;
                        return $value;
                    default: return $this->$values[$name]=$value;
                endswitch;
            }
            private $values;            
            public function __get(string $name){
                switch($name):
                    case "Value":
                        /*$ToInt="ToInt".$this->__get("Type");
                        return System:::Decimal::$ToInt($this->nudValue->Value);*/
                        return $this->nudValue->Value;
                    case "Type": return $this->nudValue->Maximum == System:::Int32::MaxValue ? 32 : 64;
                    default: return $this->values[$name];
                endswitch;
            }
            public function __isset(string $name){
                switch($name):
                    case "Value": case "Type": return true;
                    default: return isset($this->values[$name]);
                endswitch;
            }
            public function __unset(string $name){
                switch($name):
                    case "Value": case "Type": throw new CLRException(new System:::InvalidOperationException(Program::$Resources->e_Unset($name)));
                    default: unset($this->values[$name]);
                endswitch;
            }	    
	}    
}
?>