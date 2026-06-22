using UnityEngine;

namespace Biofall.Gameplay.Mission1
{
    public interface IInteractable
    {
        bool CanInteract { get; }

        string Prompt { get; }

        Vector3 Position { get; }

        void Interact(GameObject interactor);
    }
}
