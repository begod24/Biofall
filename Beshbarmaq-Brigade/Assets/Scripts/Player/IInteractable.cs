using UnityEngine;

namespace Biofall.Player
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }
}
