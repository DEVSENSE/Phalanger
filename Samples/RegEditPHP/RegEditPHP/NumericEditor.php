<?
    import namespace RegEditPHP;
    import namespace System:::Windows:::Forms;
    
    namespace RegEditPHP {
        
        [Export]
        partial class NumericEditor extends System:::Windows:::Forms:::UserControl {
            
            private $tlpMain;
            
            private $nudValue;
            
            private $optDec;
            
            private $optHex;
            
            private $components = NULL;
            
            public function __construct()
                : parent() {
                $this->InitializeComponent();
                $this->Init();
            }
            
            public function InitializeComponent() {
                $resources = new System:::ComponentModel:::ComponentResourceManager(CLRTypeOf NumericEditor );
                $this->tlpMain = new System:::Windows:::Forms:::TableLayoutPanel();
                $this->nudValue = new System:::Windows:::Forms:::NumericUpDown();
                $this->optDec = new System:::Windows:::Forms:::RadioButton();
                $this->optHex = new System:::Windows:::Forms:::RadioButton();
                $this->tlpMain->SuspendLayout();
                $this->nudValue->BeginInit();
                $this->SuspendLayout();
                // 
                // tlpMain
                // 
                $this->tlpMain->AccessibleDescription = NULL;
                $this->tlpMain->AccessibleName = NULL;
                $resources->ApplyResources($this->tlpMain, "tlpMain");
                $this->tlpMain->BackgroundImage = NULL;
                $this->tlpMain->Controls->Add($this->nudValue, 0, 0);
                $this->tlpMain->Controls->Add($this->optDec, 0, 1);
                $this->tlpMain->Controls->Add($this->optHex, 1, 1);
                $this->tlpMain->Font = NULL;
                $this->tlpMain->Name = "tlpMain";
                // 
                // nudValue
                // 
                $this->nudValue->AccessibleDescription = NULL;
                $this->nudValue->AccessibleName = NULL;
                $resources->ApplyResources($this->nudValue, "nudValue");
                $this->tlpMain->SetColumnSpan($this->nudValue, 2);
                $this->nudValue->Font = NULL;
                $this->nudValue->Name = "nudValue";
                // 
                // optDec
                // 
                $this->optDec->AccessibleDescription = NULL;
                $this->optDec->AccessibleName = NULL;
                $resources->ApplyResources($this->optDec, "optDec");
                $this->optDec->BackgroundImage = NULL;
                $this->optDec->Checked = true;
                $this->optDec->Font = NULL;
                $this->optDec->Name = "optDec";
                $this->optDec->TabStop = true;
                $this->optDec->UseVisualStyleBackColor = true;
                $this->optDec->CheckedChanged->Add(new System:::EventHandler(array($this, "opt_CheckedChanged")));
                // 
                // optHex
                // 
                $this->optHex->AccessibleDescription = NULL;
                $this->optHex->AccessibleName = NULL;
                $resources->ApplyResources($this->optHex, "optHex");
                $this->optHex->BackgroundImage = NULL;
                $this->optHex->Font = NULL;
                $this->optHex->Name = "optHex";
                $this->optHex->UseVisualStyleBackColor = true;
                $this->optHex->CheckedChanged->Add(new System:::EventHandler(array($this, "opt_CheckedChanged")));
                // 
                // NumericEditor
                // 
                $this->AccessibleDescription = NULL;
                $this->AccessibleName = NULL;
                $resources->ApplyResources($this, "\$this");
                $this->BackgroundImage = NULL;
                $this->Controls->Add($this->tlpMain);
                $this->Font = NULL;
                $this->MaximumSize = new System:::Drawing:::Size(0, 50);
                $this->MinimumSize = new System:::Drawing:::Size(149, 50);
                $this->Name = "NumericEditor";
                $this->tlpMain->ResumeLayout(false);
                $this->tlpMain->PerformLayout();
                $this->nudValue->EndInit();
                $this->ResumeLayout(false);
            }
        }
    }
?>
