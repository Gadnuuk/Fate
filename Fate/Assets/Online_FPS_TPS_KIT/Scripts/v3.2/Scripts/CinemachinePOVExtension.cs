using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;


public class CinemachinePOVExtension : CinemachineExtension
{
    [SerializeField] float maxViewAngle = 80;
    [SerializeField] private Vector3 cameraRotation;

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (vcam.Follow)
        {
            if (stage == CinemachineCore.Stage.Aim)
            {
                if (cameraRotation == null)
                {
                    cameraRotation = transform.localRotation.eulerAngles;
                }

                state.RawOrientation = Quaternion.Euler(cameraRotation);
            }
        }
    }

    public void SetCameraRotation(Vector3 rotation)
    {
        cameraRotation = rotation;
    }
}
