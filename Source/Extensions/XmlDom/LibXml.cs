using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;

namespace PHP.Library.Xml
{
    public static class PhpLibXml
    {
        [ImplementsFunction("libxml_clear_errors")]
        public static void ClearErrors()
        {
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
        public static PhpArray GetErrors()
        {
            return new PhpArray();
        }

        [ImplementsFunction("libxml_get_last_error")]
        [return: CastToFalse]
        public static PhpObject GetLastError()
        {
            return null;
        }

        [ImplementsFunction("libxml_set_streams_context")]
        public static void SetStreamContexts(PhpResource streams_context)
        {
        }

        [ImplementsFunction("libxml_use_internal_errors")]
        public static bool UseInternalErrors()
        {
            return UseInternalErrors(true);
        }

        [ImplementsFunction("libxml_use_internal_errors")]
        public static bool UseInternalErrors(bool use_errors)
        {
            return false;
        }
    }
}
