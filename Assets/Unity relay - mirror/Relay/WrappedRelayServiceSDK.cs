using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Utp
{
    public class WrappedRelayServiceSDK : IRelayServiceSDK
    {
        public Task<Allocation> CreateAllocationAsync(int maxConnections, string region = null)
        {
            return Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(maxConnections, region);
        }

        public Task<string> GetJoinCodeAsync(Guid allocationId)
        {
            return Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(allocationId);
        }

        public Task<JoinAllocation> JoinAllocationAsync(string joinCode)
        {
            return Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(joinCode);
        }

        public Task<List<Region>> ListRegionsAsync()
        {
            return Unity.Services.Relay.RelayService.Instance.ListRegionsAsync();
        }
    }
}