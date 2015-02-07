/*

 Copyright (c) 2004-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Text;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections;

using PHP.Core;
using PHP.Core.Reflection;
using System.Collections.Generic;

#if !SILVERLIGHT
using System.Web;
#else
using System.Windows.Browser;
#endif

namespace PHP.Core
{
	/// <summary>
	/// Manages displaying of information about Phalanger and external PHP modules.
	/// </summary>
	public static class PhpNetInfo
	{
		/// <summary>
		/// Sections of Phalanger information. 
		/// </summary>
		[Flags]
		public enum Sections
		{
			General = 1,
			Credits = 2,
			Configuration = 4,
			Extensions = 8,
			Environment = 16,
			Variables = 32,
			License = 64,
			All = -1
		}

		/// <summary>
		/// Writes all information about Phalanger and external PHP modules to output.
		/// </summary>
		/// <param name="output">An output where to write information.</param>
		/// <param name="sections">A mask of sections which to write.</param>
		public static void Write(Sections/*!*/ sections, TextWriter/*!*/ output)
		{
			output.Write(htmlPrologue);
			output.Write(htmlStyle, htmlCss);

			if ((sections & Sections.General) != 0)
			{
				WriteLogo(output);
			}

			if ((sections & (Sections.Configuration | Sections.General)) != 0)
			{
				output.Write("<h2>");
				output.Write(CoreResources.GetString("info_config"));
				output.Write("</h2>");
				WriteConfiguration(output);
			}

			if ((sections & Sections.Credits) != 0)
			{
				output.Write("<h2>");
				output.Write(CoreResources.GetString("info_credits"));
				output.Write("</h2>");
				WriteCredits(output);
			}

#if !SILVERLIGHT
			if ((sections & Sections.Extensions) != 0)
			{
				output.Write("<h2>");
				output.Write(CoreResources.GetString("info_loaded_extensions"));
				output.Write("</h2>");
				output.Write(Externals.PhpInfo());
			}
#endif

			if ((sections & Sections.Environment) != 0)
			{
				output.Write("<h2>");
				output.Write(CoreResources.GetString("info_environment_variables"));
				output.Write("</h2>");
				WriteEnvironmentVariables(output);
			}

			if ((sections & Sections.Variables) != 0)
			{
				output.Write("<h2>");
				output.Write(CoreResources.GetString("info_global_variables"));
				output.Write("</h2>");
				WriteGlobalVariables(output);
			}

			if ((sections & Sections.License) != 0)
			{
				output.Write("<h2>");
				output.Write(CoreResources.GetString("info_license"));
				output.Write("</h2>");
				WriteLicense(output);
			}

			output.Write(htmlEpilogue);
		}

		#region Private HTML constructs

		private const string htmlTableStart = "<table cellpadding='3' align='center'>";
		private const string htmlTableEnd = "</table>";
		private const string htmlTableBoxStart = "<tr><td>";
		private const string htmlTableHeaderBoxStart = "<tr class='header'><td>";
		private const string htmlTableBoxEnd = "</tr></td>";
		private const string htmlHorizontalLine = "<hr/>";
		private const string htmlTableColspanHeader = "<tr><td class='colHeader' colspan={0}>{1}</td></tr>";
		private const string htmlSectionCaption = "<h3>{0}</h3>";
		private const string htmlPrologue = "<div class='PhpNetInfo' align='center'>";
		private const string htmlEpilogue = "</div>";
		private const string htmlStyle = "<style type='text/css'>\n {0} \n</style>";
		private const string htmlCss =
		  "div.PhpNetInfo { font-family:sans-serif; background-color:white; color:black; text-align:center; }\n" +
		  "div.PhpNetInfo pre { margin:0px; font-family:monospace; }\n" +
		  "div.PhpNetInfo a:link { color:#000099; text-decoration:none; }\n" +
		  "div.PhpNetInfo a:hover { text-decoration: underline; }\n" +
		  "div.PhpNetInfo table { width:600px; border-collapse:collapse; text-align:left; }\n" +
		  "div.PhpNetInfo th { text-align: center; !important }\n" +
		  "div.PhpNetInfo td, div.PhpNetInfo th { border:1px solid black; font-size:75%; vertical-align:baseline; }\n" +
		  "div.PhpNetInfo td { background-color:#cccccc; }\n" +
		  "div.PhpNetInfo td.rowHeader { background-color:#ccccff; font-weight:bold; }\n" +
		  "div.PhpNetInfo tr.colHeader { background-color:#9999cc; font-weight:bold; }\n" +
		  "div.PhpNetInfo i { color:#666666; }\n" +
		  "div.PhpNetInfo img { float: right; border:0px; }\n" +
		  "div.PhpNetInfo hr { width:600px; align:center; background-color:#cccccc; border:0px; height:1px; }\n";

		/// <summary>
		/// Makes a table row containing given <c>cells</c>.
		/// </summary>
		/// <param name="doEscape">Do escape HTML entities (tag markers etc.)?</param>
		/// <param name="cells">The content of cells of the written row.</param>
		/// <returns>The row in HTML.</returns>
		private static string HtmlRow(bool doEscape, params string[] cells)
		{
			StringBuilder result = new StringBuilder();

			result.AppendFormat("<tr><td class='rowHeader'>{0}</td>", cells[0]);
			for (int i = 1; i < cells.Length; i++)
			{
				string cell = cells[i];
				if (cell == null || cell == "") cell = "<i>no value</i>";
				else
					if (doEscape) cell = HttpUtility.HtmlEncode(cell);

				result.AppendFormat("<td>{0}</td>", cell);
			}
			result.AppendFormat("</tr>");
			return result.ToString();
		}

		/// <summary>
		/// Outputs a table row containing a variable dump.
		/// </summary>
		private static void HtmlVarRow(TextWriter output, string array, object name, object variable)
		{
			string s;

			output.Write("<tr><td class='rowHeader'>{0}[\"", array);

			// name:
			if ((s = name as string) != null)
				output.Write((string)StringUtils.AddCSlashes(s, false, true));
			else
				output.Write((int)name);

			// printed value:
			output.Write("\"]</td><td>");

			IPhpPrintable printable;

			if (variable == null || (variable as string) == String.Empty)
			{
				output.Write("<i>no value</i>");
			}
			else
				if ((printable = variable as IPhpPrintable) != null)
				{
					output.Write("<pre>");
					StringWriter str_output = new StringWriter();
					printable.Print(str_output);
                    output.Write(HttpUtility.HtmlEncode(str_output.ToString()));
					output.Write("</pre>");
				}
				else
				{
					output.Write(Convert.ObjectToString(variable));
				}

			output.Write("</td></tr>");
		}

		/// <summary>
		/// Makes a table header row containing given <c>cells</c>.
		/// </summary>
		/// <param name="cells">The content of cells of the written row.</param>
		/// <returns>The row in HTML.</returns>
		private static string HtmlHeaderRow(params string[] cells)
		{
			StringBuilder result = new StringBuilder("<tr class='colHeader'>");

			foreach (string cell in cells)
				result.AppendFormat("<th>{0}</th>", (cell == null || cell == "") ? " " : cell);

			result.Append("</tr>");
			return result.ToString();
		}

		private static string HtmlEntireRowHeader(string text, int count)
		{
			StringBuilder result = new StringBuilder("<tr class='colHeader'>");

			result.AppendFormat("<th colspan='{0}' align='center'>{1}</th>", count, text);

			result.Append("</tr>");
			return result.ToString();
		}

		/// <summary>
		/// Converts option's value to string to be displayed.
		/// </summary>
		/// <param name="value">The value of the option.</param>
		/// <returns>String representation of the option's value.</returns>
		private static string OptionValueToString(object value)
		{
			// lists:
			IList list = value as IList;
			if (list != null)
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i] != null)
					{
						if (i > 0) sb.Append("; ");
						sb.Append(list[i].ToString());
					}
				}
				return sb.ToString();
			}

			// convertible:
			IPhpConvertible conv = value as IPhpConvertible;
			if (conv != null)
				return conv.ToString();

			Encoding encoding = value as Encoding;
			if (encoding != null)
				return encoding.WebName;

			// others:  
			return (value == null) ? String.Empty : value.ToString();
		}

		#endregion

		#region Info Writers

		private static void WriteLogo(TextWriter output)
		{
			output.Write("<h1>Phalanger {0}{1} {2}</h1>",
                PhalangerVersion.Current,
#if DEBUG
                ", DEBUG,",
#else
                null,
#endif
                (Environment.Is64BitProcess ? "x64" : "x86"));
			output.Write("<h4>The PHP language compiler for .NET Framework</h4>");
		}

		private static void WriteLicense(TextWriter output)
		{
			output.Write(htmlTableStart);
			output.Write("<tr><td>");
			output.Write(
			"<p align='center'>" +
            "<b>Copyright (c) Jan Benda, Miloslav Beno, Martin Maly, Tomas Matousek, Jakub Misek, Pavel Novak, Vaclav Novak, and Ladislav Prosek.</b>" +
			"</p>");
			output.Write(CoreResources.GetString("info_license_text"));
			output.Write("</td></tr>");
			output.Write(htmlTableEnd);
		}

		private static void WriteCredits(TextWriter output)
		{
			string contribution = CoreResources.GetString("credits_contribution");
			string authors = CoreResources.GetString("credits_authors");

			output.Write(htmlSectionCaption, CoreResources.GetString("credits_design"));
			output.Write(htmlTableStart);
			output.Write(HtmlHeaderRow(contribution, authors));

			ScriptContext context = ScriptContext.CurrentContext;

            output.Write(HtmlRow(false, CoreResources.GetString("credits_overall_concept"), "Tomas Matousek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_specific_features_compilation"), "Tomas Matousek, Ladislav Prosek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_oo_features_compilation"), "Ladislav Prosek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_php_clr"), "Tomas Matousek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_overall_compiler_design"), "Tomas Matousek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_code_analysis"), "Tomas Matousek, Vaclav Novak"));
            output.Write(HtmlRow(false, "Core", "Tomas Matousek, Ladislav Prosek"));
            output.Write(HtmlRow(false, "Class Library", "Tomas Matousek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_extmgr_wrappers"), "Ladislav Prosek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_ast"), "Vaclav Novak, Tomas Matousek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_compiler_tables"), "Tomas Matousek, Ladislav Prosek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_code_generator"), "Tomas Matousek, Ladislav Prosek, Martin Maly"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_configuration"), "Tomas Matousek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_aspnet"), "Ladislav Prosek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_automatic_tests"), "Pavel Novak"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_interactive_tests"), "Jan Benda, Jakub Misek"));

            output.Write(htmlTableEnd);

            output.Write(htmlSectionCaption, CoreResources.GetString("credits_implementation"));
            output.Write(htmlTableStart);
            output.Write(HtmlHeaderRow(contribution, authors));

            output.Write(HtmlRow(false, CoreResources.GetString("credits_core_functionality"), "Tomas Matousek, Ladislav Prosek, Tomas Petricek, Daniel Balas, Jakub Misek, Miloslav Beno"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_lexical_syntactic_analysis"), "Tomas Matousek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_semantic_analysis"), "Tomas Matousek, Vaclav Novak"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_code_generation"), "Tomas Matousek, Ladislav Prosek, Martin Maly, Jakub Misek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_clr_features"), "Ladislav Prosek, Tomas Matousek"));
            output.Write(HtmlRow(false, "Class Library", "Tomas Matousek, Ladislav Prosek, Jan Benda, Pavel Novak, Tomas Petricek, Daniel Balas, Miloslav Beno, Jakub Misek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_extensions_management"), "Ladislav Prosek, Daniel Balas, Jakub Misek"));
            output.Write(HtmlRow(false, "SHM Channel", "Ladislav Prosek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_aspnet"), "Ladislav Prosek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_vsnet"), "Tomas Matousek, Tomas Petricek, Jakub Misek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_streams"), "Jan Benda"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_interactive_tests"), "Jan Benda"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_automatic_tester"), "Pavel Novak, Jakub Misek, Miloslav Beno"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_utilities"), "Tomas Matousek, Ladislav Prosek"));
            output.Write(HtmlRow(false, CoreResources.GetString("credits_installation"), "Ladislav Prosek, Jakub Misek"));

			output.Write(htmlTableEnd);
		}

		private const BindingFlags ConfigBindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance;

		private static void ReflectConfigSection(TextWriter output, string prefix, Type type, object config1, object config2)
		{
			FieldInfo[] fields = type.GetFields(ConfigBindingFlags);
			PropertyInfo[] properties = type.GetProperties(ConfigBindingFlags);
			string[] cells = new string[(config2 != null) ? 3 : 2];

			// fields which contains sections:
			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i].IsDefined(typeof(NoPhpInfoAttribute), false))
				{
					fields[i] = null;
				}
				else if (typeof(IPhpConfigurationSection).IsAssignableFrom(fields[i].FieldType))
				{
					ReflectConfigSection(output, prefix + fields[i].Name + ".", fields[i].FieldType,
					  fields[i].GetValue(config1), (config2 != null) ? fields[i].GetValue(config2) : null);

					fields[i] = null;
				}
			}

			// remaining fields:
			for (int i = 0; i < fields.Length; i++)
			{
				if (fields[i] != null)
				{
					try
					{
						cells[0] = prefix + fields[i].Name;
						cells[1] = OptionValueToString(fields[i].GetValue(config1));
						if (config2 != null) cells[2] = OptionValueToString(fields[i].GetValue(config2));

						output.Write(HtmlRow(true, cells));
					}
					catch (Exception e)
					{
						cells[1] = e.Message;
						if (config2 != null) cells[2] = null;
						output.Write(HtmlRow(true, cells));
					}
				}
			}

			// properties:
			foreach (PropertyInfo property in properties)
			{
				if (!property.IsDefined(typeof(NoPhpInfoAttribute), false))
				{
					try
					{
						cells[0] = prefix + property.Name;
						cells[1] = OptionValueToString(property.GetValue(config1, null));
						if (config2 != null) cells[2] = OptionValueToString(property.GetValue(config2, null));

						output.Write(HtmlRow(true, cells));
					}
					catch (Exception e)
					{
						cells[1] = e.Message;
						if (config2 != null) cells[2] = null;
						output.Write(HtmlRow(true, cells));
					}
				}
			}
		}

		/// <summary>
		/// Writes core configuration to given output.
		/// </summary>
		/// <param name="output">The output.</param>
		/// <remarks>
		/// Configuration is traversed by reflection methods and all fields and its values are formatted to table.
		/// </remarks>
		private static void WriteConfiguration(TextWriter/*!*/ output)
		{
			ApplicationContext app_context = ScriptContext.CurrentContext.ApplicationContext;
			Debug.Assert(!app_context.AssemblyLoader.ReflectionOnly);
			
			string directive = CoreResources.GetString("info_directive");

			// script dependent configuration //

			output.Write(htmlSectionCaption, CoreResources.GetString("info_script_dependent"));
			output.Write(htmlTableStart);
			output.Write(HtmlEntireRowHeader("Core", 3));
			output.Write(HtmlHeaderRow(directive, CoreResources.GetString("info_script_value"), CoreResources.GetString("info_master_value")));

			// core:
			ReflectConfigSection(output, "", typeof(LocalConfiguration), Configuration.Local, Configuration.DefaultLocal);

			// libraries:
			foreach (PhpLibraryAssembly lib_assembly in app_context.GetLoadedLibraries())
			{
				IPhpConfiguration local = Configuration.Local.GetLibraryConfig(lib_assembly.Descriptor);
				IPhpConfiguration @default = Configuration.DefaultLocal.GetLibraryConfig(lib_assembly.Descriptor);

				if (local != null)
				{
					if (!local.GetType().IsDefined(typeof(NoPhpInfoAttribute), false))
					{
						output.Write(HtmlEntireRowHeader(HttpUtility.HtmlEncode(lib_assembly.Properties.Name), 3));
						output.Write(HtmlHeaderRow(directive, CoreResources.GetString("info_script_value"), CoreResources.GetString("info_master_value")));
						ReflectConfigSection(output, "", local.GetType(), local, @default);
					}
				}
			}

			output.Write(htmlTableEnd);

			// script independent configuration //

			output.Write(htmlSectionCaption, CoreResources.GetString("info_shared"));
			output.Write(htmlTableStart);
			output.Write(HtmlEntireRowHeader("Core", 2));
			output.Write(HtmlHeaderRow(directive, CoreResources.GetString("info_value")));

			// core:
			ReflectConfigSection(output, "", typeof(GlobalConfiguration), Configuration.Global, null);

			// libraries:
			foreach (PhpLibraryAssembly lib_assembly in app_context.GetLoadedLibraries())
			{
				object config = Configuration.Global.GetLibraryConfig(lib_assembly.Descriptor);

				if (config != null)
				{
					if (!config.GetType().IsDefined(typeof(NoPhpInfoAttribute), false))
					{
						output.Write(HtmlEntireRowHeader(HttpUtility.HtmlEncode(lib_assembly.Properties.Name), 2));
						output.Write(HtmlHeaderRow(directive, CoreResources.GetString("info_value")));
						ReflectConfigSection(output, "", config.GetType(), config, null);
					}
				}
			}

			output.Write(htmlTableEnd);
		}

		private static void WriteAutoGlobal(TextWriter output, ScriptContext context, string name, PhpReference autoglobal)
		{
			PhpArray array;

			if ((array = autoglobal.Value as PhpArray) != null)
			{
				foreach (KeyValuePair<IntStringKey, object> entry in array)
					HtmlVarRow(output, name, entry.Key.Object, entry.Value);
			}

		}

		private static void WriteGlobalVariables(TextWriter output)
		{
			output.Write(htmlTableStart);
			output.Write(HtmlHeaderRow(CoreResources.GetString("info_variable"), CoreResources.GetString("info_value")));

			ScriptContext context = ScriptContext.CurrentContext;

#if !SILVERLIGHT
			WriteAutoGlobal(output, context, AutoGlobals.GetName, context.AutoGlobals.Get);
			WriteAutoGlobal(output, context, AutoGlobals.PostName, context.AutoGlobals.Post);
			WriteAutoGlobal(output, context, AutoGlobals.CookieName, context.AutoGlobals.Cookie);
			WriteAutoGlobal(output, context, AutoGlobals.FilesName, context.AutoGlobals.Files);
			WriteAutoGlobal(output, context, AutoGlobals.SessionName, context.AutoGlobals.Session);
			WriteAutoGlobal(output, context, AutoGlobals.ServerName, context.AutoGlobals.Server);
			WriteAutoGlobal(output, context, AutoGlobals.EnvName, context.AutoGlobals.Env);
#endif

			output.Write(htmlTableEnd);
		}

		private static void WriteEnvironmentVariables(TextWriter output)
		{
#if !SILVERLIGHT
			output.Write(htmlTableStart);
			output.Write(HtmlHeaderRow(CoreResources.GetString("info_variable"), CoreResources.GetString("info_value")));

			IDictionary env_vars = Environment.GetEnvironmentVariables();
			foreach (DictionaryEntry entry in env_vars)
				output.Write(HtmlRow(true, entry.Key as string, entry.Value as string));

			output.Write(htmlTableEnd);
#endif
		}


		#endregion

		#region External callbacks

		/// <summary>
		/// Prints the section caption. 
		/// </summary>
		/// <param name="print">If true, the section caption is sent to output and returned, if false,
		/// the section caption is returned.</param>
		/// <param name="caption">The caption.</param>
		/// <returns>The section caption.</returns>
		[ExternalCallback("SECTION")]
		public static string PrintSectionCaption(bool print, string caption)
		{
			string ret = String.Format(htmlSectionCaption, caption);
			if (print) ScriptContext.CurrentContext.Output.Write(ret);
			return ret;
		}

		/// <summary>
		/// Prints the table starting tag.
		/// </summary>
		/// <param name="print"> If true, the tag is sent to output and returned, if false, the tag
		/// is returned.</param>
		/// <returns>The table starting tag.</returns>
		[ExternalCallback("php_info_print_table_start")]
		public static string PrintTableStart(bool print)
		{
			if (print) ScriptContext.CurrentContext.Output.Write(htmlTableStart);
			return htmlTableStart;
		}

		/// <summary>
		/// Prints the table ending tag.
		/// </summary>
		/// <param name="print"> If true, the tag is sent to output and returned, if false, the tag
		/// is returned.</param>
		/// <returns>The table ending tag.</returns>
		[ExternalCallback("php_info_print_table_end")]
		public static string PrintTableEnd(bool print)
		{
			if (print) ScriptContext.CurrentContext.Output.Write(htmlTableEnd);
			return htmlTableEnd;
		}

		/// <summary>
		/// Prints table row (tr) starting tag and the first column starting tag (td).
		/// </summary>
		/// <param name="print"> If true, the tags are sent to output and returned, if false, 
		/// the tags are returned.</param>
		/// <param name="isHeader">Nonzero if the row is a header row.</param>
		/// <returns>Table row (tr) starting tag and the first column starting tag (td).</returns>
		[ExternalCallback("php_info_box_start")]
		public static string PrintBoxStart(bool print, int isHeader)
		{
			string ret = (isHeader == 0) ? htmlTableBoxStart : htmlTableHeaderBoxStart;
			if (print) ScriptContext.CurrentContext.Output.Write(ret);
			return ret;
		}

		/// <summary>
		/// Prints td and tr ending tags.
		/// </summary>
		/// <param name="print"> If true, the tags are sent to output and returned, if false, 
		/// the tags are returned.</param>
		/// <returns>Td and tr ending tags.</returns>
		[ExternalCallback("php_info_box_end")]
		public static string PrintBoxEnd(bool print)
		{
			if (print) ScriptContext.CurrentContext.Output.Write(htmlTableBoxEnd);
			return htmlTableBoxEnd;
		}

		/// <summary>
		/// Prints horizontal line (hr) tag.
		/// </summary>
		/// <param name="print"> If true, the tag is sent to output and returned, if false, the tag
		/// is returned.</param>
		/// <returns>Horizontal line (hr) tag.</returns>
		[ExternalCallback("php_info_hr")]
		public static string PrintHr(bool print)
		{
			if (print) ScriptContext.CurrentContext.Output.Write(htmlHorizontalLine);
			return htmlHorizontalLine;
		}

		/// <summary>
		/// Prints table header occupying given number of columns.
		/// </summary>
		/// <param name="print">If true, the header is sent to output and returned, if false, the
		/// header is returned.</param>
		/// <param name="columnCount">The number of columns.</param>
		/// <param name="caption">The caption printed.</param>
		/// <returns>The table header.</returns>
		[ExternalCallback("php_info_print_table_colspan_header")]
		public static string PrintTableColspanHeader(bool print, int columnCount, string caption)
		{
			string ret = String.Format(htmlTableColspanHeader, columnCount, caption);
			if (print) ScriptContext.CurrentContext.Output.Write(ret);
			return ret;
		}

		/// <summary>
		/// Prints table header having several columns. 
		/// </summary>
		/// <param name="print">If true, the header is sent to output and returned, if false, the
		/// header is returned.</param>
		/// <param name="cells">Captions of columns.</param>
		/// <returns>The table header.</returns>
		[ExternalCallback("php_info_print_table_header")]
		public static string PrintTableHeader(bool print, params string[] cells)
		{
			string ret = HtmlHeaderRow(cells);
			if (print) ScriptContext.CurrentContext.Output.Write(ret);
			return ret;
		}

		/// <summary>
		/// Prints table row having several columns. 
		/// </summary>
		/// <param name="print">If true, the row is sent to output and returned, if false, the
		/// row is returned.</param>
		/// <param name="cells">Cells' content.</param>
		/// <returns>The table row.</returns>
		[ExternalCallback("php_info_print_table_row")]
		public static string PrintTableRow(bool print, params string[] cells)
		{
			string ret = HtmlRow(true, cells);
			if (print) ScriptContext.CurrentContext.Output.Write(ret);
			return ret;
		}

		[ExternalCallback("php_info_print_css")]
		public static string PrintCss(bool print)
		{
			if (print) ScriptContext.CurrentContext.Output.Write(htmlCss);
			return htmlCss;
		}

		[ExternalCallback("php_info_print_style")]
		public static string PrintStyle(bool print)
		{
			string ret = String.Format(htmlStyle, htmlCss);
			if (print) ScriptContext.CurrentContext.Output.Write(ret);
			return ret;
		}

		#endregion
	}

	#region Version

    /// <summary>
    /// Provides version information of Phalanger runtime.
    /// </summary>
    public static class PhalangerVersion
    {
        /// <summary>
        /// Current Phalanger version obtained from <see cref="AssemblyFileVersionAttribute"/> or version of this assembly.
        /// </summary>
        public static readonly string/*!*/Current;

        /// <summary>
        /// Phalanger name obtained from <see cref="AssemblyProductAttribute"/>.
        /// </summary>
        public static readonly string/*!*/ProductName;

        static PhalangerVersion()
        {
            var/*!*/ass = typeof(PhalangerVersion).Assembly;

            object[] attrsPhalangerVer = ass.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
            Current = attrsPhalangerVer.Length > 0
                ? ((AssemblyFileVersionAttribute)attrsPhalangerVer[0]).Version
                : ass.GetName().Version.ToString(4);

            object[] attrsPhalangerProduct = ass.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            Debug.Assert(attrsPhalangerProduct.Length > 0);
            ProductName = ((AssemblyProductAttribute)attrsPhalangerProduct[0]).Product;
        }
    }

	/// <summary>
	/// Provides means for working with PHP version as well as the currently supported version.
	/// </summary>
	public sealed class PhpVersion
	{
        /// <summary>
        /// Currently supported PHP major version.
        /// </summary>
        public const int Major = 5;

        /// <summary>
        /// Currently supported PHP minor version.
        /// </summary>
        public const int Minor = 3;

        /// <summary>
        /// Currently supported PHP release version.
        /// </summary>
        public const int Release = 10;

		/// <summary>
		/// Currently supported PHP version.
		/// </summary>
		public static readonly string Current = Major + "." + Minor + "." + Release;

        /// <summary>
        /// Extra version string.
        /// </summary>
        public const string Extra = "phalanger";

		/// <summary>
		/// Currently supported Zend Engine version.
		/// </summary>
		public const string Zend = "2.0.0";

		/// <summary>
		/// Compares parts of varsions delimited by '.'.
		/// </summary>
		/// <param name="part1">A part of the first version.</param>
		/// <param name="part2">A part of the second version.</param>
		/// <returns>The result of parts comparison (-1,0,+1).</returns>
		private static int CompareParts(string part1, string part2)
		{
			string[] parts = { "dev", "alpha", "a", "beta", "b", "RC", " ", "#", "pl", "p" };
			int[] order = { -1, 0, 1, 1, 2, 2, 3, 4, 5, 6, 6 };

			// GENERICS:
			int i = Array.IndexOf(parts, part1);
			int j = Array.IndexOf(parts, part2);
			return Math.Sign(order[i + 1] - order[j + 1]);
		}

		/// <summary>
		/// Parses a version and splits it into an array of parts.
		/// </summary>
		/// <param name="version">The version to be parsed (can be a <B>null</B> reference).</param>
		/// <returns>An array of parts.</returns>
		/// <remarks>
		/// Non-alphanumeric characters are eliminated.
		/// The version is split in between a digit following a non-digit and by   
		/// characters '.', '-', '+', '_'. 
		/// </remarks>
		private static string[] VersionToArray(string version)
		{
			if (version == null || version.Length == 0)
				return ArrayUtils.EmptyStrings;

			StringBuilder sb = new StringBuilder(version.Length);
			char last = '\0';

			for (int i = 0; i < version.Length; i++)
			{
				if (version[i] == '-' || version[i] == '+' || version[i] == '_' || version[i] == '.')
				{
					if (last != '.') sb.Append(last = '.');
				}
				else if (i > 0 && (Char.IsDigit(version[i]) ^ Char.IsDigit(version[i - 1])))
				{
					if (last != '.') sb.Append('.');
					sb.Append(last = version[i]);
				}
				else if (Char.IsLetterOrDigit(version[i]))
				{
					sb.Append(last = version[i]);
				}
				else
				{
					if (last != '.') sb.Append(last = '.');
				}
			}

			if (last == '.') sb.Length--;

			return sb.ToString().Split('.');
		}

		/// <summary>
		/// Compares two PHP versions.
		/// </summary>
		/// <param name="ver1">The first version.</param>
		/// <param name="ver2">The second version.</param>
		/// <returns>The result of comparison (-1,0,+1).</returns>
		public static int Compare(string ver1, string ver2)
		{
			string[] v1 = VersionToArray(ver1);
			string[] v2 = VersionToArray(ver2);
			int result;

			for (int i = 0; i < Math.Max(v1.Length, v2.Length); i++)
			{
				string item1 = (i < v1.Length) ? v1[i] : " ";
				string item2 = (i < v2.Length) ? v2[i] : " ";

				if (Char.IsDigit(item1[0]) && Char.IsDigit(item2[0]))
				{
					result = PhpComparer.CompareInteger(Convert.StringToInteger(v1[i]), Convert.StringToInteger(item2));
				}
				else
				{
					result = CompareParts(Char.IsDigit(item1[0]) ? "#" : item1, Char.IsDigit(item2[0]) ? "#" : item2);
				}

				if (result != 0)
					return result;
			}

			return 0;
		}

		/// <summary>
		/// Compares two PHP versions using a specified operator.
		/// </summary>
		/// <param name="ver1">The first version.</param>
		/// <param name="ver2">The second version.</param>
		/// <param name="op">
		/// The operator (supported are: "&lt;","lt";"&lt;=","le";"&gt;","gt";"&gt;=","ge";"==","=","eq";"!=","&lt;&gt;","ne").
		/// </param>
		/// <returns>The result of the comparison.</returns>
		public static object Compare(string ver1, string ver2, string op) // GENERICS: return value: bool?
		{
			switch (op)
			{
				case "<":
				case "lt": return Compare(ver1, ver2) < 0;

				case "<=":
				case "le": return Compare(ver1, ver2) <= 0;

				case ">":
				case "gt": return Compare(ver1, ver2) > 0;

				case ">=":
				case "ge": return Compare(ver1, ver2) >= 0;

				case "==":
				case "=":
				case "eq": return Compare(ver1, ver2) == 0;

				case "!=":
				case "<>":
				case "ne": return Compare(ver1, ver2) != 0;
			}
			return null;
		}

		#region Unit Testing
#if DEBUG

		public static void Test()
		{
			Console.WriteLine("Version to array:");
			string[] vers = { "4.0.4", "5.0-1RC", "abc099sdf2-+........3...", "4.3.2RC1" };
			foreach (string ver in vers)
				Console.WriteLine(String.Join(";", VersionToArray(ver)));

			Console.WriteLine("\nComparation of Parts:");
			string[] parts = { "#", "RC", "p", "dev", "devxxx", "ssss" };
			foreach (string part1 in parts)
				foreach (string part2 in parts)
				{
					int r = CompareParts(part1, part2);
					Console.WriteLine("{0}{1}{2}", part1, (r < 0) ? "<" : (r > 0) ? ">" : "==", part2);
				}

			Console.WriteLine("\nComparation of Versions:");
			vers = new string[] { "4.0.4", "5.0.1RC", "3", "5.0.1beta", "5.0.1", "5.0.1.0.0.0.0" };
			foreach (string ver1 in vers)
				foreach (string ver2 in vers)
				{
					int r = CompareParts(ver1, ver2);
					Console.WriteLine("{0}{1}{2}", ver1, (r < 0) ? "<" : (r > 0) ? ">" : "==", ver2);
				}

		}

#endif
		#endregion
	}

	#endregion
}
