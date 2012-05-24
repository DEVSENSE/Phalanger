<?php

class OrderDetail extends TActiveRecord
{
	const TABLE='Order Details';

	public $OrderID;
	public $ProductID;
	public $UnitPrice;
	public $Quantity;
	public $Discount;

	public $Product;
	public $Order;

	public static $RELATIONS = array
	(
		'Product' => array(self::BELONGS_TO, 'Product'),
		'Order' => array(self::BELONGS_TO, 'Order'),
	);

	public static function finder($className=__CLASS__)
	{
		return parent::finder($className);
	}
}
?>