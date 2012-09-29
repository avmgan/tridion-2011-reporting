using System.ServiceModel;

namespace Tridion.Extensions.Reporting.Storage
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IAudit" in both code and config file together.
    [ServiceContract(Namespace = "http://tridioncommunity.event.logs", SessionMode = SessionMode.NotAllowed)]
    public interface IAudit
    {
        [OperationContract(IsOneWay = true)]
        void WriteEvent(string eventData);

        // TODO: Add your service operations here
    }

}
