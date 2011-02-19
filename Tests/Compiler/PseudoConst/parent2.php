[EXPECT]

[FILE]
<?php
  class testbase
{
function DoSomething($option)
{
}
}
class testclassa extends testbase
{
function DoSomething($option)
{
testbase::DoSomething($option); 
parent::DoSomething($option); 
switch($option)
{
case 1:
{
testbase::DoSomething($option); 
parent::DoSomething($option); 
break;
}
}
}
}
?>
