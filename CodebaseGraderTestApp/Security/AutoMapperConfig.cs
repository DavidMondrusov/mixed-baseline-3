using AutoMapper;
using CodebaseGraderTestApp.Models;

namespace CodebaseGraderTestApp.Security;

/// <summary>
/// TRICKY V15.3.3: AutoMapper configuration with unrestricted ReverseMap.
///
/// The CreateProfileMapped endpoint uses a DTO, which is correct.
/// But this profile has a ReverseMap() without explicit field ignores —
/// meaning Role and IsAdmin flow from Entity→DTO AND back.
/// If a future code path maps DTO→Entity (e.g., an update endpoint),
/// the unrestricted reverse could mass-assign Role/IsAdmin.
/// </summary>
public class UserProfileMappingProfile : Profile
{
    public UserProfileMappingProfile()
    {
        CreateMap<UserProfileEntity, UserProfileDto>()
            .ReverseMap(); // 🚨 No ignores — Role and IsAdmin are writable
    }
}
