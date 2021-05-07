#pragma once
using namespace System;
using namespace System::Collections::Generic;

using namespace Bitub::Dto;
using namespace Bitub::Dto::Spatial;
using namespace Bitub::Dto::Scene;

#include <assimp/scene.h>
#include <assimp/Exporter.hpp>

namespace TRexAssimp
{
	public ref class TRexAssimpExport
	{
		Assimp::Exporter* exporter;
	public:
		TRexAssimpExport();
		virtual ~TRexAssimpExport();

		property array<String^>^ Extensions { array<String^>^ get(); }
		bool ExportTo(String^ filePathName, ComponentScene^ componentScene);

	private:
		const int GetOrCreateNodeAndParent(Component^ c, 
			std::vector<aiNode*>& nodes, 
			std::map<int, std::vector<int>>& children, 
			Dictionary<GlobalUniqueId^, unsigned int>^ nodeMap);

		const int GetOrCreateNode(GlobalUniqueId^ id,
			std::vector<aiNode*>& nodes,
			Dictionary<GlobalUniqueId^, unsigned int>^ nodeMap);

		const int CreateMesh(ShapeBody^ body, 
			std::vector<aiMesh*>& meshes);

		Dictionary<RefId^, unsigned int>^ CreateShapes(ComponentScene^ componentScene, 
			std::vector<aiMesh*>& meshes);

		Dictionary<RefId^, unsigned int>^ CreateMaterials(ComponentScene^ componentScene,
			aiScene& scene);

		aiMaterial* CreateMaterial(Material^ material);

		void SetColorChannel(ColorChannel channel, 
			aiMaterial* material, 
			aiColor3D& color, 
			float alpha);

		aiColor3D CreateColor3D(Color^ color, 
			float % alpha);

		aiMatrix4x4 CreateMat4x4(Transform^ t);
		aiVector3D CreateVec3D(XYZ^ xyz);
		aiQuaternion CreateQuat(Quaternion^ q);
	};
}