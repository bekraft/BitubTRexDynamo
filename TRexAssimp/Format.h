#pragma once
using namespace System;
using namespace System::Collections::Generic;

#include <assimp/cexport.h>
#include <assimp/cimport.h>

namespace TRexAssimp
{
	public ref class Format
	{
		String^ id;
		String^ extension;
		String^ description;

	internal:
		static Format^ FromAIExport(const aiExportFormatDesc* const descriptor);
		static IEnumerable<Format^>^ FilterByExtension(IEnumerable<Format^>^ formats, String^ extension);
		
	public:
		Format(String^ id, String^ ext, String^ descp);

		virtual property String^ Extension { String^ get() { return extension; } }
		virtual property String^ Descriptor { String^ get() { return extension; } }
		virtual property String^ ID { String^ get() { return extension; } }

		int GetHashCode() override { return String::IsNullOrWhiteSpace(ID) ? 0 : ID->GetHashCode(); }
		bool Equals(Format^ f) { return nullptr != f ? f->ID->Equals(ID) : false; }
		bool Equals(Object^ o) override;

		String^ ToString() override;
	};
}

