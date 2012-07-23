using System;
using System.Collections.Generic;
public struct CodeBlock
{
	private List<string> code;
	private int firstLine;
	private int lastLine;
	public List<string> Code
	{
		get
		{
			return this.code;
		}
	}
	public int FirstLine
	{
		get
		{
			return this.firstLine;
		}
	}
	public int LastLine
	{
		get
		{
			return this.lastLine;
		}
	}
	public CodeBlock(List<string> code, int firstLine, int lastLine)
	{
		this.code = code;
		this.firstLine = firstLine;
		this.lastLine = lastLine;
	}
}
