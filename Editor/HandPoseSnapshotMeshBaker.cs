#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Attach 베이크 후 SkinnedMeshRenderer를 정적 Mesh로 굳혀, 타깃 scale(예: 0.1)에서도 손 형태가 찌그러지지 않게 한다.
static class HandPoseSnapshotMeshBaker
{
    const string k_DefaultMeshFolder = "Assets/HandPoses/Meshes";

    public static void FreezeSkinnedMeshes(GameObject handRoot, string meshFolder = k_DefaultMeshFolder)
    {
        if (handRoot == null)
            return;

        HandPoseRecorderWindow.EnsureFolderPublic(meshFolder);

        // SkinnedMeshRenderer.updateWhenOffscreen — bounds가 작을 때 찌그러진 bake 방지
        foreach (var smr in handRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.updateWhenOffscreen = true;

        foreach (var smr in handRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.sharedMesh == null)
                continue;

            var bakedMesh = new Mesh { name = $"{smr.gameObject.name}_Baked" };
            smr.BakeMesh(bakedMesh, true);

            string meshPath = AssetDatabase.GenerateUniqueAssetPath($"{meshFolder}/{handRoot.name}_{smr.gameObject.name}.asset");
            AssetDatabase.CreateAsset(bakedMesh, meshPath);

            var go = smr.gameObject;
            var material = smr.sharedMaterial;

            Object.DestroyImmediate(smr);

            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.enabled = true;
        }

        HideBoneHierarchy(handRoot);
        handRoot.GetComponent<HandPoseSnapshotRoot>()?.SetMeshFrozen(true);
    }

    static void HideBoneHierarchy(GameObject handRoot)
    {
        foreach (var t in handRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t == handRoot.transform)
                continue;
            if (HandPoseSnapshotValidator.IsMeshContainer(t))
                continue;
            t.gameObject.SetActive(false);
        }
    }
}
#endif
