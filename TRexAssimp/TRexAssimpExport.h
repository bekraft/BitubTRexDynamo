#pragma once
using namespace System;
using namespace System::Collections::Generic;

using namespace Bitub::Dto;
using namespace Bitub::Dto::Spatial;
using namespace Bitub::Dto::Scene;

using namespace ::TRex::Export;

#include <assimp/scene.h>
#include <assimp/Exporter.hpp>

typedef unsigned int uint;

namespace TRexAssimp
{
	public ref class TRexAssimpExport
	{
		Assimp::Exporter* exporter;
		String^ statusMessage;
	public:
		TRexAssimpExport();
		virtual ~TRexAssimpExport();
		!TRexAssimpExport();

		property array<Format^>^ Formats { array<Format^>^ get(); }
		property String^ StatusMessage { String^ get() { return statusMessage; } }
		bool ExportTo(ComponentScene^ componentScene, String^ filePathName, Format^ format);

	private:
		array<Format^>^ GetAvailableFormats(Assimp::Exporter* exporter);

		const uint GetOrCreateNodeAndParent(Component^ c, 
			std::vector<aiNode*>& nodes, 
			std::map<uint, std::vector<uint>>& children, 
			Dictionary<GlobalUniqueId^, uint>^ nodeMap);

		std::vector<uint>& GetOrCreateChildIndex(std::map<uint, std::vector<uint>>& mChildren,
			const uint idx_parent_node);

		const uint GetOrCreateNode(GlobalUniqueId^ id,
			std::vector<aiNode*>& nodes,
			Dictionary<GlobalUniqueId^, uint>^ nodeMap);

		const uint CreateMesh(ShapeBody^ body, 
			std::vector<aiMesh*>& meshes);

		const uint CreateMaterialMesh(std::vector<aiMesh*>& rawMeshes,
			std::map<uint, std::map<uint, uint>>& meshByMaterialIndex,
			std::vector<aiMesh*>& materialMeshes,
			uint rawMeshIndex,
			uint materialIndex);

		Dictionary<RefId^, uint>^ CreateShapes(ComponentScene^ componentScene, 
			std::vector<aiMesh*>& meshes);

		Dictionary<RefId^, uint>^ CreateMaterials(ComponentScene^ componentScene,
			aiScene& scene);

		aiMaterial* CreateMaterial(Material^ material);

		void SetColorChannel(ColorChannel channel, 
			aiMaterial* material, 
			aiColor3D& color, 
			float alpha);

		// Simple entity converters
		aiColor3D _aiColor3D(Color^ color, float% alpha);
		aiMatrix3x3 _aiMatrix3(Rotation^ r);
		aiMatrix4x4 _aiMatrix4(const aiMatrix3x3& r, const aiVector3D& o);
		aiMatrix4x4 _aiMatrix4(Transform^ t);
		aiVector3D _aiVector3D(XYZ^ xyz);
		aiQuaternion _aiQuaternion(Quaternion^ q);
	};
}