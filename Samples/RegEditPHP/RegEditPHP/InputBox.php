<?
    import namespace RegEditPHP;
    import namespace System:::Windows:::Forms;
    
    namespace RegEditPHP {
        
        [Export]
        partial class InputBox extends System:::Windows:::Forms:::Form {
            
            private $tlpMain;
            
            private $lblPrompt;
            
            private $txtText;
            
            private $cmdOK;
            
            private $cmdCancel;
            
            private $components = NULL;
            
            public function __construct()
                : parent() {
                $this->InitializeComponent();
                $this->Init();
            }
            
            private function InitializeComponent() {
                $resources = new System:::ComponentModel:::ComponentResourceManager(CLRTypeOf InputBox );
                $this->tlpMain = new System:::Windows:::Forms:::TableLayoutPanel();
                $this->lblPrompt = new System:::Windows:::Forms:::Label();
                $this->txtText = new System:::Windows:::Forms:::TextBox();
                $this->cmdOK = new System:::Windows:::Forms:::Button();
                $this->cmdCancel = new System:::Windows:::Forms:::Button();
                $this->tlpMain->SuspendLayout();
                $this->SuspendLayout();
                // 
                // tlpMain
                // 
                $this->tlpMain->AccessibleDescription = NULL;
                $this->tlpMain->AccessibleName = NULL;
                $resources->ApplyResources($this->tlpMain, "tlpMain");
                $this->tlpMain->BackgroundImage = NULL;
                $this->tlpMain->Controls->Add($this->lblPrompt, 0, 0);
                $this->tlpMain->Controls->Add($this->txtText, 0, 1);
                $this->tlpMain->Controls->Add($this->cmdOK, 0, 2);
                $this->tlpMain->Controls->Add($this->cmdCancel, 1, 2);
                $this->tlpMain->Font = NULL;
                $this->tlpMain->Name = "tlpMain";
                // 
                // lblPrompt
                // 
                $this->lblPrompt->AccessibleDescription = NULL;
                $this->lblPrompt->AccessibleName = NULL;
                $resources->ApplyResources($this->lblPrompt, "lblPrompt");
                $this->tlpMain->SetColumnSpan($this->lblPrompt, 2);
                $this->lblPrompt->Font = NULL;
                $this->lblPrompt->Name = "lblPrompt";
                // 
                // txtText
                // 
                $this->txtText->AccessibleDescription = NULL;
                $this->txtText->AccessibleName = NULL;
                $resources->ApplyResources($this->txtText, "txtText");
                $this->txtText->BackgroundImage = NULL;
                $this->tlpMain->SetColumnSpan($this->txtText, 2);
                $this->txtText->Font = NULL;
                $this->txtText->Name = "txtText";
                // 
                // cmdOK
                // 
                $this->cmdOK->AccessibleDescription = NULL;
                $this->cmdOK->AccessibleName = NULL;
                $resources->ApplyResources($this->cmdOK, "cmdOK");
                $this->cmdOK->BackgroundImage = NULL;
                $this->cmdOK->Font = NULL;
                $this->cmdOK->Name = "cmdOK";
                $this->cmdOK->UseVisualStyleBackColor = true;
                $this->cmdOK->Click->Add(new System:::EventHandler(array($this, "cmdOK_Click")));
                // 
                // cmdCancel
                // 
                $this->cmdCancel->AccessibleDescription = NULL;
                $this->cmdCancel->AccessibleName = NULL;
                $resources->ApplyResources($this->cmdCancel, "cmdCancel");
                $this->cmdCancel->BackgroundImage = NULL;
                $this->cmdCancel->DialogResult = System:::Windows:::Forms:::DialogResult::Cancel;
                $this->cmdCancel->Font = NULL;
                $this->cmdCancel->Name = "cmdCancel";
                $this->cmdCancel->UseVisualStyleBackColor = true;
                $this->cmdCancel->Click->Add(new System:::EventHandler(array($this, "cmdCancel_Click")));
                // 
                // InputBox
                // 
                $this->AcceptButton = $this->cmdOK;
                $this->AccessibleDescription = NULL;
                $this->AccessibleName = NULL;
                $resources->ApplyResources($this, "\$this");
                $this->AutoScaleMode = System:::Windows:::Forms:::AutoScaleMode::Font;
                $this->BackgroundImage = NULL;
                $this->CancelButton = $this->cmdCancel;
                $this->Controls->Add($this->tlpMain);
                $this->Font = NULL;
                $this->FormBorderStyle = System:::Windows:::Forms:::FormBorderStyle::FixedDialog;
                $this->Icon = NULL;
                $this->MaximizeBox = false;
                $this->MinimizeBox = false;
                $this->Name = "InputBox";
                $this->ShowIcon = false;
                $this->ShowInTaskbar = false;
                $this->tlpMain->ResumeLayout(false);
                $this->tlpMain->PerformLayout();
                $this->ResumeLayout(false);
            }
        }
    }
?>
