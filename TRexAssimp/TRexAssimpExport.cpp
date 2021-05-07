#include <msclr\marshal_cppstd.h>

using namespace Bitub::Dto;
using namespace Bitub::Dto::Scene;

using namespace msclr::interop;

#include "TRexAssimpExport.h"

TRexAssimp::TRexAssimpExport::TRexAssimpExport()
{
    exporter = new Assimp::Exporter();
}

TRexAssimp::TRexAssimpExport::~TRexAssimpExport()
{
    delete exporter;
}

array<String^>^ TRexAssimp::TRexAssimpExport::Extensions::get()
{
    array<String^>^ extensions = gcnew array<String^>(exporter->GetExportFormatCount());
    for (size_t i = 0, end = extensions->Length; i < end; ++i) {
        const aiExportFormatDesc* const e = exporter->GetExportFormatDescription(i);
        String^ str = gcnew String(e->fileExtension);
        extensions[i] = str;
    }
    return extensions;
}

bool TRexAssimp::TRexAssimpExport::ExportTo(String^ filePathName, 
    ComponentScene^ componentScene)
{
    std::string stdFileName = marshal_as<std::string>(filePathName);
    aiScene scene;

    // Create meshes
    std::vector<aiMesh*> meshes;
    auto meshMap = CreateShapes(componentScene, meshes);

    // Create scene contexts
    auto contextWcsMap = gcnew Dictionary<Qualifier^, Transform^>(10);
    for (auto en_context = componentScene->Contexts->GetEnumerator(); en_context->MoveNext();)
    {
        SceneContext^ context = en_context->Current;
        contextWcsMap->Add(context->Name, context->Wcs);
    }

    // Create material map
    auto materialMap = CreateMaterials(componentScene, scene);

    // Create scene root
    scene.mRootNode = new aiNode;
    if (nullptr != componentScene->Metadata)
    {
        std::string projectName = marshal_as<std::string>(componentScene->Metadata->Name);
        scene.mRootNode->mName = aiString(projectName);
    }

    auto componentMap = gcnew Dictionary<GlobalUniqueId^, unsigned int>(10);
    std::vector<aiNode*> nodes;
    std::map<int, std::vector<int>> children;
    std::vector<aiNode*> topLevelNodes;

    int c_index = 0;

    // Create scene nodes and collect relations
    for (auto en = componentScene->Components->GetEnumerator(); en->MoveNext(); ++c_index)
    {
        auto c = en->Current;
        int index = GetOrCreateNodeAndParent(c, nodes, children, componentMap);
        aiNode* node = nodes[index];
        if (nullptr == node->mParent)
        {
            node->mParent = scene.mRootNode;
            topLevelNodes.push_back(node);
        }

        node->mNumMeshes = c->Shapes->Count;
        node->mMeshes = nullptr;
        if (0 < node->mNumMeshes)
        {   // If there are meshes, process them
            int m_index = 0;
            node->mMeshes = new unsigned int[node->mNumMeshes];

            for (auto en_shape = c->Shapes->GetEnumerator(); en_shape->MoveNext(); ++m_index)
            {
                Shape^ shape = en_shape->Current;
                Transform^ t = nullptr;
                unsigned int m_scene_index;
                if (meshMap->TryGetValue(shape->ShapeBody, m_scene_index))
                {   // Store global mesh index into local mesh index
                    node->mMeshes[m_index] = m_scene_index;
                }

                if (contextWcsMap->TryGetValue(shape->Context, t))
                {   // Get WCS transform
                    //aiMatrix4x4 t = CreateMat4x4(t);

                }
            }
        }
    }

    // Create relations    
    for (int i = 0; i < nodes.size(); ++i)
    {
        if (0 < children.count(i))
        {   // If relations exist
            std::vector<int>& childIndexes = children.at(i);
            aiNode** childNodes = new aiNode * [childIndexes.size()];

            for (auto it = childIndexes.begin(); it != childIndexes.end(); ++it)
            {
                childNodes[it - childIndexes.begin()] = nodes[*it];
            }
            nodes[i]->mChildren = childNodes;
            nodes[i]->mNumChildren = childIndexes.size();
        }
    }

    // Finally: set up top level nodes as children of root node
    scene.mRootNode->mNumChildren = topLevelNodes.size();
    scene.mRootNode->mChildren = new aiNode * [topLevelNodes.size()];
    std::copy(topLevelNodes.begin(), topLevelNodes.end(), scene.mRootNode->mChildren);

    return false;
}

