<?

/*
	AutoLoader is taken from MediaWiki codebase.
*/

global $wgAutoloadLocalClasses;

$wgAutoloadLocalClasses = array(
'Klass' => 'klass.php'
);

class AutoLoader {
	/**
	 * autoload - take a class name and attempt to load it
	 *
	 * @param $className String: name of class we're looking for.
	 * @return bool Returning false is important on failure as
	 * it allows Zend to try and look in other registered autoloaders
	 * as well.
	 */
	static function autoload( $className ) {
		global $wgAutoloadClasses, $wgAutoloadLocalClasses;

		if ( isset( $wgAutoloadLocalClasses[$className] ) ) {
			$filename = $wgAutoloadLocalClasses[$className];
		} elseif ( isset( $wgAutoloadClasses[$className] ) ) {
			$filename = $wgAutoloadClasses[$className];
		} else {
			# Try a different capitalisation
			# The case can sometimes be wrong when unserializing PHP 4 objects
			$filename = false;
			$lowerClass = strtolower( $className );

			foreach ( $wgAutoloadLocalClasses as $class2 => $file2 ) {
				if ( strtolower( $class2 ) == $lowerClass ) {
					$filename = $file2;
				}
			}

			if ( !$filename ) {
				echo "Class {$className} not found; skipped loading\n";
				# Give up
				return false;
			}
		}

		# Make an absolute path, this improves performance by avoiding some stat calls
		if ( substr( $filename, 0, 1 ) != '/' && substr( $filename, 1, 1 ) != ':' ) {
			$filename = "$filename";
		}

		require( $filename );

		return true;
	}

	/**
	 * Force a class to be run through the autoloader, helpful for things like
	 * Sanitizer that have define()s outside of their class definition. Of course
	 * this wouldn't be necessary if everything in MediaWiki was class-based. Sigh.
	 *
	 * @return Boolean Return the results of class_exists() so we know if we were successful
	 */
	static function loadClass( $class ) {
		return class_exists( $class );
	}
}

spl_autoload_register( array( 'AutoLoader', 'autoload' ) );

?>