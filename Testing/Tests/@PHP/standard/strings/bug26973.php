[expect php]
[file]
<?php

printf("%+05d\n", 200);
printf("%+05d\n", -200);
printf("%+05f\n", 200);
printf("%+05f\n", -200);
printf("%+05u\n", 200);
printf("%+05u\n", -200);
echo "---\n";
printf("%05d\n", 200);
printf("%05d\n", -200);
printf("%05f\n", 200);
printf("%05f\n", -200);
printf("%05u\n", 200);
printf("%05u\n", -200);

// added by Phalanger:
printf("%+d\n", 0);
printf("%-d\n", 0);
printf("%+d\n", 200);
printf("%+d\n", -200);
printf("%+f\n", 200);
printf("%+f\n", -200);
printf("%+u\n", 200);
printf("%+u\n", -200);
printf("%-d\n", 200);
printf("%-d\n", -200);
printf("%-f\n", 200);
printf("%-f\n", -200);
printf("%-u\n", 200);
printf("%-u\n", -200);
echo "---\n";
printf("%-b\n", -200);
printf("%+b\n", -200);
printf("%-o\n", -200);
printf("%+o\n", -200);
echo "---\n";
printf("%+5d\n", 200);
printf("%+5d\n", -200);
printf("%+05d\n", 200);
printf("%+05d\n", -200);
printf("%+'r10d\n", -200);
printf("%+'\n5d\n", -200);

?>