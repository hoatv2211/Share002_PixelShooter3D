#if UNITY_EDITOR
namespace Ragendom
{
    public class EditorDummyContainer : EditorAdsContainer
    {
        public EditorDummyContainer() : base("Dummy", "dummyContainer")
        {
        }

        protected override void SpecialButtons()
        {
            // No special buttons for dummy container
        }
    }
}
#endif
