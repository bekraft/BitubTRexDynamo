#pragma once
using namespace System;
using namespace System::Collections::Generic;

using namespace Bitub::Dto;
using namespace Bitub::Dto::Spatial;
using namespace Bitub::Dto::Scene;

#include <assimp/cexport.h>
#include "Format.h"

namespace TRexAssimp
{
	/// <summary>
	/// Format descriptor.
	/// </summary>
	public ref class FormatExport : public Format
	{
		aiExportFormatDesc* formatDescriptor;
	public:
		internal FormatExport();
		!FormatExport();
		virtual ~FormatExport();
	};
}
