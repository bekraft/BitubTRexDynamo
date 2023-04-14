#include <msclr\marshal_cppstd.h>
#include <string>
#include <sstream>

using namespace Bitub::Dto;
using namespace Bitub::Dto::Scene;

using namespace msclr::interop;

#include "TRexAssimpExport.h"
#include "TRexAssimp.h"

// TODO
// - single mesh per node & transform or multiple transformed meshes in WCS per node
// - root per context
// - texture coordinates
// - mesh units per format configuration

TRexAssimp::TRexAssimpExport::TRexAssimpExport()
    : TRexAssimpExport(gcnew TRexAssimpPreferences())
{    
}

TRexAssimp::TRexAssimpExport::TRexAssimpExport(TRexAssimpPreferences^ p)    
{
    exporter = new Assimp::Exporter();
    preferences = p;
}

TRexAssimp::TRexAssimpExport::~TRexAssimpExport()
{
    delete exporter;
}

TRexAssimp::TRexAssimpExport::!TRexAssimpExport()
{
    delete exporter;
    exporter = nullptr;
}

// Gets all available extensions
array<Format^>^ TRexAssimp::TRexAssimpExport::Formats::get()
{
    return GetAvailableFormats(this->exporter);
}

// Gets all available extensions
array<Format^>^ TRexAssimp::TRexAssimpExport::GetAvailableFormats(Assimp::Exporter* exporter)
{
    array<Format^>^ extensions = gcnew array<Format^>((uint)exporter->GetExportFormatCount());
    for (uint i = 0, end = extensions->Length; i < end; ++i) {
        const aiExportFormatDesc* const e = exporter->GetExportFormatDescription(i);
        extensions[i] = gcnew Format(gcnew String(e->id), gcnew String(e->fileExtension), gcnew String(e->description));
    }
    return extensions;
}

