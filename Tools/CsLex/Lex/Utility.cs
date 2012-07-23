using System;
using System.Text;
namespace Lex
{
	public class Utility
	{
		public const int INT_MAX = 2147483647;
		public static void enter(string descent, char lexeme, int token)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Entering ");
			stringBuilder.Append(descent);
			stringBuilder.Append(" [lexeme: '");
			if (lexeme < ' ')
			{
				lexeme += '@';
				stringBuilder.Append("^");
			}
			stringBuilder.Append(lexeme);
			stringBuilder.Append("'] [token: ");
			stringBuilder.Append(token);
			stringBuilder.Append("]");
			Console.WriteLine(stringBuilder.ToString());
		}
		public static void leave(string descent, char lexeme, int token)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Leaving ");
			stringBuilder.Append(descent);
			stringBuilder.Append(" [lexeme: '");
			if (lexeme < ' ')
			{
				lexeme += '@';
				stringBuilder.Append("^");
			}
			stringBuilder.Append(lexeme);
			stringBuilder.Append("'] [token: ");
			stringBuilder.Append(token);
			stringBuilder.Append("]");
			Console.WriteLine(stringBuilder.ToString());
		}
		public static void assert(bool expr)
		{
			if (!expr)
			{
				Console.WriteLine("Assertion Failed");
				throw new ApplicationException("Assertion Failed.");
			}
		}
		public static char[] doubleSize(char[] oldBuffer)
		{
			char[] array = new char[2 * oldBuffer.Length];
			for (int i = 0; i < oldBuffer.Length; i++)
			{
				array[i] = oldBuffer[i];
			}
			return array;
		}
		public static byte[] doubleSize(byte[] oldBuffer)
		{
			byte[] array = new byte[2 * oldBuffer.Length];
			for (int i = 0; i < oldBuffer.Length; i++)
			{
				array[i] = oldBuffer[i];
			}
			return array;
		}
		public static char hex2bin(char c)
		{
			if ('0' <= c && '9' >= c)
			{
				return (char)(c - '0');
			}
			if ('a' <= c && 'f' >= c)
			{
				return (char)(c - 'a' + '\n');
			}
			if ('A' <= c && 'F' >= c)
			{
				return (char)(c - 'A' + '\n');
			}
			Error.impos("Bad hexidecimal digit" + char.ToString(c));
			return '\0';
		}
		public static bool ishexdigit(char c)
		{
			return ('0' <= c && '9' >= c) || ('a' <= c && 'f' >= c) || ('A' <= c && 'F' >= c);
		}
		public static char oct2bin(char c)
		{
			if ('0' <= c && '7' >= c)
			{
				return (char)(c - '0');
			}
			Error.impos("Bad octal digit " + char.ToString(c));
			return '\0';
		}
		public static bool isoctdigit(char c)
		{
			return '0' <= c && '7' >= c;
		}
		public static bool IsNewline(char c)
		{
			return '\n' == c || '\r' == c;
		}
		public static bool isalpha(char c)
		{
			return ('a' <= c && 'z' >= c) || ('A' <= c && 'Z' >= c);
		}
		public static char toupper(char c)
		{
			if ('a' <= c && 'z' >= c)
			{
				return (char)(c - 'a' + 'A');
			}
			return c;
		}
		public static int bytencmp(byte[] a, int a_first, byte[] b, int b_first, int n)
		{
			for (int i = 0; i < n; i++)
			{
				if (a[a_first + i] == 0 && b[b_first + i] == 0)
				{
					return 0;
				}
				if (a[a_first + i] < b[b_first + i])
				{
					return 1;
				}
				if (a[a_first + i] > b[b_first + i])
				{
					return -1;
				}
			}
			return 0;
		}
		public static int charncmp(char[] a, int a_first, char[] b, int b_first, int n)
		{
			for (int i = 0; i < n; i++)
			{
				if (a[a_first + i] == '\0' && b[b_first + i] == '\0')
				{
					return 0;
				}
				if (a[a_first + i] < b[b_first + i])
				{
					return 1;
				}
				if (a[a_first + i] > b[b_first + i])
				{
					return -1;
				}
			}
			return 0;
		}
		public static int Compare(char[] c, string s)
		{
			for (int i = 0; i < s.Length; i++)
			{
				if (c[i] < s[i])
				{
					return 1;
				}
				if (c[i] > s[i])
				{
					return -1;
				}
			}
			return 0;
		}
	}
}
