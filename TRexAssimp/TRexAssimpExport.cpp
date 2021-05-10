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

// Gets all available extensions
array<String^>^ TRexAssimp::TRexAssimpExport::Extensions::get()
{
    array<String^>^ extensions = gcnew array<String^>((uint)exporter->GetExportFormatCount());
    for (uint i = 0, end = extensions->Length; i < end; ++i) {
        const aiExportFormatDesc* const e = exporter->GetExportFormatDescription(i);
        String^ str = gcnew String(e->fileExtension);
        extensions[i] = str;
    }
    return extensions;
}

// Exports the given scene to Assimp and finally to a file
bool TRexAssimp::TRexAssimpExport::ExportTo(String^ filePathName, 
    String^ ext,
    ComponentScene^ componentScene)
{
    std::string stdFileName = marshal_as<std::string>(filePathName);
    aiScene scene;

    // Create raw mesh buffer, material mesh buffer
    std::vector<aiMesh*> vRawMeshes;
    std::vector<aiMesh*> vMaterialMeshes;
    // Create material mesh index
    std::map<uint, std::map<uint, uint>> mRawMaterialMeshes;
    auto meshMap = CreateShapes(componentScene, vRawMeshes);

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

    auto componentMap = gcnew Dictionary<GlobalUniqueId^, uint>(10);
    std::vector<aiNode*> vNodes;
    std::map<int, std::vector<uint>> mChildren;
    std::vector<aiNode*> vTopLevelNodes;

    int c_index = 0;
    // Create scene nodes, collect parent-child relationships and materialize meshes
    for (auto en = componentScene->Components->GetEnumerator(); en->MoveNext(); ++c_index)
    {
        auto c = en->Current;
        auto idx_node = GetOrCreateNodeAndParent(c, vNodes, mChildren, componentMap);
        aiNode* node = vNodes[idx_node];
        if (nullptr == node->mParent)
        {   // Set reference to root node if no root exists
            node->mParent = scene.mRootNode;
            vTopLevelNodes.push_back(node);
        }

        node->mNumMeshes = c->Shapes->Count;
        node->mMeshes = nullptr;
        if (0 < node->mNumMeshes)
        {   // If there are meshes, process them
            int m_index = 0;
            node->mMeshes = new uint[node->mNumMeshes];

            for (auto en_shape = c->Shapes->GetEnumerator(); en_shape->MoveNext(); ++m_index)
            {
                Shape^ shape = en_shape->Current;
                Transform^ t = nullptr;
                uint idx_raw_mesh;
                if (meshMap->TryGetValue(shape->ShapeBody, idx_raw_mesh))
                {   // Store global mesh index into local mesh index
                    uint idx_material;
                    if (materialMap->TryGetValue(shape->Material, idx_material))
                    {
                        auto idx_material_mesh = CreateMaterialMesh(vRawMeshes, mRawMaterialMeshes, vMaterialMeshes, idx_raw_mesh, idx_material);
                        node->mMeshes[m_index] = idx_material_mesh;
                    }
                }

                if (contextWcsMap->TryGetValue(shape->Context, t))
                {   // Get WCS transform
                    aiMatrix4x4 wcs = _aiMatrix4(t);
                    node->mTransformation = wcs * _aiMatrix4(shape->Transform);
                }
            }
        }
    }

    // Copy final materialized meshes to scene
    scene.mNumMeshes = (uint)vMaterialMeshes.size();
    scene.mMeshes = new aiMesh * [scene.mNumMeshes];
    std::copy(vMaterialMeshes.begin(), vMaterialMeshes.end(), scene.mMeshes);

    // Create parent-child relationships
    for (int i = 0; i < vNodes.size(); ++i)
    {
        auto it_children = mChildren.find(i);
        if (it_children != mChildren.end())
        {   // If relations exist
            std::vector<uint>& vChildrenIndex = it_children->second;
            vNodes[i]->mNumChildren = (uint)vChildrenIndex.size();

            aiNode** childNodes = new aiNode * [vNodes[i]->mNumChildren];
            vNodes[i]->mChildren = childNodes;
            for (auto it = vChildrenIndex.begin(); it != vChildrenIndex.end(); ++it)
            {
                childNodes[it - vChildrenIndex.begin()] = vNodes[*it];
            }                        
        }
    }

    // Finally set up top level nodes as children of root node
    scene.mRootNode->mNumChildren = (uint)vTopLevelNodes.size();
    scene.mRootNode->mChildren = new aiNode * [scene.mRootNode->mNumChildren];
    std::copy(vTopLevelNodes.begin(), vTopLevelNodes.end(), scene.mRootNode->mChildren);

    // Try to save scene to file by given file format extension
    String^ fileExt = ext->ToLower();
    const int idx_ext = System::Array::IndexOf(Extensions, fileExt);
     
    if (0 <= idx_ext)
    { 
        const aiExportFormatDesc* formatDesc = exporter->GetExportFormatDescription(idx_ext);

        String^ givenExt = System::IO::Path::GetExtension(filePathName)->ToLower();
        if (!givenExt->Equals(fileExt))
            filePathName = System::IO::Path::ChangeExtension(filePathName, fileExt);

        const aiReturn res = exporter->Export(&scene, formatDesc->id, marshal_as<std::string>(filePathName));
        return AI_SUCCESS == res;
    }
    else
    {
        throw gcnew System::ArgumentException(gcnew String("Unknown file format extension"));
    }
}

