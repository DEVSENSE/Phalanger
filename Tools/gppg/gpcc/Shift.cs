using System;
namespace gpcc
{
	public class Shift : ParserAction
	{
		public State next;
		public Shift(State next)
		{
			this.next = next;
		}
		public override string ToString()
		{
			return "shift, and go to state " + this.next.num;
		}
		public override int ToNum()
		{
			return this.next.num;
		}
	}
}
