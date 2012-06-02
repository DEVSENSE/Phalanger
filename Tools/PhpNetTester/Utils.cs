using System;
using System.Text;
using System.Collections;

namespace PHP.Testing
{
	public class Utils
	{
		private Utils() { }

		public static string MakeTColumn(string str)
		{
			return MakeTColumn(str, false);
		}

		public static string MakeTColumn(string str, bool isHead)
		{
			return String.Concat("<td valign=\"top\">", isHead ? "<b>" : "", str, isHead ? "</b>" : "", "</td>");
		}

		public static string MakeTColumn(string str, string classAttr)
		{
			return String.Concat("<td", classAttr, " valign=\"top\">", str, "</td>");
		}

		public static string MakeTColumn(string str, string classAttr, string span, int count)
		{
			return String.Concat("<td", classAttr, " valign=\"top\" ", span, "=\"", count, "\">", str, "</td>");
		}

		public static string NlToBr(string str)
		{
			if (str == null || str.Length == 0)
				return "&nbsp;";

			StringBuilder sb = new StringBuilder();
			foreach (char ch in str)
			{
				if (ch != '\n')
					sb.Append(ch);
				else
					sb.Append("<br/>");
			}

			return sb.ToString();
		}

		public static string ResultToString(TestResult result)
		{
			switch (result)
			{
				case TestResult.CtError: return "Compile error";
				case TestResult.PhpcHangUp: return "Phalanger hung up";
				case TestResult.PhpcMisbehaviourScript: return "Phalanger misbehaviour while compiling [file] section";
				case TestResult.ScriptHangUp: return "Script hung up";
				case TestResult.Succees: return "Success";
				case TestResult.UnexpectedOutput: return "Unexpected output";
				case TestResult.PhpMisbehaviour: return "PHP (original) misbehaviour.";
				case TestResult.PhpHangUp: return "PHP (original) hung up";
				case TestResult.PhpNotFound: return "[expect php] specified and PHP executable file not found";
				case TestResult.CannotCompileExpect: return "Cannot compile expect section.";
				case TestResult.ExpectHangUp: return "Expect section hung up.";
				case TestResult.ExpectedWarningNotDisplayed: return "Expected warning not displayed.";
			}
			return "unknown test result";
		}

		public static string ArrayListToString(ArrayList al)
		{
			return ArrayListToString(al, '\n');
		}

		public static string ArrayListToString(ArrayList al, char separator)
		{
			StringBuilder sb = new StringBuilder();

			foreach (object o in al)
			{
				sb.Append(o.ToString());
				sb.Append(separator);
			}

			return sb.ToString().Trim();
		}

		public static string RemoveCR(string str)
		{
			int index;
			while ((index = str.IndexOf('\r')) >= 0)
				str = str.Remove(index, 1);

			return str;
		}

        public static bool CanBeEmptyDirective(Directive current_directive)
        {
            return
                current_directive == Directive.Comment ||
                current_directive == Directive.ExpectPhp ||
                current_directive == Directive.ExpectCtError ||
                current_directive == Directive.ExpectCtWarning ||
                current_directive == Directive.Pure ||
                current_directive == Directive.Clr;
        }

		public static string OutputWithoutCompiling(ArrayList al)
		{
			StringBuilder sb = new StringBuilder();
			foreach (string s in al)
			{
				// there is php code, we must compile
				if (s.IndexOf("<?") >= 0)
					return null;

				sb.Append(s);
				sb.Append('\n');
			}

			// there is no php code, we can return output
			return sb.ToString().Trim();
		}
	}
}