[expect php]
[file]
<?php 

// email validation and sanitization
$email_addresses = array("info@devsense.com", "a@b!@#$%^&*[].com", "a@bררר.com", "a@456.com", "123@b.com", null, "");

foreach ($email_addresses as $email)
{
	echo "FILTER_SANITIZE_EMAIL:" . var_dump(filter_var($email, FILTER_SANITIZE_EMAIL)) . "\n";
	echo "FILTER_VALIDATE_EMAIL:" . var_dump(filter_var($email, FILTER_VALIDATE_EMAIL)) . "\n";
}

?>