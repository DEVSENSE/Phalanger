[expect exact]

[file]
<?php
// "abstract public" is fine, but not "public abstract".
abstract class KlassName1 {
	public abstract function member();
}

abstract class KlassName2 {
	abstract public function member();
}

abstract class KlassName3 {
	abstract function member();
}
