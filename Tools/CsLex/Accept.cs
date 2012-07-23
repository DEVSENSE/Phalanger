using System;
using System.Collections.Generic;
public class Accept
{
	private List<CodeBlock> codeBlocks;
	public List<CodeBlock> CodeBlocks
	{
		get
		{
			return this.codeBlocks;
		}
	}
	public Accept(List<CodeBlock> codeBlocks)
	{
		this.codeBlocks = codeBlocks;
	}
	public void Dump()
	{
		foreach (CodeBlock current in this.codeBlocks)
		{
			Console.WriteLine("line: " + current.FirstLine + ":");
			foreach (string current2 in current.Code)
			{
				Console.WriteLine(current2);
			}
		}
	}
}
