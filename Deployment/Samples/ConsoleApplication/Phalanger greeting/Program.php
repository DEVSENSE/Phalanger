<?php

	// Reading user input from console

	echo "                                                                         \n";
	echo "                                                                         \n";
	echo "  $$$$$$$    $$$     $$$  $$$$$$$       $$$     $$$  $$$$$$$$$  $$$$$$$$$\n";
	echo "  $$$$$$$$$  $$$     $$$  $$$$$$$$$     $$$$    $$$  $$$$$$$$$  $$$$$$$$$\n";
	echo "  $$     $$  $$$     $$$  $$     $$     $$$$$   $$$  $$$           $$$   \n";
	echo "  $$     $$  $$$     $$$  $$     $$     $$$ $$  $$$  $$$           $$$   \n";
	echo "  $$$$$$$$$  $$$$$$$$$$$  $$$$$$$$$     $$$  $$ $$$  $$$$$$        $$$   \n";
	echo "  $$$$$$$$   $$$$$$$$$$$  $$$$$$$$      $$$   $$$$$  $$$$$$        $$$   \n";
	echo "  $$$        $$$     $$$  $$$           $$$    $$$$  $$$           $$$   \n";
	echo "  $$$        $$$     $$$  $$$           $$$     $$$  $$$           $$$   \n";
	echo "  $$$        $$$     $$$  $$$      $$$  $$$     $$$  $$$$$$$$$     $$$   \n";
	echo "  $$$        $$$     $$$  $$$      $$$  $$$     $$$  $$$$$$$$$     $$$   \n";
	echo "                                                                         \n";
	echo "                                                                         \n";
	
	echo "                    Do you like Phalanger? (yes, no)  ";
	$answer = fgets(STDIN);

	while (trim($answer) != "yes")
	{
		echo "\n";
		echo "         I'm asking you once again: DO YOU LIKE Phalanger????? (yes)  ";
		$answer = fgets(STDIN);
	}
	
	echo "\n";
	echo "              OK. I know that you are an intelligent guy :)\n";
	echo "\n";
	echo "                                Enjoy it!\n";
	echo "\n";

	fgets(STDIN);	
?>