// Exports the given scene to Assimp and finally to a file
bool TRexAssimp::TRexAssimpExport::ExportTo(ComponentScene^ componentScene,
    String^ filePathName,
    Format^ format)
{
    std::string stdFileName = marshal_as<std::string>(filePathName);
    aiScene scene = aiScene();
    scene.mMetaData = preferences->CreateMetadata();
    if (nullptr != componentScene->Metadata && nullptr != componentScene->Metadata->Name)
    {   // Use name of metadata
        std::string sceneName = marshal_as<std::string>(componentScene->Metadata->Name);
        scene.mName = aiString(sceneName);
    }    

    // Create some internal buffers to store temporary builder data
    std::vector<aiMesh*> v_meshes;
    std::map<uint, std::map<uint, uint>> m_mesh_materialmesh;

    auto shapeRefMap = gcnew Dictionary<RefId^, ShapeBody^>(10);
    auto shapeRefIndexMap = gcnew Dictionary<RefId^, uint>(10);
    for (auto en = componentScene->ShapeBodies->GetEnumerator(); en->MoveNext();)
    {
        shapeRefMap->Add(en->Current->Id, en->Current);
    }

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
    scene.mRootNode = new aiNode();
        
    if (nullptr != componentScene->Metadata)
    {
        std::string projectName = marshal_as<std::string>(componentScene->Metadata->Name);
        scene.mRootNode->mName = aiString(projectName);
    }

    auto componentMap = gcnew Dictionary<RefId^, uint>(10);
    std::vector<aiNode*> v_nodes;
    std::map<uint, std::vector<uint>> m_parent_child;
    std::vector<aiNode*> v_top_nodes;

    auto crsTransform = preferences->GetTransform();
    int idxComponent = 0;
    // Create scene nodes, collect parent-child relationships and materialize meshes
    for (auto en = componentScene->Components->GetEnumerator(); en->MoveNext(); ++idxComponent)
    {
        auto c = en->Current;
        auto idx_node = GetOrCreateNodeAndParent(c, v_nodes, m_parent_child, componentMap);
        aiNode* node = v_nodes[idx_node];
        if (nullptr == node->mParent)
        {   // Set reference to root node if no root exists
            node->mParent = scene.mRootNode;
            v_top_nodes.push_back(node);
        }
        
        if (0 < c->Shapes->Count)
        {   // If there are meshes, process them
            uint m_index = 0;

            for (auto en_shape = c->Shapes->GetEnumerator(); en_shape->MoveNext(); ++m_index)
            { 
                Shape^ shape = en_shape->Current;
                Transform^ t = nullptr;
                aiMatrix4x4 wcs;

                // Get WCS and mesh transformation
                if (contextWcsMap->TryGetValue(shape->Context, t))
                {
                    // TODO
                    wcs = TRexAssimp::AIMatrix4(t);
                }
                else
                {
                    // TODO Log
                    continue;
                }

                aiNode* meshNode;
                if (1 < c->Shapes->Count)
                {   // Build artificial mesh nodes since materialized meshes don't have transforms, only nodes have
                    std::stringstream sb;
                    sb << "Mesh " << m_index;
                    meshNode = new aiNode(sb.str());
                }
                else
                {   // If only a single mesh, use the scene node itself
                    meshNode = node;
                }

                uint idx_material;
                if (!materialMap->TryGetValue(shape->Material, idx_material))
                    throw gcnew KeyNotFoundException("Shape's material RefID references no existing material.");

                uint idx_mesh;                
                if (!shapeRefIndexMap->TryGetValue(shape->ShapeBody, idx_mesh))
                {
                    ShapeBody^ shapeBody = nullptr;
                    if (!shapeRefMap->TryGetValue(shape->ShapeBody, shapeBody))
                        throw gcnew KeyNotFoundException("No shape bound found by RefID reference.");
                    idx_mesh = CreateMaterialMesh(shapeBody, v_meshes, m_mesh_materialmesh, idx_material, -1);
                    shapeRefIndexMap->Add(shape->ShapeBody, idx_mesh);
                }
                else
                {
                    idx_mesh = CreateMaterialMesh(nullptr, v_meshes, m_mesh_materialmesh, idx_material, idx_mesh);
                }
                
                v_meshes[idx_mesh]->mName = aiString(meshNode->mName);

                meshNode->mNumMeshes = 1;
                meshNode->mMeshes = new uint[1] { idx_mesh };
                // Global transformations
                meshNode->mTransformation = crsTransform * TRexAssimp::AIMatrix4(shape->Transform);

                // Register mesh node as child of current real scene node
                if (meshNode != node)
                {
                    auto& mChildIndex = GetOrCreateChildIndex(m_parent_child, idx_node);
                    const uint idx_child = (uint)v_nodes.size();
                    v_nodes.push_back(meshNode);
                    mChildIndex.push_back(idx_child);
                    meshNode->mParent = node;
                }
            }
        }
    }

    // Copy final materialized meshes to scene
    scene.mMeshes = push_to(v_meshes, scene.mNumMeshes);

    // Create parent-child relationships
    for (int i = 0; i < v_nodes.size(); ++i)
    {
        auto it_children = m_parent_child.find(i);
        if (it_children != m_parent_child.end())
        {   // If relations exist
            std::vector<uint>& vChildrenIndex = it_children->second;
            const uint numChildren = (uint)vChildrenIndex.size();
            v_nodes[i]->mNumChildren = numChildren;
            if (0 < numChildren)
            {
                aiNode** childNodes = new aiNode * [numChildren];
                v_nodes[i]->mChildren = childNodes;
                for (auto it = vChildrenIndex.begin(); it != vChildrenIndex.end(); ++it)
                {
                    // Create relative local transformations
                    childNodes[it - vChildrenIndex.begin()] = v_nodes[*it];
                }
            }
        }
    }

    // Finally set up top level nodes as children of root node
    scene.mRootNode->mChildren = push_to(v_top_nodes, scene.mRootNode->mNumChildren);

    LocalizeSceneTransforms(scene.mRootNode, aiMatrix4x4());

    // Save the scene to file of requested format
    String^ givenExt = System::IO::Path::GetExtension(filePathName)->ToLower()->Substring(1);
    if (!givenExt->Equals(format->Extension->ToLower()))
        filePathName = System::IO::Path::ChangeExtension(filePathName, format->Extension);

    auto formatID = marshal_as<std::string>(format->ID);
    auto fileName = marshal_as<std::string>(filePathName);
    
    const aiReturn res = exporter->Export(&scene, formatID, fileName);
    this->statusMessage = gcnew String(exporter->GetErrorString());
    return (AI_SUCCESS == res);
}

