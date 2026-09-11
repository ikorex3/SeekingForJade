using System.Collections;
using UnityEngine;

namespace SeekingForJade.VFX
{
    public static class RockVFXManager
    {
        private static Material particleMat;

        public static Material GetParticleMaterial()
        {
            if (particleMat == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
                particleMat = new Material(s);
                particleMat.name = "M_VFX_StoneParticle";
                particleMat.SetColor("_BaseColor", new Color(0.7f, 0.65f, 0.6f, 0.8f));
            }
            return particleMat;
        }

        /// <summary>
        /// Spawns a high-energy dust & stone fragment burst at the impact location when a rock smashes.
        /// </summary>
        public static void SpawnSmashImpactVFX(Vector3 position, Vector3 normal)
        {
            GameObject fxHost = new GameObject("VFX_RockSmashBurst");
            fxHost.transform.position = position;
            fxHost.transform.rotation = Quaternion.LookRotation(normal);

            // 1. Dust Cloud Particle System
            ParticleSystem ps = fxHost.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystemRenderer psRenderer = fxHost.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = GetParticleMaterial();

            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor = new Color(0.65f, 0.6f, 0.55f, 0.85f);
            main.gravityModifier = 0.3f;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, 30) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.1f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 0.2f);
            curve.AddKey(0.3f, 1.0f);
            curve.AddKey(1.0f, 1.8f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.7f, 0.65f, 0.6f), 0.0f), new GradientColorKey(new Color(0.5f, 0.45f, 0.4f), 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = grad;

            ps.Play();
            Object.Destroy(fxHost, 1.5f);
        }

        /// <summary>
        /// Creates the coolant water and stone slurry particle system for the saw workbench.
        /// </summary>
        public static ParticleSystem CreateSawCuttingFX(Transform parent, Vector3 localPos)
        {
            GameObject fxHost = new GameObject("VFX_SawCoolantMist");
            fxHost.transform.SetParent(parent, false);
            fxHost.transform.localPosition = localPos;
            fxHost.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem ps = fxHost.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystemRenderer psRenderer = fxHost.GetComponent<ParticleSystemRenderer>();
            psRenderer.material = GetParticleMaterial();

            var main = ps.main;
            main.duration = 1.0f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.startColor = new Color(0.85f, 0.92f, 0.95f, 0.7f); // Water spray / stone slurry
            main.gravityModifier = 0.8f;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 45f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.05f;

            return ps;
        }
    }
}
