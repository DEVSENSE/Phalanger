using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.ComponentModel;
using PHP.Core.Reflection;

namespace PHP.Library.Xml
{
    #region LibXMLError
    /// <summary>
    /// Contains various information about errors thrown by libxml.
    /// </summary>
    [ImplementsType]
    public class LibXMLError : PhpObject
    {
        #region Properties

        // in PHP, it has runtime fields

        #endregion

        #region Constructor

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public LibXMLError(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public LibXMLError(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion
    }

    #endregion

    [ImplementsExtension("libxml")]
    public static class PhpLibXml
    {
        #region libxml constants

        /// <summary>
        /// Activate small nodes allocation optimization. This may speed up your application without needing to change the code.
        /// </summary>
        [ImplementsConstant("LIBXML_COMPACT")]
        public const int LIBXML_COMPACT = 65536;

        /// <summary>
        /// Default DTD attributes.
        /// </summary>
        [ImplementsConstant("LIBXML_DTDATTR")]
        public const int LIBXML_DTDATTR = 8;

        /// <summary>
        /// Load the external subset
        /// </summary>
        [ImplementsConstant("LIBXML_DTDLOAD")]
        public const int LIBXML_DTDLOAD = 4;

        /// <summary>
        /// Validate with the DTD.
        /// </summary>
        [ImplementsConstant("LIBXML_DTDVALID")]
        public const int LIBXML_DTDVALID = 16;

        /// <summary>
        /// Remove blank nodes.
        /// </summary>
        [ImplementsConstant("LIBXML_NOBLANKS")]
        public const int LIBXML_NOBLANKS = 256;

        /// <summary>
        /// Merge CDATA as text nodes.
        /// </summary>
        [ImplementsConstant("LIBXML_NOCDATA")]
        public const int LIBXML_NOCDATA = 16384;

        /// <summary>
        /// Expand empty tags (e.g. &lt;br/&gt; to &lt;br&gt;&lt;/br&gt;).
        /// </summary>
        [ImplementsConstant("LIBXML_NOEMPTYTAG")]
        public const int LIBXML_NOEMPTYTAG = 4;

        /// <summary>
        /// Substitute entities.
        /// </summary>
        [ImplementsConstant("LIBXML_NOENT")]
        public const int LIBXML_NOENT = 2;

        /// <summary>
        /// Suppress error reports.
        /// </summary>
        [ImplementsConstant("LIBXML_NOERROR")]
        public const int LIBXML_NOERROR = 32;

        /// <summary>
        /// Disable network access when loading documents.
        /// </summary>
        [ImplementsConstant("LIBXML_NONET")]
        public const int LIBXML_NONET = 2048;

        /// <summary>
        /// Suppress warning reports.
        /// </summary>
        [ImplementsConstant("LIBXML_NOWARNING")]
        public const int LIBXML_NOWARNING = 64;

        /// <summary>
        /// Drop the XML declaration when saving a document.
        /// </summary>
        [ImplementsConstant("LIBXML_NOXMLDECL")]
        public const int LIBXML_NOXMLDECL = 2;

        /// <summary>
        /// Remove redundant namespaces declarations.
        /// </summary>
        [ImplementsConstant("LIBXML_NSCLEAN")]
        public const int LIBXML_NSCLEAN = 8192;

        /// <summary>
        /// Sets XML_PARSE_HUGE flag, which relaxes any hardcoded limit from the parser.
        /// This affects limits like maximum depth of a document or the entity recursion, as well as limits of the size of text nodes.
        /// </summary>
        [ImplementsConstant("LIBXML_PARSEHUGE")]
        public const int LIBXML_PARSEHUGE = 524288;

        /// <summary>
        /// Implement XInclude substitution.
        /// </summary>
        [ImplementsConstant("LIBXML_XINCLUDE")]
        public const int LIBXML_XINCLUDE = 1024;

        /// <summary>
        /// A recoverable error.
        /// </summary>
        [ImplementsConstant("LIBXML_ERR_ERROR")]
        public const int LIBXML_ERR_ERROR = 2;

        /// <summary>
        /// A fatal error.
        /// </summary>
        [ImplementsConstant("LIBXML_ERR_FATAL")]
        public const int LIBXML_ERR_FATAL = 3;

        /// <summary>
        /// No errors.
        /// </summary>
        [ImplementsConstant("LIBXML_ERR_NONE")]
        public const int LIBXML_ERR_NONE = 0;

        /// <summary>
        /// A simple warning.
        /// </summary>
        [ImplementsConstant("LIBXML_ERR_WARNING")]
        public const int LIBXML_ERR_WARNING = 1;

        /// <summary>
        /// libxml version.
        /// </summary>
        [ImplementsConstant("LIBXML_VERSION")]
        public const int LIBXML_VERSION = -1;

        /// <summary>
        /// libxml version like 2.6.5 or 2.6.17.
        /// </summary>
        [ImplementsConstant("LIBXML_DOTTED_VERSION")]
        public const string LIBXML_DOTTED_VERSION = "";

        #endregion

        #region Nested class: LibXMLError

        /// <summary>
        /// Represents internal XML error.
        /// </summary>
        public class XmlError
        {
            public readonly int level, code, line, column;
            public readonly string message, file;

            public string LevelString
            {
                get
                {
                    switch (this.level)
                    {
                        case LIBXML_ERR_NONE: return "notice";
                        case LIBXML_ERR_WARNING: return "warning";
                        case LIBXML_ERR_ERROR: return "error";
                        case LIBXML_ERR_FATAL: return "fatal error";
                        default:
                            return null;
                    }
                }
            }

            public XmlError(int level, int code, int line, int column, string message, string file)
            {
                this.level = level;
                this.code = code;
                this.line = line;
                this.column = column;
                this.message = message;
                this.file = file;
            }

            /// <summary>
            /// Returns string representation of the error.
            /// </summary>
            public override string ToString()
            {
                if (this.file != null)
                    return string.Format("LibXml {4} ({0}): {1} in {2}, line: {3}", this.code, this.message, this.file, this.line, this.LevelString);
                else
                    return string.Format("LibXml {3} ({0}): {1} in Entity, line: {2}", this.code, this.message, this.line, this.LevelString);
            }

            /// <summary>
            /// Creates PHP <see cref="stdClass"/> with properties from current <see cref="XmlError"/> instance.
            /// </summary>
            /// <param name="context">Current <see cref="ScriptContext"/>.</param>
            /// <returns>An instance of <see cref="stdClass"/> with properties level, code, column, message, file, line.</returns>
            internal PhpObject/*!*/GetPhpErrorObject(ScriptContext/*!*/context)
            {
                return new LibXMLError(context, true)
                {
                    RuntimeFields = new PhpArray(6)
                    {
                        {"level", this.level},
                        {"code", this.code},
                        {"column", this.column},
                        {"message", this.message},
                        {"file", this.file},
                        {"line", this.line}
                    }
                };
            }
        }

        #endregion

        #region Fields

        [ThreadStatic]
        private static List<XmlError> error_list;

        [ThreadStatic]
        private static Action<XmlError> error_handler;

        #endregion

        #region Initialization

        static PhpLibXml()
        {
            // restores libxml at the request end,
            // clears error list and handlers:
            RequestContext.RequestEnd += () =>
                {
                    error_list = null;
                    error_handler = null;
                };
        }

        #endregion

        #region IssueXmlError

        /// <summary>
        /// Reports given <see cref="XmlError"/> using internal error handler or forwards the error to common error handler.
        /// </summary>
        /// <param name="err">Error to report;</param>
        public static void IssueXmlError(XmlError err)
        {
            if (err == null)
                return;

            if (error_handler != null)
            {
                error_handler(err);
            }
            else
            {
                PhpException.Throw(PhpError.Warning, err.ToString());
            }
        }

        #endregion

        #region libxml

        [ImplementsFunction("libxml_clear_errors")]
        public static void ClearErrors()
        {
            error_list = null;
        }

        [ImplementsFunction("libxml_disable_entity_loader")]
        public static bool DisableEntityLoader()
        {
            return DisableEntityLoader(true);
        }

        [ImplementsFunction("libxml_disable_entity_loader")]
        public static bool DisableEntityLoader(bool disable)
        {
            return false;
        }

        [ImplementsFunction("libxml_get_errors")]
        public static PhpArray/*!*/GetErrors(ScriptContext/*!*/context)
        {
            if (error_list == null)
                return new PhpArray();

            return new PhpArray(error_list.Select(x => x.GetPhpErrorObject(context)));
        }

        [ImplementsFunction("libxml_get_last_error")]
        [return: CastToFalse]
        public static PhpObject GetLastError(ScriptContext/*!*/context)
        {
            if (error_list == null || error_list.Count == 0)
                return null;

            return error_list[error_list.Count - 1].GetPhpErrorObject(context);
        }

        [ImplementsFunction("libxml_set_streams_context")]
        public static void SetStreamContexts(PhpResource streams_context)
        {
        }

        /// <summary>
        /// Disable libxml errors and allow user to fetch error information as needed.
        /// </summary>
        /// <returns>This function returns the previous value of use_errors.</returns>
        [ImplementsFunction("libxml_use_internal_errors")]
        public static bool UseInternalErrors()
        {
            return UseInternalErrors(false);
        }

        /// <summary>
        /// Disable libxml errors and allow user to fetch error information as needed.
        /// </summary>
        /// <param name="use_errors">Enable (TRUE) user error handling or disable (FALSE) user error handling. Disabling will also clear any existing libxml errors.</param>
        /// <returns>This function returns the previous value of <paramref name="use_errors"/>.</returns>
        [ImplementsFunction("libxml_use_internal_errors")]
        public static bool UseInternalErrors(bool use_errors)
        {
            bool previousvalue = error_handler != null;

            if (use_errors)
            {
                error_handler = (err) =>
                    {
                        if (error_list == null)
                            error_list = new List<XmlError>();

                        error_list.Add(err);
                    };
                //error_list = error_list;// keep error_list as it is
            }
            else
            {
                error_handler = null;   // outputs xml errors
                error_list = null;
            }

            return previousvalue;
        }

        #endregion
    }
}
