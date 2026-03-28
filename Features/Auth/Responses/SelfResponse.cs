using auth_template.Entities.Data;
using auth_template.Features.Auth.Responses.Permissions;

namespace auth_template.Features.Auth.Responses;

public class SelfResponse : UserResponse
{
    public SelfResponse()
    {
    }

    public SelfResponse(UserResponse auth, IEnumerable<string> features, IEnumerable<string> tags)
    {
        Features = features;
        Tags = tags;
        EmailConfirmed = auth.EmailConfirmed;
        UserId = auth.UserId;
        UserName = auth.UserName;
        EmailAddress = auth.EmailAddress;
    }    
    public SelfResponse(UserResponse auth, PermissionTransfer perms)
    {
        Features = perms.Features;
        Tags = perms.Tags;
        EmailConfirmed = auth.EmailConfirmed;
        UserId = auth.UserId;
        UserName = auth.UserName.ToLowerInvariant();
        this.Flagged = auth.Flagged;
        EmailAddress = auth.EmailAddress;
    }    public SelfResponse(AppUser user, IEnumerable<string> features, IEnumerable<string> tags)
    {
        Features = features;
        Tags = tags;
        EmailConfirmed = user.EmailConfirmed;
        UserId = user.Id;
        this.Flagged = user.Flagged;
        
        UserName = user.UserName.ToLowerInvariant();
        EmailAddress = user.NormalizedEmail.ToLowerInvariant();
    }    
    public SelfResponse(AppUser user, PermissionTransfer perms)
    {
        Features = perms.Features;
        Tags = perms.Tags;
        EmailConfirmed = user.EmailConfirmed;
        UserId = user.Id;
        this.Flagged = user.Flagged;
        
        UserName = user.UserName.ToLowerInvariant();
        EmailAddress = user.NormalizedEmail.ToLowerInvariant();
    }

    public IEnumerable<string> Features { get; set; }
    public IEnumerable<string> Tags { get; set; }

    public override string ToString()
    {
        return
            $"SelfResponse(UserId={UserId}, UserName={UserName}, EmailConfirmed={EmailConfirmed}, Features=[{string.Join(", ", Features)}])";
    }
}