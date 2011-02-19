<%@ WebHandler Language="PHP" Class="Handler" %><?
    import namespace System;
    import namespace System:::IO;
    import namespace System:::Web;

	// The class won't expose the needed parameterless constructor if it's not exported.
	[Export]
    class Handler implements IHttpHandler {

	    public $IsReusable = true;
    	
	    function ProcessRequest ($context) {
		    // Set up the response settings
		    $context->Response->ContentType = "image/jpeg";
		    $context->Response->Cache->SetCacheability(HttpCacheability::i'Public');
		    $context->Response->BufferOutput = false;
		    
		    $query = $context->Request->QueryString;
		    $photo_id = $context->Request->QueryString->get_Item("PhotoID");
		    $album_id = $context->Request->QueryString->get_Item("AlbumID");
		    $qs_size = $context->Request->QueryString->get_Item("Size");
		    
		    // Setup the Size Parameter
		    switch ($qs_size) {
			    case "S":
				    $size = PhotoSize::Small;
				    break;
			    case "M":
				    $size = PhotoSize::Medium;
				    break;
			    case "L":
				    $size = PhotoSize::Large;
				    break;
			    default:
				    $size = PhotoSize::Original;
				    break;
		    } 
		    // Setup the PhotoID Parameter
		    $id = -1;
		    if (isset($photo_id)) {
			    $id = Convert::ToInt32($photo_id);
			    $stream = PhotoManager::GetPhoto($id, $size);
		    } else {
			    $id = Convert::ToInt32($album_id);
			    $stream = PhotoManager::GetFirstPhoto($id, $size);
		    }
		    // Get the photo from the database, if nothing is returned, get the default "placeholder" photo
		    if ($stream == NULL) $stream = PhotoManager::GetPhotoPlaceholder($size);
		    // Write image stream to the response stream
		    $buffersize = 1024 * 16;
		    $buffer = System:::Array::CreateInstance(Type::GetType("System.Byte"), $buffersize);
		    $count = $stream->Read($buffer, 0, $buffersize);
		    while ($count > 0) {
			    $context->Response->OutputStream->Write($buffer, 0, $count);
			    $count = $stream->Read($buffer, 0, $buffersize);
		    }
	    }

    }
?>
