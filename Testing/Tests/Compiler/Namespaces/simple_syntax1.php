[expect php]
[file]
<?php

	namespace N;

    echo __NAMESPACE__;
	
	namespace M;

    echo __NAMESPACE__;
	
	namespace N\M;

    echo __NAMESPACE__;
	
?>