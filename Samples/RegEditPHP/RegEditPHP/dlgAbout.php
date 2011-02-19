<?
    import namespace RegEditPHP;
    
    namespace RegEditPHP {
        
        [Export]
        partial class dlgAbout extends System:::Windows:::Forms:::Form {
            
            private $tlpMain;
            
            private $llbPhalanger;
            
            private $picRegEditPHP;
            
            private $lblProductI;
            
            private $lblProduct;
            
            private $lblTitleI;
            
            private $lblTitle;
            
            private $lblVersionI;
            
            private $lblVersion;
            
            private $lblDescriptionI;
            
            private $lblDescription;
            
            private $lblCompanyI;
            
            private $lblCompany;
            
            private $lblCopyryghtI;
            
            private $lblCopyright;
            
            private $lblPoweredByI;
            
            private $picPhalanger;
            
            private $rtbReadMe;
            
            private $cmdOK;
            
            private $components = NULL;
            
            public function __construct()
                : parent() {
                $this->InitializeComponent();
            }
            
            private function InitializeComponent() {
                $resources = new System:::ComponentModel:::ComponentResourceManager(CLRTypeOf dlgAbout );
                $this->tlpMain = new System:::Windows:::Forms:::TableLayoutPanel();
                $this->picRegEditPHP = new System:::Windows:::Forms:::PictureBox();
                $this->lblProductI = new System:::Windows:::Forms:::Label();
                $this->lblProduct = new System:::Windows:::Forms:::Label();
                $this->lblTitleI = new System:::Windows:::Forms:::Label();
                $this->lblTitle = new System:::Windows:::Forms:::Label();
                $this->lblVersionI = new System:::Windows:::Forms:::Label();
                $this->lblVersion = new System:::Windows:::Forms:::Label();
                $this->lblDescriptionI = new System:::Windows:::Forms:::Label();
                $this->lblDescription = new System:::Windows:::Forms:::Label();
                $this->lblCompanyI = new System:::Windows:::Forms:::Label();
                $this->lblCompany = new System:::Windows:::Forms:::Label();
                $this->lblCopyryghtI = new System:::Windows:::Forms:::Label();
                $this->lblCopyright = new System:::Windows:::Forms:::Label();
                $this->lblPoweredByI = new System:::Windows:::Forms:::Label();
                $this->llbPhalanger = new System:::Windows:::Forms:::LinkLabel();
                $this->picPhalanger = new System:::Windows:::Forms:::PictureBox();
                $this->rtbReadMe = new System:::Windows:::Forms:::RichTextBox();
                $this->cmdOK = new System:::Windows:::Forms:::Button();
                $this->tlpMain->SuspendLayout();
                $this->picRegEditPHP->BeginInit();
                $this->picPhalanger->BeginInit();
                $this->SuspendLayout();
                // 
                // tlpMain
                // 
                $this->tlpMain->AccessibleDescription = NULL;
                $this->tlpMain->AccessibleName = NULL;
                $resources->ApplyResources($this->tlpMain, "tlpMain");
                $this->tlpMain->BackgroundImage = NULL;
                $this->tlpMain->Controls->Add($this->picRegEditPHP, 0, 0);
                $this->tlpMain->Controls->Add($this->lblProductI, 0, 1);
                $this->tlpMain->Controls->Add($this->lblProduct, 1, 1);
                $this->tlpMain->Controls->Add($this->lblTitleI, 0, 2);
                $this->tlpMain->Controls->Add($this->lblTitle, 1, 2);
                $this->tlpMain->Controls->Add($this->lblVersionI, 0, 3);
                $this->tlpMain->Controls->Add($this->lblVersion, 1, 3);
                $this->tlpMain->Controls->Add($this->lblDescriptionI, 0, 4);
                $this->tlpMain->Controls->Add($this->lblDescription, 1, 4);
                $this->tlpMain->Controls->Add($this->lblCompanyI, 0, 5);
                $this->tlpMain->Controls->Add($this->lblCompany, 1, 5);
                $this->tlpMain->Controls->Add($this->lblCopyryghtI, 0, 6);
                $this->tlpMain->Controls->Add($this->lblCopyright, 1, 6);
                $this->tlpMain->Controls->Add($this->lblPoweredByI, 0, 7);
                $this->tlpMain->Controls->Add($this->llbPhalanger, 1, 7);
                $this->tlpMain->Controls->Add($this->picPhalanger, 0, 8);
                $this->tlpMain->Controls->Add($this->rtbReadMe, 0, 9);
                $this->tlpMain->Controls->Add($this->cmdOK, 0, 10);
                $this->tlpMain->Font = NULL;
                $this->tlpMain->Name = "tlpMain";
                // 
                // picRegEditPHP
                // 
                $this->picRegEditPHP->AccessibleDescription = NULL;
                $this->picRegEditPHP->AccessibleName = NULL;
                $resources->ApplyResources($this->picRegEditPHP, "picRegEditPHP");
                $this->picRegEditPHP->BackgroundImage = NULL;
                $this->tlpMain->SetColumnSpan($this->picRegEditPHP, 2);
                $this->picRegEditPHP->Font = NULL;
                $this->picRegEditPHP->ImageLocation = NULL;
                $this->picRegEditPHP->Name = "picRegEditPHP";
                $this->picRegEditPHP->TabStop = false;
                // 
                // lblProductI
                // 
                $this->lblProductI->AccessibleDescription = NULL;
                $this->lblProductI->AccessibleName = NULL;
                $resources->ApplyResources($this->lblProductI, "lblProductI");
                $this->lblProductI->Font = NULL;
                $this->lblProductI->Name = "lblProductI";
                // 
                // lblProduct
                // 
                $this->lblProduct->AccessibleDescription = NULL;
                $this->lblProduct->AccessibleName = NULL;
                $resources->ApplyResources($this->lblProduct, "lblProduct");
                $this->lblProduct->Font = NULL;
                $this->lblProduct->Name = "lblProduct";
                // 
                // lblTitleI
                // 
                $this->lblTitleI->AccessibleDescription = NULL;
                $this->lblTitleI->AccessibleName = NULL;
                $resources->ApplyResources($this->lblTitleI, "lblTitleI");
                $this->lblTitleI->Font = NULL;
                $this->lblTitleI->Name = "lblTitleI";
                // 
                // lblTitle
                // 
                $this->lblTitle->AccessibleDescription = NULL;
                $this->lblTitle->AccessibleName = NULL;
                $resources->ApplyResources($this->lblTitle, "lblTitle");
                $this->lblTitle->Font = NULL;
                $this->lblTitle->Name = "lblTitle";
                // 
                // lblVersionI
                // 
                $this->lblVersionI->AccessibleDescription = NULL;
                $this->lblVersionI->AccessibleName = NULL;
                $resources->ApplyResources($this->lblVersionI, "lblVersionI");
                $this->lblVersionI->Font = NULL;
                $this->lblVersionI->Name = "lblVersionI";
                // 
                // lblVersion
                // 
                $this->lblVersion->AccessibleDescription = NULL;
                $this->lblVersion->AccessibleName = NULL;
                $resources->ApplyResources($this->lblVersion, "lblVersion");
                $this->lblVersion->Font = NULL;
                $this->lblVersion->Name = "lblVersion";
                // 
                // lblDescriptionI
                // 
                $this->lblDescriptionI->AccessibleDescription = NULL;
                $this->lblDescriptionI->AccessibleName = NULL;
                $resources->ApplyResources($this->lblDescriptionI, "lblDescriptionI");
                $this->lblDescriptionI->Font = NULL;
                $this->lblDescriptionI->Name = "lblDescriptionI";
                // 
                // lblDescription
                // 
                $this->lblDescription->AccessibleDescription = NULL;
                $this->lblDescription->AccessibleName = NULL;
                $resources->ApplyResources($this->lblDescription, "lblDescription");
                $this->lblDescription->Font = NULL;
                $this->lblDescription->Name = "lblDescription";
                // 
                // lblCompanyI
                // 
                $this->lblCompanyI->AccessibleDescription = NULL;
                $this->lblCompanyI->AccessibleName = NULL;
                $resources->ApplyResources($this->lblCompanyI, "lblCompanyI");
                $this->lblCompanyI->Font = NULL;
                $this->lblCompanyI->Name = "lblCompanyI";
                // 
                // lblCompany
                // 
                $this->lblCompany->AccessibleDescription = NULL;
                $this->lblCompany->AccessibleName = NULL;
                $resources->ApplyResources($this->lblCompany, "lblCompany");
                $this->lblCompany->Font = NULL;
                $this->lblCompany->Name = "lblCompany";
                // 
                // lblCopyryghtI
                // 
                $this->lblCopyryghtI->AccessibleDescription = NULL;
                $this->lblCopyryghtI->AccessibleName = NULL;
                $resources->ApplyResources($this->lblCopyryghtI, "lblCopyryghtI");
                $this->lblCopyryghtI->Font = NULL;
                $this->lblCopyryghtI->Name = "lblCopyryghtI";
                // 
                // lblCopyright
                // 
                $this->lblCopyright->AccessibleDescription = NULL;
                $this->lblCopyright->AccessibleName = NULL;
                $resources->ApplyResources($this->lblCopyright, "lblCopyright");
                $this->lblCopyright->Font = NULL;
                $this->lblCopyright->Name = "lblCopyright";
                // 
                // lblPoweredByI
                // 
                $this->lblPoweredByI->AccessibleDescription = NULL;
                $this->lblPoweredByI->AccessibleName = NULL;
                $resources->ApplyResources($this->lblPoweredByI, "lblPoweredByI");
                $this->lblPoweredByI->Font = NULL;
                $this->lblPoweredByI->Name = "lblPoweredByI";
                // 
                // llbPhalanger
                // 
                $this->llbPhalanger->AccessibleDescription = NULL;
                $this->llbPhalanger->AccessibleName = NULL;
                $resources->ApplyResources($this->llbPhalanger, "llbPhalanger");
                $this->llbPhalanger->Font = NULL;
                $this->llbPhalanger->Name = "llbPhalanger";
                $this->llbPhalanger->TabStop = true;
                $this->llbPhalanger->LinkClicked->Add(new System:::Windows:::Forms:::LinkLabelLinkClickedEventHandler(array($this, "llbPhalanger_LinkClicked")));
                // 
                // picPhalanger
                // 
                $this->picPhalanger->AccessibleDescription = NULL;
                $this->picPhalanger->AccessibleName = NULL;
                $resources->ApplyResources($this->picPhalanger, "picPhalanger");
                $this->picPhalanger->BackgroundImage = NULL;
                $this->tlpMain->SetColumnSpan($this->picPhalanger, 2);
                $this->picPhalanger->Cursor = System:::Windows:::Forms:::Cursors::$Hand;
                $this->picPhalanger->Font = NULL;
                $this->picPhalanger->ImageLocation = NULL;
                $this->picPhalanger->Name = "picPhalanger";
                $this->picPhalanger->TabStop = false;
                $this->picPhalanger->Click->Add(new System:::EventHandler(array($this, "picPhalanger_Click")));
                // 
                // rtbReadMe
                // 
                $this->rtbReadMe->AccessibleDescription = NULL;
                $this->rtbReadMe->AccessibleName = NULL;
                $resources->ApplyResources($this->rtbReadMe, "rtbReadMe");
                $this->rtbReadMe->BackColor = System:::Drawing:::SystemColors::$Control;
                $this->rtbReadMe->BackgroundImage = NULL;
                $this->tlpMain->SetColumnSpan($this->rtbReadMe, 2);
                $this->rtbReadMe->Font = NULL;
                $this->rtbReadMe->Name = "rtbReadMe";
                $this->rtbReadMe->ReadOnly = true;
                // 
                // cmdOK
                // 
                $this->cmdOK->AccessibleDescription = NULL;
                $this->cmdOK->AccessibleName = NULL;
                $resources->ApplyResources($this->cmdOK, "cmdOK");
                $this->cmdOK->BackgroundImage = NULL;
                $this->tlpMain->SetColumnSpan($this->cmdOK, 2);
                $this->cmdOK->DialogResult = System:::Windows:::Forms:::DialogResult::Cancel;
                $this->cmdOK->Font = NULL;
                $this->cmdOK->Name = "cmdOK";
                $this->cmdOK->UseVisualStyleBackColor = true;
                $this->cmdOK->Click->Add(new System:::EventHandler(array($this, "cmdOK_Click")));
                // 
                // dlgAbout
                // 
                $this->AcceptButton = $this->cmdOK;
                $this->AccessibleDescription = NULL;
                $this->AccessibleName = NULL;
                $resources->ApplyResources($this, "\$this");
                $this->AutoScaleMode = System:::Windows:::Forms:::AutoScaleMode::Font;
                $this->BackgroundImage = NULL;
                $this->CancelButton = $this->cmdOK;
                $this->Controls->Add($this->tlpMain);
                $this->Font = NULL;
                $this->Icon = NULL;
                $this->MaximizeBox = false;
                $this->MinimizeBox = false;
                $this->Name = "dlgAbout";
                $this->Opacity = 0.9;
                $this->ShowIcon = false;
                $this->ShowInTaskbar = false;
                $this->Load->Add(new System:::EventHandler(array($this, "dlgAbout_Load")));
                $this->tlpMain->ResumeLayout(false);
                $this->tlpMain->PerformLayout();
                $this->picRegEditPHP->EndInit();
                $this->picPhalanger->EndInit();
                $this->ResumeLayout(false);
            }
        }
    }
?>
