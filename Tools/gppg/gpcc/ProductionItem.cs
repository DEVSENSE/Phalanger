using System;
using System.Collections.Generic;
using System.Text;
namespace gpcc
{
	public class ProductionItem
	{
		public Production production;
		public int pos;
		public bool expanded;
		public Set<Terminal> LA;
		public ProductionItem(Production production, int pos)
		{
			this.production = production;
			this.pos = pos;
		}
		public override bool Equals(object obj)
		{
			ProductionItem productionItem = (ProductionItem)obj;
			return productionItem.pos == this.pos && productionItem.production == this.production;
		}
		public override int GetHashCode()
		{
			return this.production.GetHashCode() + this.pos;
		}
		public static bool SameProductions(List<ProductionItem> list1, List<ProductionItem> list2)
		{
			if (list1.Count != list2.Count)
			{
				return false;
			}
			foreach (ProductionItem current in list1)
			{
				bool flag = false;
				foreach (ProductionItem current2 in list2)
				{
					if (current.Equals(current2))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
			}
			return true;
		}
		public bool isReduction()
		{
			return this.pos == this.production.rhs.Count;
		}
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat("{0} {1}: ", this.production.num, this.production.lhs);
			for (int i = 0; i < this.production.rhs.Count; i++)
			{
				if (i == this.pos)
				{
					stringBuilder.Append(". ");
				}
				stringBuilder.AppendFormat("{0} ", this.production.rhs[i]);
			}
			if (this.pos == this.production.rhs.Count)
			{
				stringBuilder.Append(".");
			}
			if (this.LA != null)
			{
				stringBuilder.AppendFormat("\t\t{0}", this.LA);
			}
			return stringBuilder.ToString();
		}
	}
}
