<?
	import namespace System;
	import namespace System:::Collections;
	import namespace System:::Collections:::Generic;
	import namespace System:::Configuration;
	import namespace System:::Data;
	import namespace System:::Data:::SqlClient;
	import namespace System:::Drawing;
	import namespace System:::Drawing:::Drawing2D;
	import namespace System:::Drawing:::Imaging;
	import namespace System:::IO;
	import namespace System:::Web;

	[Export]
	class PhotoManager {

		// Photo-Related Methods

		public static function GetPhoto($photoid, $size) {
			$connection = new SqlConnection(ConfigurationManager::$ConnectionStrings->get_Item("Personal")->ConnectionString); {
				$command = new SqlCommand("GetPhoto", $connection); {
					$command->CommandType = CommandType::StoredProcedure;
					$command->Parameters->Add("@PhotoID", SqlDbType::i'Int')->Value = $photoid;
					$command->Parameters->Add("@Size", SqlDbType::i'Int')->Value = $size;
					$filter = !(HttpContext::$Current->User->IsInRole("Friends") || HttpContext::$Current->User->IsInRole("Administrators"));
					$command->Parameters->Add("@IsPublic", SqlDbType::Bit)->Value = $filter;
					$connection->Open();
					$result = $command->ExecuteScalar();
				}
				$command->Dispose();
			}
			$connection->Dispose();
			
			if ($result == NULL) return NULL;
			try {
				return new MemoryStream($result);
			} catch(System:::Exception $e) {
				return NULL;
			}
		}

		public static function GetPhotoPlaceholder($size) {
			$path = HttpContext::$Current->Server->MapPath("~/Images/");
			switch ($size) {
				case PhotoSize::Small:
					$path .= "placeholder-100.jpg";
					break;
				case PhotoSize::Medium:
					$path .= "placeholder-200.jpg";
					break;
				case PhotoSize::Large:
					$path .= "placeholder-600.jpg";
					break;
				default:
					$path .= "placeholder-600.jpg";
					break;
			}
			
			return new FileStream($path, FileMode::Open, FileAccess::Read, FileShare::Read);
		}

		public static function GetFirstPhoto($albumid, $size) {
			$connection = new SqlConnection(ConfigurationManager::$ConnectionStrings->get_Item("Personal")->ConnectionString); {
				$command = new SqlCommand("GetFirstPhoto", $connection); {
					$command->CommandType = CommandType::StoredProcedure;
					$command->Parameters->Add("@AlbumID", SqlDbType::i'Int')->Value = $albumid;
					$command->Parameters->Add("@Size", SqlDbType::i'Int')->Value = $size;
					$filter = !(HttpContext::$Current->User->IsInRole("Friends") || HttpContext::$Current->User->IsInRole("Administrators"));
					$command->Parameters->Add("@IsPublic", SqlDbType::Bit)->Value = $filter;
					$connection->Open();
					$result = $command->ExecuteScalar();
				}
				$command->Dispose();
			}
			$connection->Dispose();
			
			if ($result == NULL) return NULL;
			try {
				return new MemoryStream($result);
			} catch(System:::Exception $e) {
				return NULL;
			}
		}

		public static function GetPhotos($AlbumID = NULL) {
			if ($AlbumID == NULL)
				$AlbumID = self::GetRandomAlbumID();
		
			$connection = new SqlConnection(ConfigurationManager::$ConnectionStrings->get_Item("Personal")->ConnectionString); {
				$command = new SqlCommand("GetPhotos", $connection); {
					$command->CommandType = CommandType::StoredProcedure;
					$command->Parameters->Add("@AlbumID", SqlDbType::i'Int')->Value = $AlbumID;
					$filter = !(HttpContext::$Current->User->IsInRole("Friends") || HttpContext::$Current->User->IsInRole("Administrators"));
					$command->Parameters->Add("@IsPublic", SqlDbType::Bit)->Value = $filter;
					
					$connection->Open();
					$list = new i'List'<:Photo:>;
					$reader = $command->ExecuteReader(); {
						while ($reader->Read()) {
							$temp = new Photo(
								$reader->get_Item("PhotoID"),
								$reader->get_Item("AlbumID"),
								$reader->get_Item("Caption"));
							$list->Add($temp);
						}
					}
					$reader->Dispose();
				}
				$command->Dispose();
			}
			$connection->Dispose();
			return $list;
		}

		public static function AddPhoto($AlbumID, $Caption, $BytesOriginal) {
			$connection = new SqlConnection(ConfigurationManager::$ConnectionStrings->get_Item("Personal")->ConnectionString); {
				$command = new SqlCommand("AddPhoto", $connection); {
					$command->CommandType = CommandType::StoredProcedure;
					$command->Parameters->Add("@AlbumID", SqlDbType::i'Int')->Value = $AlbumID;
					$command->Parameters->Add("@Caption", SqlDbType::NVarChar)->Value = $Caption;
					$command->Parameters->Add("@BytesOriginal", SqlDbType::Image)->Value = $BytesOriginal;
					$command->Parameters->Add("@BytesFull", SqlDbType::Image)->Value = self::ResizeImageFile($BytesOriginal, 600);
					$command->Parameters->Add("@BytesPoster", SqlDbType::Image)->Value = self::ResizeImageFile($BytesOriginal, 198);
					$command->Parameters->Add("@BytesThumb", SqlDbType::Image)->Value = self::ResizeImageFile($BytesOriginal, 100);
					$connection->Open();
					$command->ExecuteNonQuery();
				}
				$command->Dispose();
			}
			$connection->Dispose();
		}

		public static function RemovePhoto($PhotoID) {
			$connection = new SqlConnection(ConfigurationManager::$ConnectionStrings->get_Item("Personal")->ConnectionString); {
				$command = new SqlCommand("RemovePhoto", $connection); {
					$command->CommandType = CommandType::StoredProcedure;
					$command->Parameters->Add("@PhotoID", SqlDbType::i'Int')->Value = $PhotoID;
					$connection->Open();
					$command->ExecuteNonQuery();
				}
				$command->Dispose();
			}
			$connection->Dispose();
		}

		public static function EditPhoto($Caption, $PhotoID) {
			$connection = new SqlConnection(ConfigurationManager::$ConnectionStrings->get_Item("Personal")->ConnectionString); {
				$command = new SqlCommand("EditPhoto", $connection); {
					$command->CommandType = CommandType::StoredProcedure;
					$command->Parameters->Add("@Caption", SqlDbType::NVarChar)->Value = $Caption;
					$command->Parameters->Add("@PhotoID", SqlDbType::i'Int')->Value = $PhotoID;
					$connection->Open();
					$command->ExecuteNonQuery();
				}
				$command->Dispose();
			}
			$connection->Dispose();
		}

		// Album-Related Methods

		public static function GetAlbums() {
			$connection = new SqlConnection(ConfigurationManager::$ConnectionStrings->get_Item("Personal")->ConnectionString); {
				$command = new SqlCommand("GetAlbums", $connection); {
					$command->CommandType = CommandType::StoredProcedure;
					$filter = !(HttpContext::$Current->User->IsInRole("Friends") || HttpContext::$Current->User->IsInRole("Administrators"));
					$command->Parameters->Add("@IsPublic", SqlDbType::Bit)->Value = $filter;
					$connection->Open();
					$list = new i'List'<:Album:>;
					$reader = $command->ExecuteReader(); {
						while ($reader->Read()) {
							$temp = new Album(
								$reader->get_Item("AlbumID"),
								$reader->get_Item("NumberOfPhotos"),
								$reader->get_Item("Caption"),
								$reader->get_Item("IsPublic"));
							$list->Add($temp);
						}
					}
					$reader->Dispose();
				}
				$command->Dispose();
			}
			$connection->Dispose();
			return $list;
		}

		public static function AddAlbum($Caption, $IsPublic) {
			$connection = new SqlConnection(ConfigurationManager::$ConnectionStrings->get_Item("Personal")->ConnectionString); {
				$command = new SqlCommand("AddAlbum", $connection); {
					$command->CommandType = CommandType::StoredProcedure;
					$command->Parameters->Add("@Caption", SqlDbType::NVarChar)->Value = $Caption;
					$command->Parameters->Add("@IsPublic", SqlDbType::Bit)->Value = $IsPublic;
					$connection->Open();
					$command->ExecuteNonQuery();
				}
				$command->Dispose();
			}
			$connection->Dispose();
		}

		public static function RemoveAlbum($AlbumID) {
			$connection = new SqlConnection(ConfigurationManager::$ConnectionStrings->get_Item("Personal")->ConnectionString); {
				$command = new SqlCommand("RemoveAlbum", $connection); {
					$command->CommandType = CommandType::StoredProcedure;
					$command->Parameters->Add("@AlbumID", SqlDbType::i'Int')->Value = $AlbumID;
					$connection->Open();
					$command->ExecuteNonQuery();
				}
				$command->Dispose();
			}
			$connection->Dispose();
		}

		public static function EditAlbum($Caption, $IsPublic, $AlbumID) {
			$connection = new SqlConnection(ConfigurationManager::$ConnectionStrings->get_Item("Personal")->ConnectionString); {
				$command = new SqlCommand("EditAlbum", $connection); {
					$command->CommandType = CommandType::StoredProcedure;
					$command->Parameters->Add("@Caption", SqlDbType::NVarChar)->Value = $Caption;
					$command->Parameters->Add("@IsPublic", SqlDbType::Bit)->Value = $IsPublic;
					$command->Parameters->Add("@AlbumID", SqlDbType::i'Int')->Value = $AlbumID;
					$connection->Open();
					$command->ExecuteNonQuery();
				}
				$command->Dispose();
			}
			$connection->Dispose();
		}

		public static function GetRandomAlbumID() {
			$connection = new SqlConnection(ConfigurationManager::$ConnectionStrings->get_Item("Personal")->ConnectionString); {
				$command = new SqlCommand("GetNonEmptyAlbums", $connection); {
					$command->CommandType = CommandType::StoredProcedure;
					$connection->Open();
					$list = new i'List'<:Album:>;
					$reader = $command->ExecuteReader(); {
						while ($reader->Read()) {
							$temp = new Album(Convert::ToInt32($reader->get_Item("AlbumID")), 0, "", false);
							$list->Add($temp);
						}
					}
					$reader->Dispose();
				}
				$command->Dispose();
			}
			$connection->Dispose();
			try {
			    $r = new Random;
				return $list->get_Item($r->Next($list->Count))->AlbumID;
			} catch(System:::Exception $e) {
				return -1;
			}
		}

		// Helper Functions

		private static function ResizeImageFile($imageFile, $targetSize) {
			$oldImage = Image::FromStream(new MemoryStream($imageFile)); {
				$newSize = self::CalculateDimensions($oldImage->Size, $targetSize);
				$newImage = new Bitmap($newSize->Width, $newSize->Height, PixelFormat::Format24bppRgb); {
					$canvas = Graphics::FromImage($newImage); {
						$canvas->SmoothingMode = SmoothingMode::AntiAlias;
						$canvas->InterpolationMode = InterpolationMode::HighQualityBicubic;
						$canvas->PixelOffsetMode = PixelOffsetMode::HighQuality;
						$canvas->DrawImage($oldImage, 0, 0, $newSize->Width, $newSize->Height);
						$m = new MemoryStream();
						$newImage->Save($m, ImageFormat::$Jpeg);
					}
					$canvas->Dispose();
				}
				$newImage->Dispose();
			}
			$oldImage->Dispose();
			return $m->GetBuffer();
		}

		private static function CalculateDimensions($oldSize, $targetSize) {
			$newSize = new Size(0, 0);
			if ($oldSize->Height > $oldSize->Width) {
				$newSize->Width = (int)($oldSize->Width * ($targetSize / $oldSize->Height));
				$newSize->Height = $targetSize;
			} else {
				$newSize->Width = $targetSize;
				$newSize->Height = (int)($oldSize->Height * ($targetSize / $oldSize->Width));
			}
			return $newSize;
		}

		public static function ListUploadDirectory() {
			$d = new DirectoryInfo(System:::Web:::HttpContext::$Current->Server->MapPath("~/Upload"));
			return $d->GetFileSystemInfos("*.jpg");
		}

	}
?>
