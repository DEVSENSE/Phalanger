[expect php]
[file]
<?php

	namespace
	{
		const A = 2;
		const a = 3;
		const B = 4;

		class X
		{
			const A = 5;
			const a = 6;
		}
	}

	namespace A
	{
		const A = 7;
	}

	namespace
	{
		echo A, a, \X::A, X::a, A\A;
	}
	
?>