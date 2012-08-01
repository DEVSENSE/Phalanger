using System;
namespace gpcc
{
	public class Reduce : ParserAction
	{
		public ProductionItem item;
		public Reduce(ProductionItem item)
		{
			this.item = item;
		}
		public override string ToString()
		{
			return string.Concat(new object[]
			{
				"reduce using rule ",
				this.item.production.num,
				" (",
				this.item.production.lhs,
				")"
			});
		}
		public override int ToNum()
		{
			return -this.item.production.num;
		}
	}
}
