#pragma once

using namespace Bitub::Dto::Spatial;
using namespace Bitub::Dto::Scene;

using namespace ::TRex::Log;
using namespace ::TRex::Export;
using namespace ::TRex::Geom;

#include <assimp/scene.h>

namespace TRexAssimp
{
	public ref class TRexAssimpPreferences
	{
		float fScale;
		bool bSeparateContextScenes;
		bool bUseSourceWCS;

		Transform^ transform;
		Logger^ logger;

		aiMetadata* metadata;
		
	public:
		TRexAssimpPreferences(Logger^ logger);
		TRexAssimpPreferences(Logger^ logger, CRSTransform ^ t, UnitScale ^ s);
		virtual ~TRexAssimpPreferences();

		property Logger^ Logger
		{
			TRex::Log::Logger^ get() { return logger; }
		}
		
		/// Indicates that the export shall respect the original scene WCS (if present)
		property bool IsUsingSourceWCS
		{
			bool get() { return bUseSourceWCS; }
			void set(bool isExportingWCS) { bUseSourceWCS = isExportingWCS; }
		}
		
		property float Scale 
		{ 
			float get() { return fScale; }
			void set(float s) { fScale = s; }
		}
		
		property XYZ^ Ry 
		{ 
			XYZ^ get() { return transform->R->Ry; }
			void set(XYZ^ up) { transform->R->Ry = up; }
		}

		property XYZ^ Rz 
		{ 
			XYZ^ get() { return transform->R->Rz; }
			void set(XYZ^ forward) { transform->R->Rz = forward; }
		}

		property XYZ^ Rx 
		{ 
			XYZ^ get() { return transform->R->Rx; }
			void set(XYZ^ right) { transform->R->Rx = right; }
		}

		property XYZ^ Translation
		{
			XYZ^ get() { return transform->T; }
			void set(XYZ^ t) { transform->T = t; }
		}

	internal:
		void InitMetadata();
		aiMatrix4x4 GetTransform();
		aiMetadata* CreateMetadata();

		template <typename T> int sgn(T val) {
			return (T(0) < val) - (val < T(0));
		}
	};
}