// Get or create a node and its parent by ID reference
const uint TRexAssimp::TRexAssimpExport::GetOrCreateNodeAndParent(Component^ c,
    std::vector<aiNode*>& vNodes, // the nodes
    std::map<int, std::vector<uint>>& mChildren, // parent-children relation per index
    Dictionary<GlobalUniqueId^, uint>^ nodeMap) // DTO reference to index
{
    int idx_node = GetOrCreateNode(c->Id, vNodes, nodeMap);
    aiNode* node = vNodes[idx_node];
    std::string s_name = msclr::interop::marshal_as<std::string>(c->Name);
    node->mName = aiString(s_name);

    if (nullptr != c->Parent)
    {
        auto idx_parent_node = GetOrCreateNode(c->Parent, vNodes, nodeMap);
        node->mParent = vNodes[idx_parent_node];
        auto it_children = mChildren.find(idx_parent_node);
        if (it_children == mChildren.end())
        {
            auto it_inserted = mChildren.insert(std::make_pair(idx_parent_node, std::vector<uint>()));
            it_children = it_inserted.first;
        }
        // add current node as child of referenced parent
        it_children->second.push_back(idx_node);
    }

    return idx_node;
}

// Get or create a node by global ID.
const uint TRexAssimp::TRexAssimpExport::GetOrCreateNode(GlobalUniqueId^ id, 
    std::vector<aiNode*>& vNodes, // Node buffer of scene
    Dictionary<GlobalUniqueId^, uint>^ nodeMap) // Reference cache of ID to node index
{
    aiNode* node;
    uint idx_node;
    if (nodeMap->TryGetValue(id, idx_node))
    {
        node = vNodes[idx_node];
    }
    else
    {
        node = new aiNode;
        idx_node = (uint)vNodes.size();
        nodeMap->Add(id, idx_node);
        vNodes.push_back(node);
    }
    return idx_node;
}

// Creates a single mesh from shape body and stores this into an existing mesh cache.
const uint TRexAssimp::TRexAssimpExport::CreateMesh(ShapeBody^ body, 
    std::vector<aiMesh*>& vRawMeshes) // Raw meshes buffer
{    
    auto subMeshes = body->GetContinuousFacets(0);
    auto allVertices = body->GetCoordinateTriples();
    const uint countVertices = body->GetTotalPointCount();
    
    aiMesh* mesh = new aiMesh;
    mesh->mNumVertices = countVertices;
    mesh->mVertices = new aiVector3D[countVertices];

    // Transfer vertices
    uint index = 0;
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
            face.mIndices = new uint[3] { facet->A, facet->B, facet->C };
            faces.push_back(face);
        }
    }

    mesh->mNumFaces = (uint)faces.size();
    mesh->mFaces = new aiFace[mesh->mNumFaces];

    std::copy(faces.begin(), faces.end(), mesh->mFaces);

    index = (uint)vRawMeshes.size();
    vRawMeshes.push_back(mesh);
    return index;
}

