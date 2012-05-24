<?php
/**
 * TPropertyAccess class file.
 *
 * @author Wei Zhuo <weizhuo[at]gmail[dot]com>
 * @link http://www.pradosoft.com/
 * @copyright Copyright &copy; 2005-2008 PradoSoft
 * @license http://www.pradosoft.com/license/
 * @version $Id: TPropertyAccess.php 2644 2009-05-01 08:30:05Z godzilla80@gmx.net $
 * @package System.Data.SqlMap
 */

/**
 * TPropertyAccess class provides dot notation stype property access and setting.
 *
 * Access object's properties (and subproperties) using dot path notation.
 * The following are equivalent.
 * <code>
 * echo $obj->property1;
 * echo $obj->getProperty1();
 * echo $obj['property1']; //$obj may be an array or object
 * echo TPropertyAccess($obj, 'property1');
 * </code>
 *
 * Setting a property value.
 * <code>
 * $obj1->propert1 = 'hello';
 * $obj->setProperty('hello');
 * $obj['property1'] = 'hello'; //$obj may be an array or object
 * TPropertyAccess($obj, 'property1', 'hello');
 * </code>
 *
 * Subproperties are supported using the dot notation. E.g.
 * <code>
 * echo $obj->property1->property2->property3
 * echo TPropertyAccess::get($obj, 'property1.property2.property3');
 * </code>
 *
 * @author Wei Zhuo <weizho[at]gmail[dot]com>
 * @version $Id: TPropertyAccess.php 2644 2009-05-01 08:30:05Z godzilla80@gmx.net $
 * @package System.Data.SqlMap
 * @since 3.1
 */
class TPropertyAccess
{
	/**
	 * Gets the property value.
	 * @param mixed object or path.
	 * @param string property path.
	 * @return mixed property value.
	 * @throws TInvalidDataValueException if property path is invalid.
	 */
	public static function get($object,$path)
	{
		if(!is_array($object) && !is_object($object))
			return $object;
		$properties = explode('.', $path);
		foreach($properties as $prop)
		{
			if(is_array($object) || $object instanceof ArrayAccess)
			{
				if(array_key_exists($prop, $object))
					$object = $object[$prop];
				else
					throw new TInvalidPropertyException('sqlmap_invalid_property',$path);
			}
			else if(is_object($object))
			{
				$getter = 'get'.$prop;
				if(method_exists($object, $getter) && is_callable(array($object, $getter)))
					$object = $object->{$getter}();
				else if(in_array($prop, array_keys(get_object_vars($object))))
					$object = $object->{$prop};
				elseif(method_exists($object, '__get') && is_callable(array($object, '__get')))
					$object = $object->{$prop};
				else
					throw new TInvalidPropertyException('sqlmap_invalid_property',$path);
			}
			else
				throw new TInvalidPropertyException('sqlmap_invalid_property',$path);
		}
		return $object;
	}

	/**
	 * @param mixed object or array
	 * @param string property path.
	 * @return boolean true if property path is valid
	 */
	public static function has($object, $path)
	{
		if(!is_array($object) && !is_object($object))
			return false;
		$properties = explode('.', $path);
		foreach($properties as $prop)
		{
			if(is_array($object) || $object instanceof ArrayAccess)
			{
				if(array_key_exists($prop, $object))
					$object = $object[$prop];
				else
					return false;
			}
			else if(is_object($object))
			{
				$getter = 'get'.$prop;
				if(method_exists($object, $getter) && is_callable(array($object, $getter)))
					$object = $object->{$getter}();
				else if(in_array($prop, array_keys(get_object_vars($object))))
					$object = $object->{$prop};
				elseif(method_exists($object, '__get') && is_callable(array($object, '__get')))
					$object = $object->{$prop};
				else
					return false;
			}
			else
				return false;
		}
		return true;
	}

	/**
	 * Sets the property value.
	 * @param mixed object or array
	 * @param string property path.
	 * @param mixed new property value.
	 * @throws TInvalidDataValueException if property path is invalid.
	 */
	public static function set(&$originalObject, $path, $value)
	{
		$properties = explode('.', $path);
		$prop = array_pop($properties);
		if(count($properties) > 0)
			$object = self::get($originalObject, implode('.',$properties));
		else
			$object = &$originalObject;

		if(is_array($object) || $object instanceof ArrayAccess)
		{
			$object[$prop] = $value;
		}
		else if(is_object($object))
		{
			$setter = 'set'.$prop;
			if (method_exists($object, $setter) && is_callable(array($object, $setter)))
				$object->{$setter}($value);
			else
				$object->{$prop} = $value;
		}
		else
			throw new TInvalidPropertyException('sqlmap_invalid_property_type',$path);
	}

}

?>
