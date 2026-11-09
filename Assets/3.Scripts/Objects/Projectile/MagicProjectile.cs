using UnityEngine;

public class MagicProjectile : ProjectileBase
{
    [SerializeField] ParticleSystem flightEffect;
    [SerializeField] ParticleSystem smokeEffect;

    protected override void OnInitialized()
    {
        if (flightEffect)
        {
            flightEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // 본체와 연결된 Sub Emitters를 시작한다.
            flightEffect.Play(false);
        }

        if (smokeEffect)
        {
            smokeEffect.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            // 이미 나온 연기는 화염구를 따라가지 않고 지나간 자리에 남긴다.
            var smokeMain = smokeEffect.main;
            smokeMain.simulationSpace = ParticleSystemSimulationSpace.World;
            smokeEffect.Play(false);
        }
    }

    protected override void OnHit(
    CharacterBase target, Collider hitCollider)
    {
        ParticleSystem[] effects = GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem effect in effects)
        {
            if (effect == smokeEffect)
            {
                // 새로운 연기만 멈추고, 이미 나온 연기는 남긴다.
                effect.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
            else
            {
                // 불덩이와 나머지 불꽃은 즉시 제거한다.
                effect.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        float remainTime = smokeEffect ? smokeEffect.main.startLifetime.constantMax : 0f;

        ReturnAfter(remainTime);
    }

    protected override void OnReleased()
    {
        if (flightEffect)
        {
            flightEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (smokeEffect)
        {
            smokeEffect.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
