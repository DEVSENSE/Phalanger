<?
    import namespace RegEditPHP;
    import namespace Microsoft:::Win32;
    import namespace System:::Windows:::Forms;
    
    namespace RegEditPHP {
        
        [Export]
        partial class ValueEditor extends System:::Windows:::Forms:::Form {
            
            private $tlpMain;
            
            private $lblName;
            
            private $txtName;
            
            private $lblTypeI;
            
            private $lblType;
            
            private $lblValue;
            
            private $panValue;
            
            private $tlpButtons;
            
            private $cmdOK;
            
            private $cmdCancel;
            
            private $lblHelp;
            
            private $components = NULL;
            
            public function __construct()
                : parent() {
                $this->InitializeComponent();
                $this->Init();
            }
            
            private function InitializeComponent() {
                $resources = new System:::ComponentModel:::ComponentResourceManager(CLRTypeOf ValueEditor );
                $this->tlpMain = new System:::Windows:::Forms:::TableLayoutPanel();
                $this->lblName = new System:::Windows:::Forms:::Label();
                $this->txtName = new System:::Windows:::Forms:::TextBox();
                $this->lblTypeI = new System:::Windows:::Forms:::Label();
                $this->lblType = new System:::Windows:::Forms:::Label();
                $this->lblValue = new System:::Windows:::Forms:::Label();
                $this->panValue = new System:::Windows:::Forms:::Panel();
                $this->tlpButtons = new System:::Windows:::Forms:::TableLayoutPanel();
                $this->cmdOK = new System:::Windows:::Forms:::Button();
                $this->cmdCancel = new System:::Windows:::Forms:::Button();
                $this->lblHelp = new System:::Windows:::Forms:::Label();
                $this->tlpMain->SuspendLayout();
                $this->tlpButtons->SuspendLayout();
                $this->SuspendLayout();
                // 
                // tlpMain
                // 
                $this->tlpMain->AccessibleDescription = NULL;
                $this->tlpMain->AccessibleName = NULL;
                $resources->ApplyResources($this->tlpMain, "tlpMain");
                $this->tlpMain->BackgroundImage = NULL;
                $this->tlpMain->Controls->Add($this->lblName, 0, 0);
                $this->tlpMain->Controls->Add($this->txtName, 1, 0);
                $this->tlpMain->Controls->Add($this->lblTypeI, 0, 1);
                $this->tlpMain->Controls->Add($this->lblType, 1, 1);
                $this->tlpMain->Controls->Add($this->lblValue, 0, 2);
                $this->tlpMain->Controls->Add($this->panValue, 1, 2);
                $this->tlpMain->Controls->Add($this->tlpButtons, 0, 4);
                $this->tlpMain->Controls->Add($this->lblHelp, 1, 3);
                $this->tlpMain->Font = NULL;
                $this->tlpMain->Name = "tlpMain";
                // 
                // lblName
                // 
                $this->lblName->AccessibleDescription = NULL;
                $this->lblName->AccessibleName = NULL;
                $resources->ApplyResources($this->lblName, "lblName");
                $this->lblName->Font = NULL;
                $this->lblName->Name = "lblName";
                // 
                // txtName
                // 
                $this->txtName->AccessibleDescription = NULL;
                $this->txtName->AccessibleName = NULL;
                $resources->ApplyResources($this->txtName, "txtName");
                $this->txtName->BackgroundImage = NULL;
                $this->txtName->Font = NULL;
                $this->txtName->Name = "txtName";
                $this->txtName->ReadOnly = true;
                // 
                // lblTypeI
                // 
                $this->lblTypeI->AccessibleDescription = NULL;
                $this->lblTypeI->AccessibleName = NULL;
                $resources->ApplyResources($this->lblTypeI, "lblTypeI");
                $this->lblTypeI->Font = NULL;
                $this->lblTypeI->Name = "lblTypeI";
                // 
                // lblType
                // 
                $this->lblType->AccessibleDescription = NULL;
                $this->lblType->AccessibleName = NULL;
                $resources->ApplyResources($this->lblType, "lblType");
                $this->lblType->Font = NULL;
                $this->lblType->Name = "lblType";
                // 
                // lblValue
                // 
                $this->lblValue->AccessibleDescription = NULL;
                $this->lblValue->AccessibleName = NULL;
                $resources->ApplyResources($this->lblValue, "lblValue");
                $this->lblValue->Font = NULL;
                $this->lblValue->Name = "lblValue";
                // 
                // panValue
                // 
                $this->panValue->AccessibleDescription = NULL;
                $this->panValue->AccessibleName = NULL;
                $resources->ApplyResources($this->panValue, "panValue");
                $this->panValue->BackgroundImage = NULL;
                $this->panValue->Font = NULL;
                $this->panValue->Name = "panValue";
                // 
                // tlpButtons
                // 
                $this->tlpButtons->AccessibleDescription = NULL;
                $this->tlpButtons->AccessibleName = NULL;
                $resources->ApplyResources($this->tlpButtons, "tlpButtons");
                $this->tlpButtons->BackgroundImage = NULL;
                $this->tlpMain->SetColumnSpan($this->tlpButtons, 2);
                $this->tlpButtons->Controls->Add($this->cmdOK, 0, 0);
                $this->tlpButtons->Controls->Add($this->cmdCancel, 1, 0);
                $this->tlpButtons->Font = NULL;
                $this->tlpButtons->Name = "tlpButtons";
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
                // lblHelp
                // 
                $this->lblHelp->AccessibleDescription = NULL;
                $this->lblHelp->AccessibleName = NULL;
                $resources->ApplyResources($this->lblHelp, "lblHelp");
                $this->lblHelp->Font = NULL;
                $this->lblHelp->Name = "lblHelp";
                // 
                // ValueEditor
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
                $this->Name = "ValueEditor";
                $this->ShowIcon = false;
                $this->ShowInTaskbar = false;
                $this->tlpMain->ResumeLayout(false);
                $this->tlpMain->PerformLayout();
                $this->tlpButtons->ResumeLayout(false);
                $this->tlpButtons->PerformLayout();
                $this->ResumeLayout(false);
            }
        }
    }
?>
