<? //© Ðonny 2009 - Part of Phalanger project
    import namespace RegEditPHP;
    import namespace System:::Configuration;    
    import namespace System:::Drawing;
    import namespace System:::Windows:::Forms;
    import namespace System:::Xml;
    
    namespace RegEditPHP{
        ///<summary>Privides access to settings in XML</summary>
 	    class SettingsProvider{
 	        //This class is aesy to extend
 	        //By now it is not possible to use .NEt settings fronm Phalanger
 	        //XML documant stroing settings
 	        private $doc;
 	        //Load from file
	        public function Load(){
	            $path = self::GetPath();    
	            if(System:::IO:::File::Exists($path))://Load
	                $doc = new XmlDocument();
	                $doc->Load($path);
	            else://Create
	                $doc = new XmlDocument();
	                $doc->LoadXml(<<<xmlstring
<?xml version="1.0"?>
<settings/>
xmlstring
);	                
	            endif;
	            $this->doc=$doc;
	        } 
	        public function Save(){//Save to file
	            if(!System:::IO:::Directory::Exists(self::GetFolder()))
	                System:::IO:::Directory::CreateDirectory(self::GetFolder());
	            $docn="doc";
	            $xdoc7=$this->$docn;
	            $xdoc7->Save(self::GetPath());
	        }
	        //get fath of XML file
	        private static function GetPath(){
	            return System:::IO:::Path::Combine(
	                self::GetFolder(),"settings.xml");
	        }
	        //Get folder of assembly
	        private static function GetFolder(){
	            return System:::IO:::Path::Combine(
	                System:::Environment::GetFolderPath(System:::Environment:::SpecialFolder::ApplicationData),
	                "RegEditPHP");
	        }
	        //Get node for setting
	        private function getNode(string $name){
	            $xdoc=$this->doc;
	            return $xdoc->SelectSingleNode("//settings/setting[@id='$name']");    
	        }
	        //Set node value
	        private function setNode(string $name,string $innerText,bool $raw=false){
	            $node=$this->getNode($name);
	            if(is_null($node)):
	                $xdoc=$this->doc;
	                $node=$xdoc->CreateElement("setting");
	                $node->SetAttribute("id",$name);
	                $xdoc->DocumentElement->AppendChild($node);
	            endif;
	            if($raw)
	                $node->InnerText=$innerXml;
	            else
	                $node->InnerText=$innerText;
	        }
	       
	        //Get setting value
	        public function __get(string $name){
	            $el=$this->getNode($name);
	            if(is_null($el)) return null;
	            switch($name):
	                 case "MainSize": 
	                    $parts = explode(",",$el->InnerText->Trim());
	                    return new Size((int)$parts[0],(int)$parts[1]);
                     case "ColumnWidths":
                        foreach(explode(",",$el->InnerText->Trim()) as $w)
                            $ret[]=(int)$w;
                        return $ret;
                     case "SplitterDistance":
                     case "MainState":
                        return (int)$el->InnerText->Trim();
                     case "Culture":
                     default:
                        return $el->InnerText->Trim();
                endswitch;
	        }
	        //Set setting value
	        public function __set(string $name, $value){
	            $raw=false;
	            switch($name):
	                 case "MainSize": 
	                    $text="$value->Width,$value->Height";
	                 break;
                     case "ColumnWidths":
                        $text=System:::String::Join(",",$value);
                     break;
                     case "SplitterDistance":
                     case "MainState":
                     case "Culture":
                     default:
                        $text=(string)$value;
                endswitch;
                $this->setNode($name,$text,$raw);
	        }
	        public function __isset(string $name){
	            if (is_null($this->getNode($name)))
	                return false;
	            return true;
	        }
	        public function __unset($name){
	            if($node=$this->getNode($name))
	                $node->Parent->RemoveChild($node);
	        }
	    }
    }
?>