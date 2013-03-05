using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web.Services.Configuration;
using System.Web.Services.Protocols;

namespace PHP.Library.Soap
{
    /// <summary>
    /// Summary description for PipelineConfiguration.
    /// </summary>
    internal class PipelineConfiguration
    {
        /// <summary>
        /// Injects the extension.
        /// </summary>
        /// <param name="extension">Extension.</param>
        internal static void InjectExtension(Type extension)
        {
            try
            {
                RegisterSoapExtension(extension, 1, PriorityGroup.High);
            }
            catch (Exception ex)
            {
                throw new PipelineConfigurationException("Problem occured when trying to inject SoapExtension into pipeline", ex);
            }
        }

        /// <summary>
        /// This code was taken from Mike Bouck's July 31st, 2005 blog post located here: http://blog.gatosoft.com/
        /// Many thanks to Mike for this updated .NET 2.0-compatible code.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="priority"></param>
        /// <param name="group"></param>
        private static void RegisterSoapExtension(Type type, int priority, PriorityGroup group)
        {
            if (!type.IsSubclassOf(typeof(SoapExtension)))
            {
                throw new ArgumentException("Type must be derived from SoapException.", "type");
            }

            if (priority < 1)
            {
                throw new ArgumentOutOfRangeException("priority", priority, "Priority must be greater or equal to 1.");
            }

            // get the current web services settings...
            WebServicesSection wss = WebServicesSection.Current;

            // set SoapExtensionTypes collection to read/write...
            FieldInfo readOnlyField = typeof(System.Configuration.ConfigurationElementCollection).GetField("bReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            readOnlyField.SetValue(wss.SoapExtensionTypes, false);

            // inject SoapExtension...
            wss.SoapExtensionTypes.Add(new SoapExtensionTypeElement(type, priority, group));

            // set SoapExtensionTypes collection back to readonly and clear modified flags...
            MethodInfo resetModifiedMethod = typeof(System.Configuration.ConfigurationElement).GetMethod("ResetModified", BindingFlags.NonPublic | BindingFlags.Instance);
            resetModifiedMethod.Invoke(wss.SoapExtensionTypes, null);
            MethodInfo setReadOnlyMethod = typeof(System.Configuration.ConfigurationElement).GetMethod("SetReadOnly", BindingFlags.NonPublic | BindingFlags.Instance);
            setReadOnlyMethod.Invoke(wss.SoapExtensionTypes, null);
        }
    }
}
