using UnityEngine;
using System.Collections;

namespace Units
{
    public static class EffectManager 
    {
        public static GameObject InitializeParticleSystems(GameObject parentContainer)
        {
            // Instanciate if a least one particle system is specified
            if (parentContainer != null && parentContainer.GetComponentsInChildren<ParticleSystem>() != null)
            {
                // Instanciate and disable the container GameObject so that the effects do not play
                parentContainer = Object.Instantiate(parentContainer);
                parentContainer.SetActive(false);

                // Ensure that all particle system use the 'PlayOnAwake' option
                //    This allows to automatically call play/stop of all sub-effects at once simply by using the 
                //    enabled state of the the parent GameObject containing all ParticleSystems
                foreach (var ps in parentContainer.GetComponentsInChildren<ParticleSystem>())
                {
                    ps.playOnAwake = true;
                }
                return parentContainer;
            }
            return null;
        }


        public static IEnumerator PlayParticleSystems(GameObject parentContainer)
        {
            if (parentContainer != null)
            {
                // Force disable of the containing GameObject if not already done to ensure that the following 
                // activation will automatically launch all particle effects with 'PlayOnAwake'
                //    The GameObject could not have been disabled automatically if this function gets called again 
                //    before the maximum duration of the ParticleSystem would have had enough time to be elapsed 
                //    ('WaitForSeconds' didn't reach the end of the delay on the previous call)
                // If this happens too often, the effect's total duration is probably too long for the required
                // call intervals (ParticleSystem should be modified in the Editor to reduce start/end life time)
                if (parentContainer.activeSelf) parentContainer.SetActive(false);

                // Activate to start all ParticleSystem animation effects
                parentContainer.SetActive(true);

                // Get the maximum delay to wait to ensure all animations have enough time to complete
                float maxTime = 0;
                foreach (var ps in parentContainer.GetComponentsInChildren<ParticleSystem>()) 
                {           
                    maxTime = Mathf.Max(ps.duration, maxTime);
                }
                yield return new WaitForSeconds(maxTime);

                // Disable the containing GameObject to display the effects on the next activation (PlayOnAwake)
                parentContainer.SetActive(false);
            }
        }
    }        
}
