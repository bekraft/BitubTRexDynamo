#pragma once

using namespace Bitub::Dto::Spatial;
using namespace Bitub::Dto::Scene;

using namespace ::TRex::Export;

#include <assimp/scene.h>

namespace TRexAssimp
{
	public ref class TRexAssimpPreferences
	{
		float scale;
		bool separateContextScenes;

		Transform^ transform;		
		
	public:
		TRexAssimpPreferences();
		TRexAssimpPreferences(::TRex::Geom::CRSTransform ^ t, UnitScale ^ s);
		
		property float Scale 
		{ 
			float get() { return scale; }
			void set(float s) { scale = s; }
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
		aiMatrix4x4 GetTransform();

	};
}

