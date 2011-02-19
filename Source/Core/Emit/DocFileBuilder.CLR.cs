/*

 Copyright (c) 2005-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Text;

using PHP.Core;

namespace PHP.Core.Emit
{
	/// <summary>
	/// Implements generator of XML documentation.
	/// </summary>
	public sealed class XmlDocFileBuilder
	{
		/// <summary>
		/// Full canonical path to the generated file.
		/// </summary>
		private string/*!*/ path;

		private XmlTextWriter/*!*/ writer;

		public XmlDocFileBuilder(string/*!*/ path, string /*!*/assemblyName)
		{
			Debug.Assert(path != null && assemblyName != null);

			this.path = path;
			writer = new XmlTextWriter(path, Encoding.UTF8);
			writer.WriteStartDocument();

			writer.Formatting = Formatting.Indented;
			writer.Indentation = 1;
			writer.IndentChar = '\t';

			writer.WriteStartElement("doc");

			// assembly info:
			writer.WriteStartElement("assembly");
			writer.WriteElementString("name", assemblyName);
			writer.WriteEndElement();

			writer.WriteStartElement("members");
		}

		private void StartMember(string/*!*/ id)
		{
			writer.WriteStartElement("member");
			writer.WriteAttributeString("name", id);
		}

		public void WriteFunction(string/*!*/ clrName, string/*!*/ comment)
		{
			StartMember(String.Concat("M:", clrName));
			ProcessPhpDoc(comment);
			writer.WriteEndElement();
		}

		public void WriteType(string/*!*/ clrName, string/*!*/ comment)
		{
			StartMember(String.Concat("T:", clrName));
			ProcessPhpDoc(comment);
			writer.WriteEndElement();
		}

		public void WriteMethod(string/*!*/ clrName, string/*!*/ comment)
		{
			StartMember(String.Concat("M:", clrName));
			ProcessPhpDoc(comment);
			writer.WriteEndElement();
		}

		public void WriteField(string/*!*/ clrName, string/*!*/ comment)
		{
			StartMember(String.Concat("F:", clrName));
			ProcessPhpDoc(comment);
			writer.WriteEndElement();
		}

		public void WriteClassConstant(string/*!*/ clrName, string/*!*/ comment)
		{
			StartMember(String.Concat("F:", clrName));
			ProcessPhpDoc(comment);
			writer.WriteEndElement();
		}

		public void Dispose()
		{
			writer.WriteEndElement(); // </members>
			writer.WriteEndElement(); // </doc>
			writer.WriteEndDocument();

			writer.Close();
		}

		#region PHPdoc Parsing

		private void ReadEoln(string/*!*/ str, ref int pos)
		{
			if (str[pos] == '\n') pos++;
			else if (str[pos] == '\r' && str[pos + 1] == '\n') pos += 2;
		}

		private void ReadSpaces(string/*!*/ str, ref int pos)
		{
			while (Char.IsWhiteSpace(str, pos) && str[pos] != '\n' && str[pos] != '\r') pos++;
		}

		private string ReadWord(string/*!*/ str, ref int pos)
		{
			ReadSpaces(str, ref pos);

			int begin = pos;
			while (pos < str.Length - 2 && !Char.IsWhiteSpace(str, pos)) pos++;
			return str.Substring(begin, pos - begin);
		}

		private string ReadWord(string/*!*/ str, char first, ref int pos)
		{
			ReadSpaces(str, ref pos);

			if (str[pos] != first) return null;

			pos++;
			int begin = pos;
			while (pos < str.Length - 2 && !Char.IsWhiteSpace(str, pos)) pos++;
			return str.Substring(begin, pos - begin);
		}

		private string ReadLineToken(string/*!*/ str, ref int pos, out bool wholeLine)
		{
			// eats initial whitespace and optional '*':
			ReadSpaces(str, ref pos);
			if (str[pos] == '*') pos++;
			ReadSpaces(str, ref pos);

			// '@' at the beginning of the line means tag:
			if (str[pos] == '@')
			{
				wholeLine = false;
				return "";
			}

			// reads non-whitespace to substring [begin,end] and all whitespace up to the end of the line:
			int begin = pos;
			int end = pos - 1;
			while (pos < str.Length - 2 && str[pos] != '\n' && str[pos] != '\r')
			{
				// found inlined tag:
				// TODO:
				//        if (str[pos]=='{' && str[pos+1]=='@')
				//        {
				//          wholeLine = false;
				//          return str.Substring(begin,end - begin + 1);
				//        }  

				if (!Char.IsWhiteSpace(str[pos])) end = pos;
				pos++;
			}

			ReadEoln(str, ref pos);
			wholeLine = true;

			Debug.Assert(begin < str.Length);
			return str.Substring(begin, end - begin + 1);
		}

		/// <summary>
		/// Reads and writes until empty line or tag appear. Processes inlined tags as well.
		/// </summary>
		private void ProcessText(string/*!*/ str, ref int pos)
		{
			bool first_text = true;
			for (; ; )
			{
				bool whole_line;
				string line = ReadLineToken(str, ref pos, out whole_line);
				if (whole_line)
				{
					if (line == "") break;
					if (!first_text) writer.WriteWhitespace(" "); else first_text = false;
					writer.WriteRaw(line);
				}
				else
				{
					if (str[pos] == '@') break;
					ProcessInlineTag(str, ref pos);
				}
			}
		}

		private void ProcessTag(string/*!*/ str, ref int pos)
		{
			Debug.Assert(str[pos] == '@');

			int begin = pos;
			string tag = ReadWord(str, '@', ref pos);
			Debug.Assert(tag != null);

			switch (tag)
			{
				case "param":  	// @param type $varname description
					{
						writer.WriteStartElement("param");

						string type = ReadWord(str, ref pos); // TODO: check validity 
						string name = ReadWord(str, '$', ref pos); // TODO: check validity

						if (name == null) name = ""; // TODO

						writer.WriteAttributeString("name", name);
						writer.WriteAttributeString("type", type);

						ProcessText(str, ref pos);
						writer.WriteEndElement();
						break;
					}

				case "return": 	// @return type description
					{
						writer.WriteStartElement("returns");

						string type = ReadWord(str, ref pos); // TODO: check validity 
						ProcessText(str, ref pos);

						writer.WriteEndElement();
						break;
					}

				case "access": 	// @access [public|protected|private]
					// TODO check with real access
					break;

				case "see":		      // @see element(,element)*
				case "link":        // @link url
					// TODO
					break;

				case "version":	    // @version text
				case "copyright":	  // @copyright text
				case "author": 	    // @author text
				case "since":  	    // @since text
				case "deprecated":	// @deprecated text
				case "deprec":	    // @deprec text
				case "magic":	      // @magic text
				case "todo":		    // @todo text
				case "exception":	  // @exception text
				case "throws":  	  // @throws text
				case "var":		      // @var type
				case "package":	    // @package text
				case "subpackage":	// @subpackage text
					writer.WriteStartElement(tag);
					ProcessText(str, ref pos);
					writer.WriteEndElement();
					break;

				default:   // unknown tag: (warning?)
					writer.WriteStartElement(tag);
					ProcessText(str, ref pos);
					writer.WriteEndElement();
					break;
			}
		}

		private void ProcessInlineTag(string/*!*/ str, ref int pos)
		{
			Debug.Assert(str[pos] == '{' && str[pos + 1] == '@');

			pos++;

			switch (ReadWord(str, '@', ref pos))
			{
				case "link":

					break;

				case "see":
					break;
			}
		}

		private void ProcessPhpDoc(string/*!*/ comment)
		{
			const string start_mark = "/**";
			const string end_mark = "*/";
			const int state_init = -1;
			const int state_summary = 0;
			const int state_remarks = 1;
			const int state_tags = 2;

			Debug.Assert(comment != null && comment.Length >= start_mark.Length + end_mark.Length);
			Debug.Assert(comment.StartsWith(start_mark) && comment.EndsWith(end_mark));

			int pos = start_mark.Length;
			int state = state_init;
			int last_state = state_init;
			bool tag_open = false;

			do
			{
				bool whole_line;
				string line = ReadLineToken(comment, ref pos, out whole_line);

				if (whole_line)
				{
					switch (state)
					{
						case state_init:
							if (line != "")
							{
								last_state = state;
								state = state_summary;
								goto case state_summary;
							}
							break;

						case state_summary:
							if (line != "")
							{
								if (last_state != state_summary)
								{
									if (tag_open) writer.WriteEndElement();
									writer.WriteStartElement("summary");
									tag_open = true;
								}
								else
								{
									writer.WriteWhitespace(" ");
								}
								writer.WriteRaw(line);

								last_state = state;
								state = state_summary;
							}
							else
							{
								// switch to remarks:
								last_state = state;
								state = state_remarks;
							}
							break;

						case state_remarks:
							if (line != "")
							{
								if (last_state != state_remarks)
								{
									if (tag_open) writer.WriteEndElement();
									writer.WriteStartElement("remarks");
									tag_open = true;
								}
								else
								{
									writer.WriteWhitespace(" ");
								}
								writer.WriteRaw(line);

								last_state = state;
								state = state_summary;
							}
							break;

						case state_tags:
							last_state = state;
							state = state_remarks;
							goto case state_remarks;
					}
				}
				else
				{
					if (comment[pos] == '@')
					{
						// close current summary/remarks:
						if (tag_open)
						{
							writer.WriteEndElement();
							tag_open = false;
						}

						// switch to tags:
						last_state = state;
						state = state_tags;

						ProcessTag(comment, ref pos);
					}
					else
					{
						ProcessInlineTag(comment, ref pos);
					}
				}
			}
			while (pos < comment.Length - 2);

			// close any open tag:
			if (tag_open) writer.WriteEndElement();
		}

		#endregion
	}
}
