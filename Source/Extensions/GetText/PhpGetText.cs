/*

 Copyright (c) 2005-2011 Devsense.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using PHP.Core;
using PHP.Library.GetText.GetTextSharp;

namespace PHP.Library.GetText
{
    /// <summary>
    /// Implements PHP functions provided by gettext extension.
    /// </summary>
    [ImplementsExtension("gettext")]
    public static class PhpGetText
    {
        public static GettextResourceManager manager = null;

        #region bind_textdomain_codeset

        /// <summary>
        /// Specify the character encoding in which the messages from the DOMAIN message catalog will be returned.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="codeset"></param>
        /// <returns></returns>
        [ImplementsFunction("bind_textdomain_codeset", FunctionImplOptions.NotSupported)]
        public static string bind_textdomain_codeset(string domain, string codeset)
        {
            return "";
        }

        #endregion

        #region bindtextdomain

        /// <summary>
        /// Sets the path for a domain.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="directory"></param>
        /// <returns>The full pathname for the domain currently being set.</returns>
        [ImplementsFunction("bindtextdomain", FunctionImplOptions.NotSupported)]
        [return: CastToFalse]
        public static string bindtextdomain(string domain, string directory)
        {
            if (domain.Length == 0)
            {
                return null;
            }

            directory = Path.GetFullPath(directory);

            //directory = Path.Combine(ScriptContext.CurrentContext.WorkingDirectory, directory);

            if (!System.IO.Directory.Exists(directory))
            {
                return null;
            }

            manager = new GettextResourceManager(domain, directory, "{{resource}}.po");

            return directory;
        }

        #endregion

        #region dcgettext

        /// <summary>
        /// Overrides the domain for a single lookup.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="message"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        [ImplementsFunction("dcgettext", FunctionImplOptions.NotSupported)]
        public static string dcgettext(string domain, string message, int category)
        {
            /*string directory;

            if (manager.Path == null)
            {
                directory = manager.Path;
            }
            else
            {
                directory = Path.Combine(ScriptContext.CurrentContext.WorkingDirectory, "");
            }

            GettextResourceManager tempManager = new GettextResourceManager(domain, directory, "{{resource}}.po");

            tempManager.GetString(message);*/

            return "";
        }

        #endregion

        #region dcngettext

        /// <summary>
        /// Plural version of dcgettext.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="msgid1"></param>
        /// <param name="msgid2"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        [ImplementsFunction("dcngettext", FunctionImplOptions.NotSupported)]
        public static string dcngettext(string domain, string msgid1, string msgid2, int n)
        {
            return "";
        }

        #endregion

        #region dgettext

        /// <summary>
        /// Override the current domain for a single message lookup.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        [ImplementsFunction("dgettext", FunctionImplOptions.NotSupported)]
        public static string dgettext(string domain, string message)
        {
            return "";
        }

        #endregion

        #region dcngettext

        /// <summary>
        /// Plural version of dgettext.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="msgid1"></param>
        /// <param name="msgid2"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        [ImplementsFunction("dngettext", FunctionImplOptions.NotSupported)]
        public static string dngettext(string domain, string msgid1, string msgid2, int n)
        {
            return "";
        }

        #endregion

        #region gettext

        /// <summary>
        /// Lookup a message in the current domain
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        [ImplementsFunction("gettext")]
        [return: CastToFalse]
        public static string gettext(string message)
        {
            if (manager == null)
            {
                return message;
            }

            string text = manager.GetString(message);

            if (text == null)
            {
                return message;
            }
            else
            {
                return text;
            }
        }

        // This is alternative alias of gettext
        [ImplementsFunction("_")]
        [return: CastToFalse]
        public static string gettext_(string message)
        {
            return gettext(message);
        }

        #endregion

        #region ngettext

        /// <summary>
        /// Plural version of gettext
        /// </summary>
        /// <param name="msgid1"></param>
        /// <param name="msgid2"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        [ImplementsFunction("ngettext", FunctionImplOptions.NotSupported)]
        public static string ngettext(string msgid1, string msgid2, int n)
        {
            return "";
        }

        #endregion

        #region textdomain

        /// <summary>
        /// This function sets the domain to search within when calls are made to gettext(), usually the named after an application.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="directory"></param>
        /// <returns>The full pathname for the domain currently being set.</returns>
        [ImplementsFunction("textdomain", FunctionImplOptions.NotSupported)]
        [return: CastToFalse]
        public static string textdomain(string domain, string directory)
        {
            return "";
        }

        #endregion
    }
}
