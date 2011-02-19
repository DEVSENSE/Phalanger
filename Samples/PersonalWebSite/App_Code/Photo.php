<?
	class Photo {

		[Export]
		public $PhotoID;
			
		[Export]
		public $AlbumID;
			
		[Export]
		public $Caption;

		public function __construct($id, $albumid, $caption) {
			$this->PhotoID = $id;
			$this->AlbumID = $albumid;
			$this->Caption = $caption;
		}

	}
?>
