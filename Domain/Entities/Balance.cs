namespace task2.Dtos;

public record BalanceResponse(
    Guid UserId,
    decimal Balance
);

public record SettlementSuggestionResponse(
    Guid FromUserId,
    Guid ToUserId,
    decimal Amount
);