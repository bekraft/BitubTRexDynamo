#include "TRexAssimp.h"

// Adapts a color and returns color and alpha (opacity) separately
aiColor3D TRexAssimp::TRexAssimp::AIColor3D(Color^ color, float% alpha)
{
    aiColor3D c;
    c.r = color->R;
    c.g = color->G;
    c.b = color->B;
    alpha = color->A;
    return c;
}

// Adapts a rotation transform to a matrix 3x3
aiMatrix3x3 TRexAssimp::TRexAssimp::AIMatrix3(M33^ r)
{
    return aiMatrix3x3(
        r->Rx->X, r->Rx->Y, r->Rx->Z,
        r->Ry->X, r->Ry->Y, r->Ry->Z,
        r->Rz->X, r->Rz->Y, r->Rz->Z
    );
}

// Aggregates a rotation matrix and translation vector to a single matrix 4x4
aiMatrix4x4 TRexAssimp::TRexAssimp::AIMatrix4(const aiMatrix3x3& r, const aiVector3D& o)
{
    aiMatrix4x4 m = aiMatrix4x4(r);
    m.a4 = o.x;
    m.b4 = o.y;
    m.c4 = o.z;
    return m;
}

// Adapts a full transform to a transform matrix 4x4
aiMatrix4x4 TRexAssimp::TRexAssimp::AIMatrix4(Transform^ t)
{
    switch (t->RotationOrQuaternionCase)
    {
    case Transform::RotationOrQuaternionOneofCase::Q:
        return aiMatrix4x4(aiVector3D(1), AIQuaternion(t->Q), AIVector3D(t->T));
    case Transform::RotationOrQuaternionOneofCase::R:
        return AIMatrix4(AIMatrix3(t->R), AIVector3D(t->T));
    default:
        throw gcnew System::NotImplementedException();
    }
}

aiMatrix4x4 TRexAssimp::TRexAssimp::AIMatrix4(CRSTransform^ thisCrs)
{
    auto r = aiMatrix4x4();
    for (auto enumCrs = thisCrs->ExpandLeft()->GetEnumerator(); enumCrs->MoveNext();)
    {
    }
    return r;
}

// Adapts a simple XYZ vector
aiVector3D TRexAssimp::TRexAssimp::AIVector3D(XYZ^ xyz)
{
    return aiVector3D(xyz->X, xyz->Y, xyz->Z);
}

// Adapts a quaternion 
aiQuaternion TRexAssimp::TRexAssimp::AIQuaternion(Quat^ q)
{
    aiQuaternion quat;
    quat.x = q->X;
    quat.y = q->Y;
    quat.z = q->Z;
    quat.w = q->W;
    return quat;
}
