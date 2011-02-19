<?
    import namespace RegEditPHP;
    import namespace System:::Windows:::Forms;
    import namespace Microsoft:::Win32;
    import namespace System;
    
    namespace RegEditPHP {
        
        [Export]
        partial class frmMain extends System:::Windows:::Forms:::Form {
            
            private $tscMain;
            
            private $mnsMain;
            
            private $tmiFile;
            
            private $tmiExit;
            
            private $cohName;
            
            private $cohValue;
            
            private $splMain;
            
            private $tvwRegistry;
            
            private $lvwRegistry;
            
            private $cohType;
            
            private $imlImages;
            
            private $cmsContext;
            
            private $tmiNew;
            
            private $tmiNewKey;
            
            private $tmiNewSep1;
            
            private $tmiNewBinary;
            
            private $tmiNewDWord;
            
            private $tmiNewQWord;
            
            private $tmiNewString;
            
            private $tmiNewExpandable;
            
            private $tmiNewMulti;
            
            private $tmiRename;
            
            private $tmiEdit;
            
            private $tmiDelete;
            
            private $stsStatus;
            
            private $tslKey;
            
            private $cohAlternative;
            
            private $tssCms1;
            
            private $tmiRefreshNode;
            
            private $tmiSelectAll;
            
            private $tmiTools;
            
            private $tmiLanguage;
            
            private $tmiJump;
            
            private $tmiRefreshAll;
            
            private $tssTools1;
            
            private $tmiJumpToSame;
            
            private $tmiCopyFullPath;
            
            private $tmiExport;
            
            private $sfdExport;
            
            private $tmiHelp;
            
            private $tmiAbout;
            
            private $components;
            
            public function __construct()
                : parent() {
                $this->InitializeComponent();
                $this->Init();
            }
            
            public function InitializeComponent() {
                $this->components = new System:::ComponentModel:::Container();
                $resources = new System:::ComponentModel:::ComponentResourceManager(CLRTypeOf frmMain );
                $this->tscMain = new System:::Windows:::Forms:::ToolStripContainer();
                $this->stsStatus = new System:::Windows:::Forms:::StatusStrip();
                $this->tslKey = new System:::Windows:::Forms:::ToolStripStatusLabel();
                $this->splMain = new System:::Windows:::Forms:::SplitContainer();
                $this->tvwRegistry = new System:::Windows:::Forms:::TreeView();
                $this->cmsContext = new System:::Windows:::Forms:::ContextMenuStrip($this->components);
                $this->tmiNew = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiNewKey = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiNewSep1 = new System:::Windows:::Forms:::ToolStripSeparator();
                $this->tmiNewBinary = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiNewDWord = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiNewQWord = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiNewString = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiNewExpandable = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiNewMulti = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiRename = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiEdit = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiDelete = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tssCms1 = new System:::Windows:::Forms:::ToolStripSeparator();
                $this->tmiRefreshNode = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiSelectAll = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiCopyFullPath = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiExport = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->imlImages = new System:::Windows:::Forms:::ImageList($this->components);
                $this->lvwRegistry = new System:::Windows:::Forms:::ListView();
                $this->cohName = new System:::Windows:::Forms:::ColumnHeader($resources->GetString("lvwRegistry.Columns"));
                $this->cohType = new System:::Windows:::Forms:::ColumnHeader();
                $this->cohValue = new System:::Windows:::Forms:::ColumnHeader();
                $this->cohAlternative = new System:::Windows:::Forms:::ColumnHeader();
                $this->mnsMain = new System:::Windows:::Forms:::MenuStrip();
                $this->tmiFile = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiExit = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiTools = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiLanguage = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tssTools1 = new System:::Windows:::Forms:::ToolStripSeparator();
                $this->tmiJump = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiJumpToSame = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiRefreshAll = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiHelp = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->tmiAbout = new System:::Windows:::Forms:::ToolStripMenuItem();
                $this->sfdExport = new System:::Windows:::Forms:::SaveFileDialog();
                $this->tscMain->BottomToolStripPanel->SuspendLayout();
                $this->tscMain->ContentPanel->SuspendLayout();
                $this->tscMain->TopToolStripPanel->SuspendLayout();
                $this->tscMain->SuspendLayout();
                $this->stsStatus->SuspendLayout();
                $this->splMain->Panel1->SuspendLayout();
                $this->splMain->Panel2->SuspendLayout();
                $this->splMain->SuspendLayout();
                $this->cmsContext->SuspendLayout();
                $this->mnsMain->SuspendLayout();
                $this->SuspendLayout();
                // 
                // tscMain
                // 
                $this->tscMain->AccessibleDescription = NULL;
                $this->tscMain->AccessibleName = NULL;
                $resources->ApplyResources($this->tscMain, "tscMain");
                // 
                // tscMain.BottomToolStripPanel
                // 
                $this->tscMain->BottomToolStripPanel->AccessibleDescription = NULL;
                $this->tscMain->BottomToolStripPanel->AccessibleName = NULL;
                $this->tscMain->BottomToolStripPanel->BackgroundImage = NULL;
                $resources->ApplyResources($this->tscMain->BottomToolStripPanel, "tscMain.BottomToolStripPanel");
                $this->tscMain->BottomToolStripPanel->Controls->Add($this->stsStatus);
                $this->tscMain->BottomToolStripPanel->Font = NULL;
                // 
                // tscMain.ContentPanel
                // 
                $this->tscMain->ContentPanel->AccessibleDescription = NULL;
                $this->tscMain->ContentPanel->AccessibleName = NULL;
                $resources->ApplyResources($this->tscMain->ContentPanel, "tscMain.ContentPanel");
                $this->tscMain->ContentPanel->BackgroundImage = NULL;
                $this->tscMain->ContentPanel->Controls->Add($this->splMain);
                $this->tscMain->ContentPanel->Font = NULL;
                $this->tscMain->Font = NULL;
                // 
                // tscMain.LeftToolStripPanel
                // 
                $this->tscMain->LeftToolStripPanel->AccessibleDescription = NULL;
                $this->tscMain->LeftToolStripPanel->AccessibleName = NULL;
                $this->tscMain->LeftToolStripPanel->BackgroundImage = NULL;
                $resources->ApplyResources($this->tscMain->LeftToolStripPanel, "tscMain.LeftToolStripPanel");
                $this->tscMain->LeftToolStripPanel->Font = NULL;
                $this->tscMain->Name = "tscMain";
                // 
                // tscMain.RightToolStripPanel
                // 
                $this->tscMain->RightToolStripPanel->AccessibleDescription = NULL;
                $this->tscMain->RightToolStripPanel->AccessibleName = NULL;
                $this->tscMain->RightToolStripPanel->BackgroundImage = NULL;
                $resources->ApplyResources($this->tscMain->RightToolStripPanel, "tscMain.RightToolStripPanel");
                $this->tscMain->RightToolStripPanel->Font = NULL;
                // 
                // tscMain.TopToolStripPanel
                // 
                $this->tscMain->TopToolStripPanel->AccessibleDescription = NULL;
                $this->tscMain->TopToolStripPanel->AccessibleName = NULL;
                $this->tscMain->TopToolStripPanel->BackgroundImage = NULL;
                $resources->ApplyResources($this->tscMain->TopToolStripPanel, "tscMain.TopToolStripPanel");
                $this->tscMain->TopToolStripPanel->Controls->Add($this->mnsMain);
                $this->tscMain->TopToolStripPanel->Font = NULL;
                // 
                // stsStatus
                // 
                $this->stsStatus->AccessibleDescription = NULL;
                $this->stsStatus->AccessibleName = NULL;
                $resources->ApplyResources($this->stsStatus, "stsStatus");
                $this->stsStatus->BackgroundImage = NULL;
                $this->stsStatus->Font = NULL;
                $this->stsStatus->Items->AddRange(array($this->tslKey));
                $this->stsStatus->Name = "stsStatus";
                // 
                // tslKey
                // 
                $this->tslKey->AccessibleDescription = NULL;
                $this->tslKey->AccessibleName = NULL;
                $resources->ApplyResources($this->tslKey, "tslKey");
                $this->tslKey->BackgroundImage = NULL;
                $this->tslKey->Name = "tslKey";
                // 
                // splMain
                // 
                $this->splMain->AccessibleDescription = NULL;
                $this->splMain->AccessibleName = NULL;
                $resources->ApplyResources($this->splMain, "splMain");
                $this->splMain->BackgroundImage = NULL;
                $this->splMain->Font = NULL;
                $this->splMain->Name = "splMain";
                // 
                // splMain.Panel1
                // 
                $this->splMain->Panel1->AccessibleDescription = NULL;
                $this->splMain->Panel1->AccessibleName = NULL;
                $resources->ApplyResources($this->splMain->Panel1, "splMain.Panel1");
                $this->splMain->Panel1->BackgroundImage = NULL;
                $this->splMain->Panel1->Controls->Add($this->tvwRegistry);
                $this->splMain->Panel1->Font = NULL;
                // 
                // splMain.Panel2
                // 
                $this->splMain->Panel2->AccessibleDescription = NULL;
                $this->splMain->Panel2->AccessibleName = NULL;
                $resources->ApplyResources($this->splMain->Panel2, "splMain.Panel2");
                $this->splMain->Panel2->BackgroundImage = NULL;
                $this->splMain->Panel2->Controls->Add($this->lvwRegistry);
                $this->splMain->Panel2->Font = NULL;
                // 
                // tvwRegistry
                // 
                $this->tvwRegistry->AccessibleDescription = NULL;
                $this->tvwRegistry->AccessibleName = NULL;
                $resources->ApplyResources($this->tvwRegistry, "tvwRegistry");
                $this->tvwRegistry->BackgroundImage = NULL;
                $this->tvwRegistry->ContextMenuStrip = $this->cmsContext;
                $this->tvwRegistry->Font = NULL;
                $this->tvwRegistry->HideSelection = false;
                $this->tvwRegistry->ImageList = $this->imlImages;
                $this->tvwRegistry->LabelEdit = true;
                $this->tvwRegistry->Name = "tvwRegistry";
                $this->tvwRegistry->Nodes->AddRange(array($resources->GetObject("tvwRegistry.Nodes"), $resources->GetObject("tvwRegistry.Nodes1"), $resources->GetObject("tvwRegistry.Nodes2"), $resources->GetObject("tvwRegistry.Nodes3"), $resources->GetObject("tvwRegistry.Nodes4"), $resources->GetObject("tvwRegistry.Nodes5"), $resources->GetObject("tvwRegistry.Nodes6")));
                $this->tvwRegistry->AfterCollapse->Add(new System:::Windows:::Forms:::TreeViewEventHandler(array($this, "tvwRegistry_AfterCollapse")));
                $this->tvwRegistry->AfterLabelEdit->Add(new System:::Windows:::Forms:::NodeLabelEditEventHandler(array($this, "tvwRegistry_AfterLabelEdit")));
                $this->tvwRegistry->BeforeExpand->Add(new System:::Windows:::Forms:::TreeViewCancelEventHandler(array($this, "tvwRegistry_BeforeExpand")));
                $this->tvwRegistry->AfterSelect->Add(new System:::Windows:::Forms:::TreeViewEventHandler(array($this, "tvwRegistry_AfterSelect")));
                $this->tvwRegistry->MouseDown->Add(new System:::Windows:::Forms:::MouseEventHandler(array($this, "tvwRegistry_MouseDown")));
                $this->tvwRegistry->BeforeLabelEdit->Add(new System:::Windows:::Forms:::NodeLabelEditEventHandler(array($this, "tvwRegistry_BeforeLabelEdit")));
                $this->tvwRegistry->KeyDown->Add(new System:::Windows:::Forms:::KeyEventHandler(array($this, "tvwRegistry_KeyDown")));
                $this->tvwRegistry->AfterExpand->Add(new System:::Windows:::Forms:::TreeViewEventHandler(array($this, "tvwRegistry_AfterExpand")));
                // 
                // cmsContext
                // 
                $this->cmsContext->AccessibleDescription = NULL;
                $this->cmsContext->AccessibleName = NULL;
                $resources->ApplyResources($this->cmsContext, "cmsContext");
                $this->cmsContext->BackgroundImage = NULL;
                $this->cmsContext->Font = NULL;
                $this->cmsContext->Items->AddRange(array($this->tmiNew, $this->tmiRename, $this->tmiEdit, $this->tmiDelete, $this->tssCms1, $this->tmiRefreshNode, $this->tmiSelectAll, $this->tmiCopyFullPath, $this->tmiExport));
                $this->cmsContext->Name = "cmsContext";
                $this->cmsContext->Opening->Add(new System:::ComponentModel:::CancelEventHandler(array($this, "cmsContext_Opening")));
                // 
                // tmiNew
                // 
                $this->tmiNew->AccessibleDescription = NULL;
                $this->tmiNew->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiNew, "tmiNew");
                $this->tmiNew->BackgroundImage = NULL;
                $this->tmiNew->DropDownItems->AddRange(array($this->tmiNewKey, $this->tmiNewSep1, $this->tmiNewBinary, $this->tmiNewDWord, $this->tmiNewQWord, $this->tmiNewString, $this->tmiNewExpandable, $this->tmiNewMulti));
                $this->tmiNew->Name = "tmiNew";
                $this->tmiNew->ShortcutKeyDisplayString = NULL;
                // 
                // tmiNewKey
                // 
                $this->tmiNewKey->AccessibleDescription = NULL;
                $this->tmiNewKey->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiNewKey, "tmiNewKey");
                $this->tmiNewKey->BackgroundImage = NULL;
                $this->tmiNewKey->Name = "tmiNewKey";
                $this->tmiNewKey->Click->Add(new System:::EventHandler(array($this, "tmiNewKey_Click")));
                // 
                // tmiNewSep1
                // 
                $this->tmiNewSep1->AccessibleDescription = NULL;
                $this->tmiNewSep1->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiNewSep1, "tmiNewSep1");
                $this->tmiNewSep1->Name = "tmiNewSep1";
                // 
                // tmiNewBinary
                // 
                $this->tmiNewBinary->AccessibleDescription = NULL;
                $this->tmiNewBinary->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiNewBinary, "tmiNewBinary");
                $this->tmiNewBinary->BackgroundImage = NULL;
                $this->tmiNewBinary->Name = "tmiNewBinary";
                $this->tmiNewBinary->Click->Add(new System:::EventHandler(array($this, "tmiNewValue_Click")));
                // 
                // tmiNewDWord
                // 
                $this->tmiNewDWord->AccessibleDescription = NULL;
                $this->tmiNewDWord->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiNewDWord, "tmiNewDWord");
                $this->tmiNewDWord->BackgroundImage = NULL;
                $this->tmiNewDWord->Name = "tmiNewDWord";
                $this->tmiNewDWord->Click->Add(new System:::EventHandler(array($this, "tmiNewValue_Click")));
                // 
                // tmiNewQWord
                // 
                $this->tmiNewQWord->AccessibleDescription = NULL;
                $this->tmiNewQWord->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiNewQWord, "tmiNewQWord");
                $this->tmiNewQWord->BackgroundImage = NULL;
                $this->tmiNewQWord->Name = "tmiNewQWord";
                $this->tmiNewQWord->Click->Add(new System:::EventHandler(array($this, "tmiNewValue_Click")));
                // 
                // tmiNewString
                // 
                $this->tmiNewString->AccessibleDescription = NULL;
                $this->tmiNewString->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiNewString, "tmiNewString");
                $this->tmiNewString->BackgroundImage = NULL;
                $this->tmiNewString->Name = "tmiNewString";
                $this->tmiNewString->Click->Add(new System:::EventHandler(array($this, "tmiNewValue_Click")));
                // 
                // tmiNewExpandable
                // 
                $this->tmiNewExpandable->AccessibleDescription = NULL;
                $this->tmiNewExpandable->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiNewExpandable, "tmiNewExpandable");
                $this->tmiNewExpandable->BackgroundImage = NULL;
                $this->tmiNewExpandable->Name = "tmiNewExpandable";
                $this->tmiNewExpandable->Click->Add(new System:::EventHandler(array($this, "tmiNewValue_Click")));
                // 
                // tmiNewMulti
                // 
                $this->tmiNewMulti->AccessibleDescription = NULL;
                $this->tmiNewMulti->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiNewMulti, "tmiNewMulti");
                $this->tmiNewMulti->BackgroundImage = NULL;
                $this->tmiNewMulti->Name = "tmiNewMulti";
                $this->tmiNewMulti->Click->Add(new System:::EventHandler(array($this, "tmiNewValue_Click")));
                // 
                // tmiRename
                // 
                $this->tmiRename->AccessibleDescription = NULL;
                $this->tmiRename->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiRename, "tmiRename");
                $this->tmiRename->BackgroundImage = NULL;
                $this->tmiRename->Name = "tmiRename";
                $this->tmiRename->Click->Add(new System:::EventHandler(array($this, "tmiRename_Click")));
                // 
                // tmiEdit
                // 
                $this->tmiEdit->AccessibleDescription = NULL;
                $this->tmiEdit->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiEdit, "tmiEdit");
                $this->tmiEdit->BackgroundImage = NULL;
                $this->tmiEdit->Name = "tmiEdit";
                $this->tmiEdit->Click->Add(new System:::EventHandler(array($this, "tmiEdit_Click")));
                // 
                // tmiDelete
                // 
                $this->tmiDelete->AccessibleDescription = NULL;
                $this->tmiDelete->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiDelete, "tmiDelete");
                $this->tmiDelete->BackgroundImage = NULL;
                $this->tmiDelete->Name = "tmiDelete";
                $this->tmiDelete->Click->Add(new System:::EventHandler(array($this, "tmiDelete_Click")));
                // 
                // tssCms1
                // 
                $this->tssCms1->AccessibleDescription = NULL;
                $this->tssCms1->AccessibleName = NULL;
                $resources->ApplyResources($this->tssCms1, "tssCms1");
                $this->tssCms1->Name = "tssCms1";
                // 
                // tmiRefreshNode
                // 
                $this->tmiRefreshNode->AccessibleDescription = NULL;
                $this->tmiRefreshNode->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiRefreshNode, "tmiRefreshNode");
                $this->tmiRefreshNode->BackgroundImage = NULL;
                $this->tmiRefreshNode->Name = "tmiRefreshNode";
                $this->tmiRefreshNode->ShortcutKeyDisplayString = NULL;
                $this->tmiRefreshNode->Click->Add(new System:::EventHandler(array($this, "tmiRefreshNode_Click")));
                // 
                // tmiSelectAll
                // 
                $this->tmiSelectAll->AccessibleDescription = NULL;
                $this->tmiSelectAll->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiSelectAll, "tmiSelectAll");
                $this->tmiSelectAll->BackgroundImage = NULL;
                $this->tmiSelectAll->Name = "tmiSelectAll";
                $this->tmiSelectAll->Click->Add(new System:::EventHandler(array($this, "tmiSelectAll_Click")));
                // 
                // tmiCopyFullPath
                // 
                $this->tmiCopyFullPath->AccessibleDescription = NULL;
                $this->tmiCopyFullPath->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiCopyFullPath, "tmiCopyFullPath");
                $this->tmiCopyFullPath->BackgroundImage = NULL;
                $this->tmiCopyFullPath->Name = "tmiCopyFullPath";
                $this->tmiCopyFullPath->ShortcutKeyDisplayString = NULL;
                $this->tmiCopyFullPath->Click->Add(new System:::EventHandler(array($this, "tmiCopyFullPath_Click")));
                // 
                // tmiExport
                // 
                $this->tmiExport->AccessibleDescription = NULL;
                $this->tmiExport->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiExport, "tmiExport");
                $this->tmiExport->BackgroundImage = NULL;
                $this->tmiExport->Name = "tmiExport";
                $this->tmiExport->ShortcutKeyDisplayString = NULL;
                $this->tmiExport->Click->Add(new System:::EventHandler(array($this, "tmiExport_Click")));
                // 
                // imlImages
                // 
                $this->imlImages->ImageStream = $resources->GetObject("imlImages.ImageStream");
                $this->imlImages->TransparentColor = System:::Drawing:::Color::$Transparent;
                $this->imlImages->Images->SetKeyName(0, "binary");
                $this->imlImages->Images->SetKeyName(1, "closed");
                $this->imlImages->Images->SetKeyName(2, "open");
                $this->imlImages->Images->SetKeyName(3, "string");
                $this->imlImages->Images->SetKeyName(4, "unknown");
                $this->imlImages->Images->SetKeyName(5, "numeric");
                $this->imlImages->Images->SetKeyName(6, "desc");
                $this->imlImages->Images->SetKeyName(7, "asc");
                // 
                // lvwRegistry
                // 
                $this->lvwRegistry->AccessibleDescription = NULL;
                $this->lvwRegistry->AccessibleName = NULL;
                $resources->ApplyResources($this->lvwRegistry, "lvwRegistry");
                $this->lvwRegistry->BackgroundImage = NULL;
                $this->lvwRegistry->Columns->AddRange(array($this->cohName, $this->cohType, $this->cohValue, $this->cohAlternative));
                $this->lvwRegistry->ContextMenuStrip = $this->cmsContext;
                $this->lvwRegistry->Font = NULL;
                $this->lvwRegistry->FullRowSelect = true;
                $this->lvwRegistry->HideSelection = false;
                $this->lvwRegistry->LabelEdit = true;
                $this->lvwRegistry->Name = "lvwRegistry";
                $this->lvwRegistry->SmallImageList = $this->imlImages;
                $this->lvwRegistry->Sorting = System:::Windows:::Forms:::SortOrder::Ascending;
                $this->lvwRegistry->UseCompatibleStateImageBehavior = false;
                $this->lvwRegistry->View = System:::Windows:::Forms:::View::Details;
                $this->lvwRegistry->ItemActivate->Add(new System:::EventHandler(array($this, "lvwRegistry_ItemActivate")));
                $this->lvwRegistry->AfterLabelEdit->Add(new System:::Windows:::Forms:::LabelEditEventHandler(array($this, "lvwRegistry_AfterLabelEdit")));
                $this->lvwRegistry->ColumnClick->Add(new System:::Windows:::Forms:::ColumnClickEventHandler(array($this, "lvwRegistry_ColumnClick")));
                $this->lvwRegistry->KeyDown->Add(new System:::Windows:::Forms:::KeyEventHandler(array($this, "lvwRegistry_KeyDown")));
                // 
                // cohName
                // 
                $resources->ApplyResources($this->cohName, "cohName");
                // 
                // cohType
                // 
                $resources->ApplyResources($this->cohType, "cohType");
                // 
                // cohValue
                // 
                $resources->ApplyResources($this->cohValue, "cohValue");
                // 
                // cohAlternative
                // 
                $resources->ApplyResources($this->cohAlternative, "cohAlternative");
                // 
                // mnsMain
                // 
                $this->mnsMain->AccessibleDescription = NULL;
                $this->mnsMain->AccessibleName = NULL;
                $resources->ApplyResources($this->mnsMain, "mnsMain");
                $this->mnsMain->BackgroundImage = NULL;
                $this->mnsMain->Font = NULL;
                $this->mnsMain->Items->AddRange(array($this->tmiFile, $this->tmiTools, $this->tmiHelp));
                $this->mnsMain->Name = "mnsMain";
                // 
                // tmiFile
                // 
                $this->tmiFile->AccessibleDescription = NULL;
                $this->tmiFile->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiFile, "tmiFile");
                $this->tmiFile->BackgroundImage = NULL;
                $this->tmiFile->DropDownItems->AddRange(array($this->tmiExit));
                $this->tmiFile->Name = "tmiFile";
                $this->tmiFile->ShortcutKeyDisplayString = NULL;
                // 
                // tmiExit
                // 
                $this->tmiExit->AccessibleDescription = NULL;
                $this->tmiExit->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiExit, "tmiExit");
                $this->tmiExit->BackgroundImage = NULL;
                $this->tmiExit->Name = "tmiExit";
                $this->tmiExit->Click->Add(new System:::EventHandler(array($this, "tmiExit_Click")));
                // 
                // tmiTools
                // 
                $this->tmiTools->AccessibleDescription = NULL;
                $this->tmiTools->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiTools, "tmiTools");
                $this->tmiTools->BackgroundImage = NULL;
                $this->tmiTools->DropDownItems->AddRange(array($this->tmiLanguage, $this->tssTools1, $this->tmiJump, $this->tmiJumpToSame, $this->tmiRefreshAll));
                $this->tmiTools->Name = "tmiTools";
                $this->tmiTools->ShortcutKeyDisplayString = NULL;
                $this->tmiTools->DropDownOpening->Add(new System:::EventHandler(array($this, "tmiTools_DropDownOpening")));
                // 
                // tmiLanguage
                // 
                $this->tmiLanguage->AccessibleDescription = NULL;
                $this->tmiLanguage->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiLanguage, "tmiLanguage");
                $this->tmiLanguage->BackgroundImage = NULL;
                $this->tmiLanguage->Name = "tmiLanguage";
                $this->tmiLanguage->ShortcutKeyDisplayString = NULL;
                $this->tmiLanguage->Click->Add(new System:::EventHandler(array($this, "tmiLanguage_Click")));
                // 
                // tssTools1
                // 
                $this->tssTools1->AccessibleDescription = NULL;
                $this->tssTools1->AccessibleName = NULL;
                $resources->ApplyResources($this->tssTools1, "tssTools1");
                $this->tssTools1->Name = "tssTools1";
                // 
                // tmiJump
                // 
                $this->tmiJump->AccessibleDescription = NULL;
                $this->tmiJump->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiJump, "tmiJump");
                $this->tmiJump->BackgroundImage = NULL;
                $this->tmiJump->Name = "tmiJump";
                $this->tmiJump->ShortcutKeyDisplayString = NULL;
                $this->tmiJump->Click->Add(new System:::EventHandler(array($this, "tmiJump_Click")));
                // 
                // tmiJumpToSame
                // 
                $this->tmiJumpToSame->AccessibleDescription = NULL;
                $this->tmiJumpToSame->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiJumpToSame, "tmiJumpToSame");
                $this->tmiJumpToSame->BackgroundImage = NULL;
                $this->tmiJumpToSame->Name = "tmiJumpToSame";
                $this->tmiJumpToSame->ShortcutKeyDisplayString = NULL;
                $this->tmiJumpToSame->Click->Add(new System:::EventHandler(array($this, "tmiJumpToSame_Click")));
                // 
                // tmiRefreshAll
                // 
                $this->tmiRefreshAll->AccessibleDescription = NULL;
                $this->tmiRefreshAll->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiRefreshAll, "tmiRefreshAll");
                $this->tmiRefreshAll->BackgroundImage = NULL;
                $this->tmiRefreshAll->Name = "tmiRefreshAll";
                $this->tmiRefreshAll->ShortcutKeyDisplayString = NULL;
                $this->tmiRefreshAll->Click->Add(new System:::EventHandler(array($this, "tmiRefreshAll_Click")));
                // 
                // tmiHelp
                // 
                $this->tmiHelp->AccessibleDescription = NULL;
                $this->tmiHelp->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiHelp, "tmiHelp");
                $this->tmiHelp->BackgroundImage = NULL;
                $this->tmiHelp->DropDownItems->AddRange(array($this->tmiAbout));
                $this->tmiHelp->Name = "tmiHelp";
                $this->tmiHelp->ShortcutKeyDisplayString = NULL;
                // 
                // tmiAbout
                // 
                $this->tmiAbout->AccessibleDescription = NULL;
                $this->tmiAbout->AccessibleName = NULL;
                $resources->ApplyResources($this->tmiAbout, "tmiAbout");
                $this->tmiAbout->BackgroundImage = NULL;
                $this->tmiAbout->Name = "tmiAbout";
                $this->tmiAbout->ShortcutKeyDisplayString = NULL;
                $this->tmiAbout->Click->Add(new System:::EventHandler(array($this, "tmiAbout_Click")));
                // 
                // sfdExport
                // 
                $this->sfdExport->DefaultExt = "reg";
                $resources->ApplyResources($this->sfdExport, "sfdExport");
                // 
                // frmMain
                // 
                $this->AccessibleDescription = NULL;
                $this->AccessibleName = NULL;
                $resources->ApplyResources($this, "\$this");
                $this->AutoScaleMode = System:::Windows:::Forms:::AutoScaleMode::Font;
                $this->BackgroundImage = NULL;
                $this->Controls->Add($this->tscMain);
                $this->Font = NULL;
                $this->MainMenuStrip = $this->mnsMain;
                $this->Name = "frmMain";
                $this->Load->Add(new System:::EventHandler(array($this, "frmMain_Load")));
                $this->FormClosed->Add(new System:::Windows:::Forms:::FormClosedEventHandler(array($this, "frmMain_FormClosed")));
                $this->tscMain->BottomToolStripPanel->ResumeLayout(false);
                $this->tscMain->BottomToolStripPanel->PerformLayout();
                $this->tscMain->ContentPanel->ResumeLayout(false);
                $this->tscMain->TopToolStripPanel->ResumeLayout(false);
                $this->tscMain->TopToolStripPanel->PerformLayout();
                $this->tscMain->ResumeLayout(false);
                $this->tscMain->PerformLayout();
                $this->stsStatus->ResumeLayout(false);
                $this->stsStatus->PerformLayout();
                $this->splMain->Panel1->ResumeLayout(false);
                $this->splMain->Panel2->ResumeLayout(false);
                $this->splMain->ResumeLayout(false);
                $this->cmsContext->ResumeLayout(false);
                $this->mnsMain->ResumeLayout(false);
                $this->mnsMain->PerformLayout();
                $this->ResumeLayout(false);
            }
        }
    }
?>
