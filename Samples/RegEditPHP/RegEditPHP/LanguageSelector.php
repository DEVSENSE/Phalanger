<?
    import namespace RegEditPHP;
    
    namespace RegEditPHP {
        
        [Export]
        partial class LanguageSelector extends System:::Windows:::Forms:::Form {
            
            private $tlpMain;
            
            private $tlpButtons;
            
            private $cmdOK;
            
            private $cmdCancel;
            
            private $lblLanguage;
            
            private $cmbLanguage;
            
            private $picWarning;
            
            private $lblWarning;
            
            private $lblCulture;
            
            private $components = NULL;
            
            public function __construct()
                : parent() {
                $this->InitializeComponent();
                $this->Init();
            }
            
            private function InitializeComponent() {
                $resources = new System:::ComponentModel:::ComponentResourceManager(CLRTypeOf LanguageSelector );
                $this->tlpMain = new System:::Windows:::Forms:::TableLayoutPanel();
                $this->tlpButtons = new System:::Windows:::Forms:::TableLayoutPanel();
                $this->cmdOK = new System:::Windows:::Forms:::Button();
                $this->cmdCancel = new System:::Windows:::Forms:::Button();
                $this->lblLanguage = new System:::Windows:::Forms:::Label();
                $this->cmbLanguage = new System:::Windows:::Forms:::ComboBox();
                $this->picWarning = new System:::Windows:::Forms:::PictureBox();
                $this->lblWarning = new System:::Windows:::Forms:::Label();
                $this->lblCulture = new System:::Windows:::Forms:::Label();
                $this->tlpMain->SuspendLayout();
                $this->tlpButtons->SuspendLayout();
                $this->picWarning->BeginInit();
                $this->SuspendLayout();
                // 
                // tlpMain
                // 
                $this->tlpMain->AccessibleDescription = NULL;
                $this->tlpMain->AccessibleName = NULL;
                $resources->ApplyResources($this->tlpMain, "tlpMain");
                $this->tlpMain->BackgroundImage = NULL;
                $this->tlpMain->Controls->Add($this->tlpButtons, 0, 3);
                $this->tlpMain->Controls->Add($this->lblLanguage, 0, 0);
                $this->tlpMain->Controls->Add($this->cmbLanguage, 1, 0);
                $this->tlpMain->Controls->Add($this->picWarning, 0, 2);
                $this->tlpMain->Controls->Add($this->lblWarning, 1, 2);
                $this->tlpMain->Controls->Add($this->lblCulture, 0, 1);
                $this->tlpMain->Font = NULL;
                $this->tlpMain->Name = "tlpMain";
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
                // lblLanguage
                // 
                $this->lblLanguage->AccessibleDescription = NULL;
                $this->lblLanguage->AccessibleName = NULL;
                $resources->ApplyResources($this->lblLanguage, "lblLanguage");
                $this->lblLanguage->Font = NULL;
                $this->lblLanguage->Name = "lblLanguage";
                // 
                // cmbLanguage
                // 
                $this->cmbLanguage->AccessibleDescription = NULL;
                $this->cmbLanguage->AccessibleName = NULL;
                $resources->ApplyResources($this->cmbLanguage, "cmbLanguage");
                $this->cmbLanguage->BackgroundImage = NULL;
                $this->cmbLanguage->DropDownStyle = System:::Windows:::Forms:::ComboBoxStyle::DropDownList;
                $this->cmbLanguage->Font = NULL;
                $this->cmbLanguage->FormattingEnabled = true;
                $this->cmbLanguage->Name = "cmbLanguage";
                $this->cmbLanguage->SelectedIndexChanged->Add(new System:::EventHandler(array($this, "cmbLanguage_SelectedIndexChanged")));
                // 
                // picWarning
                // 
                $this->picWarning->AccessibleDescription = NULL;
                $this->picWarning->AccessibleName = NULL;
                $resources->ApplyResources($this->picWarning, "picWarning");
                $this->picWarning->BackgroundImage = NULL;
                $this->picWarning->Font = NULL;
                $this->picWarning->ImageLocation = NULL;
                $this->picWarning->Name = "picWarning";
                $this->picWarning->TabStop = false;
                // 
                // lblWarning
                // 
                $this->lblWarning->AccessibleDescription = NULL;
                $this->lblWarning->AccessibleName = NULL;
                $resources->ApplyResources($this->lblWarning, "lblWarning");
                $this->lblWarning->Font = NULL;
                $this->lblWarning->Name = "lblWarning";
                // 
                // lblCulture
                // 
                $this->lblCulture->AccessibleDescription = NULL;
                $this->lblCulture->AccessibleName = NULL;
                $resources->ApplyResources($this->lblCulture, "lblCulture");
                $this->tlpMain->SetColumnSpan($this->lblCulture, 2);
                $this->lblCulture->Font = NULL;
                $this->lblCulture->Name = "lblCulture";
                // 
                // LanguageSelector
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
                $this->Name = "LanguageSelector";
                $this->ShowIcon = false;
                $this->ShowInTaskbar = false;
                $this->tlpMain->ResumeLayout(false);
                $this->tlpMain->PerformLayout();
                $this->tlpButtons->ResumeLayout(false);
                $this->tlpButtons->PerformLayout();
                $this->picWarning->EndInit();
                $this->ResumeLayout(false);
            }
        }
    }
?>
