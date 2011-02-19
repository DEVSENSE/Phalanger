[expect php]
[file]
<?

	var_dump(preg_split( "/(a)|(b)/i", "''a''", -1, PREG_SPLIT_DELIM_CAPTURE ));
	var_dump(preg_split( "/(a)|(b)/i", "''b''", -1, PREG_SPLIT_DELIM_CAPTURE ));
	var_dump(preg_split( "/(a)|(b)/i", "''x''", -1, PREG_SPLIT_DELIM_CAPTURE ));
	
	var_dump(preg_split( "/(a)|(b)/i", "''a''a", -1, PREG_SPLIT_DELIM_CAPTURE ));
	var_dump(preg_split( "/(a)|(b)/i", "''b''a", -1, PREG_SPLIT_DELIM_CAPTURE ));
	var_dump(preg_split( "/(a)|(b)/i", "''x''a", -1, PREG_SPLIT_DELIM_CAPTURE ));
	
	var_dump(preg_split( "/(a)|(b)/i", "''a''b", -1, PREG_SPLIT_DELIM_CAPTURE ));
	var_dump(preg_split( "/(a)|(b)/i", "''b''b", -1, PREG_SPLIT_DELIM_CAPTURE ));
	var_dump(preg_split( "/(a)|(b)/i", "''x''b", -1, PREG_SPLIT_DELIM_CAPTURE ));
	
	var_dump(preg_split( "/(a)|(b)/i", "''x''bababbbabbbbaaaaaa''''xxxaaaabbbabab  abba;;;a ba a aa   ", -1, PREG_SPLIT_DELIM_CAPTURE ));
	var_dump(preg_split( "/(a)|(b)/i", "", -1, PREG_SPLIT_DELIM_CAPTURE ));
	
	var_dump(preg_split( "/(a)|a(b)c|(c)|(d)|(e)|(f)/i", "abc", -1, PREG_SPLIT_DELIM_CAPTURE ));
	var_dump(preg_split( "/(a)|(b)|(c)|(d)|(e)|(f)/i", "abc", -1, PREG_SPLIT_DELIM_CAPTURE ));
	var_dump(preg_split( "/(a)|(b)|(c)|(d)|(e)|(f)/i", "abce", -1, PREG_SPLIT_DELIM_CAPTURE ));
	var_dump(preg_split( "/(a)|(b)|(c)|(d)|(e)|(f)/i", "abcef", -1, PREG_SPLIT_DELIM_CAPTURE ));
	var_dump(preg_split( "/(a)|(b)|(c)|(d)|(e)|(f)/i", "ace", -1, PREG_SPLIT_DELIM_CAPTURE ));
	
	var_dump(preg_split( "/(a)|(b)|(c)|(d)|(e)|(f)/i", " a b cdabd e bbb a d", -1, PREG_SPLIT_DELIM_CAPTURE ));
	
	var_dump(preg_split( "/<(nowiki)|<(!--)/i", "'''<nowiki>something</nowiki>'''", 2, PREG_SPLIT_DELIM_CAPTURE ));
	print_r(preg_split("/<(nowiki)(\\s+[^>]*?|\\s*?)(\/?>)|<(!--)/i", "'''<!-- something -->'''", 2, PREG_SPLIT_DELIM_CAPTURE ));
?>
