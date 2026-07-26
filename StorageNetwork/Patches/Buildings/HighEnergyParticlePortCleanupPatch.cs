using HarmonyLib;

namespace StorageNetwork.Patches
{
    /// <summary>
    /// The game's particle port cleanup only unregisters the port. If a radbolt is
    /// captured in the same frame, the particle can retain a reference to the
    /// destroyed port and crash during its next update.
    /// </summary>
    [HarmonyPatch(typeof(HighEnergyParticlePort), "OnCleanUp")]
    public static class HighEnergyParticlePortCleanupPatch
    {
        public static void Prefix(HighEnergyParticlePort __instance)
        {
            if (__instance == null)
            {
                return;
            }

            __instance.particleInputEnabled = false;
            __instance.particleOutputEnabled = false;

            HighEnergyParticle particle = __instance.currentParticle;
            __instance.currentParticle = null;
            if (particle != null && particle.capturedBy == __instance)
            {
                particle.Uncapture();
            }
        }
    }
}
