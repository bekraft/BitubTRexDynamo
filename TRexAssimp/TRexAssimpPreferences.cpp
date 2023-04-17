#include "TRexAssimpPreferences.h"
#include "TRexAssimp.h"

#include <string>
#include <sstream>

#include <assimp/commonMetaData.h>
#include <assimp/version.h>

TRexAssimp::TRexAssimpPreferences::TRexAssimpPreferences(::TRex::Log::Logger^ logger) 
	: fScale(1.0f), bSeparateContextScenes(false), bUseSourceWCS(false), metadata(nullptr)
{
	this->transform = Transform::Identity;
	this->logger = logger;

	InitMetadata();
}

TRexAssimp::TRexAssimpPreferences::TRexAssimpPreferences(::TRex::Log::Logger^ logger, ::TRex::Geom::CRSTransform^ t, UnitScale^ s)
	: TRexAssimpPreferences(logger)
{
	if (nullptr != t)
		transform = t->GlobalTransform;
	if (nullptr != s)
		fScale = s->UnitsPerMeter;

	InitMetadata();
}

TRexAssimp::TRexAssimpPreferences::~TRexAssimpPreferences()
{
	delete metadata;
}

void TRexAssimp::TRexAssimpPreferences::InitMetadata()
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
