<? //© Ðonny 2009 - Part of Phalanger project
    import namespace RegEditPHP;
    import namespace System:::Windows:::Forms;
    import namespace Microsoft:::Win32;
    import namespace System;
    
    namespace RegEditPHP{
        ///<summary>Main application form</summary>
	    partial class frmMain extends System:::Windows:::Forms:::Form{
	        //This class represents main form of this application. It contains the most of the code.
//Initialization
             ///<summary>This name is used as name and text of placeholder child key</summary>
             const dummy = "dummy node"; //Each tree node in TreeView that have never been expanded is filled with one dummy node to have the + sign in front of itself (to appear expandable). This node is replaced with actual nodes before expanding.
             ///<summary>Gives access to localized resources</summary>
             private $Resources;//See ResourceAccessor.php
             ///<summary>Internal initialization method</summary>
             private function Init(){
                //Call to this method wa manually added to __construct() in frmMain.php
                //Following commented lines are no longer needed because pictures now can be added form designer
                //$this->tmiNew->Image = Program::$Resources->new;
                //$this->tmiNewKey->Image = Program::$Resources->folder->ToBitmap();
                //$this->tmiNewDWord->Image = 
                //$this->tmiNewQWord->Image = Program::$Resources->numeric->ToBitmap();
                //$this->tmiNewString->Image = 
                //$this->tmiNewExpandable->Image = 
                //$this->tmiNewMulti->Image = Program::$Resources->string->ToBitmap();
                //$this->tmiNewBinary->Image = Program::$Resources->binary->ToBitmap();
                //$this->tmiRename->Image = Program::$Resources->rename;
                //$this->tmiEdit->Image = Program::$Resources->edit;
                //$this->tmiDelete->Image = Program::$Resources->delete;
                //$this->tmiRefreshNode->Image = Program::$Resources->refresh;
                //Initialize sorter to sort list of values
                $this->Sorter = new Sorter($this->lvwRegistry);
                //Fill nodes added in designer with placeholder subnodes
                //This can be done in designer as well but it's so much clicking and use of self::dummy is better than use of "dummy key" (which'd be case of designer)
                foreach($this->tvwRegistry->Nodes as $node):
                    $node->Nodes->Add(self::dummy)->Name=self::dummy;
                endforeach;
             }
//Main menu  
             //Handles click to Exit menu item
             private function tmiExit_Click(System:::Object $sender, System:::EventArgs $e) {
                $this->Close();//Close the main form (it terminates the application)
             }
            //Handles click to Language menu item
            private function tmiLanguage_Click(System:::Object $sender, System:::EventArgs $e) {
                //Create instance of form for language sleection
                $lSel = new LanguageSelector();
                //Show it and test if user clicked OK
                if($lSel->ShowDialog($this) == DialogResult::OK):
                    //If so, the application must be restarted (changing UI strings without restart is too much work)
                    //To restart the application we need know path of it - location of assembly
                    //So get the assembly from type contained in it
                    $MyType = CLRTypeOf frmMain;
                    $MyPath = $MyType->Assembly->Location;
                    try{ //Launch it
                        System:::Diagnostics:::Process::Start($MyPath);
                    }catch(System:::Exception $ex){ //This probably will not happen
                        self::OnError(Program::$Resources->e_Restart."\r\n$ex->Message");
                        return;
                    }
                    //If application successfully started, this can be closed
                    $this->Close();
                endif;
            }
            //Handles click on the Jump menu item
            private function tmiJump_Click(System:::Object $sender, System:::EventArgs $e) {
                //get path to jump to using InputBox (see InputBox.user.php)
                $Path = InputBox::GetInput(Program::$Resources->JumpToKey,Program::$Resources->JumpToKey_t);
                if(!is_null($Path)) //InputBox::GetInput returns null on cancel
                    $this->JumpToKey($Path);//Do the jump
            }
            //Handles click on the Refresh all menu item
            private function tmiRefreshAll_Click(System:::Object $sender, System:::EventArgs $e) {
                //Remember currently selected key
                $OldPath = $this->tvwRegistry->SelectedNode->FullPath;
                //Clear content of all top-level nodes
                foreach($this->tvwRegistry->Nodes as $node):
                    $node->Nodes->Clear;
                    //And add placeholder into each of them to appear expandable
                    $node->Nodes->Add(self::dummy)->Name=self::dummy;
                endforeach; 
                //For user convenience, navigate to node that was selected when reloading (I hate when reloading changes my status)
                $this->JumpToKey($OldPath);    
            }
            ///<summary>Navigates to key with given path</summary>
            ///<param name="Path">Path full to navigate too</param>
            ///<returns type="bool">success</returns>
            private function JumpToKey(string $Path){
                //Firts trim trailing backslashes
                //We must use $Path->ToString() because we want to work with .NET string and .NET string is not same as PHP string (i think this is bug, teher should be only one string)
                while($Path->ToString()->EndsWith("\\"))
                    $Path = $Path->ToString()->Substring(0,strlen($Path) - 1);
                //You can use many of PHP built-in function you are familiar with. Sometimes they are nicer/better tna .NET ones - as explode()
                $PathParts = explode("\\",$Path);
                $CurrentNodes = $this->tvwRegistry->Nodes;//Starting with top-level nodes
                foreach($PathParts as $Part)://For each path part
                    foreach($CurrentNodes as $Node)://Wee look for node with same at current level
                        if($Node->Name->ToLower() == $Part->ToString()->ToLower())://Note: Usage of ToString() on PHP string is required to use ToLower(); $Node->Name is not PHP string but .NET string
                            $this->NoExpandQuestion=true;//Turn of expand question ofr HKCR
                            $Node->Expand();//Expand node
                            $this->NoExpandQuestion=false;//Turn the question back on
                            $CurrentNodes = $Node->Nodes;//Dive one more level
                            $LastNode = $Node;//Remember last succesfully found node (in case of failure, we will navigate to last-match level)
                            continue 2;//Next (deeper) level
                        endif;
                    endforeach;
                    self::OnError(Program::$Resources->e_KeyDoesNotExist($Path));//continue 2 above skips this line if node is found
                    break;
                endforeach;
                if(!is_null($LastNode))://If there was matc at least at one level
                    $this->tvwRegistry->SelectedNode = $LastNode;//select it
                    $LastNode->EnsureVisible();//And scroll it to view
                    return $LastNode->FullPath->ToLower() == $Path->ToString()->ToLower();//Only when all levels succeeded we consider jump successfull
                else:
                    return false;//The root name was unknown
                endif;
            }
            //Hanndles click on the Jump to same key under ... menu item
            //It navigates to node with same addres but sapped first part HKLM<->HKCU (if possible)
            private function tmiJumpToSame_Click(System:::Object $sender, System:::EventArgs $e) {
                if($this->tvwRegistry->SelectedNode->FullPath->StartsWith("HKEY_CURRENT_USER\\")):
                    $start=HKEY_CURRENT_USER;//current 1st part
                    $new=HKEY_LOCAL_MACHINE;//new 1st part
                else:
                    $new=HKEY_CURRENT_USER;//new 1st part
                    $start=HKEY_LOCAL_MACHINE;//current 1st part
                endif;
                $new= $new . $this->tvwRegistry->SelectedNode->FullPath->Substring(strlen($start));///replace 1st part
                $this->JumpToKey($new);//Navigate to new path (if navigation is not possible, navigates as near as can)
            }
            //Handles dropdown opening of the Tools top-level menu item
            private function tmiTools_DropDownOpening(System:::Object $sender, System:::EventArgs $e) {
                //We enable/disable Jump to same menu item depending on if user is under HKCU or HKLM or not
                $this->tmiJumpToSame->Enabled = (
                    $this->tvwRegistry->SelectedNode->FullPath->StartsWith("HKEY_CURRENT_USER\\") or
                    $this->tvwRegistry->SelectedNode->FullPath->StartsWith("HKEY_LOCAL_MACHINE\\"));
                //And adjust text to show name of target hive (top-level node)
                if($this->tmiJumpToSame->Enabled and $this->tvwRegistry->SelectedNode->FullPath->StartsWith("HKEY_CURRENT_USER\\")):
                    $this->tmiJumpToSame->Text = Program::$Resources->JumToSameKey("HKLM");
                elseif($this->tmiJumpToSame->Enabled):
                    $this->tmiJumpToSame->Text = Program::$Resources->JumToSameKey("HKCU");
                endif;
            }
            //Handles click on About menu item
            private function tmiAbout_Click(System:::Object $sender, System:::EventArgs $e) {
                $ad=new dlgAbout();//Create about dialog
                $ad->ShowDialog($this);//And show it
            }

             
//Tree
             ///<summary>If false, user is askt if (s)he really wants to expand HKCR</summary>
             private $NoExpandQuestion = false;
             //Called before tree node is expanded
             //Dummy sub keys are deleted in this event and actual node content is loaded
             private function tvwRegistry_BeforeExpand(System:::Object $sender, System:::Windows:::Forms:::TreeViewCancelEventArgs $e) {
                if(!$this->NoExpandQuestion and $e->Node->FullPath==HKEY_CLASSES_ROOT and $e->Node->Nodes->Count == 1):
                    //PHPBUG:
                    //We use Sorter::Iterate because indexing and default properties does not work well in current version of Phalanger
                    if(Sorter::Iterate($e->Node->Nodes->GetEnumerator(),0)->Name == self::dummy):
                        //If node is HKCR and it was never expanded before, ask user if (s)he really wants to expand it (because it typically contains as many 2nd-level subkey as loading take a while (even in MS's regedit)
                        if(MessageBox::Show(Program::$Resources->ExpandHKCR,Program::$Resources->ExpandHKCR_t,MessageBoxButtons::YesNo,MessageBoxIcon::Information) != DialogResult::Yes):
                            //Cancels expanding (node will remain unexpanded)
                            $e->Cancel = true;
                            return;
                        endif;
                    endif;
                endif;
                //Test if node hals only one child - the Dummy
                if($e->Node->Nodes->Count==1):
                    //Following 2 lines are same as Sorter::Iterate($e->Node->Nodes->GetEnumerator(),0) (legacy)
                    $nem=$e->Node->Nodes->GetEnumerator();
                    $nem->MoveNext();
                    if($nem->Current->Name=self::dummy):
                        //Need load childs from registry
                        $this->Cursor = Cursors::$WaitCursor;//Set cursor to horglass (it may take a while)
                        try{
                            self::FillNode($e->Node);//Load subkeys
                        }catch(System:::Exception $ex){
                            $e->Node->Nodes->Clear();//When there is error, treat key as empty
                            self::OnError($ex->Message);//Show error message
                        }
                        $this->Cursor = Cursors::$Default;//Revert cursor icon
                    endif;
                endif;    
             }
             //Caled after tree node is expanded   
             private function tvwRegistry_AfterExpand(System:::Object $sender, System:::Windows:::Forms:::TreeViewEventArgs $e) {
                //We handle change of icon here
                //Note: Tree node has 2 icons - normal and selected
                // Both icons are taken from associated image list (set in designer)
                // and we want them to be the same, thus same key
                $e->Node->ImageKey="open";
                $e->Node->SelectedImageKey="open";
             }
            //Called after tree node is collapsed
             private function tvwRegistry_AfterCollapse(System:::Object $sender, System:::Windows:::Forms:::TreeViewEventArgs $e) {
                //Same as above, but change icon to closed
                $e->Node->ImageKey="closed";
                $e->Node->SelectedImageKey="closed";
             }
             ///<summary>Fills node with subnodes representing corresponding key subkeys</summary>
             ///<param name="Node">Node to fill</param>
             ///<returns type="void"/>
             private static function FillNode(TreeNode $Node){
                //1st clear any current content
                //It's typically dummy node, but for refresh it may be anything
                $Node->Nodes->Clear();
                //Get key for node path
                //Nice with TreeViewIs that it gives path of node which is immediatelly usable for registry lookup
                $Key=self::getRegistryKey($Node->FullPath);     
                $SubKeys=$Key->GetSubKeyNames();//Enumerate sub keys (this may throw an exception and it must be handles by caller)
                foreach($SubKeys as $SubKeyName)://Add the keys to current node
                    $SubKey = $Node->Nodes->Add($SubKeyName);//Add it (this creates node with given text, adds it and returns it)
                    $SubKey->Name = $SubKeyName;//Set its name (it is used by Fullpath)
                    $SubKey->ImageKey = "closed";//Set its image
                    $SubKey->SelectedImageKey = "closed";
                    $SubKey->Nodes->Add(self::dummy)->Name=self::dummy;//And fill it with dummy child (to seem to be axpandable)
                endforeach;
                //Note: Every node seems to be expandable if it has been never expanded
                // so user may experience following:
                // (S)he clicks the + and instead of node expanding, the + vanishes
                // it's not 100% good behavior, but is quite common and saves us some pre-testing
             }
             ///<summary>Gets registry key for its path</summary>
             ///<param name="Path">Full path of key to open</param>
             ///<param name="writeable">True to open key for writing, false to open it as readonly</param>
             ///<returns type="registryKey">Opened key</param>
             private static function getRegistryKey(string $Path,bool $writeable = false){
                //We must firts parse the path
                $parts=explode("\\",$Path);//Explode it by \
                //The bad thisng with .NET registry API is that there is no general-purpose function for opening registry key
                // The top-level keys (so-called hives) must be treated separatelly
                switch($parts[0])://So switch for them
                    case HKCR://Shortcut names are not currently utilized by this program
                    case HKEY_CLASSES_ROOT: $root=Registry::$ClassesRoot;
                    break;
                    case HKCU:
                    case HKEY_CURRENT_USER: $root=Registry::$CurrentUser;
                    break;
                    case HKLM:
                    case HKEY_LOCAL_MACHINE: $root=Registry::$LocalMachine;
                    break;
                    case HKU:
                    case HKEY_USERS: $root=Registry::$Users;
                    break;
                    case HKCC:
                    case HKEY_CURRENT_CONFIG: $root=Registry::$CurrentConfig;
                    break;
                    case HKDD:
                    case HKEY_DYN_DATA: $root=Registry::$DynData;
                    break;
                    case HKPD:
                    case HKEY_PERFORMANCE_DATA: $root=Registry::$PerformanceData;
                    break;
                    default: throw new CLRException(new ArgumentException(Program::$Resources->e_UnknownRoot));
                endswitch;
                if(count($parts)==1) return $root;//Return root for single-part path
                //Otherwise open root's sub key (now ve have general-purpose function to open multiplùe levels at once)
                return $root->OpenSubKey(i'String'::Join("\\",$parts,1,count($parts)-1),$writeable);
                //Note: Two uncommon things at line above:
                //1) i'String' is Phalanger way how to point to class or function that has same name as Phalanger keyword (in this case string). We use this i-preceded string in single quotes. i'String' means System:::String
                //2) I pass $parts (PHP array) where .NET String[] array is expected
                //PHPBUG:
                //   It is strange, but Phalanger currently has not silent implicit conversion from string to System::String but has conversion of array to String[]
             } 
            //Called after node in tree is selected
            private function tvwRegistry_AfterSelect(System:::Object $sender, System:::Windows:::Forms:::TreeViewEventArgs $e) {
                //We need show values in this key
                try{ //get key and show values for it
                    $this->LoadValues(self::GetRegistryKey($e->Node->FullPath));
                }catch(System:::Exception $ex){
                    self::OnError($ex->Message);     
                }
                $this->tslKey->Text=$e->Node->FullPath;
            }
            
            ///<summary>Loads values for given key rto ListView>/summary>
            ///<param name="key">Key to load values from</param>
            ///<returns type="void"/>
            private function LoadValues(RegistryKey $key){
                //Gate value names (may cause Exception, must be caught by caller)
                $ValueNames=$key->GetValueNames();
                //Clear list
                $this->lvwRegistry->Items->Clear();
                foreach($ValueNames as $Name)://For each name
                    $item=$this->lvwRegistry->Items->Add($Name);//Create and add item
                    $item->Name=$Name;//Set its name
                    switch($key->GetValueKind($Name))://We must support regtlue types separately
                        case RegistryValueKind::Binary://Binary (got as array of bytes)
                            $item->ImageKey="binary";
                            $kind=REG_BINARY;//You know - undefined constant's
                            $value=$key->GetValue($Name);
                            if(!is_null($value))$value=self::GetBytesString($value,&$alt);//Call helper method to display byte array
                        break;    
                        case RegistryValueKind::DWord://DWord (got as int32)
                            $item->ImageKey="numeric";
                            $kind=REG_DWORD;
                            $value=$key->GetValue($Name);
                            $alt="0x".$value->ToString("X");//Hexa
                            $value=$value->ToString();//Decadic                
                        break;    
                        case RegistryValueKind::ExpandString://Expandable string (%something% is replaced by value of system variable something)
                            $item->ImageKey="string";
                            $kind=REG_EXPAND_SZ;
                            $value=$key->GetValue($Name,null,RegistryValueOptions::DoNotExpandEnvironmentNames);//Unexpanded value
                            $alt=$key->GetValue($Name);//Expanded value
                        break;
                        case RegistryValueKind::MultiString://MultistringgotasString
                            $item->ImageKey="string";
                            $kind=REG_MULTI_SZ;
                            $value=$key->GetValue($Name);
                            $value=System:::String::Join("; ",$value);//Just join it using semicolon
                            $alt=$value;//No alternative sight
                        break;
                        case RegistryValueKind::QWord://QWord (got as int64) XP regedit does not support it, but we DO!
                            $item->ImageKey="numeric";
                            $kind=REG_QWORD;
                            $value=$key->GetValue($Name);
                            $alt="0x".$value->ToString("X");//Hexa
                            $value=$value->ToString();//decadic
                        break;
                        case RegistryValueKind::i'String'://Normal string
                            //PHPBUG:
                            //This is littele inconsistent behavior:
                            // While RegistryValueKind::String is invalid
                            // System:::String is valid
                            $item->ImageKey="string";
                            $kind=REG_SZ;
                            $value=$key->GetValue($Name);
                            $alt=$value;//There is no alternative sight of plain text
                        break;
                        default://Unknown kind (tehre are more kinds of registry values - uncommon and I dunno what good for)
                            //Just show that there is sth
                            $item->ImageKey="unknown";
                            $kind="unknown";    
                            $value=Program::$Resources->UnknownValue;
                            $alt=Program::$Resources->UnknownValueAlt;
                    endswitch;
                    //Fill subitems (2nd, 3rd, 4th columns in list view)
                    $item->SubItems->Add($kind);
                    $item->SubItems->Add($value);
                    $item->SubItems->Add($alt);
                endforeach;
                $this->lvwRegistry->Sort();//Sort list (sorting is actually provided by Sorter.php)
                $this->AfterSort();//Some after-sort settings
            }  
            ///<summary>Gets display string for byte array</summary>
            ///<param name="bytes">Byte array to get values from</param>
            ///<param name="alt" type=´"string">Output parameter filled with alternative (decimal) representation of <paramref name="bytes"/></param>
            ///<returns type="string">String representation of byte array (2-digits hexadecimal numbers separated by spaces)</returns>
            private static function GetBytesString(i'Array' $bytes,&$alt){
                //Note: i'Array' means that we acceps System:::array, not PHP array
                //Output parameters passed by reference are possible in PHP as well, but not commonly used. This uis the rare case when it is useful
                // In phalanger you can use [out] attribute, whoch I'm not using here
                //StringBuolder is efficient way of concatenating strings in .NET
                $ret=new System:::Text:::StringBuilder(count($bytes)*3);//Initial capacity count*3 is not compulsory but is god for efficiency
                $ret2=new System:::Text:::StringBuilder(count($bytes)*3);//For decimal capacity as approximate
                foreach($bytes as $byte):
                    if($ret->Length>0) $ret->Append(" ");//Add space, if non empty
                    if($ret2->Length>0) $ret2->Append(" ");
                    $ret->Append($byte->ToString("X2"));//Hexadecimal, 2 didigts    
                    $ret2->Append($byte->ToString());//Decimal
                endforeach;
                $alt=$ret2->ToString(); //StringBuilder must be converted to string explicitly
                return $ret->ToString(); 
            }             
            //Handles click on list view collumn
            private function lvwRegistry_ColumnClick(System:::Object $sender, System:::Windows:::Forms:::ColumnClickEventArgs $e) {
                //We do sorting here
                if($this->Sorter->Column == $e->Column):
                    //If user click same column 2nd or more times
                    switch($this->Sorter->Order)://reverse order
                        case SortOrder::Ascending:
                            $this->Sorter->Order=SortOrder::Descending;
                        break;
                        default:$this->Sorter->Order=SortOrder::Ascending;
                    endswitch;
                else://CHange sort column and set order to ASC
                    $this->Sorter->Order=SortOrder::Ascending;
                    $this->Sorter->Column = $e->Column;
                endif;
                $this->lvwRegistry->Sort();//Force apply sorting
                $this->AfterSort();//After sort operations
            }            
            ///<summary>Does common after ListView sort operations</summary>
            ///<returns type="void"/>
            private function AfterSort(){
                foreach($this->lvwRegistry->Columns as $col)
                    $col->ImageKey=null;//Remove sort image from all columns
                //And set it for the used one using direction-aware image
                Sorter::Iterate($this->lvwRegistry->Columns->GetEnumerator(),$this->Sorter->Column)->ImageKey = $this->Sorter->Order == SortOrder::Ascending ? 'asc' : 'desc';
            }
//Context menu and actions
            //Called befor context menu is opened
            private function cmsContext_Opening(System:::Object $sender, System:::ComponentModel:::CancelEventArgs $e) {
                //Note: ContextMenuStrip is associated with TreeView and ListView in designer
                //We hide/show and enable/disable certain menu items here based on conditions
                //Note: The === compares 2 instances for bering reference equal (the same instance). It's stronger than ==.Phalanger behavior with CLR classes is a little different form original PHP usage.
                if($this->cmsContext->SourceControl === $this->tvwRegistry and is_null($this->tvwRegistry->SelectedNode)):
                    //No TreeNode selected - this should not happedn
                    $e->Cancel=true;//Do not show the menu
                    return;
                endif;
                //Edit only for values
                $this->tmiEdit->Visible = (
                    $this->cmsContext->SourceControl === $this->lvwRegistry and
                    $this->lvwRegistry->SelectedItems->Count == 1);
                //No rename for no value selected or more values selected
                $this->tmiRename->Visible =
                    !($this->cmsContext->SourceControl === $this->lvwRegistry and $this->lvwRegistry->SelectedItems->Count <> 1);
                //No delete for no values selected
                $this->tmiDelete->Visible = 
                    !($this->cmsContext->SourceControl === $this->lvwRegistry and $this->lvwRegistry->SelectedItems->Count == 0);
                //No delete for root
                $this->tmiDelete->Enabled = 
                    !($this->cmsContext->SourceControl === $this->tvwRegistry and is_null($this->tvwRegistry->SelectedNode->Parent));
                //Select all only for values
                $this->tmiSelectAll->Visible = $this->cmsContext->SourceControl === $this->lvwRegistry;
                //If there is nothig to select ...
                $this->tmiSelectAll->Enabled = $this->lvwRegistry->Items->Count > 0;
                //Refres only for nodes
                $this->tmiRefreshNode->Visible = $this->cmsContext->SourceControl === $this->tvwRegistry;
                //Copy only for single key or current values
                //Note: LIst veiw recognizec selected values and focused vakues
                $this->tmiCopyFullPath->Enabled = (
                    $this->cmsContext->SourceControl === $this->tvwRegistry or
                    ($this->cmsContext->SourceControl === $this->lvwRegistry and !is_null($this->lvwRegistry->FocusedItem)));
            }
            //Handles attempt to rename tree node
            private function tvwRegistry_BeforeLabelEdit(System:::Object $sender, System:::Windows:::Forms:::NodeLabelEditEventArgs $e) {
                //Top-level cannot be renamed
                $e->CancelEdit = $e->CancelEdit || is_null($e->Node->Parent);
            }
            //Hanldes confirmed rename of tree node
            private function tvwRegistry_AfterLabelEdit(System:::Object $sender, System:::Windows:::Forms:::NodeLabelEditEventArgs $e) {
                //Reaname can be cancled or it is not rename at all (user haven't changed text - indinated by null Label)
                if(is_null($e->Label) or $e->Label == $e->Node->Text) $e->CancelEdit=true;//No change
                if($e->CancelEdit) return;
                $e->CancelEdit = !$this->RenameKey($e->Node, $e->Label);//Try to rename
            }
            //Handles confirned rename of values
            private function lvwRegistry_AfterLabelEdit(System:::Object $sender, System:::Windows:::Forms:::LabelEditEventArgs $e) {
                //Same as rename of key - the event can indicate no rename at all (null Label)
                if(is_null($e->Label)) $e->CancelEdit = true;
                $node = Sorter::Iterate($this->lvwRegistry->Items->GetEnumerator(),$e->Item);
                if($node->Text == $e->Label) $e->CancelEdit = true;//No change
                if($e->CancelEdit) return;
                $e->CancelEdit = $this->RenameValue($node, $e->Label);//Try to rename
            }
            //Handles click on New key context menu item
            private function tmiNewKey_Click(System:::Object $sender, System:::EventArgs $e) {
                $this->AddKey();//Add the key
            }
            //Handles click on Rename context menu item
            private function tmiRename_Click(System:::Object $sender, System:::EventArgs $e) {
                //We just detect which control context menu is show on and let the control to deal with rename on its own (in fact 2 functions above)
                if($this->cmsContext->SourceControl === $this->tvwRegistry):
                    $this->tvwRegistry->SelectedNode->BeginEdit();//Tree
                else:
                   Sorter::Iterate($this->lvwRegistry->SelectedItems,0)->BeginEdit();//List
                endif;
            }
            //Handles click on Edit context menu item
            private function tmiEdit_Click(System:::Object $sender, System:::EventArgs $e) {
                $this->EditValue();//Do the edit
            }
            //Handles doble-click on list view item
            //Note: Some lama-users like to instruct system to activate list view item on single click instead of on double click (link behavior)
            // If your ListView follows this user decision can be set in designer
            private function lvwRegistry_ItemActivate(System:::Object $sender, System:::EventArgs $e) {
                $this->EditValue();//Edit value value
            }
            //Handles click on delete on context menu item 
            private function tmiDelete_Click(System:::Object $sender, System:::EventArgs $e) {
                //We just detect control contet menu is shown on and call appropriate method
                if($this->cmsContext->SourceControl === $this->tvwRegistry):
                    $this->DeleteKey();//Delete key
                else:
                    $this->DeleteValues();//Delete value
                endif;
            }
            //Handles key down when tree view is active
            private function tvwRegistry_KeyDown(System:::Object $sender, System:::Windows:::Forms:::KeyEventArgs $e) {
                //We are intrested only in some keys
                //KeyData contains or combination of KeyCode and Ctrl+Alt+Shift state
                switch($e->KeyData):
                    case Keys::Delete: $this->DeleteKey();//Delete key (w/o any Ctrl+Shift+Alt)
                    break;
                    case Keys::F2://F2 key (w/o ...)
                        if(!is_null($this->tvwRegistry->SelectedNode) and !is_null($this->tvwRegistry->SelectedNode->Parent)):
                            $this->tvwRegistry->SelectedNode->BeginEdit();        
                        endif;
                    break;  
                    case Keys::F7: $this->AddKey();//F7 - I love Total Commander :-)
                endswitch;
            }
            //Handles key down on list view
            private function lvwRegistry_KeyDown(System:::Object $sender, System:::Windows:::Forms:::KeyEventArgs $e) {
                //Same note as abowe but we're interested in more keys
                switch($e->KeyData):
                    case Keys::Delete: $this->DeleteValues();//delete
                    break;
                    case Keys::F2://rename
                        if(!is_null($this->lvwRegistry->FocusedItem))
                            $this->lvwRegistry->FocusedItem->BeginEdit();
                    break;
                    case Keys::Control | Keys::A://This is how to uttilize the key combination
                        foreach($this->lvwRegistry->Items as $item)//Select all items
                            $item->Selected=true;
                    break;
                    case Keys::F4://Edit (like in TC)
                    case Keys::Enter: $this->EditValue();//Edit
                    break;
                    case Keys::Shift | Keys::F4://New string (like in TC)
                        $this->Addvalue(RegistryValueKind::i'String');
                    break;
                endswitch;
            }
            //Handles click on any of New value menu items
            //Note: More events can have sam handler and more hahndlers can be attached to single event in .NET
            private function tmiNewValue_Click(System:::Object $sender, System:::EventArgs $e) {
                //Detect kind from item being clicked
                if($sender === $this->tmiNewBinary)
                    $kind = RegistryValueKind::Binary;
                elseif($sender === $this->tmiNewDWord)
                    $kind = RegistryValueKind::DWord;
                elseif($sender === $this->tmiNewExpandable)
                    $kind = RegistryValueKind::ExpandString;    
                elseif($sender === $this->tmiNewMulti)
                    $kind = RegistryValueKind::MultiString;
                elseif($sender === $this->tmiNewQWord)
                    $kind = RegistryValueKind::QWord;
                elseif($sender === $this->tmiNewString)
                    $kind = RegistryValueKind::i'String';
                $this->Addvalue($kind);//Do add
            }
            ///<summary>deletes currently selected registry key</summary>
            ///<returns type="bool">success</returns>
            private function DeleteKey(){
                $KeyName=$this->tvwRegistry->SelectedNode->Text;
                //Ask user
                if(MessageBox::Show(Program::$Resources->DeleteKey($KeyName),Program::$Resources->DeleteKey_t,MessageBoxButtons::YesNo,MessageBoxIcon::Question) == DialogResult::Yes):
                    //We must open parent key and ask it to delete its child
                    $parts=explode("\\",$this->tvwRegistry->SelectedNode->FullPath);
                    $ParentPath=i'String'::Join("\\",$parts,0,count($parts)-1);//Parent path
                    try{
                        $ParentKey=$this->getRegistryKey($ParentPath,true);//Open parent
                    }catch(System:::Exception $ex){
                        self::OnError($ex->Message);
                        return false;
                    }
                    try{
                        $ParentKey->DeleteSubKeyTree($parts[count($parts)-1]);//Delete child
                    }catch(System:::Exception $ex){
                        self::OnError($ex->Message);
                        return false;
                    }
                    $this->tvwRegistry->SelectedNode->Remove();//On success delete the node visualy as vell
                    return true;
                else:
                    return false;
                endif;
            }
            ///<summary>Deletes selected values</param>
            ///<returns type="bool">success</returns>
            private function DeleteValues(){
                //First chose message
                if($this->lvwRegistry->SelectedItems->Count == 1):
                    //For one-value deletion as with value name
                    $item1=Sorter::Iterate($this->lvwRegistry->SelectedItems->GetEnumerator(),0)->Text;
                    $message= Program::$Resources->DeleteValue($item1);
                else://Otherwise simly ask on values
                    $message=Program::$Resources->DeleteValues;
                endif;
                //Ask user
                if(MessageBox::Show($message,Program::$Resources->DeleteValues_t,MessageBoxButtons::YesNo,MessageBoxIcon::Question)==DialogResult::Yes):
                    try{//Open key
                        $key=$this->getRegistryKey($this->tvwRegistry->SelectedNode->FullPath,true);
                    }catch(System:::Exception $ex){
                        self::OnError($ex->Message);
                        return false;
                    }
                    //Index of st selected item (to preselect something after deletion, users like it)
                    $fsi=Sorter::Iterate($this->lvwRegistry->SelectedIndices->GetEnumerator(),0);
                    //Number of items to delete (to detect skip)
                    $todelcnt=$this->lvwRegistry->SelectedItems->Count;
                    foreach($this->lvwRegistry->SelectedItems as $item):
                        //Label is target for goto statement (Phalanger PHP extension)
DeleteValue:            try{
                            $key->DeleteValue($item->Name);//Try to delete
                        }catch(System:::Exception $ex){//On error ask user what to do
                            switch(self::OnError(Program::$Resources->e_DeleteValue($item->Name)."\r\n$ex->Message",MessageBoxButtons::AbortRetryIgnore)):
                                case DialogResult::Ignore: continue 2;//Ignore, just procede to next selected value
                                case DialogResult::Retry: goto DeleteValue;//Retry (this is where goto is usefully).
                                default: break 2;//Stop deleteing more values (exit foreach loop)
                            endswitch;
                        }
                        $deleted[]=$item;//Remeber deleted items (to delete them visually as well)
                    endforeach;
                    foreach($deleted as $deleteditem)//Delete items visually
                        $this->lvwRegistry->Items->Remove($deleteditem);
                    //If all selected items was deleted
                    // there is no selected item now
                    // if there is somethign to select    
                    if(count($deleted) == $todelcnt and $this->lvwRegistry->Items->Count > 0):
                        //We will select item near the deleted one
                        $newIndex = max(min($this->lvwRegistry->Items->Count-1,$fsi-1),0);
                        Sorter::Iterate($this->lvwRegistry->Items->GetEnumerator(),$newIndex)->Selected = true;    
                    endif;
                    return count($deleted) > 0;//It may indicate success or semi-success
                else:
                    return false;
                endif;                
            }
            ///<summary>Adds key under curent key</summary>
            ///<param name="parent">Node (not key) to add key into</param>
            ///<param name="newname">Name of key to add. If empty user is asked.</param>
            ///<param name="norefresh">True to not add the key to TreeView and return it instead</param>
            ///<returns>Bool success; created key on success when norefresh was true</returns>
            private function AddKey($parent=null, string $newname="",  bool $norefresh=false){
                //This is somewhat complicated because it ca operate in ineractive and non-interactive mode
                if(is_null($parent)):
                    //Get parent node if we haven't got it
                    if(is_null($this->tvwRegistry->SelectedNode)) return false;
                    $parent = $this->tvwRegistry->SelectedNode;
                endif; 
                $Name=$newname;
                if($newname<>"") goto testBS;//Do not ask for name
                //Ask for name
EnterName:      if(!is_null($Name=InputBox::GetInput(Program::$Resources->NameOfNewKey,Program::$Resources->CreateKey_t,$Name))):
testBS:             if($Name->Contains("\\"))://Name cannot contain \
                        self::OnError(Program::$Resources->e_InvalidName($Name));
                        if($newname=="")
                            goto EnterName;//Interactive mode (repeat enter)
                        else return false;
                    endif;
                    try{ //Open paren registry key for writing
                        $parentkey=$this->getRegistryKey($parent->FullPath,true);
                    }catch(System:::Exception $ex){
                        $this->OnError($ex->Message);
                        return false;
                    }
                    //Check if key to be created already exists
                    try{
                        $existing = $parentkey->OpenSubKey($Name,false);
                    }catch(System:::Exception $ex){
                        $this->OnError($ex->Message);
                        return false;
                    }
                    if(!is_null($existing)):
                        self::OnError(Program::$Resources->e_KeyExists($Name));
                        return false;
                    endif;
                    try{//Create it
                       $newKey = $parentkey->CreateSubKey($Name);
                    }catch(System:::Exception $ex){
                        $this->OnError($ex->Message);
                        return false;
                    }
                    if($norefresh) return $newKey;//Not interactive, return created key to caller
                    //Otherwise navigate to the new key
                    $wasdummy = 
                        ($parent->Nodes->Count == 1 and Sorter::Iterate($parent->Nodes->GetEnumerator(),0)->Name=self::dummy);
                    $parent->Expand();//Expand prent
                    if($wasdummy)://If parent was not loaded, it is now loaded and contain newly created key
                        foreach($parent->Nodes as $node)://search for it
                            if($node->Name == $Name):
                                $newNode=$node;//found
                                break;
                            endif;
                        endforeach;
                    else://Otherwise add it visually
                        $newNode = $parent->Nodes->Add($Name);
                        $newNode->Name =  $Name;
                        $newNode->ImageKey = "closed";
                        $newNode->SelectedImageKey = "closed";
                    endif;
                    if (!is_null($newNode)):
                        $this->tvwRegistry->SelectedNode = $newNode;//Select new key
                        $newNode->EnsureVisible();//and scrollit to view
                    endif;
                    return true;
                else:
                    return false;
                endif;
            }
            ///<summary>Adds value of given type</summary>
            ///<param name="type">Type of value to add</param>
            ///<returns type="bool">success</returns>
            private function Addvalue(int $type){
                //PHPBUG:
                //Note: We cannot declare $type as RegistryValueKind because it then will not assept values like RegistryValueKind::Binary (being it treated as int)
                $editor = new ValueEditor();//Create value editor
                $editor->SetValue(null,$type);//Initialize it
                $editor->NameReadOnly=false;//Allow enter of name
                if($editor->ShowDialog($this)==DialogResult::OK)://Show it
                    try{
                        //Open registry key to add value into
                        $key=$this->getRegistryKey($this->tvwRegistry->SelectedNode->FullPath,true);
                        try{//Check if value with same name exists
                            $key->GetValueKind($editor->ValueName);
                            self::OnError(Program::$Resources->e_ValueExists($editor->ValueName));
                            return false;
                        }catch(System:::IO:::IOException $ex){}//Non existent value is indicated by System:::IO:::IOExcpetion - and its what we want
                        $key->SetValue($editor->ValueName,$editor->Value,$type);//Create it (create and change value are same operations)
                    }catch(System:::Exception $ex){
                        self::OnError($ex->Message);
                        return false;
                    }
                    $this->LoadValues($key);//refresh list of values
                    foreach($this->lvwRegistry->Items as $item)://Search for newly added
                        if($item->Name==$editor->ValueName):
                            $item->Selected=true;//select it
                            $item->EnsureVisible();//and scroll it into view
                            break;
                        endif;
                    endforeach; 
                    return true;
                else:
                    return false;
                endif;
            }
            ///<summary>Attempts to rename registry key</summary>
            ///<param name="node">Tree node representing the key to reanem</param>
            ///<param name="newname>New name of key</param>
            ///<returns type="bool">success</returns>
            private function RenameKey(TreeNode $node, string $newname){
                //This is tricky operation because neither .NET registry API nor Win32 ASPI supports key rename
                // we must clone the key and then delete original
                //Create target for copy
                $newKey = $this->AddKey($node->Parent,$newname,true);    
                if(is_bool($newKey)) return false;
                $copyed=false;
                $this->Cursor = Cursors::$WaitCursor;//Set cursor to hourglass
                try{
                    $OldKey=$this->getRegistryKey($node->FullPath,false);//Open source key for reading
                    $copyed = self::CopyKey($OldKey,$newKey,false);//Copy old key to new key
                }catch(System:::Exception $ex){
                    self::OnError($ex->Message);
                }
                $this->Cursor = Cursors::$Default;//Reset cursor back to normal
                $OldKey->Close();//Closing old key is necessary to be done explicitly (PHP closes IDisposable for you somewhen), but open key cannot be deleted
                unset($OldKey);
                if($copyed):
                    try{
                        $pathparts = explode("\\",$node->FullPath);
                        //Open old key's parent
                        $parent = $this->getRegistryKey(System:::String::Join("\\",$pathparts,0,count($pathparts)-1),true);
                        $parent->DeleteSubKeyTree($node->Name);//delete old key
                    }catch(System:::Exception $ex){
                        //Delete failed - now we have two identical keys, super :-(
                        $TwoKeys=true;
                        if(isset($parent)):
                            //When we're puzzled, puzzle user as well - let him decide
                            if(self::OnError(Program::$Resources->e_DeleteOriginalKey.":\r\n$ex->Message\r\n".Program::$Resources->KeepBoth."\r\n".Program::$Resources->KeepBoth_note,MessageBoxButtons::YesNo)<>DialogResult::Yes):
                                try{//Delete old key
                                    $parent->DeleteSubKeyTree($newname);
                                    $TwoKeys=false;
                                }catch(System:::Exception $ex){
                                    //Faild. It'sn't probable, but it's possible
                                    self::OnError(Program::$Resources->e_DeleteDuplicated."\r\n$ex->Message");
                                }
                            endif;    
                        endif;
                        if($TwoKeys)://Add doplicate key to tree to user see it
                            $newNode = $this->tvwRegistry->Nodes->Add($newKey->Name);
                            $newenameparts=explode("\\",$newKey->Name);
                            $newNode->Name = $newenameparts[count($newenameparts)-1];
                            $newNode->ImageKey="closed";
                            $newNode->SelectedImageKey="closed";
                            $newNode->Nodes->Add(self::dummy)->Name=self::dummy;
                        endif;
                        return false;
                    }
                    $node->Name=$newname;//Change of name (change of label is automatic)
                    return true;
                else:
                    return false;
                endif;
            }
            ///<summary>Renames registry value</summary>
            ///<param name="item"><see cref="ListViewItem"/> representing item to rename</prama>
            ///<param name="newname">Proposed new name</praram>
            private function RenameValue(ListViewItem $item, string $newname){
                //Technically the same situation as key rename, but copying is quite easy
                try{//Open key
                    $key=$this->getRegistryKey($this->tvwRegistry->SelectedNode->FullPath,true);
                }catch(System:::Exception $ex){
                    self::OnError($ex->Message);
                    return false;
                }
                try{//Chekck existence of newname-named value
                    $key->GetValueKind($newname);
                    self::OnError(Program::$Resources->e_Rename($item->Text,$newname));
                    return false;
                }catch(System:::IO:::IOException $ex){}//If thrown it does not exist
                try{//Set new value
                    $key->SetValue($newname,
                        $key->GetValue($item->Name,null,RegistryValueOptions::DoNotExpandEnvironmentNames),
                        $key->GetValueKind($item->Name));
                }catch(System:::Exception $ex){
                    self::OnError($ex->Message);
                    return false;
                }
                try{//Delete old value
                    $key->DeleteValue($item->Name);
                }catch(System:::Exception $ex){
                    //Failed of delete (inprobable when we were allowed to create one). Let user see both
                    $this->LoadValues($key);
                    foreach($this->lvwRegistry->Items as $item2):
                        $item2->Selected = ($item2->Name == $newname or $item2->Name == $item->Name);
                    endforeach;
                    return true;
                } 
                //Do the rename
                $item->Name=$newname;
                $item->Text=$newname;#is that necessary?
                return true;
            }
            ///<summary>Performs value editing</summary>
            ///<returns type="bool">success</returns>
            private function EditValue(){
                if ($this->lvwRegistry->SelectedItems->Count<>1) return false;
                $name = Sorter::Iterate($this->lvwRegistry->SelectedItems->GetEnumerator(),0)->Name;
                try{
                    //Open key
                    $key=$this->getRegistryKey($this->tvwRegistry->SelectedNode->FullPath,true);
                    $type = $key->GetValueKind($name);//get kind
                    if($type==RegistryValueKind::Unknown)://Cannot edit unknown
                        self::OnError(Program::$Resources->e_editUnknown);
                        return false;
                    endif;
                    //Get value
                    $value = $key->GetValue($name,null,RegistryValueOptions::DoNotExpandEnvironmentNames);
                }catch(System:::Exception $ex){
                    self::OnError($ex->Message);
                    return false;
                }
                $editor = new ValueEditor();//Create editor
                $editor->SetValue($value,$type);//Initialize it
                $editor->ValueName=$name;//Set value name
                if($editor->ShowDialog($this)==DialogResult::OK)://Show it
                    try{
                        $key->SetValue($name,$editor->Value,$type);//Write changed value    
                    }catch(System:::Exception $ex){
                        self::OnError($ex->Message);
                        return false;
                    }
                    $this->LoadValues($key);//referesh list
                    foreach($this->lvwRegistry->Items as $item):
                        if($item->Name == $name)://Search for edited value
                            $item->Selected=true;//Select it
                            $item->EnsureVisible();//Scroll it into view
                            break;
                        endif; 
                    endforeach;
                    return true;
                else:
                    return false;
                endif;    
            }
            ///<summary>Show error message box</summary>
            ///<param name="message">Message</param>
            ///<param name="buttons">Buttons to show</param>
            ///<returns type="int">Message box result (clicked button)</returns>
            public static function OnError(string $message, $buttons = MessageBoxButtons::OK){
                //This is the most common Windows API
                return MessageBox::Show($message, Program::$Resources->Error_t, $buttons, MessageBoxIcon::Error);
            } 
            //Handles mosued down on tree
            private function tvwRegistry_MouseDown(System:::Object $sender, System:::Windows:::Forms:::MouseEventArgs $e) {
                if ($e->Button == MouseButtons::Right and !is_null($node = $sender->GetNodeAt($e->X,$e->Y))):
                    //This little hack selects node befor context menu is shown for right click
                    $sender->SelectedNode = $node;
                endif;
            }
            //Handles clcick on tzhe Referesh cionetx menu item
            private function tmiRefreshNode_Click(System:::Object $sender, System:::EventArgs $e) {
                if($this->tvwRegistry->SelectedNode->FullPath == HKEY_CLASSES_ROOT)
                    //Ask for HKCR because it may take long    
                    if(MessageBox::Show(Program::$Resources->RefreshHKCR,Program::$Resources->RefreshHKCR_t,MessageBoxButtons::YesNo,MessageBoxIcon::Question)<>DialogResult::Yes)
                        return;
                $this->Cursor = Cursors::$WaitCursor;
                self::FillNode($this->tvwRegistry->SelectedNode);//Reload nodes
                $this->tvwRegistry->SelectedNode->Expand();//expand it
                $this->Cursor = Cursors::$Default;
            }
            //Hasndles clcick on Select all context menu item
            private function tmiSelectAll_Click(System:::Object $sender, System:::EventArgs $e) {
                foreach($this->lvwRegistry->Items as $item)
                    $item->Selected = true;
            }
            //Handkles click on Copy full path context menu item
            private function tmiCopyFullPath_Click(System:::Object $sender, System:::EventArgs $e) {
                //we can copy full poath of key as welkl as of value
                if($this->cmsContext->SourceControl === $this->tvwRegistry):
                    $str = $this->tvwRegistry->SelectedNode->FullPath;
                else://Just append value name
                    $str = $this->tvwRegistry->SelectedNode->FullPath . "\\" . $this->lvwRegistry->FocusedItem->Name;
                endif;
                //And place it into clipboard
                Clipboard::SetText($str,TextDataFormat::UnicodeText);
            }

//Settings
            //Called when form loads
            private function frmMain_Load(System:::Object $sender, System:::EventArgs $e) {
                $this->LoadSettings();//Load settings
            }
            //Called after form closes
            private function frmMain_FormClosed(System:::Object $sender, System:::Windows:::Forms:::FormClosedEventArgs $e) {
                $this->SaveSettings();//Save settings
            }
            ///<summary>Loads form settings from settings class</summary>
            private function LoadSettings(){
                //Size
                if(!is_null($value=Program::$Settings->MainSize))
                    $this->ClientSize = $value;
                //State    
                if(!is_null($value=Program::$Settings->MainState))
                    $this->WindowState = $value;
                //Splitter (between tree view and listview) distance
                if(!is_null($value=Program::$Settings->SplitterDistance)) 
                    $this->splMain->SplitterDistance = $value;
                //Widths of columns    
                if(!is_null($value=Program::$Settings->ColumnWidths)):   
                    if(count($value) == $this->lvwRegistry->Columns->Count):
                        $i=0;
                        foreach($this->lvwRegistry->Columns as $col):
                            $col->Width = Program::$Settings->ColumnWidths[$i++];
                        endforeach;
                    endif;
                endif;
            }
            ///<summary>Saves settings to settings class</summary>
            private function SaveSettings(){
                Program::$Settings->MainSize = $this->ClientSize;//Size
                Program::$Settings->MainState = $this->WindowState;//State
                Program::$Settings->SplitterDistance = $this->splMain->SplitterDistance;//Splitter distance
                foreach($this->lvwRegistry->Columns as $col)://Column widths
                        $widths[] = $col->Width;
                endforeach;
                Program::$Settings->ColumnWidths = $widths;
                Program::$Settings->Save();//Write to disc
            }
//Copy
            ///<summary>Copies content of given registry key to another</summary>
            ///<param name="src">Source key</param>
            ///<param name="dest">Destination key</param>
            ///<param name="interactive">Allow user to recover form errors (if false only error message is displayed)</param>
            ///<returns>Success</param> 
            private static function CopyKey(RegistryKey $src, RegistryKey $dest,bool $interactive){
                //Copy values
                if (!self::CopyValues($src,$dest,$interactive)) return false;
                //and copy keys
                return self::CopySubKeys($src,$dest,$interactive);
            }
            ///<summary>Copies value ofrom one key to another</summary>
            ///<param name="src">Source key</param>
            ///<param name="dest">Destination key</param>
            ///<param name="interactive">Allow user to recover form errors (if false only error message is displayed)</param>
            ///<returns>Success</param> 
            private static function CopyValues(RegistryKey $src, RegistryKey $dest, bool $interactive){
                //Prepare message box buttons
                $mb=$interactive ? MessageBoxButtons::AbortRetryIgnore : MessageBoxButtons::OK;
InitCopy:       try{
                    foreach($src->GetValueNames() as $name):
CopyValue:              try{//Copy value
                            $dest->SetValue($name,
                                $src->GetValue($name,null,RegistryValueOptions::DoNotExpandEnvironmentNames),
                                $src->GetValueKind($name));
                        }catch(System:::Exception $ex){
                            switch(self::OnError(Program::$Resources->e_CopyValue($name,$src->Name,$dest->Name)."\r\n$ex->Message",$mb)):
                                case DialogResult::Retry: goto CopyValue;
                                case DialogResult::Ignore: break;
                                default:return false;
                            endswitch;
                        }
                    endforeach;
                }catch(System:::Exception $ex){
                    switch(self::OnError(Program::$Resources->e_EnlistValues($src->Name)."\r\n$ex->Message",$mb)):
                        case DialogResult::Retry: goto InitCopy;
                        case DialogResult::Ignore: return true;
                        default:return false;
                    endswitch;
                }
                return true;
            }
            ///<summary>Copies keys from one key to another</summary>
            ///<param name="src">Source key</param>
            ///<param name="dest">Destination key</param>
            ///<param name="interactive">Allow user to recover form errors (if false only error message is displayed)</param>
            ///<returns>Success</param> 

            private static function CopySubKeys(RegistryKey $src, RegistryKey $dest, bool $interactive){
                //Prepare message box buttons
                $mb=$interactive ? MessageBoxButtons::AbortRetryIgnore : MessageBoxButtons::OK;
InitCopy:       try{
                    foreach($src->GetSubKeyNames() as $skName)://For each sub key
OpenSrc:                try{
                            $SrcSubKey=$src->OpenSubKey($skName);//Open it
                        }catch(System:::Exception $ex){
                            switch(self::OnError(Program::$Resources->e_OpenSubKey($src->Name,$skName)."\r\n$ex->Message",$mb)):
                                case DialogResult::Retry: goto OpenSrc;
                                case DialogResult::Ignore: continue 2;
                                default:return false;
                            endswitch;
                        }
OpenDest:               try{
                            $DestSubKey=$dest->CreateSubKey($skName);//Create it in target
                        }catch(System:::Exception $ex){
                            switch(self::OnError(Program::$Resources->e_CreateSubKey($dest->Name,$skName)."\r\n$ex->Message",$mb)):
                                case DialogResult::Retry: goto OpenDest;
                                case DialogResult::Ignore:
                                    $SrcSubKey->Close();
                                    continue 2;
                                default:return false;
                            endswitch;
                        }
                        //Recurse
                        if(!self::CopyKey($SrcSubKey,$DestSubKey,$interactive)) return false;
                        //PHP does this automatically for IDisposable 
                        // but for massive operations it's better to do it manually
                        $SrcSubKey->Close();
                        $DestSubKey->Close();       
                    endforeach;
                }catch(System:::Exception $ex){
                    switch (self::OnError(Program::$Resources->e_EnlistSubKeys($src->Name)."\r\n$ex->Message",$mb)):
                        case DialogResult::Retry: goto InitCopy;
                        case DialogResult::Ignore: return true;
                        default:return false;
                    endswitch;
                }
                return true;
            }
//Export            
            //Handles clcik on Export context menu item
            private function tmiExport_Click(System:::Object $sender, System:::EventArgs $e) {
                //Show save file dialog
                if($this->sfdExport->ShowDialog() == DialogResult::OK):
                    unset($kw);
                    //Depending on format selected by user
                    switch($this->sfdExport->FilterIndex):
                       case 1:
                            $kw="export";
                       case 3:
                           $kw = isset($kw) ? $kw : "save";
                           $Keypath = $this->tvwRegistry->SelectedNode->FullPath;
                           $file = $this->sfdExport->FileName;
                           if(System:::IO:::File::Exists($file)):
                                //If file exist delete is as reg does not delete it automatically
                                try{System:::IO:::File::Delete($file);}
                                catch(System:::Exception $ex){
                                    self::OnError($ex->Message);
                                    return;
                                }    
                           endif;
                           //Call reg to do the work
                           // because .NET does not have function for it
                           // doing it manually is too much work
                           //PHPBUG:
                           // using Win32 API cannot be done from Phalanger yet
                           $result = `reg $kw "$Keypath" "$file"`;//This curious feature of PHP requires at least 7 lines of code in C#/VB :-)
                           MessageBox::Show($result,Program::$Resources->ExportKey_t,MessageBoxButtons::OK,MessageBoxIcon::Information);
                       break;
                       case 2:
                            //To XML do it manually (it's not so hard)
                            //Create XML
                            $xml=self::KeysToXml(array(self::getRegistryKey($this->tvwRegistry->SelectedNode->FullPath)),true);
                            if(is_null($xml)) return;
                            try{
                                $xml->Save($this->sfdExport->FileName);//Save XML
                            }catch(System:::Exception $ex){
                                self::OnError($ex-Message);
                                return;
                            }
                            MessageBox::Show(Program::$Resources->ExportSuccessful,Program::$Resources->ExportKey_t,MessageBoxButtons::OK,MessageBoxIcon::Information);
                       break;
                       default: self::OnError(Program::$Resources->e_UnknownregistryFileType);
                    endswitch;
                endif;
            }
            //private static function GetKeyHandle(RegistryKey $key){
                //$KeyType = CLRTypeOf RegistryKey;
                //$hKey = $KeyType->GetField("hkey",System:::Reflection:::BindingFlags::NonPublic |  System:::Reflection:::BindingFlags::Instance);
                //$hKey = $hKey->GetValue($key);
                //return $hKey;
            //}
            ///<summary>XML namespace name for registry XML</summary>
            const XMLNS = "http://dzonny.cz/xml/registry";
            ///<summary>Converts registry keys to XML</summary>
            ///<param name="Keys">Keys tio convert>/param>
            ///<param name="Interactive">Allow user to recover from errors</param>
            ///<returns type="System:::Xml:::XmlDocument">XML document with exported nodes</returns>
            private static function KeysToXml(array $Keys,bool $Interactive){
                $doc = new System:::Xml:::XmlDocument();
                $ns=self::XMLNS;
                //Creating XML document this way is easier than using DOM
                $doc->LoadXml(<<<xmlstring
<?xml version="1.0"?>
<registry xmlns="$ns"/>
xmlstring
                );
                try{//Convert keys to XML
                    foreach($Keys as $key){//Ooops I've used C-syntax, I've must be drunken :-)
                        $keyXml = self::KeyToXml($key,$doc,$Interactive);//Get XML
                        $keyXml->SetAttribute("name",$key->Name);//Change name to be fully qualified
                        $doc->DocumentElement->AppendChild($keyXml,$Interactive);//Add to document
                    }
                }catch(System:::Exception $ex){return null;}
                catch(Exception $ex){return null;}
                return $doc;
            }
            ///<summary>COnverts single registry key to XML</summary>
            ///<param name="key">Key to export</param>
            ///<param name="doc">Document to create element for</param>
            ///<param name="Interactive">Allow user to recover form errros</prama>
            ///<returns type="System:::Xml:::XmlElement">Element with exported key</returns>
            private static function KeyToXml(RegistryKey $key,System:::Xml:::XmlDocument $doc,bool $Interactive){
                //Prepare message box buttons
                $mb=$interactive ? MessageBoxButtons::AbortRetryIgnore : MessageBoxButtons::OK;    
                //Create element
                $el = $doc->CreateElement("key",self::XMLNS);
                $parts = explode("\\",$key->Name);
                $el->SetAttribute("name",$parts[count($parts)-1]);//Name it
GetNames:       try{//Get value names
                    $names=$key->GetValueNames();
                }catch(System:::Exception $ex){
                    switch(self::OnError(Program::$Resources->e_EnlistValues($key->Name)."\r\n$ex->Message",$mb)):
                        case DialogResult::Retry: goto GetNames;
                        case DialogResult::Ignore: goto GetKNames;
                        default: throw new CLRException($ex);
                    endswitch;
                }
                foreach($names as $name)://Serialize values
GetValue:           unset($eln);
                    try{
                        switch($key->GetValueKind($name))://Depending on type
                            case RegistryValueKind::Binary://Base 64
                                $vel=$doc->CreateElement("binary",self::XMLNS);
                                $vel->InnerText = System:::Convert::ToBase64String($key->GetValue($name));
                            break;
                            case RegistryValueKind::QWord://This is easy
                                $eln="qword";
                            case RegistryValueKind::DWord:
                                $eln = isset($eln)?$eln:"dword";
                                $vel=$doc->CreateElement($eln,self::XMLNS);
                                //Just better use invariant culture to prevent arabicx or even georgian numerals in your XML
                                $vel->InnerText = $key->GetValue($name)->ToString("D",System:::Globalization:::CultureInfo::$InvariantCulture);
                            break;
                            case RegistryValueKind::ExpandString:
                                $eln="expand";
                            case RegistryValueKind::i'String':
                                $eln = isset($eln)?$eln:"string";
                                $vel=$doc->CreateElement($eln,self::XMLNS);
                                //String mkust be saved non-expanded
                                $vel->InnerText = $key->GetValue($name,null,RegistryValueOptions::DoNotExpandEnvironmentNames);
                            break;
                            case RegistryValueKind::MultiString:
                                //Store in multible subelements
                                $vel=$doc->CreateElement("multi",self::XMLNS);
                                foreach($key->GetValue($name) as $string):
                                    $subel=$doc->CreateElement("string",self::XMLNS);
                                    $subel->InnerText = $string;
                                    $vel->AppendChild($subel);
                                endforeach;
                            break;
                            default://Cannot store unknown
                                switch(self::OnError(Program::$Resources->e_GetValue($key->Name,$name)."\r\n$ex->Message",$mb)):
                                    case DialogResult::Retry: goto GetValue;
                                    case DialogResult::Ignore: continue 3;
                                    default: throw new CLRException($ex);
                                endswitch;
                        endswitch;
                    }catch(System:::Exception $ex){
                        switch(self::OnError(Program::$Resources->e_GetValue($key->Name,$name)."\r\n$ex->Message",$mb)):
                            case DialogResult::Retry: goto GetValue;
                            case DialogResult::Ignore: continue 2;
                            default: throw new CLRException($ex);
                        endswitch;
                    }
                    $el->AppendChild($vel);
                    $vel->SetAttribute("name",$name);
                endforeach; 
GetKNames:      try{//Get sub key names
                    $names=$key->GetSubKeyNames();
                }catch(System:::Exception $ex){
                    switch(self::OnError(Program::$Resources->e_EnlistSubKeys($key->Name)."\r\n$ex->Message",$mb)):
                        case DialogResult::Retry: goto GetKNames;
                        case DialogResult::Ignore: return $el;
                        default: throw new CLRException($ex);
                    endswitch;
                }
                foreach($names as $name)://Save sub keys
GetKey:             unset($SubKey);
                    try{
                        $SubKey = $key->OpenSubKey($name);//Open
                        $kel = self::KeyToXml($SubKey,$doc,$Interactive);//Recurse
                        $el->AppendChild($kel);
                    }catch(System:::Exception $ex){
                        switch(self::OnError(Program::$Resources->e_OpenSubKey($key->Name,$name)."\r\n$ex->Message",$mb)):
                            case DialogResult::Retry: goto GetKey;
                            case DialogResult::Ignore: break 2;
                            default: throw new CLRException($ex);
                        endswitch;
                    }
                    if(!is_null($SubKey))$SubKey->Close();
                endforeach;
                return $el;
            }
	    }    
    }
?>