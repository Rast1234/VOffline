namespace VOffline.Models.Storage
{
    public abstract class Category
    {
        protected Category(long ownerId)
        {
            OwnerId = ownerId;
        }

        public long OwnerId { get; }
    }
}