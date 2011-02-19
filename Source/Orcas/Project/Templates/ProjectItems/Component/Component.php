<?
import namespace System;
import namespace System:::ComponentModel;
namespace $rootnamespace${
	partial class $safeitemrootname$ extends Component{
		/// <summary>Default CTor</summary>
		/// <remarks>You can replace this CTor by your own, but do not forget to call InitializeComponent()</remarks>
		public function __construct(){
			$this->InitializeComponent();
		}
		/// <summary>Required designer variable.</summary>
		private $components = null;
		/*/// <summary>Clean up any resources being used.</summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected function Dispose(bool $disposing){
			if($disposing and $components)
				$this->components->Dispose();
			parent::Dispose($disposing);
		}*/
		/// <summary>Required method for Designer support - do not modify the contents of this method with the code editor.</summary>
		private function InitializeComponent(){
			$this->components = new System.ComponentModel.Container();
		}
	}
}
?>