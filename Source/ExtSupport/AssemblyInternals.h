//
// ExtSupport - substitute for php4ts.dll/php5ts.dll
//
// AssemblyInternals.h 
// - contains definition of ExtResources class
//

#pragma once

#include "stdafx.h"

using namespace System;
using namespace System::Resources;

/*

	Designed and implemented by Tomas Matousek.

*/

namespace PHP
{
	namespace ExtManager
	{
		/// <summary>
		/// Manages resources of the ExtSupport and ExtManager.
		/// </summary>
		public ref class ExtResources sealed
		{
		private:
			/// <summary>
			/// Resource manager of strings.
			/// </summary>
			static ResourceManager ^strings;

			/// <summary>
			/// Initializes the resource manager.
			/// </summary>
			static ExtResources()
			{
				strings = gcnew ResourceManager("ExtManager.Strings",
					System::Reflection::Assembly::GetExecutingAssembly());
			}

		public:
			/// <summary>
			/// Retrieves a string resource.
			/// </summary>
			/// <param name="id">The string identifier.</param>
			/// <returns>The string.</returns>
			static String ^GetString(String ^id)
			{
				return strings->GetString(id);
			}

			/// <summary>
			/// Retrieves formatted string resource.
			/// </summary>
			/// <param name="id">The string identifier.</param>
			/// <param name="args">An <see cref="System.Object"/> array containing zero or more objects to format.</param>
			/// <returns>The formatted string resource.</returns>
			/// <exception cref="ArgumentNullException">The string resource with <paramref name="id"/> doesn't exist.</exception>
			/// <exception cref="FormatException">The string resource is not valid formatting string for specified arguments.</exception>
			static String ^GetString(String ^id, ... array<Object ^> ^args)
			{
				return String::Format(strings->GetString(id), args);
			}

			/// <summary>
			/// Retrieves formatted string resource.
			/// </summary>
			/// <param name="id">The string identifier.</param>
			/// <param name="arg">An <see cref="System.Object"/> to format.</param>
			/// <returns>The formatted string resource.</returns>
			/// <exception cref="ArgumentNullException">The string resource with <paramref name="id"/> doesn't exist.</exception>
			/// <exception cref="FormatException">The string resource is not valid formatting string for specified arguments.</exception>
			static String ^GetString(String ^id, Object ^arg)
			{
				return String::Format(strings->GetString(id), arg);
			}

			/// <summary>
			/// Retrieves formatted string resource.
			/// </summary>
			/// <param name="id">The string identifier.</param>
			/// <param name="arg1">An <see cref="System.Object"/> to format.</param>
			/// <param name="arg2">An <see cref="System.Object"/> to format.</param>
			/// <returns>The formatted string resource.</returns>
			/// <exception cref="ArgumentNullException">The string resource with <paramref name="id"/> doesn't exist.</exception>
			/// <exception cref="FormatException">The string resource is not valid formatting string for specified arguments.</exception>
			static String ^GetString(String ^id, Object ^arg1, Object ^arg2)
			{
				return String::Format(strings->GetString(id), arg1, arg2);
			}

			/// <summary>
			/// Retrieves formatted string resource.
			/// </summary>
			/// <param name="id">The string identifier.</param>
			/// <param name="arg1">An <see cref="System.Object"/> to format.</param>
			/// <param name="arg2">An <see cref="System.Object"/> to format.</param>
			/// <param name="arg3">An <see cref="System.Object"/> to format.</param>
			/// <returns>The formatted string resource.</returns>
			/// <exception cref="ArgumentNullException">The string resource with <paramref name="id"/> doesn't exist.</exception>
			/// <exception cref="FormatException">The string resource is not valid formatting string for specified arguments.</exception>
			static String ^GetString(String ^id, Object ^arg1, Object ^arg2, Object ^arg3)
			{
				return String::Format(strings->GetString(id), arg1, arg2, arg3);
			}
		};
	}
}
