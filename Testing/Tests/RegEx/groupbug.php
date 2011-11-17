[expect php]
[file]
<?php
	$yy_global_pattern = "/^(\\{)|^(\\})|^([ \n\r\t]+)|^(\"[^\"]*\"|'[^']*')|^(==)|^(!=)|^(<=)|^(>=)|^(\\|\\|)|^(&&)|^(OR)|^(AND)|^(\\$[a-zA-Z0-9_]+)|^([a-zA-Z0-9_]+)|^([;:,.[\]()|^&+-\/*=%!~$<>?@])|^([a-zA-Z]+)/";
	$data = <<<EOF
{
		//
		var users = new Array();
		var arrCount = 0;
		for (i = 0; i < tform.elements.length; i++)
		{
		//
			var element = tform.elements[i];
			if ((element.name != "allbox") && (element.type == "checkbox") && (element.checked == true))
			{
				users[arrCount] = element.value;
				arrCount++;
			}
		}
		if (arrCount == 0)
		{
			alert("{vb:rawphrase no_users_selected}");
		}
		else
		{
			//
			var querystring = "";
			for (i = 0; i < users.length; i++)
			{
				querystring += "&userid[]=" + users[i];
			}
			if (opener && !opener.closed)
			{ // parent window is still open
				opener.location="private.php?{vb:raw session.sessionurl}do=newpm" + querystring;
			}
			else
			{ // parent window has closed or went to a different URL.
				window.open(getBaseUrl() + "private.php?{vb:raw session.sessionurl}do=newpm" + querystring, "pm");
			}
		}
	}
	// -->
	</script>
	{vb:raw headinclude_bottom}
</head>

<body>
	<form action="private.php" method="post" target="_blank" name="vbform" id="contacts" class="block">
		<h2 class="blockhead">{vb:rawphrase contacts}</h2>
		<div class="blockbody">
			<h3 class="blocksubhead"><strong>{vb:rawphrase online}</strong></h3>
			<ul class="posterlist">
				{vb:raw onlineusers}
			</ul>
		</div>
			
		<div class="blockbody">
			<h3 class="blocksubhead"><strong>{vb:rawphrase offline}</strong></h3>
			<ul class="posterlist">
				{vb:raw offlineusers}
			</ul>
		</div>
		
		<div class="blockfoot actionbuttons">
			<div class="group">
				<input type="button" class="button" value="{vb:rawphrase reload}" onclick="window.location = 'misc.php?{vb:raw session.sessionurl}do=buddylist&amp;buddies={vb:raw buddies}';" />
				<input type="button" class="button" value="{vb:rawphrase pm_users}" onclick="pm(this.form);" title="{vb:rawphrase send_private_message_to_selected_users}" />
			</div>
		</div>
	</form>
	
	<vb:if condition="\$show['playsound']">
	<embed src="YourAlertSound.wav" hidden="True" />
	</vb:if>

</body>
</html> 
EOF;

	$yymatches = array();
	preg_match($yy_global_pattern, substr($data, 187), $yymatches);

	var_dump( array_filter($yymatches, 'strlen') );

?>