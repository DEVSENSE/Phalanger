using System;
namespace gpcc
{
	public class Terminal : Symbol
	{
		private static int count;
		private static int max;
		public Precedence prec;
		private int n;
		public bool symbolic;
		public override int num
		{
			get
			{
				if (this.symbolic)
				{
					return Terminal.max + this.n;
				}
				return this.n;
			}
		}
		public Terminal(bool symbolic, string name) : base(symbolic ? name : ("'" + name.Replace("\n", "\\n") + "'"))
		{
			this.symbolic = symbolic;
			if (symbolic)
			{
				this.n = ++Terminal.count;
				return;
			}
			this.n = (int)name[0];
			if (this.n > Terminal.max)
			{
				Terminal.max = this.n;
			}
		}
		public override bool IsNullable()
		{
			return false;
		}
	}
}
