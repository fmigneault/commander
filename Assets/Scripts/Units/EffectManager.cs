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
                yield return StopParticleSystemsFinishAnimation(parentContainer);
            }
        }


        public static IEnumerator LoopParticleSystems(GameObject parentContainer)
        {

            Debug.Log(string.Format("Name: {0}, Active? {1}", parentContainer.name, parentContainer.activeSelf));
            foreach (var ps in parentContainer.GetComponentsInChildren<ParticleSystem>())
            {                    
                Debug.Log(string.Format("Name: {0}, Loop? {1}, Time: {2}", ps.name, ps.loop, ps.time));
            }



            // If the effect is a looping effect, let it continue by itself
            if (!parentContainer.activeSelf) parentContainer.SetActive(true);
            foreach (var ps in parentContainer.GetComponentsInChildren<ParticleSystem>())
            {
                if (!ps.isPlaying)
                {                   
                    ps.loop = true;
                    ps.Play();
                }
            }
            yield return null;
        }


        // Immediately stops the animations (event if not finished) by disabling the ParticleSystems
        public static void StopParticleSystemsImmediate(GameObject parentContainer) 
        {
            if (parentContainer != null && parentContainer.activeSelf) parentContainer.SetActive(false);
        }


        // Waits for the animations to complete before disabling the ParticleSystems
        public static IEnumerator StopParticleSystemsFinishAnimation(GameObject parentContainer) 
        {
            if (parentContainer != null && parentContainer.activeSelf)
            {
                // Stop particle emission for progressive stop of the animations (in case of looping effects)
                SetEmissionStatus(parentContainer, false);

                // Get the maximum delay to wait to ensure all animations have enough time to complete
                float maxDelay = GetMaximumDuration(parentContainer);
                yield return new WaitForSeconds(maxDelay);               

                // Reset emission for next call
                // Disable the containing GameObject to display the effects on the next activation (PlayOnAwake)
                SetEmissionStatus(parentContainer, true);
                parentContainer.SetActive(false);
            }
        }


        private static float GetMaximumDuration(GameObject parentContainer) 
        {
            float maxTime = 0;
            foreach (var ps in parentContainer.GetComponentsInChildren<ParticleSystem>())
            {           
                maxTime = Mathf.Max(ps.duration, maxTime);
            }
            return maxTime;
        }


        private static void SetEmissionStatus(GameObject parentContainer, bool status)
        {
            foreach (var ps in parentContainer.GetComponentsInChildren<ParticleSystem>())
            {
                if (ps.loop && ps.isPlaying)
                {
                    var em = ps.emission;
                    em.enabled = status;
                }
            }
        }
    }        
}