const int TRexAssimp::TRexAssimpExport::GetOrCreateNodeAndParent(Component^ c,
    std::vector<aiNode*>& nodes, // the nodes
    std::map<int, std::vector<int>>& children, // parent-children relation per index
    Dictionary<GlobalUniqueId^, unsigned int>^ nodeMap) // DTO reference to index
{
    int index = GetOrCreateNode(c->Id, nodes, nodeMap);
    aiNode* node = nodes[index];
    std::string cName = msclr::interop::marshal_as<std::string>(c->Name);
    node->mName = aiString(cName);

    if (nullptr != c->Parent)
    {
        int pIndex = GetOrCreateNode(c->Parent, nodes, nodeMap);
        node->mParent = nodes[pIndex];
        if (0 == children.count(pIndex))
        {
            children.insert(std::make_pair(pIndex, std::vector<int>()));
        }

        children.at(pIndex).push_back(index);
    }

    return index;
}

/*
 * Get or create a node by global ID.
 */
const int TRexAssimp::TRexAssimpExport::GetOrCreateNode(GlobalUniqueId^ id, 
    std::vector<aiNode*>& nodes, 
    Dictionary<GlobalUniqueId^, unsigned int>^ nodeMap)
{
    aiNode* node;
    unsigned int index;
    if (nodeMap->TryGetValue(id, index))
    {
        node = nodes[index];
    }
    else
    {
        node = new aiNode;
        index = nodes.size();
        nodeMap->Add(id, index);
        nodes.push_back(node);
    }
    return index;
}

/*
 * Creates a single mesh from shape body and stores this into an existing mesh cache.
 */
const int TRexAssimp::TRexAssimpExport::CreateMesh(ShapeBody^ body, 
    std::vector<aiMesh*>& meshes)
{    
    auto subMeshes = body->GetContinuousFacets(0);
    auto allVertices = body->GetCoordinateTriples();
    const unsigned int countVertices = body->GetTotalPointCount();
    
    aiMesh* mesh = new aiMesh;
    mesh->mNumVertices = countVertices;
    mesh->mVertices = new aiVector3D[countVertices];

    // Transfer vertices
    int index = 0;
    for (auto en = allVertices->GetEnumerator(); en->MoveNext(); ++index)
    {
        aiVector3D& v = mesh->mVertices[index / 3];
        v[index % 3] = en->Current;
    }

    // Transfer meshes
    std::vector<aiFace> faces;
    for (auto subMesh = subMeshes->GetEnumerator(); subMesh->MoveNext();)
    {
        const int numTriangles = subMesh->Current->Length;
        for (int k = 0; k < numTriangles; ++k)
        {
            Facet^ facet = subMesh->Current[k];
            aiFace face;
            face.mNumIndices = 3;
            face.mIndices = new unsigned int[3] { facet->A, facet->B, facet->C };
            faces.push_back(face);
        }
    }

    mesh->mNumFaces = faces.size();
    mesh->mFaces = new aiFace[faces.size()];

    std::copy(faces.begin(), faces.end(), mesh->mFaces);

    index = meshes.size();
    meshes.push_back(mesh);
    return index;
}

Dictionary<RefId^, unsigned int>^ TRexAssimp::TRexAssimpExport::CreateShapes(ComponentScene^ componentScene, 
    std::vector<aiMesh*>& meshes)
{
    auto map = gcnew Dictionary<RefId^, unsigned int>(10);
    for (auto en = componentScene->ShapeBodies->GetEnumerator(); en->MoveNext();)
    {
        int mesh_index = CreateMesh(en->Current, meshes);
        map->Add(en->Current->Id, mesh_index);
    }
    return map;
}

