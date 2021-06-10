#include "TRexAssimpPreferences.h"
#include "TRexAssimp.h"

TRexAssimp::TRexAssimpPreferences::TRexAssimpPreferences() 
	: fScale(1.0f), bSeparateContextScenes(false), bExportSceneWCS(false)
{
	transform = gcnew Transform();
	transform->R = Rotation::NewIdentity();
	transform->T = gcnew XYZ(0, 0, 0);
}

TRexAssimp::TRexAssimpPreferences::TRexAssimpPreferences(::TRex::Geom::CRSTransform^ t, UnitScale^ s)
	: TRexAssimpPreferences()
{
	if (nullptr != t)
		transform = t->Transform;
	if (nullptr != s)
		fScale = s->UnitsPerMeter;
}

aiMatrix4x4 TRexAssimp::TRexAssimpPreferences::GetTransform()
{
	aiMatrix4x4 scaled;
	aiMatrix4x4::Scaling(aiVector3D(fScale), scaled);
	return TRexAssimp::AIMatrix4(transform) * scaled;
}

aiMetadata* TRexAssimp::TRexAssimpPreferences::CreateMetadata()
{
	aiMetadata* metadata = new aiMetadata();

	return metadata;
}
