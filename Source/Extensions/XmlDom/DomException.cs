/*

 Copyright (c) 2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;

using PHP.Core;

namespace PHP.Library.Xml
{
	#region ExceptionCode

	/// <summary>
	/// Enumerates <see cref="DOMException"/> codes.
	/// </summary>
	public enum ExceptionCode
	{
		/// <summary>
		/// Index or size is negative, or greater than the allowed value. 
		/// </summary>
		[ImplementsConstant("DOM_INDEX_SIZE_ERR")]
		IndexOutOfBounds = 1,

		/// <summary>
		/// The specified range of text does not fit into a string.
		/// </summary>
		[ImplementsConstant("DOMSTRING_SIZE_ERR")]
		StringTooLong = 2,

		/// <summary>
		/// A node is inserted somewhere it doesn't belong.
		/// </summary>
		[ImplementsConstant("DOM_HIERARCHY_REQUEST_ERR")]
		BadHierarchy = 3,

		/// <summary>
		/// A node is used in a different document than the one that created it.
		/// </summary>
		[ImplementsConstant("DOM_WRONG_DOCUMENT_ERR")]
		WrongDocument = 4,

		/// <summary>
		/// An invalid or illegal character is specified, such as in a name.
		/// </summary>
		[ImplementsConstant("DOM_INVALID_CHARACTER_ERR")]
		InvalidCharacter = 5,

		/// <summary>
		/// Data is specified for a node which does not support data.
		/// </summary>
		[ImplementsConstant("DOM_NO_DATA_ALLOWED_ERR")]
		DataNotAllowed = 6,

		/// <summary>
		/// An attempt is made to modify an object where modifications are not allowed.
		/// </summary>
		[ImplementsConstant("DOM_NO_MODIFICATION_ALLOWED_ERR")]
		DomModificationNotAllowed = 7,

		/// <summary>
		/// An attempt is made to reference a node in a context where it does not exist.
		/// </summary>
		[ImplementsConstant("DOM_NOT_FOUND_ERR")]
		NotFound = 8,

		/// <summary>
		/// The implementation does not support the requested type of object or operation.
		/// </summary>
		[ImplementsConstant("DOM_NOT_SUPPORTED_ERR")]
		NotSupported = 9,

		/// <summary>
		/// An attempt is made to add an attribute that is already in use elsewhere.
		/// </summary>
		[ImplementsConstant("DOM_INUSE_ATTRIBUTE_ERR")]
		AttributeInUse = 10,

		/// <summary>
		/// An attempt is made to use an object that is not, or is no longer, usable.
		/// </summary>
		[ImplementsConstant("DOM_INVALID_STATE_ERR")]
		InvalidState = 11,

		/// <summary>
		/// An invalid or illegal string is specified.
		/// </summary>
		[ImplementsConstant("DOM_SYNTAX_ERR")]
		SyntaxError = 12,

		/// <summary>
		/// An attempt is made to modify the type of the underlying object.
		/// </summary>
		[ImplementsConstant("DOM_INVALID_MODIFICATION_ERR")]
		ModificationNotAllowed = 13,

		/// <summary>
		/// An attempt is made to create or change an object in a way which is incorrect with
		/// regard to namespaces.
		/// </summary>
		[ImplementsConstant("DOM_NAMESPACE_ERR")]
		NamespaceError = 14,

		/// <summary>
		/// A parameter or an operation is not supported by the underlying object.
		/// </summary>
		[ImplementsConstant("DOM_INVALID_ACCESS_ERR")]
		InvalidAccess = 15,

		/// <summary>
		/// A call to a method such as <B>insertBefore</B> or <B>removeChild</B> would make the
		/// node invalid with respect to &quot;partial validity&quot;, this exception would be
		/// raised and the operation would not be done. 
		/// </summary>
		[ImplementsConstant("DOM_VALIDATION_ERR")]
		ValidationError = 16
	}

	#endregion

	/// <summary>
	/// The exception thrown by the DOM extension.
	/// </summary>
	[ImplementsType]
	public sealed partial class DOMException : PHP.Library.SPL.Exception
	{
		#region Fields and Properties

		private ExceptionCode _code;

		/// <summary>
		/// Returns the exception code.
		/// </summary>
		[PhpVisible]
		public new object code
		{
			get
			{ return (int)_code; }
		}

		#endregion

		#region Construction

		/// <summary>
		/// Creates a new instance (short constructor).
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public DOMException(ScriptContext context, bool newInstance)
			: base(context, newInstance)
		{ }

		#endregion

		#region Throw

		/// <summary>
		/// Throws a <see cref="DOMException"/> user exception with the given code.
		/// </summary>
		/// <param name="code">The exception code.</param>
		/// <exception cref="PhpUserException"/>
		internal static void Throw(ExceptionCode code)
		{
			string msg;

			switch (code)
			{
				case ExceptionCode.IndexOutOfBounds: msg = Resources.ErrorIndexOutOfBounds; break;
				case ExceptionCode.StringTooLong: msg = Resources.ErrorStringTooLong; break;
				case ExceptionCode.BadHierarchy: msg = Resources.ErrorBadHierarchy; break;
				case ExceptionCode.WrongDocument: msg = Resources.ErrorWrongDocument; break;
				case ExceptionCode.InvalidCharacter: msg = Resources.ErrorInvalidCharacter; break;
				case ExceptionCode.DataNotAllowed: msg = Resources.ErrorDataNotAllowed; break;
				case ExceptionCode.DomModificationNotAllowed: msg = Resources.ErrorDomModificationNotAllowed; break;
				case ExceptionCode.NotFound: msg = Resources.ErrorNotFound; break;
				case ExceptionCode.NotSupported: msg = Resources.ErrorNotSupported; break;
				case ExceptionCode.AttributeInUse: msg = Resources.ErrorAttributeInUse; break;
				case ExceptionCode.InvalidState: msg = Resources.ErrorInvalidState; break;
				case ExceptionCode.SyntaxError: msg = Resources.ErrorSyntaxError; break;
				case ExceptionCode.ModificationNotAllowed: msg = Resources.ErrorModificationNotAllowed; break;
				case ExceptionCode.NamespaceError: msg = Resources.ErrorNamespaceError; break;
				case ExceptionCode.InvalidAccess: msg = Resources.ErrorInvalidAccess; break;
				case ExceptionCode.ValidationError: msg = Resources.ErrorValidationError; break;

				default:
					throw new InvalidOperationException();
			}

			Throw(code, msg);
		}

		/// <summary>
		/// Throws a <see cref="DOMException"/> user exception with the given code and message.
		/// </summary>
		/// <param name="code">The exception code.</param>
		/// <param name="message">The exception message.</param>
		/// <exception cref="PhpUserException"/>
		internal static void Throw(ExceptionCode code, string message)
		{
			ScriptContext context = ScriptContext.CurrentContext;

			DOMException exception = new DOMException(context, true);
			exception.__construct(context, message, (int)code);
			exception._code = code;

			throw new PhpUserException(exception);
		}

		#endregion
	}
}
