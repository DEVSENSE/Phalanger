using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PHP.Testing
{
	public static class Utils
	{
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
		    return String.IsNullOrWhiteSpace(str) ? "&nbsp;" : str.Replace("\n", "<br />");
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
                case TestResult.Skipped: return "Skipped";
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

		public static string ListToString(List<string> list)
		{
			return ListToString(list, '\n');
		}

        public static string ListToString(List<string> list, char separator)
        {
            return String.Join(separator.ToString(CultureInfo.InvariantCulture), list).Trim();
		}

		public static string RemoveCR(string str)
		{
		    return str.Replace("\r", String.Empty);
		}

        public static bool CanBeEmptyDirective(Directive current_directive)
        {
            return
                current_directive == Directive.Comment ||
                current_directive == Directive.SkipIf ||
                current_directive == Directive.ExpectPhp ||
                current_directive == Directive.ExpectCtError ||
                current_directive == Directive.ExpectCtWarning ||
                current_directive == Directive.Pure ||
                current_directive == Directive.Clr;
        }

		public static string OutputWithoutCompiling(List<string> lines)
		{
			var sb = new StringBuilder(lines.Count * 10);
            foreach (string s in lines)
			{
				// there is php code, we must compile
                //FIXME: This could be legitimate output.
				if (s.IndexOf("<?") >= 0)
				{
				    return null;
				}

                sb.Append(s);
				sb.Append('\n');
			}

			// there is no php code, we can return output
			return sb.ToString().Trim();
		}

	    public static void DumpToFile(IEnumerable<string> script, string path)
	    {
	        using (var sw = new StreamWriter(path))
	        {
	            foreach (var s in script)
	            {
	                sw.WriteLine(s);
	            }

	            sw.Close();
	        }
	    }

	    public static string RemoveWhitespace(string str)
	    {
	        var sb = new StringBuilder(str.Length);
	        foreach (var c in str.Where(c => !Char.IsWhiteSpace(c)))
	        {
	            sb.Append(c);
	        }

	        return sb.ToString();
	    }
	}
}
