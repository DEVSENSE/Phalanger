<?
	import namespace System;
	import namespace System:::Configuration;
	import namespace System:::Web;
	import namespace System:::Web:::Security;
	import namespace System:::Web:::UI;
	import namespace System:::Web:::UI:::WebControls;
	import namespace System:::Web:::UI:::WebControls:::WebParts;
	import namespace System:::Web:::UI:::HtmlControls;
	import namespace System:::Data;
	import namespace System:::Data:::OleDb;
	import namespace System:::IO;

	partial class Admin_Photos_aspx extends System:::Web:::UI:::Page {

		protected function FormView1_ItemInserting($sender, $e) {
			if ($e->Values->get_Item("BytesOriginal")->Length == 0) $e->Cancel = true;
		}

		protected function Button1_Click($sender, $e) {
			$d = new System:::IO:::DirectoryInfo($this->Server->MapPath("~/Upload"));
			$files = $d->GetFiles("*.jpg");
			
			for ($i = 0; $i < $files->Length; $i++) {
				$f = $files->GetValue($i);
			
				$len = Convert::ToInt32($f->OpenRead()->Length);
			
				$buffer = System:::Array::CreateInstance(Type::GetType("System.Byte"), $len);
				$f->OpenRead()->Read($buffer, 0, $len);
				PhotoManager::AddPhoto(Convert::ToInt32($this->Request->QueryString->get_Item("AlbumID")), $f->Name, $buffer);
			}
			$this->GridView1->DataBind();
		}

	}
?>