// Get or create a node and its parent by ID reference
const uint TRexAssimp::TRexAssimpExport::GetOrCreateNodeAndParent(Component^ c,
    std::vector<aiNode*>& vNodes, // the nodes
    std::map<uint, std::vector<uint>>& mChildren, // parent-children relation per index
    Dictionary<RefId^, uint>^ nodeMap) // DTO reference to index
{
    int idx_node = GetOrCreateNode(c->Id, vNodes, nodeMap);
    aiNode* node = vNodes[idx_node];
    std::string s_name = msclr::interop::marshal_as<std::string>(c->Name);
    node->mName = aiString(s_name);

    if (nullptr != c->Parent)
    {
        auto idx_parent_node = GetOrCreateNode(c->Parent, vNodes, nodeMap);
        node->mParent = vNodes[idx_parent_node];
        auto& mChildIndex = GetOrCreateChildIndex(mChildren, idx_parent_node);
        // add current node as child of referenced parent
        mChildIndex.push_back(idx_node);
    }

    return idx_node;
}

// Will crawl down the scene and replace/remap all transforms by their localized representation
const void TRexAssimp::TRexAssimpExport::LocalizeSceneTransforms(aiNode* node, 
    const aiMatrix4x4 tGlobalParent)
{
    // Build inverse transformation in order to localize children's transform
    aiMatrix4x4 tGlobalInverseParent = aiMatrix4x4(tGlobalParent).Inverse();
    aiMatrix4x4 tParent = tGlobalParent;

    if (!node->mTransformation.IsIdentity())
    {   // Any node having a transform (and global transform therefore since compnent scene only defines global transforms)
        // Leave parents within the hierarchy without meshes as they are, since they already have I as local transform
        tParent = node->mTransformation;
        node->mTransformation = tGlobalInverseParent * node->mTransformation;        
    }

    for (uint k = 0; k < node->mNumChildren; ++k)
    {
        LocalizeSceneTransforms(node->mChildren[k], tParent);
    }
}

// Gets or creates an index vector of children indices
std::vector<uint>& TRexAssimp::TRexAssimpExport::GetOrCreateChildIndex(std::map<uint, 
    std::vector<uint>>& mChildren, 
    const uint idx_parent_node)
{
    auto it_children = mChildren.find(idx_parent_node);
    if (it_children == mChildren.end())
    {
        auto it_inserted = mChildren.insert(std::make_pair(idx_parent_node, std::vector<uint>()));
        it_children = it_inserted.first;
    }
    return it_children->second;
}

// Get or create a node by global ID.
const uint TRexAssimp::TRexAssimpExport::GetOrCreateNode(RefId^ id, 
    std::vector<aiNode*>& vNodes, // Node buffer of scene
    Dictionary<RefId^, uint>^ nodeMap) // Reference cache of ID to node index
{
    aiNode* node;
    uint idx_node;
    if (nodeMap->TryGetValue(id, idx_node))
    {
        node = vNodes[idx_node];
    }
    else
    {
        node = new aiNode();
        idx_node = (uint)vNodes.size();
        nodeMap->Add(id, idx_node);
        vNodes.push_back(node);
    }
    return idx_node;
}

