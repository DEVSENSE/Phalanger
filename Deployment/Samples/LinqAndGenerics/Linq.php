<?
	// This script demonstrates the LINQ support in Phalanger.

	namespace Phalanger
	{
        use System\Collections\Generic as G;
        use System as S;

		class Product
		{
			public function Product($productID, $name, $category, $unitPrice, $unitsInStock)
			{
				$this->ProductID = $productID;
				$this->Name = $name;
				$this->Category = $category;
				$this->UnitPrice = $unitPrice;
				$this->UnitsInStock = $unitsInStock;
			}
		
			public $ProductID; 
			public $Name;
			public $Category;
			public $UnitPrice;
			public $UnitsInStock;
		}
		
		class Order
		{
			public function Order($orderID, $productID, $quantity, $customerName)
			{
				$this->OrderID = $orderID;
				$this->ProductID = $productID;
				$this->Quantity = $quantity;
				$this->CustomerName = $customerName;
			}
		
			public $OrderID;
			public $ProductID;
			public $Quantity;
			public $CustomerName;
		}
	
		class Linq
		{
			private static function Display($label, $data)
			{
				echo "$label\n";
				
				$first = true;
				foreach ($data as $item)
				{
					if ($first) $first = false;
					else echo ", ";
					
					\print_r( $item );
				}
				
				echo "\n\n";
			}
		
			static function Run()
			{
				$random = new S\Random;

				for ($i = 0; $i < 20; $i++)
				{
					$numbers[] = $random->Next(100);
				}
			
				// simple LINQ queries on an integer array
				self::Display("All numbers:",
					from $numbers as $n
					select $n
				);
				
				self::Display("Numbers >50:",
					from $numbers as $n
					where $n > 50
					select $n
				);
				
				$products = self::GetProductList();
				$orders = self::GetOrderList();

				self::Display("Sold out products:",
					from $products as $p
					where $p->UnitsInStock == 0
					select $p->Name
				);
				
				self::Display("In-stock products that cost more than 3.00:",
					from $products as $p
					where $p->UnitsInStock > 0 && $p->UnitPrice > 3.00
					select $p->Name
				);
				
				self::Display("Products with pending orders:",
					from $products as $p, $orders as $o
					where $p->ProductID == $o->ProductID
					select "$p->Name ordered by $o->CustomerName"
				);
				
				self::Display("Ordered products that do not have sufficient in-stock count:",
					from $products as $p, $orders as $o
					where $p->ProductID == $o->ProductID && $p->UnitsInStock < $o->Quantity
					select "$o->Quantity of $p->Name ordered by $o->CustomerName"
				);
				
				echo "\n";
			}
			
			private static function GetProductList()
			{
				$list = new i'G\List'<:Product:>;
				
				$list->Add(new Product(1, "Chai", "Beverages", 18.0, 39));
				$list->Add(new Product(2, "Chang", "Beverages",	19.0, 17));
				$list->Add(new Product(3, "Aniseed Syrup", "Condiments", 10.0, 13));
				$list->Add(new Product(4, "Chef Anton's Cajun Seasoning", "Condiments", 22.0, 53));
				$list->Add(new Product(5, "Chef Anton's Gumbo Mix", "Condiments", 21.35, 0));
				$list->Add(new Product(6, "Grandma's Boysenberry Spread", "Condiments", 25.0, 120));
				$list->Add(new Product(7, "Uncle Bob's Organic Dried Pears", "Produce", 30.0, 15));
				$list->Add(new Product(8, "Northwoods Cranberry Sauce", "Condiments", 40.0, 6));
				$list->Add(new Product(9, "Mishi Kobe Niku", "Meat/Poultry", 97.0, 29));
				$list->Add(new Product(10, "Ikura", "Seafood", 31.0, 31));
				$list->Add(new Product(11, "Queso Cabrales", "Dairy Products", 21.0, 22));
				$list->Add(new Product(12, "Queso Manchego La Pastora", "Dairy Products", 38.0, 86));
				$list->Add(new Product(13, "Konbu", "Seafood", 6.0, 24));
				$list->Add(new Product(14, "Tofu", "Produce", 23.25, 35));
				$list->Add(new Product(15, "Genen Shouyu", "Condiments", 15.5, 39));
				$list->Add(new Product(16, "Pavlova", "Confections", 17.45, 29));
				$list->Add(new Product(17, "Alice Mutton", "Meat/Poultry", 39.0, 0));
				$list->Add(new Product(18, "Carnarvon Tigers", "Seafood", 62.5, 42));
				$list->Add(new Product(19, "Teatime Chocolate Biscuits", "Confections", 9.2, 25));
				$list->Add(new Product(20, "Sir Rodney's Marmalade", "Confections", 81.0, 40));
				$list->Add(new Product(21, "Sir Rodney's Scones", "Confections", 10.0, 3));
				$list->Add(new Product(22, "Gustaf's Knäckebröd", "Grains/Cereals", 21.0, 104));
				$list->Add(new Product(23, "Tunnbröd", "Grains/Cereals", 9.0, 61));
				$list->Add(new Product(24, "Guaraná Fantástica", "Beverages", 4.5, 20));
				$list->Add(new Product(25, "NuNuCa Nuß-Nougat-Creme", "Confections", 14.0, 76));
				$list->Add(new Product(26, "Gumbär Gummibärchen", "Confections", 31.23, 15));
				$list->Add(new Product(27, "Schoggi Schokolade", "Confections", 43.9, 49));
				$list->Add(new Product(28, "Rössle Sauerkraut", "Produce", 45.6, 26));
				$list->Add(new Product(29, "Thüringer Rostbratwurst", "Meat/Poultry", 123.79, 0));
				$list->Add(new Product(30, "Nord-Ost Matjeshering", "Seafood", 25.89, 10));
				$list->Add(new Product(31, "Gorgonzola Telino", "Dairy Products", 12.5, 0));
				$list->Add(new Product(32, "Mascarpone Fabioli", "Dairy Products", 32.0, 9));
				$list->Add(new Product(33, "Geitost", "Dairy Products", 2.5, 112));
				$list->Add(new Product(34, "Sasquatch Ale", "Beverages", 14.0, 111));
				$list->Add(new Product(35, "Steeleye Stout", "Beverages", 18.0, 20));
				$list->Add(new Product(36, "Inlagd Sill", "Seafood", 19.0, 112));
				$list->Add(new Product(37, "Gravad lax", "Seafood", 26.0, 11));
				$list->Add(new Product(38, "Côte de Blaye", "Beverages", 263.5, 17));
				$list->Add(new Product(39, "Chartreuse verte", "Beverages", 18.0, 69));
				$list->Add(new Product(40, "Boston Crab Meat", "Seafood", 18.4, 123));
				$list->Add(new Product(41, "Jack's New England Clam Chowder", "Seafood", 9.65, 85));
				$list->Add(new Product(42, "Singaporean Hokkien Fried Mee", "Grains/Cereals", 14.0, 26));
				$list->Add(new Product(43, "Ipoh Coffee", "Beverages", 46.0, 17));
				$list->Add(new Product(44, "Gula Malacca", "Condiments", 19.45, 27));
				$list->Add(new Product(45, "Rogede sild", "Seafood", 9.5, 5));
				$list->Add(new Product(46, "Spegesild", "Seafood", 12.0, 95));
				$list->Add(new Product(47, "Zaanse koeken", "Confections", 9.5, 36));
				$list->Add(new Product(48, "Chocolade", "Confections", 12.75, 15));
				$list->Add(new Product(49, "Maxilaku", "Confections", 20.0, 10));
				$list->Add(new Product(50, "Valkoinen suklaa", "Confections", 16.25, 65));
				$list->Add(new Product(51, "Manjimup Dried Apples", "Produce", 53.0, 20));
				$list->Add(new Product(52, "Filo Mix", "Grains/Cereals", 7.0, 38));
				$list->Add(new Product(53, "Perth Pasties", "Meat/Poultry", 32.8, 0));
				$list->Add(new Product(54, "Tourtiere", "Meat/Poultry", 7.45, 21));
				$list->Add(new Product(55, "Pâté chinois", "Meat/Poultry", 24.0, 115));
				$list->Add(new Product(56, "Gnocchi di nonna Alice", "Grains/Cereals", 38.0, 21));
				$list->Add(new Product(57, "Ravioli Angelo", "Grains/Cereals", 19.5, 36));
				$list->Add(new Product(58, "Escargots de Bourgogne", "Seafood", 13.25, 62));
				$list->Add(new Product(59, "Raclette Courdavault", "Dairy Products", 55.0, 79));
				$list->Add(new Product(60, "Camembert Pierrot", "Dairy Products", 34.0, 19));
				$list->Add(new Product(61, "Sirop d'érable", "Condiments", 28.5, 113));
				$list->Add(new Product(62, "Tarte au sucre", "Confections", 49.3, 17));
				$list->Add(new Product(63, "Vegie-spread", "Condiments", 43.9, 24));
				$list->Add(new Product(64, "Wimmers gute Semmelknödel", "Grains/Cereals", 33.25, 22));
				$list->Add(new Product(65, "Louisiana Fiery Hot Pepper Sauce", "Condiments", 21.05, 76));
				$list->Add(new Product(66, "Louisiana Hot Spiced Okra", "Condiments", 17.0, 4));
				$list->Add(new Product(67, "Laughing Lumberjack Lager", "Beverages", 14.0, 52));
				$list->Add(new Product(68, "Scottish Longbreads", "Confections", 12.5, 6));
				$list->Add(new Product(69, "Gudbrandsdalsost", "Dairy Products", 36.0, 26));
				$list->Add(new Product(70, "Outback Lager", "Beverages", 15.0, 15));
				$list->Add(new Product(71, "Flotemysost", "Dairy Products", 21.5, 26));
				$list->Add(new Product(72, "Mozzarella di Giovanni", "Dairy Products", 34.8, 14));
				$list->Add(new Product(73, "Röd Kaviar", "Seafood", 15.0, 101));
				$list->Add(new Product(74, "Longlife Tofu", "Produce", 10.0, 4));
				$list->Add(new Product(75, "Rhönbräu Klosterbier", "Beverages", 7.75, 125));
				$list->Add(new Product(76, "Lakkalikööri", "Beverages", 18.0, 57));
				$list->Add(new Product(77, "Original Frankfurter grüne Soße", "Condiments", 13.0, 32));
				
				return $list;
			}
			
			private static function GetOrderList()
			{
				$list = new i'G\List'<:Order:>;
			
				$list->Add(new Order(1, 30, 3, "Jan Benda"));
				$list->Add(new Order(2, 12, 4, "Martin Maly"));
				$list->Add(new Order(3, 11, 99, "Tomas Matousek"));
				$list->Add(new Order(4, 64, 1, "Pavel Novak"));
				$list->Add(new Order(5, 45, 2, "Vaclav Novak"));
				$list->Add(new Order(6, 73, 102, "Ladislav Prosek"));
				
				return $list;
			}
		}
	}
?>
