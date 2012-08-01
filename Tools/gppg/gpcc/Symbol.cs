using System;
namespace gpcc
{
	public abstract class Symbol
	{
		private string name;
		public string kind;
		public abstract int num
		{
			get;
		}
		public Symbol(string name)
		{
			this.name = name;
		}
		public override string ToString()
		{
			return this.name;
		}
		public abstract bool IsNullable();
	}
}
