using auth_template.Entities.Data;

namespace auth_template.Features.Auth.Responses;

public class TagListingResponse
{
    public string Name { get; set; }
    public Guid Id { get; set; }

    public TagListingResponse()
    {
        
    }

    public TagListingResponse(AppUserTag tag)
    {
        this.Name = tag.Name;
        this.Id = tag.Id;
    }
}