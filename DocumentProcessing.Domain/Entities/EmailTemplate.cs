using DocumentProcessing.Domain.Common;

namespace DocumentProcessing.Domain.Entities
{
    public class EmailTemplate : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string BodyTemplate { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object> DefaultVariables { get; set; } = new();
    }
}
