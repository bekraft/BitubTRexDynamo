#pragma once
using namespace System;
using namespace System::Collections::Generic;

using namespace Bitub::Dto;
using namespace Bitub::Dto::Spatial;
using namespace Bitub::Dto::Scene;

using namespace ::TRex::Export;

#include "TRexAssimpPreferences.h"

#include <assimp/scene.h>
#include <assimp/Exporter.hpp>

typedef ai_uint uint;

namespace TRexAssimp
{
	public ref class TRexAssimpExport
	{
		Assimp::Exporter* exporter;
		String^ statusMessage;
		TRexAssimpPreferences^ preferences;

	public:
		TRexAssimpExport();
		TRexAssimpExport(TRexAssimpPreferences^ p);

		virtual ~TRexAssimpExport();
		!TRexAssimpExport();

		property array<Format^>^ Formats { array<Format^>^ get(); }
		property String^ StatusMessage { String^ get() { return statusMessage; } }
		bool ExportTo(ComponentScene^ componentScene, String^ filePathName, Format^ format);

	private:
		array<Format^>^ GetAvailableFormats(
			Assimp::Exporter* exporter);

		const uint GetOrCreateNodeAndParent(
			Component^ c, 
			std::vector<aiNode*>& nodes, 
			std::map<uint, std::vector<uint>>& children, 
			Dictionary<GlobalUniqueId^, uint>^ nodeMap);

		const void LocalizeSceneTransforms(aiNode* parent, const aiMatrix4x4 transform);

		std::vector<uint>& GetOrCreateChildIndex(
			std::map<uint, std::vector<uint>>& mChildren,
			const uint idx_parent_node);

		const uint GetOrCreateNode(
			GlobalUniqueId^ id,
			std::vector<aiNode*>& nodes,
			Dictionary<GlobalUniqueId^, uint>^ nodeMap);

		// Returns a new mesh
		aiMesh* CreateMesh(ShapeBody^ body, 
			const uint materialId);

		// Returns an index into scene mesh array
		const uint CreateMaterialMesh(
			ShapeBody^ shapeBody,
			std::vector<aiMesh*>& vMeshes,
			std::map<uint, std::map<uint, uint>>& mMeshMaterialIndex,
			const uint materialIndex,
			const int keyMeshIndex);

		Dictionary<RefId^, uint>^ CreateMaterials(
			ComponentScene^ componentScene,
			aiScene& scene);

		aiMaterial* CreateMaterial(Material^ material);

		void SetColorChannel(
			ColorChannel channel, 
			aiMaterial* material, 
			aiColor3D& color, 
			float alpha);

		// Generic helper methods
		template<typename T>
		T* push_to(const std::vector<T>& v, uint& size_variable)
		{
			size_variable = (uint)v.size();
			if (0 < size_variable)
			{
				T* buffer = new T[size_variable];
				std::copy(v.begin(), v.end(), buffer);
				return buffer;
			}
			else
			{
				return nullptr;
			}
		}
	};
}