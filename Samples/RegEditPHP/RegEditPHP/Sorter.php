<?
    import namespace RegEditPHP;
    import namespace System:::Windows:::Forms;
    import namespace System;
    
    namespace RegEditPHP {

        class Sorter implements System:::Collections:::IComparer{
            private $ListView;
            public $Column;
            public $Order = SortOrder::Ascending;
            private $Comparer;
            public function __construct(ListView $ListView, int $column=0){
                $this->ListView = $ListView;
                $this->Column=$column;
                $this->Comparer=System:::StringComparer::Create(System:::Globalization:::CultureInfo::$CurrentCulture,true);
                $this->ListView->ListViewItemSorter=$this;
                $this->ListView->Sort();                                
            }
            public function Compare($a,$b){
                if(! $a instanceof ListViewItem) throw new CLRException(new ArgumentException(Program::$Resources->e_ListViewItem,"a"));
                if(! $b instanceof ListViewItem) throw new CLRException(new ArgumentException(Program::$Resources->e_ListViewItem,"b"));
                if($this->Column==0):
                    $va=$a->Text;
                    $vb=$b->Text;
                else:
                    $va=self::Iterate($a->SubItems->GetEnumerator(),$this->Column)->Text;
                    $vb=self::Iterate($b->SubItems->GetEnumerator(),$this->Column)->Text;
                endif;
                switch($this->Order):
                    case SortOrder::Ascending:
                        $mul=1;
                    break;
                    case SortOrder::Descending:
                        $mul=-1;
                    break;
                    default: $mul=0;
                endswitch;
                return $mul * (int)$this->Comparer->Compare($va,$vb);
            }
            public static function Iterate(System:::Collections:::IEnumerator $e, int $n){
                $n++;
                for($i=0;$i<$n;$i++):
                    if(!$e->MoveNext()) throw new CLRException(new ArgumentOutOfRangeException("n"));
                endfor;
                return $e->Current;    
            }
        } 
    }
?>