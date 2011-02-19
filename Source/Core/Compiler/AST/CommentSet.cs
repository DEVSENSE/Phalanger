using System;
using System.Collections.Generic;
using System.Text;

namespace PHP.Core.AST
{
	/// <summary>
	/// Specifies the type of a commentary.
	/// </summary>
	public enum CommentType
	{
		/// <summary>
		/// Single line comment.
		/// </summary>
		/// <example>
		/// // call of the foo function
		/// foo(...);
		/// </example>
		SingleLine,

		/// <summary>
		/// Multi line comment.
		/// </summary>
		/// <example>
		/// /*
		///	  Multiline comment
		///	  of foo call.
		///	*/
		///	foo(...); 
		/// </example>
		MultiLine,

		/// <summary>
		/// Documentation comment in the PhpDocumentor format.
		/// </summary>
		/// <example>
		///		<code>
		///		/**
		///		  * Summary of foo function documentation.
		///		  * @param int $a
		///		  * @param int $b
		///		  * @return bool
		///		  */
		///		function foo($a, $b)
		///		{
		///			return $a &lt; $b;
		///		}
		///		</code>
		/// </example>
		Documentation
	}

	/// <summary>
	/// Specifies the position of a comment.
	/// </summary>
	public enum CommentPosition
	{
		/// <summary>
		/// The comment stands before the language element.
		/// </summary>
		Before,

		/// <summary>
		/// The comment stands after the language element.
		/// </summary>
		After
	}

	/// <summary>
	/// Class representing a comment of a language element. Each language element is bound to comments before
	/// </summary>
	public struct Comment
	{
		/// <summary>
		/// Type of the comment.
		/// </summary>
		public CommentType Type { get { return type; } }
		private CommentType type;

		/// <summary>
		/// Position of the comment in relatively to language element owning this structure.
		/// </summary>
		public CommentPosition Position { get { return position; } }
		private CommentPosition position;
		
		/// <summary>
		/// Content text of the comment.
		/// </summary>
		public string Content { get { return content; } }
		private string content;

		/// <summary>
		/// Initializes a comment.
		/// </summary>
		/// <param name="type">Type of this comment.</param>
		/// <param name="position">Position of this comment.</param>
		/// <param name="content">Content text of this comment.</param>
		public Comment(CommentType type, CommentPosition position, string content)
		{
			this.type = type;
			this.position = position;
			this.content = content;
		}
	}


	/// <summary>
	/// Set of commentaries which is used as annotation of a language element.
	/// </summary>
	public class CommentSet
	{
		/// <summary>
		/// Gets the list of preceeding commentaries.
		/// </summary>
		public IList<Comment> Preceeding { get { return preceeding; } }
		private List<Comment> preceeding;

		/// <summary>
		/// Gets the list of succeeding commentaries.
		/// </summary>
		public IList<Comment> Succeeding { get { return succeeding; } }
		private List<Comment> succeeding;

		/// <summary>
		/// Gets the list of all commentaries.
		/// </summary>
		public IList<Comment> All { get { return all; } }
		private List<Comment> all;

		/// <summary>
		/// Constructs a new instance of CommentSet.
		/// </summary>
		/// <param name="comments">Comments to be associated with this CommentSet.</param>
		public CommentSet(IEnumerable<Comment> comments)
		{
			all = new List<Comment>(comments);
			preceeding = new List<Comment>();
			succeeding = new List<Comment>();

			foreach(Comment c in comments)
			{
				if (c.Position == CommentPosition.Before)
				{
					preceeding.Add(c);
				}
				else if (c.Position == CommentPosition.After)
				{
					succeeding.Add(c);
				}
				else
				{
					Debug.Assert(false); 
				}
			}
		}
	}
}
