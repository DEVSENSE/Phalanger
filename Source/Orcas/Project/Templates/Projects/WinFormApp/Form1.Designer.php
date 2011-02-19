<?
import namespace System:::Windows:::Forms;
import namespace System:::ComponentModel;
import namespace System:::Drawing;

namespace $safeprojectname$
{
	partial class Form1 extends System:::Windows:::Forms:::Form
	{
		/// <summary>Default CTor</summary>
		/// <remarks>You can replace this CTor by your own, but do not forget to call InitializeComponent()</remarks>
		public function __construct(){
			$this->InitializeComponent();
		}
		/// <summary>Required method for Designer support - do not modify the contents of this method with the code editor.</summary>
		function InitializeComponent(){
			$this->components = new System:::ComponentModel:::Container();
			$this->AutoScaleMode = System:::Windows:::Forms:::AutoScaleMode::Font;
			$this->SuspendLayout();
			$this->ClientSize = new System:::Drawing:::Size(292, 266);
			$this->Name = '$safeitemname$';
			$this->Text = '$safeitemname$';
			$this->ResumeLayout(false);
			$this->PerformLayout();
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
	}    
}
?>