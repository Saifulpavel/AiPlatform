namespace AiPlatform.Application.Common.Interfaces;

// নাম দিয়ে provider বাছে; null হলে config-এর default। অজানা নাম → null।
public interface IChatProviderResolver
{
    IChatProvider? Resolve(string? providerName);
}
