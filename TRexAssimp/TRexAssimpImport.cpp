using namespace System::Collections::Generic;

#include <stack>
#include <assimp/Importer.hpp>
#include <msclr\marshal_cppstd.h>

#include "TRexAssimpImport.h"

TRexAssimp::TRexAssimpImport::TRexAssimpImport()
{
    importer = new Assimp::Importer();
}

TRexAssimp::TRexAssimpImport::~TRexAssimpImport()
{
    delete importer;
}

array<String^>^ TRexAssimp::TRexAssimpImport::Extensions::get()
{
    aiString s;
    importer->GetExtensionList(s);
    String^ str = gcnew String(s.C_Str());
    return str->Split(';');
}

bool TRexAssimp::TRexAssimpImport::ImportFrom(String^ filePathName, System::Collections::IDictionary^ metaDataMap)
{
    std::string stdFileName = msclr::interop::marshal_as<std::string>(filePathName);
    const aiScene* importedScene = importer->ReadFile(stdFileName, 0);
    List<Component^>^ components = CreateComponents(importedScene);
    return true;
}

MeshBody^ TRexAssimp::TRexAssimpImport::CreateMeshBody(const aiMesh* mesh)
{
    throw gcnew System::NotImplementedException();
    // TODO: hier return-Anweisung eingeben
}

Component^ TRexAssimp::TRexAssimpImport::CreateComponent(const aiNode* node)
{
    Component^ c = gcnew Component();
    c->Name = gcnew String(node->mName.C_Str());
    return c;
}

Material^ TRexAssimp::TRexAssimpImport::CreateMaterial(const aiNode* node)
{
    throw gcnew System::NotImplementedException();
    // TODO: hier return-Anweisung eingeben
}

List<Component^>^ TRexAssimp::TRexAssimpImport::CreateComponents(const aiScene* scene)
{
    List<Component^>^ components = gcnew List<Component^>(10);
    std::stack<aiNode*> nodeStack;
    nodeStack.push(scene->mRootNode);
    while (!nodeStack.empty()) 
    {
        const aiNode* node = nodeStack.top();
        nodeStack.pop();
        for (unsigned int i = 0; i < node->mNumChildren; i++)
        {
            nodeStack.push(node->mChildren[i]);
        }
        components->Add(CreateComponent(node));
    }
    return components;
}

List<Material^>^ TRexAssimp::TRexAssimpImport::CreateMaterials(const aiScene* scene)
{
    throw gcnew System::NotImplementedException();
    // TODO: hier return-Anweisung eingeben
}
