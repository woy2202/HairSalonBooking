using HairSalon.Booking.Core.Models;
using Microsoft.Azure.Cosmos;

namespace HairSalon.Booking.Api.Data;

public interface ICosmosContainerResolver
{
    Container GetContainer<T>() where T : BookingEntity;
}
