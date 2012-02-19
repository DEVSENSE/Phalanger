<?php 
/* The Computer Language Benchmarks Game
   http://shootout.alioth.debian.org/

   contributed by Peter Baltruschat
   modified by Levi Cameron
*/

final class BinaryTreesTest
{

	static function bottomUpTree($item, $depth)
	{
	   if (!$depth) return array(null,null,$item);
	   $item2 = $item + $item;
	   $depth--;
	   return array(
		  self::bottomUpTree($item2-1,$depth),
		  self::bottomUpTree($item2,$depth),
		  $item);
	}

	static function itemCheck($treeNode) { 
	   return $treeNode[2]
		  + ($treeNode[0][0] === null ? self::itemCheck($treeNode[0]) : $treeNode[0][2])
		  - ($treeNode[1][0] === null ? self::itemCheck($treeNode[1]) : $treeNode[1][2]);
	}

	static function binaryTrees($n)
	{
		$minDepth = 4;

		$maxDepth = max($minDepth + 2, $n);
		$stretchDepth = $maxDepth + 1;

		$stretchTree = self::bottomUpTree(0, $stretchDepth);
		//printf("stretch tree of depth %d\t check: %d\n", $stretchDepth, self::itemCheck($stretchTree));
		unset($stretchTree);

		$longLivedTree = self::bottomUpTree(0, $maxDepth);

		$iterations = 1 << ($maxDepth);
		do
		{
		   $check = 0;
		   for($i = 1; $i <= $iterations; ++$i)
		   {
			  $t = self::bottomUpTree($i, $minDepth);
			  $check += self::itemCheck($t);
			  unset($t);
			  $t = self::bottomUpTree(-$i, $minDepth);
			  $check += self::itemCheck($t);
			  unset($t);
		   }
		   
		   //printf("%d\t trees of depth %d\t check: %d\n", $iterations<<1, $minDepth, $check);
		   
		   $minDepth += 2;
		   $iterations >>= 2;
		}
		while($minDepth <= $maxDepth);

		//printf("long lived tree of depth %d\t check: %d\n", $maxDepth, self::itemCheck($longLivedTree));
	}
	
	
	static function main()
	{
		Timing::Start("BinaryTrees");
		self::binaryTrees(15);
		Timing::Stop();		
	}
}

?>
