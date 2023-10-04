using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPVItem_AK12 : FPVItem_GunBase
{
    private Animator m_ArmsAnimator = null;

    protected void Awake()
    {
        base.Awake();

        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            m_ArmsAnimator = FirstPersonViewManager.singleton.firstPersonArmsAnimator;
        }
    }

    protected void Start()
    {
        base.Start();
    }

    protected void Update()
    {
        base.Update();

        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            if (!m_ArmsAnimator.GetCurrentAnimatorStateInfo(0).IsName("Arms_AK12_Idle") || !(m_animator.GetCurrentAnimatorStateInfo(0).IsName("idle") || m_animator.GetCurrentAnimatorStateInfo(0).IsName("empty")))
            {
                m_reloadingBlocked = true;
            }
            else
            {
                m_reloadingBlocked = false;
            }

            if (m_ArmsAnimator.GetCurrentAnimatorStateInfo(0).IsName("Arms_AK12_Idle") || m_ArmsAnimator.GetCurrentAnimatorStateInfo(0).IsName("Arms_AK12_aimIn_idle"))
            {
                m_shootingPrimaryBlocked = false;
            }
            else
            {
                m_shootingPrimaryBlocked = false;
            }
        }
    }

    protected override void onItemActivated()
    {
        base.onItemActivated();
        //m_ArmsAnimator.Rebind();
        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            m_ArmsAnimator.SetBool("ItemAK12", true);
        }
    }

    protected override void onGunFire()
    {
        startShot();
        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            m_animator.SetBool("input_fire", true);
            m_ArmsAnimator.SetBool("UseItem", true);
            //FirstPersonAnimationManager.singleton.playMuzzleFlashEffect();
            SoundManager.singleton.playGlobalSound(m_shotSoundIndex, Sound.SoundPlaystyle.Once);
        }
    }

    protected override void onItemUsagePrimaryEnded()
    {
        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            m_animator.SetBool("input_fire", false);
            m_ArmsAnimator.SetBool("UseItem", false);
        }
    }

    protected override void onItemUsageSecondary()
    {
        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            m_ArmsAnimator.SetBool("ZoomIn", true);
            CameraStack.m_singleton.fadeFieldOfView(m_zoomedFOV, m_zoomSpeed);
        }
    }

    protected override void onItemUsageSecondaryEnded()
    {
        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            m_ArmsAnimator.SetBool("ZoomIn", false);
            CameraStack.m_singleton.fadeDefaultFieldOfView(m_zoomSpeed);
        }
    }

    protected override void onEndMagazinEmpty()
    {
        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            m_animator.SetBool("Empty", true);
        }
    }

    protected override void onStartReloading()
    {
        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            m_animator.SetBool("Empty", false);
            m_animator.SetBool("Reload", true);
            m_ArmsAnimator.SetBool("Reload", true);
        }
    }

    protected override void onEndReloading()
    {
        if (GameManager_Custom.singleton.isClient || GameManager_Custom.singleton.isServerAndClient)
        {
            m_animator.SetBool("Reload", false);
            m_ArmsAnimator.SetBool("Reload", false);
        }
    }
}
