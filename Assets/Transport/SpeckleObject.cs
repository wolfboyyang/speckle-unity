using Realms;

namespace Speckle.Core.Transports
{
    public class SpeckleObject : RealmObject
    {
        [PrimaryKey]
        public string Hash { get; set; }

        [Required]
        public string SerializedObject { get; set; }
    }
}
