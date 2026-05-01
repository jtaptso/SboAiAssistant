namespace SapAiAssistant.Domain.Entities;

public enum AssistantMode
{
    /// <summary>Business user assistant — data lookup and SAP-grounded Q&amp;A.</summary>
    BusinessUser,

    /// <summary>Developer assistant — SAP B1 C# code generation and guidance.</summary>
    Developer
}
