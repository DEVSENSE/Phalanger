<?
    import namespace System:::Windows:::Forms;
    import namespace System:::ComponentModel;
    import namespace System:::Drawing;
    import namespace WindowsApplication1;
    
    namespace WindowsApplication1 {
        
        [Export]
        class Form1 extends System:::Windows:::Forms:::Form {
            
            private $button1;
            
            public function __construct()
                : parent() {
                $this->InitializeComponent();
            }
            
            public function InitializeComponent() {
                $this->button1 = new System:::Windows:::Forms:::Button();
                $this->SuspendLayout();
                // 
                // button1
                // 
                $this->button1->Location = new System:::Drawing:::Point(212, 62);
                $this->button1->Name = "button1";
                $this->button1->Size = new System:::Drawing:::Size(75, 23);
                $this->button1->TabIndex = 0;
                $this->button1->Text = "button1";
                $this->button1->UseVisualStyleBackColor = true;
                // 
                // Form1
                // 
                $this->ClientSize = new System:::Drawing:::Size(292, 273);
                $this->Controls->Add($this->button1);
                $this->Name = "Form1";
                $this->Text = "Form1";
                $this->ResumeLayout(false);
            }
        }
    }
?>
