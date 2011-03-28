[expect php]
[file]
<?

	$tagValues = array();
	$tagSortOrders = array();
	$sortedTags = array();
		
	// order by sortOrder, then tag value
	array_multisort($tagSortOrders, SORT_NUMERIC, $tagValues, SORT_ASC, $sortedTags);


?>