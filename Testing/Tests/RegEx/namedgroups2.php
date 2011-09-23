[expect php]
[file]
<?

$flux = 'flux.transitions.bars3d
		 flux.transitions.wrap';

preg_match_all('/flux\.transitions\.(?<10>[a-z0-9]+)/', $flux, $transitions);

var_dump($transitions);

?>