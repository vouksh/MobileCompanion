using MobileCompanion.Shared.Enums;
namespace MobileCompanion.Shared.Interfaces;

public interface ICommunicationPacket
{
    public CommandType Command { get; set; }
    public ICommandObject CommandData { get; set; }
    public string ErrorMessage { get; set; }
}
