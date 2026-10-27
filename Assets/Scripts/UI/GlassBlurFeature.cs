using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace MontAR
{
    /// <summary>
    /// Feature do URP que desfoca a imagem da câmera (RA ou bancada 3D com o fundo dos menus) numa
    /// textura pequena e a publica como <c>_MontAR_GlassBlur</c>. Os cartões de vidro da interface
    /// (shader MontAR/UI Glass) mostram esse desfoque atrás deles. A textura é própria e persiste entre
    /// quadros, então a UI em Screen Space Overlay (desenhada depois da câmera) ainda a encontra.
    /// Câmeras que só desenham a camada UI (ex.: a de captura dos testes) não mexem no desfoque.
    /// </summary>
    public class GlassBlurFeature : ScriptableRendererFeature
    {
        public static readonly int GlobalTextureId = Shader.PropertyToID("_MontAR_GlassBlur");
        private static readonly int SourceTexelId = Shader.PropertyToID("_MontAR_SourceTexel");
        private static readonly int BlurTexelId = Shader.PropertyToID("_MontAR_BlurTexel");
        private static readonly int BlurSpreadId = Shader.PropertyToID("_MontAR_BlurSpread");

        [SerializeField] private Shader blurShader;
        [Tooltip("Divisor da resolução da câmera (4 = um quarto). Maior = mais leve e mais desfocado.")]
        [Range(2, 8)]
        [SerializeField] private int downsample = 4;
        [Tooltip("Afastamento entre as amostras da gaussiana, em texels da textura reduzida.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float spread = 1.8f;

        private Material material;
        private BlurPass pass;
        private RenderTexture target;
        private RTHandle targetHandle;

        public override void Create()
        {
            pass = new BlurPass(this)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents,
                // O passe lê a cor da câmera: o URP não pode desenhar direto no backbuffer.
                requiresIntermediateTexture = true
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (material == null)
            {
                Shader shader = blurShader != null ? blurShader : Shader.Find("Hidden/MontAR/GlassBlur");
                if (shader == null || !shader.isSupported)
                    return;
                material = CoreUtils.CreateEngineMaterial(shader);
            }
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            ReleaseTarget();
            CoreUtils.Destroy(material);
            material = null;
        }

        private void Record(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            var resources = frameData.Get<UniversalResourceData>();
            Camera camera = cameraData.camera;
            int uiLayer = LayerMask.NameToLayer("UI");
            bool onlyUi = uiLayer >= 0 && (camera.cullingMask & ~(1 << uiLayer)) == 0;
            if (cameraData.cameraType != CameraType.Game || onlyUi || resources.isActiveTargetBackBuffer)
                return;

            TextureHandle source = resources.activeColorTexture;
            if (!source.IsValid())
                return;

            RenderTextureDescriptor cameraTarget = cameraData.cameraTargetDescriptor;
            int width = Mathf.Max(8, cameraTarget.width / downsample);
            int height = Mathf.Max(8, cameraTarget.height / downsample);
            TextureHandle blurred = renderGraph.ImportTexture(EnsureTarget(width, height));
            TextureDesc tempDesc = renderGraph.GetTextureDesc(blurred);
            tempDesc.name = "MontAR vidro (meio do desfoque)";
            tempDesc.clearBuffer = false;
            TextureHandle temp = renderGraph.CreateTexture(tempDesc);

            material.SetVector(SourceTexelId, new Vector4(1f / cameraTarget.width, 1f / cameraTarget.height, 0f, 0f));
            material.SetVector(BlurTexelId, new Vector4(1f / width, 1f / height, 0f, 0f));
            material.SetFloat(BlurSpreadId, spread);
            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, blurred, material, 0), "MontAR vidro: reduzir");
            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(blurred, temp, material, 1), "MontAR vidro: desfoque horizontal");
            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(temp, blurred, material, 2), "MontAR vidro: desfoque vertical");
            Shader.SetGlobalTexture(GlobalTextureId, target);
        }

        private RTHandle EnsureTarget(int width, int height)
        {
            if (target != null && target.width == width && target.height == height && target.IsCreated())
                return targetHandle;

            ReleaseTarget();
            target = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "MontAR vidro (desfoque)",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            target.Create();
            targetHandle = RTHandles.Alloc(target);
            return targetHandle;
        }

        private void ReleaseTarget()
        {
            if (targetHandle != null)
                RTHandles.Release(targetHandle);
            targetHandle = null;
            if (target != null)
            {
                target.Release();
                CoreUtils.Destroy(target);
            }
            target = null;
        }

        private class BlurPass : ScriptableRenderPass
        {
            private readonly GlassBlurFeature owner;

            public BlurPass(GlassBlurFeature owner)
            {
                this.owner = owner;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                owner.Record(renderGraph, frameData);
            }
        }
    }
}
