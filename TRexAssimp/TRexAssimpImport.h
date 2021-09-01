#pragma once
using namespace System;
using namespace System::Collections::Generic;
using namespace Bitub::Dto::Scene;

#include <assimp/scene.h>
#include <assimp/Importer.hpp>

namespace TRexAssimp
{
	public ref class TRexAssimpImport
	{
		// Assimp references
		Assimp::Importer* importer;

	public:
		TRexAssimpImport();
		virtual ~TRexAssimpImport();

		property array<String^>^ Extensions { array<String^>^ get(); }
		bool ImportFrom(String^ filePathName, System::Collections::IDictionary^ metaDataMap);

	private:
		MeshBody^ CreateMeshBody(const aiMesh* mesh);
		Component^ CreateComponent(const aiNode* node);
		Material^ CreateMaterial(const aiNode* node);
		List<Component^>^ CreateComponents(const aiScene* scene);
		List<Material^>^ CreateMaterials(const aiScene* scene);
	};
}

