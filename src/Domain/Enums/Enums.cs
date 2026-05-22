namespace TaxiCompare.Domain.Enums;

public enum RideStatus
{
    Pending = 0,
    Searching = 1,
    Completed = 2,
    Cancelled = 3,
    Ordered = 4   // пользователь нажал «Заказать» и был перенаправлен к провайдеру
}

public enum NotificationType
{
    PriceAlert = 0,
    PriceDrop = 1,
    System = 2,
    Promotion = 3
}

public enum VehicleClass
{
    Economy = 0,
    Comfort = 1,
    Business = 2,
    Premium = 3,
    XL = 4
}
