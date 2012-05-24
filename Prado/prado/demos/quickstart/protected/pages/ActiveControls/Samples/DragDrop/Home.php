<?php

prado::using ('System.Web.UI.ActiveControls.*');

class Home extends TPage
{
	public function onInit ($param)
	{
		parent::onInit($param);
		if (!$this->getIsPostBack() && !$this->getIsCallBack())
		{
			
			$this->populateProductList();
			$this->populateShoppingList();
		}
	}
	
	private function getProductData ()
	{
		return array (
			array (
				'ProductId' => 'Product1',
				'ProductImageUrl' => $this->publishAsset('images/product1.png'),
				'ProductTitle' => 'Cup'
			),
			array (
				'ProductId' => 'Product2',
				'ProductImageUrl' => $this->publishAsset('images/product2.png'),
				'ProductTitle' => 'T-Shirt'
			)
		);
	}
	
	private function getProduct ($key)
	{
		foreach ($this->getProductData() as $product)
			if ($product['ProductId']==$key) return $product;
		return null;	
	}
	
	protected function populateProductList ()
	{
		$this->ProductList->DataSource=$this->getProductData();
		$this->ProductList->Databind();
	}
	
	protected function populateShoppingList ()
	{
		$this->ShoppingList->DataSource=$this->getShoppingListData();
		$this->ShoppingList->Databind();
		
	}
	
	
	public function getShoppingListData ()
	{
		return $this->getViewState('ShoppingList', array ());
	}
	
	public function setShoppingListData ($value)
	{
		$this->setViewState('ShoppingList', TPropertyValue::ensureArray($value), array ());
	}
	
	public function addItemToCart ($sender, $param)
	{
		$control=$param->getDroppedControl();
		// Get the Key from the repeater item
		$item=$control->getNamingContainer();
		$key=$this->ProductList->getDataKeys()->itemAt($item->getItemIndex());
		$product=$this->getProduct($key);
		$shoppingList=$this->getShoppingListData();
		if (isset ($shoppingList[$key]))
		{
			// Already an item of this type, increment counter
			$shoppingList[$key]['ProductCount']++;
		}
		else
		{
			// Add an item to the shopping list
			$shoppingList[$key]=$product;
			$shoppingList[$key]['ProductCount']=1;
		}
		$this->setShoppingListData($shoppingList);
		
	}
	
	public function removeItemFromCart ($sender, $param)
	{
		$control=$param->getDroppedControl();
		$item=$control->getNamingContainer();
		$key=$this->ShoppingList->getDataKeys()->itemAt($item->getItemIndex());
		$shoppingList=$this->getShoppingListData();
		if (isset($shoppingList[$key]))
		{
			if ($shoppingList[$key]['ProductCount'] > 1)
				$shoppingList[$key]['ProductCount'] --;
			else
				unset($shoppingList[$key]);
		}
		$this->setShoppingListData($shoppingList);
		
	}
	
	public function redrawCart ($sender, $param)
	{
		$this->populateShoppingList();
		$this->cart->render($param->NewWriter);
		
	}
}
?>