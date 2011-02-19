<?
    import namespace WinForms;
    
    namespace WinForms {
        
        [Export]
        partial class Form1 extends System:::Windows:::Forms:::Form {
            
            private $timer;
            
            private $Button;
            
            private $ToolTip;
            
            private $components;
            
            public function __construct()
                : parent() {
                $this->InitializeComponent();
            }
            
            public function InitializeComponent() {
                $this->components = new System:::ComponentModel:::Container();
                $this->timer = new System:::Windows:::Forms:::Timer($this->components);
                $this->Button = new System:::Windows:::Forms:::Button();
                $this->ToolTip = new System:::Windows:::Forms:::ToolTip($this->components);
                $this->SuspendLayout();
                // 
                // timer
                // 
                $this->timer->Interval = 10;
                $this->timer->Tick->Add(new System:::EventHandler(array($this, "timer_Tick")));
                // 
                // Button
                // 
                $this->Button->Location = new System:::Drawing:::Point(98, 231);
                $this->Button->Name = "Button";
                $this->Button->Size = new System:::Drawing:::Size(75, 23);
                $this->Button->TabIndex = 0;
                $this->Button->Text = "Phalanger!";
                $this->Button->UseVisualStyleBackColor = true;
                // 
                // ToolTip
                // 
                $this->ToolTip->AutoPopDelay = 5000;
                $this->ToolTip->InitialDelay = 200;
                $this->ToolTip->IsBalloon = true;
                $this->ToolTip->ReshowDelay = 100;
                $this->ToolTip->ToolTipIcon = System:::Windows:::Forms:::ToolTipIcon::Info;
                $this->ToolTip->ToolTipTitle = "Click the image to zoom in and out";
                $this->ToolTip->SetToolTip($this, "Left button zooms in, right button zooms out.");
                // 
                // Form1
                // 
                $this->AutoScaleDimensions = new System:::Drawing:::SizeF(6, 13);
                $this->AutoScaleMode = System:::Windows:::Forms:::AutoScaleMode::Font;
                $this->ClientSize = new System:::Drawing:::Size(292, 266);
                $this->Controls->Add($this->Button);
                $this->Name = "Form1";
                $this->Text = "Form1";
                $this->Load->Add(new System:::EventHandler(array($this, "Form1_Load")));
                $this->Click->Add(new System:::EventHandler(array($this, "Form1_Click")));
                $this->Move->Add(new System:::EventHandler(array($this, "Form1_Move")));
                $this->Resize->Add(new System:::EventHandler(array($this, "Form1_Resize")));
                $this->ResumeLayout(false);
            }
        }
    }
?>
