[expect php]
[file]
<?php
  include('Phalanger.inc');
  
	// simple checks
	__var_dump(basename("bar"));
	__var_dump(basename("/foo/bar"));
	__var_dump(basename("/bar"));

	// simple checks with trailing slashes
	__var_dump(basename("bar/"));
	__var_dump(basename("/foo/bar/"));
	__var_dump(basename("/bar/"));

	// suffix removal checks
	__var_dump(basename("bar.gz", ".gz"));
	__var_dump(basename("/foo/bar.gz", ".gz"));
	__var_dump(basename("/bar.gz", ".gz"));

	// suffix removal checks with trailing slashes
	__var_dump(basename("bar.gz/", ".gz"));
	__var_dump(basename("/foo/bar.gz/", ".gz"));
	__var_dump(basename("/bar.gz/", ".gz"));

	// suffix removal checks
	__var_dump(basename("/.gz", ".gz"));
	__var_dump(basename("/foo/.gz", ".gz"));
	__var_dump(basename("/.gz", ".gz"));

	// binary safe?
	__var_dump(basename("foo".chr(0)."bar"));
	__var_dump(basename("foo".chr(0)."bar.gz", ".gz"));
?>