#pragma once

using namespace Bitub::Dto::Spatial;
using namespace Bitub::Dto::Scene;

using namespace ::TRex::Export;
using namespace ::TRex::Geom;

#include <assimp/scene.h>

namespace TRexAssimp
{
	public ref class TRexAssimpPreferences
	{
		float fScale;
		bool bSeparateContextScenes;
		bool bExportSceneWCS;

		Transform^ transform;

		aiMetadata* metadata;
		
	public:
		TRexAssimpPreferences();
		TRexAssimpPreferences(CRSTransform ^ t, UnitScale ^ s);
		virtual ~TRexAssimpPreferences();

		property bool IsExportingSceneWCS
		{
			bool get() { return bExportSceneWCS; }
			void set(bool isExportingWCS) { bExportSceneWCS = isExportingWCS; }
		}
		
		property float Scale 
		{ 
			float get() { return fScale; }
			void set(float s) { fScale = s; }
		}
		
		property XYZ^ Up 
		{ 
			XYZ^ get() { return transform->R->Ry; }
			void set(XYZ^ up) { transform->R->Ry = up; }
		}

		property XYZ^ Forward 
		{ 
			XYZ^ get() { return transform->R->Rz; }
			void set(XYZ^ forward) { transform->R->Rz = forward; }
		}

		property XYZ^ Right 
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
		void InitMetadata(GlobalReferenceAxis up, GlobalReferenceAxis forward, GlobalReferenceAxis right);
		aiMatrix4x4 GetTransform();
		aiMetadata* CreateMetadata();

		template <typename T> int sgn(T val) {
			return (T(0) < val) - (val < T(0));
		}
	};
}

