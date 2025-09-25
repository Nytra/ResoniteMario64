﻿#region

using Elements.Assets;
using FrooxEngine;

#endregion

namespace ResoniteMario64.Mario64;

public static class ColliderShapeExtensions
{
    public static MeshX GetColliderMesh(this Collider c)
    {
        MeshX meshx = new MeshX();
        switch (c)
        {
            case TriangleCollider col:
                TriangleSubmesh triSubMesh = meshx.AddSubmesh<TriangleSubmesh>();
                meshx.AddVertex(col.A.Value + col.Offset.Value);
                meshx.AddVertex(col.B.Value + col.Offset.Value);
                meshx.AddVertex(col.C.Value + col.Offset.Value);
                triSubMesh.AddTriangle(0, 1, 2);
                break;
            case CapsuleCollider col:
                // Utils.GetPrimitiveMesh(Utils.PrimitiveType.Capsule);
                UVSphereCapsule uvcapsule = new UVSphereCapsule(meshx, 8, 16, UVSphereCapsule.Shading.Flat, true)
                {
                    Radius = col.Radius.Value,
                    Height = col.Height.Value
                };
                uvcapsule.Update();
                break;
            case ConeCollider col:
                ConicalFrustum cone = new ConicalFrustum(meshx, 8, true)
                {
                    Radius = col.Radius.Value,
                    RadiusTop = 0,
                    Height = col.Height.Value
                };
                cone.Update();
                break;
            case CylinderCollider col:
                ConicalFrustum cylinder = new ConicalFrustum(meshx, 8, true)
                {
                    Radius = col.Radius.Value,
                    Height = col.Height.Value
                };
                cylinder.Update();
                break;
            case BoxCollider col:
                Box box = new Box(meshx)
                {
                    Size = col.Size.Value
                };
                box.Update();
                break;
            case SphereCollider col:
                UVSphereCapsule uvsphere = new UVSphereCapsule(meshx, 8, 16, UVSphereCapsule.Shading.Flat)
                {
                    Radius = col.Radius.Value
                };
                uvsphere.Update();
                break;
            case MeshCollider col:
                if (col.Mesh.IsAssetAvailable)
                {
                    return col.Mesh.Target.Asset.Data;
                }

                if (Utils.CheckDebug()) Logger.Warn($"- [{col.GetType()}] {col.Slot.Name} ({col.ReferenceID}) Mesh is null or not readable");

                break;
            case ConvexHullCollider col:
                if (col.Mesh.IsAssetAvailable)
                {
                    return col.Mesh.Target.Asset.Data;
                }

                if (Utils.CheckDebug()) Logger.Warn($"- [{col.GetType()}] {col.Slot.Name} ({col.ReferenceID}) Mesh is null or not readable");

                break;
        }

        return meshx;
    }
}