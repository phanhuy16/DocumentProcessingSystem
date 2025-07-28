using DocumentProcessing.Application.Common;
using DocumentProcessing.Application.DTOs.Email;

namespace DocumentProcessing.Application.Interfaces
{
    public interface IEmailTemplateService
    {
        Task<Result<EmailTemplateDto>> GetTemplateAsync(string name);
        Task<Result<EmailTemplateDto>> CreateTemplateAsync(CreateEmailTemplateDto createDto);
        Task<Result<EmailTemplateDto>> UpdateTemplateAsync(Guid id, UpdateEmailTemplateDto updateDto);
        Task<Result<bool>> DeleteTemplateAsync(Guid id);
        Task<Result<List<EmailTemplateDto>>> GetAllTemplatesAsync();
        Task<string> RenderTemplateAsync(string templateName, Dictionary<string, object> variables);
    }

}
