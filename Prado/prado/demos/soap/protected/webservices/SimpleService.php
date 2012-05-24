<?php

class SimpleService
{
	/**
	 * Highlights a string as php code
	 * @param string $input The php code to highlight
	 * @return string The highlighted text.
	 * @soapmethod
	 */
	public function highlight($input)
	{
		return highlight_string($input, true);
	}

	/**
	 * Simply add two operands
	 * @param int $a
	 * @param int $b
	 * @return int The result
	 * @soapmethod
	 */
	public function add($a, $b)
	{
		return $a + $b;
	}
}

?>