// Materializes a raw mesh by the component's material reference
const uint TRexAssimp::TRexAssimpExport::CreateMaterialMesh(std::vector<aiMesh*>& vRawMeshes, // raw meshes
    std::map<uint, std::map<uint, uint>>& mRawMaterialMeshes, // cache index raw mesh vs material by scene mesh index
    std::vector<aiMesh*>& vMaterialMeshes, // future mesh buffer of scene having material assignments
    uint idxRawMesh, // index of raw mesh
    uint idxMaterial) // index of material in scene material buffer
{
    auto it_mesh_map = mRawMaterialMeshes.find(idxRawMesh);
    if (it_mesh_map == mRawMaterialMeshes.end())
    {   // create new mesh index vs material mapping
        auto it_inserted = mRawMaterialMeshes.insert(
            std::pair<uint, std::map<uint, uint>>(idxRawMesh, std::map<uint, uint>()));
        it_mesh_map = it_inserted.first;
    }

    auto it_material_map = it_mesh_map->second.find(idxMaterial);
    if (it_material_map == it_mesh_map->second.end())
    {   // create new material mesh using next index in buffer
        auto it_inserted = it_mesh_map->second.insert(std::pair<uint, uint>(idxMaterial, vMaterialMeshes.size()));
        it_material_map = it_inserted.first;
        aiMesh* materialMesh = new aiMesh(*vRawMeshes[idxRawMesh]);
        materialMesh->mMaterialIndex = idxMaterial;
        vMaterialMeshes.push_back(materialMesh);
    }

    return it_material_map->second;
}

Dictionary<RefId^, uint>^ TRexAssimp::TRexAssimpExport::CreateShapes(ComponentScene^ componentScene, 
    std::vector<aiMesh*>& meshes)
{
    auto map = gcnew Dictionary<RefId^, uint>(10);
    for (auto en = componentScene->ShapeBodies->GetEnumerator(); en->MoveNext();)
    {
        int mesh_index = CreateMesh(en->Current, meshes);
        map->Add(en->Current->Id, mesh_index);
    }
    return map;
}

// Creates all materials of scene and returns a mapping of scene ID and internal reference index
Dictionary<RefId^, uint>^ TRexAssimp::TRexAssimpExport::CreateMaterials(ComponentScene^ componentScene, 
    aiScene& scene)
{
    auto map = gcnew Dictionary<RefId^, uint>(10);
    std::vector<aiMaterial*> materials;
    for (auto en_material = componentScene->Materials->GetEnumerator(); en_material->MoveNext();)
    {
        materials.push_back(CreateMaterial(en_material->Current));
    }

    scene.mNumMaterials = (uint)materials.size();
    scene.mMaterials = new aiMaterial * [scene.mNumMaterials];
    std::copy(materials.begin(), materials.end(), scene.mMaterials);
    return map;
}

// Creates a single Assimp material by scene material
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
            c = _aiColor3D(color->Color, alpha);
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

// Sets the matrix color channel using the given color and opacity (only for diffuse channels)
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

// Adapts a color and returns color and alpha (opacity) separately
aiColor3D TRexAssimp::TRexAssimpExport::_aiColor3D(Color^ color, float % alpha)
{
    aiColor3D c;
    c.r = color->R;
    c.g = color->G;
    c.b = color->B;
    alpha = color->A;
    return c;
}

// Adapts a rotation transform to a matrix 3x3
aiMatrix3x3 TRexAssimp::TRexAssimpExport::_aiMatrix3(Rotation^ r)
{
    return aiMatrix3x3(
        r->Rx->X, r->Rx->Y, r->Rx->Z,
        r->Ry->X, r->Ry->Y, r->Ry->Z,
        r->Rz->X, r->Rz->Y, r->Rz->Z
    );
}

// Aggregates a rotation matrix and translation vector to a single matrix 4x4
aiMatrix4x4 TRexAssimp::TRexAssimpExport::_aiMatrix4(const aiMatrix3x3& r, const aiVector3D& o)
{
    aiMatrix4x4 m = aiMatrix4x4(r);
    m.a4 = o.x;
    m.b4 = o.y;
    m.c4 = o.z;
    return m;
}

// Adapts a full transform to a transform matrix 4x4
aiMatrix4x4 TRexAssimp::TRexAssimpExport::_aiMatrix4(Transform^ t)
{
    switch (t->RotationOrQuaternionCase)
    {
    case Transform::RotationOrQuaternionOneofCase::Q:
        return aiMatrix4x4(aiVector3D(1), _aiQuaternion(t->Q), _aiVector3D(t->T));        
    case Transform::RotationOrQuaternionOneofCase::R:        
        return _aiMatrix4(_aiMatrix3(t->R), _aiVector3D(t->T));
    default:
        throw gcnew System::NotImplementedException();
    }
}

// Adapts a simple XYZ vector
aiVector3D TRexAssimp::TRexAssimpExport::_aiVector3D(XYZ^ xyz)
{
    return aiVector3D(xyz->X, xyz->Y, xyz->Z);
}

// Adapts a quaternion 
aiQuaternion TRexAssimp::TRexAssimpExport::_aiQuaternion(Quaternion^ q)
{
    aiQuaternion quat;
    quat.x = q->X;
    quat.y = q->Y;
    quat.z = q->Z;
    quat.w = q->W;
    return quat;
}
