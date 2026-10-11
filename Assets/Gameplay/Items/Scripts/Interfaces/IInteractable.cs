namespace Gameplay
{
    public interface IInteractable
    {
        public Outline outline { get; }

        public void SetOutline(bool state);
        
        public void Interact(ulong clientId);
    }
}