using InvestLab.Models;
using InvestLab.Models.DTOs.Contact;

public interface IContactService
{
    Task<Response> SendAsync(ContactMessageDto dto);
}