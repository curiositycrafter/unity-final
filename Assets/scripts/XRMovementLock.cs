using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public class XRMovementLock : MonoBehaviour
{
    private XRDeviceSimulator deviceSimulator;
    private bool isLocked = true;

    void Start()
    {
        deviceSimulator = GetComponent<XRDeviceSimulator>();
        LockMovement(true);
    }

    public void LockMovement(bool lockState)
    {
        isLocked = lockState;
        if (deviceSimulator != null)
        {
            deviceSimulator.enabled = !lockState;
        }
    }

    public bool IsLocked()
    {
        return isLocked;
    }
}
