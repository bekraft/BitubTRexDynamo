#include "TRexAssimpPreferences.h"
#include "TRexAssimp.h"

#include <string>
#include <sstream>

#include <assimp/commonMetaData.h>
#include <assimp/version.h>

TRexAssimp::TRexAssimpPreferences::TRexAssimpPreferences() 
	: fScale(1.0f), bSeparateContextScenes(false), bExportSceneWCS(false), metadata(nullptr)
{
	transform = gcnew Transform();
	transform->R = M33::Identity;
	transform->T = XYZ::Zero;

	InitMetadata(GlobalReferenceAxis::PositiveZ, GlobalReferenceAxis::PositiveY, GlobalReferenceAxis::PositiveX);
}

TRexAssimp::TRexAssimpPreferences::TRexAssimpPreferences(::TRex::Geom::CRSTransform^ t, UnitScale^ s)
	: TRexAssimpPreferences()
{
	if (nullptr != t)
		transform = t->Transform;
	if (nullptr != s)
		fScale = s->UnitsPerMeter;

	InitMetadata(t->Up, t->Forward, t->Right);
}

TRexAssimp::TRexAssimpPreferences::~TRexAssimpPreferences()
{
	delete metadata;
}

void TRexAssimp::TRexAssimpPreferences::InitMetadata(GlobalReferenceAxis up, GlobalReferenceAxis forward, GlobalReferenceAxis right)
{
	delete metadata;
	metadata = aiMetadata::Alloc(2);
	
	std::stringstream sb; 
	sb << aiGetVersionMajor() << "." << aiGetVersionMinor() << "." << aiGetVersionRevision();

	metadata->Set(0, AI_METADATA_SOURCE_FORMAT_VERSION, aiString(sb.str()));
	metadata->Set(1, AI_METADATA_SOURCE_GENERATOR, aiString("BitubTRexDynamo (assimp)"));
}

aiMatrix4x4 TRexAssimp::TRexAssimpPreferences::GetTransform()
{
	aiMatrix4x4 scaled;
	aiMatrix4x4::Scaling(aiVector3D(fScale), scaled);
	return scaled * TRexAssimp::AIMatrix4(transform);
}

aiMetadata* TRexAssimp::TRexAssimpPreferences::CreateMetadata()
{	
	// Copy clone metadata
	if (nullptr != metadata)
		return new aiMetadata(*metadata);
	else
		return new aiMetadata();
}
