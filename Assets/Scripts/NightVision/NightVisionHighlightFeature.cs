using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Draws night-vision marks after post-processing so yellow and red stay readable.
/// </summary>
public class NightVisionHighlightFeature : ScriptableRendererFeature
{
    public static bool Enabled;
    public static float Range = 25f;

    NightVisionHighlightPass pass;
    Material interactableMaterial;
    Material threatMaterial;

    public override void Create()
    {
        pass = new NightVisionHighlightPass();
        pass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        EnsureMaterials();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!Enabled)
            return;

        if (renderingData.cameraData.cameraType != CameraType.Game)
            return;

        EnsureMaterials();
        if (interactableMaterial == null || threatMaterial == null)
            return;

        pass.interactableMaterial = interactableMaterial;
        pass.threatMaterial = threatMaterial;
        renderer.EnqueuePass(pass);
    }

    void EnsureMaterials()
    {
        if (interactableMaterial != null && threatMaterial != null)
            return;

        Shader shader = Shader.Find("NightVision/Highlight");
        if (shader == null)
            return;

        if (interactableMaterial == null)
        {
            interactableMaterial = CoreUtils.CreateEngineMaterial(shader);
            interactableMaterial.SetColor("_Color", new Color(1f, 0.82f, 0.05f, 1f));
        }

        if (threatMaterial == null)
        {
            threatMaterial = CoreUtils.CreateEngineMaterial(shader);
            threatMaterial.SetColor("_Color", new Color(1f, 0.08f, 0.05f, 1f));
        }
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(interactableMaterial);
        CoreUtils.Destroy(threatMaterial);
        interactableMaterial = null;
        threatMaterial = null;
    }

    class NightVisionHighlightPass : ScriptableRenderPass
    {
        public Material interactableMaterial;
        public Material threatMaterial;

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (interactableMaterial == null || threatMaterial == null || NightVisionMark.Active.Count == 0)
                return;

            Camera camera = renderingData.cameraData.camera;
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            Vector3 cameraPosition = camera.transform.position;
            float rangeSqr = Range * Range;

            CommandBuffer cmd = CommandBufferPool.Get("NightVisionHighlight");
            cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTarget);
            for (int i = 0; i < NightVisionMark.Active.Count; i++)
            {
                NightVisionMark mark = NightVisionMark.Active[i];
                if (mark == null || !mark.isActiveAndEnabled)
                    continue;

                Renderer[] renderers = mark.Renderers;
                if (renderers == null)
                    continue;

                Material drawMaterial = mark.Category == NightVisionMark.Kind.Interactable
                    ? interactableMaterial
                    : threatMaterial;

                for (int r = 0; r < renderers.Length; r++)
                {
                    Renderer renderer = renderers[r];
                    if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                        continue;

                    if (!GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                        continue;

                    Vector3 closest = renderer.bounds.ClosestPoint(cameraPosition);
                    if ((closest - cameraPosition).sqrMagnitude > rangeSqr)
                        continue;

                    int subMeshes = SubMeshCount(renderer);
                    for (int s = 0; s < subMeshes; s++)
                        cmd.DrawRenderer(renderer, drawMaterial, s);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        static int SubMeshCount(Renderer renderer)
        {
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer skinned)
                mesh = skinned.sharedMesh;
            else
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null)
                    mesh = filter.sharedMesh;
            }

            if (mesh != null && mesh.subMeshCount > 0)
                return mesh.subMeshCount;

            return renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0
                ? renderer.sharedMaterials.Length
                : 1;
        }
    }
}
