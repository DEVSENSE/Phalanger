[expect php]
[file]
<?php

class xmltest
{

	public function tagStart($parser, $name, array $attribs) {
		echo $name." begins\n<br/>";
	}

	public function tagEnd($parser, $name) {
		echo $name." ends\n<br/>";
	}
	
	function test()
	{

		echo "Test begins\n<br/>\n<br/>";
		$parser = xml_parser_create();
		xml_parser_set_option($parser, XML_OPTION_CASE_FOLDING, false);
		xml_set_element_handler($parser, array($this,'tagStart'), array($this,'tagEnd'));

		$data = '<test>
			<empty att="3" />
			<nocontent></nocontent>
			<content>
				<empty/>
				<empty/>
			</content>
			<empty/>
			<empty att="5" />
		</test>
		';

		xml_parse($parser, $data);
		
		echo "Test ends\n<br/>\n<br/>";

	}
}

$test = new xmltest();
$test->test();

?>

