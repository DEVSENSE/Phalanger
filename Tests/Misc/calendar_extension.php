[expect php]
[file]
<?
echo frenchtojd(-1,-1,-1), "\n";
echo frenchtojd(0,0,0), "\n";
echo frenchtojd(1,1,1), "\n";
echo frenchtojd(14,31,15), "\n";
echo easter_days(1999), "\n";
echo easter_days(1492), "\n";
echo easter_days(1913), "\n";
$num = cal_days_in_month(CAL_GREGORIAN, 8, 2003); 
echo "There are $num days in August 2003\n";
$num = cal_days_in_month(CAL_GREGORIAN, 2, 2003); 
echo "There are $num days in February 2003\n";
$num = cal_days_in_month(CAL_GREGORIAN, 2, 2004); 
echo "There are $num days in February 2004\n";
$num = cal_days_in_month(CAL_GREGORIAN, 12, 2034); 
echo "There are $num days in December 2034\n";
echo cal_to_jd(CAL_GREGORIAN, 8, 26, 74), "\n";
echo cal_to_jd(CAL_JULIAN, 8, 26, 74), "\n";
echo cal_to_jd(CAL_JEWISH, 8, 26, 74), "\n";
echo cal_to_jd(CAL_FRENCH, 8, 26, 74), "\n";
echo bin2hex(jdtojewish(gregoriantojd(10,28,2002))),"\n";
echo bin2hex(jdtojewish(gregoriantojd(10,28,2002),true))."\n";
echo bin2hex(jdtojewish(gregoriantojd(10,28,2002),true, CAL_JEWISH_ADD_ALAFIM_GERESH))."\n";
echo bin2hex(jdtojewish(gregoriantojd(10,28,2002),true, CAL_JEWISH_ADD_ALAFIM))."\n";
echo bin2hex(jdtojewish(gregoriantojd(10,28,2002),true, CAL_JEWISH_ADD_ALAFIM_GERESH+CAL_JEWISH_ADD_ALAFIM))."\n";
echo bin2hex(jdtojewish(gregoriantojd(10,28,2002),true, CAL_JEWISH_ADD_GERESHAYIM))."\n";
echo bin2hex(jdtojewish(gregoriantojd(10,8,2002),true, CAL_JEWISH_ADD_GERESHAYIM))."\n";
echo bin2hex(jdtojewish(gregoriantojd(10,8,2002),true, CAL_JEWISH_ADD_GERESHAYIM+CAL_JEWISH_ADD_ALAFIM_GERESH))."\n";
echo bin2hex(jdtojewish(gregoriantojd(10,8,2002),true, CAL_JEWISH_ADD_GERESHAYIM+CAL_JEWISH_ADD_ALAFIM))."\n";
echo bin2hex(jdtojewish(gregoriantojd(10,8,2002),true, CAL_JEWISH_ADD_GERESHAYIM+CAL_JEWISH_ADD_ALAFIM+CAL_JEWISH_ADD_ALAFIM_GERESH))."\n";
?>