// Creates a single mesh from shape body and stores this into an existing mesh cache.
aiMesh* TRexAssimp::TRexAssimpExport::CreateMesh(ShapeBody^ body, const uint materialId)    
{    
    auto subMeshes = body->GetContinuousFacets(0);
    auto allVertices = body->GetCoordinateTriples();
    const uint countVertices = body->GetTotalPointCount();
    
    aiMesh* mesh = new aiMesh();
    mesh->mPrimitiveTypes = aiPrimitiveType::aiPrimitiveType_TRIANGLE;
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
            aiFace face = aiFace();
            face.mNumIndices = 3;
            face.mIndices = new uint[3] { facet->A, facet->B, facet->C };
            faces.push_back(face);
        }
    }

    mesh->mFaces = push_to(faces, mesh->mNumFaces);
    mesh->mMaterialIndex = materialId;

    return mesh;
}

// Materializes a raw mesh by the component's material reference
const uint TRexAssimp::TRexAssimpExport::CreateMaterialMesh(
    ShapeBody^ shapeBody,
    std::vector<aiMesh*>& vMeshes, 
    std::map<uint, std::map<uint, uint>>& mMeshMaterialIndex, 
    const uint idxMaterial, 
    const int idxMesh) // -1 if intentionally new otherwise representative mesh which to be cloned
{
    int m_index;
    std::map<uint, std::map<uint, uint>>::iterator it_mesh_map;
    if (0 > idxMesh)
    {   // If intenionally new
        it_mesh_map = mMeshMaterialIndex.end();
    }
    else
    {   // If known by key index
        m_index = (uint)idxMesh;
        it_mesh_map = mMeshMaterialIndex.find(m_index);
    }

    if (it_mesh_map == mMeshMaterialIndex.end())
    {   // create new pair of mesh index vs material mapping
        aiMesh* newMesh = CreateMesh(shapeBody, idxMaterial);
        m_index = (uint)vMeshes.size();
        vMeshes.push_back(newMesh);

        auto it_inserted = mMeshMaterialIndex.insert(
            std::pair<uint, std::map<uint, uint>>(m_index, std::map<uint, uint>()));
        it_mesh_map = it_inserted.first;
        // Add mapping from material index vs mesh index
        it_mesh_map->second.insert(std::pair<uint, uint>(idxMaterial, m_index));
        return m_index;
    }

    std::map<uint, uint>& mMaterialMesh = it_mesh_map->second;
    auto it_material_map = mMaterialMesh.find(idxMaterial);
    if (it_material_map == mMaterialMesh.end())
    {   
        const aiMesh* firstMesh = vMeshes[it_mesh_map->first];
        // Clone the representative mesh and set material index 
        aiMesh* newMesh = new aiMesh(*firstMesh);
        newMesh->mMaterialIndex = idxMaterial;
        m_index = (uint)vMeshes.size();
        auto it_inserted = mMaterialMesh.insert(std::pair<uint, uint>(idxMaterial, m_index));
        it_material_map = it_inserted.first;
        vMeshes.push_back(newMesh);
    }
    else
    {   // Or if already mapped, use existing mesh index
        m_index = it_material_map->second;
    }

    return m_index;
}

// Creates all materials of scene and returns a mapping of scene ID and internal reference index
Dictionary<RefId^, uint>^ TRexAssimp::TRexAssimpExport::CreateMaterials(ComponentScene^ componentScene, 
    aiScene& scene)
{
    auto map = gcnew Dictionary<RefId^, uint>(10);
    std::vector<aiMaterial*> materials;
    for (auto en_material = componentScene->Materials->GetEnumerator(); en_material->MoveNext();)
    {
        auto material = en_material->Current;
        map->Add(material->Id, (uint)materials.size());
        materials.push_back(CreateMaterial(material));
    }

    scene.mMaterials = push_to(materials, scene.mNumMaterials);

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
            c = TRexAssimp::AIColor3D(color->Color, alpha);
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
    case ColorChannel::Emmissive:
        material->AddProperty(&color, 1, AI_MATKEY_COLOR_EMISSIVE);
        break;
    }
}