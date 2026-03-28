using auth_template.Features.Auth.Entities;

namespace auth_template.Features.Auth.Responses;

public class PreferenceResponse
{
    public string Locale { get; set; }

    public PreferenceResponse()
    {
        
    }

    public PreferenceResponse(AppUserPreferences prefs)
    {
        this.Locale = prefs.Locale;
    }
}