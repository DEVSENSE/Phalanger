<?
	class Album {

		[Export]
		public $AlbumID;
		
		[Export]
		public $Count;

		[Export]
		public $Caption;

		[Export]
		public $IsPublic;

		public function __construct($id, $count, $caption, $ispublic) {
			$this->AlbumID = $id;
			$this->Count = $count;
			$this->Caption = $caption;
			$this->IsPublic = $ispublic;
		}

	}
?>
