/*

 Copyright (c) 2004-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

//
// ExtSupport - substitute for php4ts.dll/php5ts.dll
//
// WrapperTypeDef.cpp
// - contains definition of FunctionTypeInfo class
// - contains definition of OverloadTypeInfo class
// - contains definition of WrapperTypeDef class
//

#include "stdafx.h"

#include "WrapperTypeDef.h"

using namespace System;
using namespace System::IO;
using namespace System::Xml;
using namespace System::Xml::XPath;
using namespace System::Text;
using namespace System::Collections::Generic;

using namespace PHP::Core;
using namespace PHP::ExtManager;

namespace PHP
{
	namespace ExtManager
	{
		// FunctionTypeInfo implementation:

		/// <summary>
		/// Adds a parameter type information (left-to-right order).
		/// </summary>
		/// <param name="param">The parameter.</param>
		/// <exception cref="ArgumentException">A mandatory parameter is being added after an optional
		/// parameter or vararg parameter is not the last one.</exception>
		void FunctionTypeInfo::AddParameter(ParameterTypeInfo ^param)
		{
			if (VarargParamDefined)
			{
				throw gcnew ArgumentException(ExtResources::GetString("invalid_vararg_param", Name), "param");
			}

			switch (param->Optional)
			{
				case OptionalFlag::Vararg:
					VarargParamDefined = true;
					break;

				case OptionalFlag::Yes:
					if (FirstOptionalParamIndex == -1) FirstOptionalParamIndex = Params->Count;
					break;

				case OptionalFlag::No:
					if (FirstOptionalParamIndex >= 0)
					{
						throw gcnew ArgumentException(ExtResources::GetString("invalid_optional_param", Name));
					}
					break;
			}
			Params->Add(param);
		}

		// Converts this instance to argfull overload type info.
		FunctionTypeInfo ^FunctionTypeInfo::ToArgFullSignature()
		{
			FunctionTypeInfo ^result = Clone();

			result->ReturnType = Object::typeid;
			result->Params = gcnew scg::List<ParameterTypeInfo ^>(Params->Count);

			// convert parameters
			for (int i = 0; i < Params->Count; i++)
			{
				if (Params[i]->Optional != OptionalFlag::Vararg)
				{
					ParameterTypeInfo ^info = Params[i]->Clone();
					info->ParamType = (info->IsOut ? PHP::Core::PhpReference::typeid : Object::typeid);

					result->Params->Add(info);
				}
			}

			result->VarargParamDefined = false;
			return result;
		}

		// OverloadTypeInfo implementation:

		// Creates a new empty OverloadTypeInfo.
		OverloadTypeInfo::OverloadTypeInfo()
		{
			MarshalBoundVars = MarshalBoundVarsFlag::None;
			BindParamMap = gcnew BitArray(0);
			ByrefParamMap = gcnew BitArray(0);
			ByrefVarargPosition = -1;
			VarargPosition = -1;
			CastToFalse = false;
			IsStatic = false;
			ReturnType = Object::typeid;
			neverCombined = true;
		}

		/// Fills this OverloadTypeInfo with information contained in a given FunctionTypeInfo.
		void OverloadTypeInfo::Load(FunctionTypeInfo ^info)
		{
			Name = info->Name;
			MarshalBoundVars = info->MarshalBoundVars;
			CastToFalse = info->CastToFalse;
			ReturnType = info->ReturnType;
			IsStatic = info->Static;

			// process parameters
			for (int j = 0; j < info->Params->Count; j++)
			{
				ParameterTypeInfo ^param_info = info->GetParameterByIndex(j);
				if (param_info->Bind)
				{
					if (j >= BindParamMap->Count) BindParamMap->Length = j + 1;
					BindParamMap->Set(j, true);
				}
				if (param_info->IsOut)
				{
					if (j >= ByrefParamMap->Count) ByrefParamMap->Length = j + 1;
					ByrefParamMap->Set(j, true);
					if (param_info->Optional == OptionalFlag::Vararg)
					{
						ByrefVarargPosition = j;
					}
				}
				if (param_info->Optional == OptionalFlag::Vararg)
				{
					VarargPosition = j;
				}
			}
		}

		// Combines this OverloadTypeInfo with information contained in a given FunctionTypeInfo.
		void OverloadTypeInfo::Combine(FunctionTypeInfo ^info)
		{
			if (neverCombined)
			{
				// initialize the AND-combined properties
				IsStatic = true;
				CastToFalse = true;
			}

			Name = info->Name;
			MarshalBoundVars = MarshalBoundVars | info->MarshalBoundVars;
			CastToFalse &= info->CastToFalse;
			IsStatic &= info->Static;
			
			if (neverCombined) ReturnType = info->ReturnType;
			else
			{
				if (!ReturnType->Equals(info->ReturnType)) ReturnType = Object::typeid;
			}

			// process parameters
			for (int j = 0; j < info->Params->Count; j++)
			{
				ParameterTypeInfo ^param_info = info->GetParameterByIndex(j);
				if (param_info->Bind)
				{
					if (j >= BindParamMap->Count) BindParamMap->Length = j + 1;
					BindParamMap->Set(j, true);
				}
				if (param_info->IsOut)
				{
					if (j >= ByrefParamMap->Count) ByrefParamMap->Length = j + 1;
					ByrefParamMap->Set(j, true);
					if (param_info->Optional == OptionalFlag::Vararg &&
						(ByrefVarargPosition == -1 || ByrefVarargPosition > j)) ByrefVarargPosition = j;
				}
				if (param_info->Optional == OptionalFlag::Vararg &&
					(VarargPosition == -1 || VarargPosition > j)) VarargPosition = j;
			}

			neverCombined = false;
		}

		// WrapperTypeDef implementation:

		// Reads type information for the extension from the typedef XML file.
		ModuleTypeInfo WrapperTypeDef::GetModuleTypeInfo(String ^fileName, StringBuilder ^message,
			int estimatedFunctionCount, int estimatedClassCount)
		{
			String ^xml_file_name;
		
			try
			{
				xml_file_name = Path::Combine(PHP::Core::Configuration::Application->Paths->ExtTypeDefs,
					String::Concat(fileName, ".xml"));
			}
			catch (ArgumentException ^)
			{
				message->Append(ExtResources::GetString("invalid_typedef_path",
					PHP::Core::Configuration::Application->Paths->ExtTypeDefs));
				return ModuleTypeInfo();
			}

			// read the XML file into DOM
			ModuleTypeInfo type_info = ModuleTypeInfo(
				gcnew Dictionary<String ^, FunctionTypeInfo ^>(estimatedFunctionCount),
				gcnew Dictionary<String ^, ClassTypeInfo ^>(estimatedClassCount),
				false);

			XmlReader ^reader;
			try
			{
				XmlReaderSettings ^settings = gcnew XmlReaderSettings();
				settings->ProhibitDtd = false;
				settings->ValidationType = ValidationType::DTD;

				//reader = gcnew XmlValidatingReader(gcnew XmlTextReader(xml_file_name));
				reader = XmlReader::Create(gcnew XmlTextReader(xml_file_name), settings);
				
				try
				{
					XmlDocument ^xml_doc = gcnew XmlDocument();
					xml_doc->Load(reader);

					XPathNavigator ^navigator = xml_doc->CreateNavigator();

					// determine the earlyInit attribute
					XmlNode ^attr = xml_doc->DocumentElement->Attributes->GetNamedItem("earlyInit");
					type_info.EarlyInit = (attr != nullptr && attr->Value->CompareTo("true") == 0);

					// iterate over functions
					XPathNodeIterator ^iterator = navigator->Select("/module/function");
					while (iterator->MoveNext())
					{
						// get type information for this function
						GetFunctionTypeInfo(iterator->Current->Clone(), type_info.Functions);
					}

					// iterate over classes
					iterator = navigator->Select("/module/class");
					while (iterator->MoveNext())
					{
						XPathNavigator ^cls_nav = iterator->Current->Clone();
						
						ClassTypeInfo ^class_type_info = gcnew ClassTypeInfo();
						class_type_info->Name = cls_nav->GetAttribute("name", cls_nav->NamespaceURI);
						
						class_type_info->ArrayGetter = cls_nav->GetAttribute("arrayGetter", cls_nav->NamespaceURI);
						class_type_info->ArraySetter = cls_nav->GetAttribute("arraySetter", cls_nav->NamespaceURI);
						if (class_type_info->ArrayGetter->Length == 0) class_type_info->ArrayGetter = nullptr;
						if (class_type_info->ArraySetter->Length == 0) class_type_info->ArraySetter = nullptr;

						class_type_info->Methods = gcnew Dictionary<String ^, FunctionTypeInfo ^>();

						// iterate over methods
						if (cls_nav->MoveToFirstChild())
						{
							do
							{
								// get type information for this method
								GetFunctionTypeInfo(cls_nav->Clone(), class_type_info->Methods);
							}
							while (cls_nav->MoveToNext());
						}

						try
						{
							type_info.Classes->Add(class_type_info->Name->ToLower(), class_type_info);
						}
						catch (ArgumentException ^e)
						{
							throw gcnew Schema::XmlSchemaException(ExtResources::GetString("class_already_defined",
								class_type_info->Name, xml_file_name), e);
						}
					}
				}
				catch(Exception ^ex)
				{
					throw gcnew Exception("Unable to load XML typedef.", ex);
				}
				finally
				{
					reader->Close();
				}
			}
			catch (FileNotFoundException ^)
			{
				message->Append(ExtResources::GetString("typedef_not_found", xml_file_name));
				return ModuleTypeInfo();
			}
			catch (Schema::XmlSchemaException ^e)
			{
				message->Append(ExtResources::GetString("typedef_not_validated", xml_file_name, e->Message));
				return ModuleTypeInfo();
			}
			catch (Exception ^e)
			{
				message->Append(ExtResources::GetString("typedef_not_parsed", xml_file_name, e->Message));
#ifdef DEBUG
				Debug::WriteLine("EXT SUP", e->ToString());
#endif
				return ModuleTypeInfo();
			}

			return type_info;
		}

		// Adds function type info to the parent type info dictionary.
		void WrapperTypeDef::AddFunctionTypeInfo(FunctionTypeInfo ^functionInfo,
			Dictionary<String ^, FunctionTypeInfo ^> ^typeInfo)
		{
			String ^func_name_lower = functionInfo->Name->ToLower();

			FunctionTypeInfo ^orig_type_info;
			if (typeInfo->TryGetValue(func_name_lower, orig_type_info))
			{
				// we have more function nodes with the same name -> add this one to the linked list
				functionInfo->Next = orig_type_info->Next;
				orig_type_info->Next = functionInfo;
			}
			else typeInfo->Add(func_name_lower, functionInfo);
		}

		// Reads type information for a function or method from the typedef XML file.
		void WrapperTypeDef::GetFunctionTypeInfo(XPathNavigator ^funNav, Dictionary<String ^, FunctionTypeInfo ^> ^typeInfo)
		{
			FunctionTypeInfo ^func_descr = gcnew FunctionTypeInfo();
			String ^func_name;

			// get function attributes
			func_name = funNav->GetAttribute("name", funNav->NamespaceURI); //->ToLower();
			func_descr->Description = funNav->GetAttribute("description", funNav->NamespaceURI);

			// returnType
			TypeTriple ret_triple;
			if (!typeMapping->TryGetValue(funNav->GetAttribute("returnType", funNav->NamespaceURI), ret_triple))
			{
				ret_triple = typeMapping["mixed"];
			}

			func_descr->ReturnType = ret_triple.T;

			// convert "" into NULL
			if (String::Empty->CompareTo(func_name) == 0) func_name = nullptr;
			if (String::Empty->CompareTo(func_descr->Description) == 0) func_descr->Description = nullptr;
		
			// castToFalse
			String ^cast_false = funNav->GetAttribute("castToFalse", funNav->NamespaceURI);
			if (cast_false->CompareTo("true") == 0) func_descr->CastToFalse = true;

			// castToFalse
			String ^is_static = funNav->GetAttribute("static", funNav->NamespaceURI);
			if (is_static->CompareTo("true") == 0) func_descr->Static = true;

			// marshalBoundVars
			String ^marshal_vars = funNav->GetAttribute("marshalBoundVars", funNav->NamespaceURI);
			if (marshal_vars->CompareTo("in") == 0) func_descr->MarshalBoundVars = MarshalBoundVarsFlag::In;
			else if (marshal_vars->CompareTo("out") == 0) func_descr->MarshalBoundVars = MarshalBoundVarsFlag::Out;
			else if (marshal_vars->CompareTo("inout") == 0) func_descr->MarshalBoundVars = MarshalBoundVarsFlag::InOut;

			if (func_name != nullptr)
			{
				func_descr->Name = func_name;
				AddFunctionTypeInfo(func_descr, typeInfo);

				// iterate over this function's parameters
				if (funNav->MoveToFirstChild())
				{
					do
					{
						if (funNav->Name->CompareTo("alias") == 0)
						{
							// alias subelement
							func_descr = func_descr->Clone();
							func_descr->Name = funNav->GetAttribute("name", funNav->NamespaceURI);

							AddFunctionTypeInfo(func_descr, typeInfo);
						}
						else
						{
							// param subelement
							ParameterTypeInfo ^param_descr = gcnew ParameterTypeInfo();

							// get parameter attributes
							param_descr->ParamName = funNav->GetAttribute("name", funNav->NamespaceURI);

							String ^opt = funNav->GetAttribute("optional", funNav->NamespaceURI);
							if (opt->CompareTo("true") == 0) param_descr->Optional = OptionalFlag::Yes;
							else if (opt->CompareTo("vararg") == 0) param_descr->Optional =	OptionalFlag::Vararg;
							
							String ^direction = funNav->GetAttribute("direction", funNav->NamespaceURI);
							if (direction->CompareTo("inout") == 0) param_descr->Direction = DirectionFlag::InOut;
							else if (direction->CompareTo("out") == 0) param_descr->Direction =	DirectionFlag::Out;
							else param_descr->Direction = DirectionFlag::In;

							String ^bind = funNav->GetAttribute("bind", funNav->NamespaceURI);
							if (bind->CompareTo("true") == 0)
							{
								param_descr->Bind = true;
								if (param_descr->Optional == OptionalFlag::Vararg)
								{
									param_descr->ParamType = array<PhpReference ^>::typeid;
								}
								else param_descr->ParamType = PhpReference::typeid;
							}
							else
							{
								String ^type_name = funNav->GetAttribute("type", funNav->NamespaceURI);

								// pick the right type mapping from hashtable
								TypeTriple triple;
								if (!typeMapping->TryGetValue(type_name, triple)) triple = typeMapping["mixed"];

								if (param_descr->Optional == OptionalFlag::Vararg)
								{
									param_descr->ParamType = triple.Tarr;
								}
								else
								{
									if (param_descr->IsOut) param_descr->ParamType = triple.Tref;
									else param_descr->ParamType = triple.T;
								}
							}

							func_descr->AddParameter(param_descr);
						}
					}
					while (funNav->MoveToNext());
				}
			}
		}
	}
}
