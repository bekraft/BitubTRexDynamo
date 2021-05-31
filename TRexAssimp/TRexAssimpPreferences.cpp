#include "TRexAssimpPreferences.h"
#include "TRexAssimp.h"

TRexAssimp::TRexAssimpPreferences::TRexAssimpPreferences() 
	: scale(1.0f), separateContextScenes(false)
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
		scale = s->UnitsPerMeter;
}

aiMatrix4x4 TRexAssimp::TRexAssimpPreferences::GetTransform()
{
	aiMatrix4x4 scaled;
	aiMatrix4x4::Scaling(aiVector3D(scale), scaled);
	return TRexAssimp::AIMatrix4(transform) * scaled;
}
