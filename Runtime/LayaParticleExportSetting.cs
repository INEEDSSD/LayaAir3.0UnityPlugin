using UnityEngine;

/// <summary>
/// 挂到带 ParticleSystem 的 GameObject 上，控制该粒子系统的导出模式。
/// 不挂此组件的粒子系统使用 ExportConfig 中的全局默认模式。
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
[AddComponentMenu("LayaAir/Particle Export Setting")]
public class LayaParticleExportSetting : MonoBehaviour
{
    public enum ParticleExportMode
    {
        /// <summary>Laya Shuriken Particle (GPU 渲染)</summary>
        ShurikenParticle = 0,
        /// <summary>Laya CPU Particle (CPU 模拟)</summary>
        CPUParticle = 1
    }

    [Tooltip("选择该粒子系统导出为 Shuriken(GPU) 还是 CPU 粒子")]
    public ParticleExportMode exportMode = ParticleExportMode.ShurikenParticle;
}
