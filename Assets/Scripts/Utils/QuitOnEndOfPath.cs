using System.Collections;
using Unity.MegaCity.UI;
using UnityEngine;

namespace Unity.MegaCity.Utils
{
    /// <summary>
    /// Takes ModeBootstrap that allows set parameters for the end of the path.
    /// PlayableDirector is used to read when the camera Cinemachine completes the animation.
    /// If the animation is completed, the execution is Stopped.
    /// </summary>
    public class QuitOnEndOfPath : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField] ModeBootstrap bootstrap;
        [SerializeField] Cinemachine.CinemachineSmoothPath path;
        [SerializeField] Cinemachine.CinemachineDollyCart track;
        [SerializeField] float secondsBeforeQuitting = 2f;
        private bool RequestForQuit = false;
#pragma warning restore 0649

        void Awake() => enabled = bootstrap.Options.QuitAfterFlyover;

        private void Update()
        {
            if (Mathf.Abs(path.PathLength - track.m_Position) < 0.2f && !RequestForQuit)
            {
                RequestForQuit = true;
                StartCoroutine(OnFlythroughStopped());
            }
        }

        IEnumerator OnFlythroughStopped()
        {
            yield return new WaitForSeconds(secondsBeforeQuitting);
            Application.Quit(0);
        }
    }
}
