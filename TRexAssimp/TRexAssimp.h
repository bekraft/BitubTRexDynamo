#pragma once

using namespace Bitub::Dto::Scene;
using namespace Bitub::Dto::Spatial;

#include <assimp/scene.h>

namespace TRexAssimp
{
	ref class TRexAssimp
	{
	internal:
		// Simple internal entity converters
		static aiColor3D AIColor3D(Color^ color, float% alpha);
		static aiMatrix3x3 AIMatrix3(Rotation^ r);
		static aiMatrix4x4 AIMatrix4(const aiMatrix3x3& r, const aiVector3D& o);
		static aiMatrix4x4 AIMatrix4(Transform^ t);
		static aiVector3D AIVector3D(XYZ^ xyz);
		static aiQuaternion AIQuaternion(Quaternion^ q);
	private:
		TRexAssimp() {}		
	};
}
