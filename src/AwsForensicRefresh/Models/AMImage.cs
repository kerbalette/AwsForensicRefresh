namespace AwsForensicRefresh.Models
{
    public class AMImage
    {
        public AMImage(string imageId, string name, string description, string creationDate, string architecture, string platform, string state, string ownerId  )
        {
            ImageId = imageId;
            Name = name;
            Description = description;
            CreationDate = creationDate;
            Architecture = architecture;
            Platform = platform;
            State = state;
            OwnerId = ownerId;
        }
        
        public string ImageId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string CreationDate { get; set; }
        public string Architecture { get; set; }
        public string Platform { get; set; }
        public string State { get; set; }
        public string OwnerId { get; set; }
        
    }
}