Dictionary<RefId^, unsigned int>^ TRexAssimp::TRexAssimpExport::CreateMaterials(ComponentScene^ componentScene, 
    aiScene& scene)
{
    auto map = gcnew Dictionary<RefId^, unsigned int>(10);
    std::vector<aiMaterial*> materials;
    for (auto en_material = componentScene->Materials->GetEnumerator(); en_material->MoveNext();)
    {
        materials.push_back(CreateMaterial(en_material->Current));
    }

    scene.mNumMaterials = materials.size();
    scene.mMaterials = new aiMaterial * [materials.size()];
    std::copy(materials.begin(), materials.end(), scene.mMaterials);
    return map;
}

aiMaterial* TRexAssimp::TRexAssimpExport::CreateMaterial(Material^ material)
{
    aiMaterial* mat = new aiMaterial();

    aiString name = aiString(marshal_as<std::string>(material->Name));
    mat->AddProperty(&name, AI_MATKEY_NAME);

    if (material->HintRenderBothFaces) 
    {
        int i = 1;
        mat->AddProperty<int>(&i, 1, AI_MATKEY_TWOSIDED);
    }
       
    for (auto en = material->ColorChannels->GetEnumerator(); en->MoveNext();) 
    { 
        ColorOrNormalised^ color = en->Current;
        aiColor3D c;
        switch (color->ColorOrValueCase)
        {
        case ColorOrNormalised::ColorOrValueOneofCase::Normalised:
            c = aiColor3D(color->Normalised);
            SetColorChannel(color->Channel, mat, c, 1);
            break;
        case ColorOrNormalised::ColorOrValueOneofCase::Color:
            float alpha;
            c = CreateColor3D(color->Color, alpha);
            SetColorChannel(color->Channel, mat, c, alpha);
            break;
        case ColorOrNormalised::ColorOrValueOneofCase::None:
            // TODO Enable logging
            break;
        default:
            throw gcnew System::NotImplementedException();
        }
    }

    return mat;
}

void TRexAssimp::TRexAssimpExport::SetColorChannel(ColorChannel channel, aiMaterial* material, aiColor3D& color, float alpha)
{
    switch (channel)
    {
    case ColorChannel::Albedo:
    case ColorChannel::Diffuse:
        material->AddProperty(&color, 1, AI_MATKEY_COLOR_DIFFUSE);
        material->AddProperty<ai_real>(&alpha, 1, AI_MATKEY_OPACITY);
        break;
    case ColorChannel::Specular:
        material->AddProperty(&color, 1, AI_MATKEY_COLOR_SPECULAR);
        break;
    case ColorChannel::Reflective:
        material->AddProperty(&color, 1, AI_MATKEY_COLOR_REFLECTIVE);
        break;
    case ColorChannel::DiffuseEmmisive:
    case ColorChannel::Emmisive:
        material->AddProperty(&color, 1, AI_MATKEY_COLOR_EMISSIVE);
        break;
    }
}


aiColor3D TRexAssimp::TRexAssimpExport::CreateColor3D(Color^ color, float % alpha)
{
    aiColor3D c;
    c.r = color->R;
    c.g = color->G;
    c.b = color->B;
    alpha = color->A;
    return c;
}

aiMatrix4x4 TRexAssimp::TRexAssimpExport::CreateMat4x4(Transform^ t)
{
    switch (t->RotationOrQuaternionCase)
    {
    case Transform::RotationOrQuaternionOneofCase::Q:
        return aiMatrix4x4(aiVector3D(1), CreateQuat(t->Q), CreateVec3D(t->T));        
    case Transform::RotationOrQuaternionOneofCase::R:
        break;
    default:
        throw gcnew System::NotImplementedException();
    }
}

aiVector3D TRexAssimp::TRexAssimpExport::CreateVec3D(XYZ^ xyz)
{
    return aiVector3D(xyz->X, xyz->Y, xyz->Z);
}

aiQuaternion TRexAssimp::TRexAssimpExport::CreateQuat(Quaternion^ q)
{
    aiQuaternion quat;
    quat.x = q->X;
    quat.y = q->Y;
    quat.z = q->Z;
    quat.w = q->W;
    return quat;
}
