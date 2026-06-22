using UnityEngine;

namespace Biofall.Gameplay.Mission1
{
    /// <summary>
    /// Anything the player can walk up to and use with the interact key (E):
    /// the generator button, the beacon, etc. Implementers register themselves
    /// with the <see cref="PlayerInteractor"/> registry while enabled.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>True when using this object is currently allowed (e.g. beacon only after the generator).</summary>
        bool CanInteract { get; }

        /// <summary>Short verb shown in the prompt, e.g. "Turn On Generator".</summary>
        string Prompt { get; }

        /// <summary>World position used for distance checks and prompt placement.</summary>
        Vector3 Position { get; }

        /// <summary>Run the interaction. Called once when the player presses E in range.</summary>
        void Interact(GameObject interactor);
    }
}
