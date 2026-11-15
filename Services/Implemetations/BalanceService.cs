using Microsoft.EntityFrameworkCore;
using task2.Data;
using task2.Dtos;
using task2.Services.Interfaces;

namespace task2.Services.Implementations;

public class BalanceService : IBalanceService
{
    private readonly SplitwiseDbContext _context;

    public BalanceService(SplitwiseDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BalanceResponse>> GetGroupBalancesAsync(Guid groupId)
    {
        var balances = new Dictionary<Guid, decimal>();

        var members = await _context.GroupMembers
            .Where(gm => gm.GroupId == groupId)
            .Select(gm => gm.UserId)
            .ToListAsync();

        foreach (var memberId in members)
        {
            balances[memberId] = 0;
        }

        var expenses = await _context.Expenses
            .Include(e => e.Splits)
            .Where(e => e.GroupId == groupId)
            .ToListAsync();

        foreach (var expense in expenses)
        {
            if (balances.ContainsKey(expense.PaidById))
            {
                balances[expense.PaidById] += expense.Amount;
            }

            foreach (var split in expense.Splits)
            {
                if (balances.ContainsKey(split.UserId))
                {
                    balances[split.UserId] -= split.AmountOwed;
                }
            }
        }

        var settlements = await _context.Settlements
            .Where(s => s.GroupId == groupId)
            .ToListAsync();

        foreach (var settlement in settlements)
        {
            if (balances.ContainsKey(settlement.FromUserId))
            {
                balances[settlement.FromUserId] += settlement.Amount;
            }
            if (balances.ContainsKey(settlement.ToUserId))
            {
                balances[settlement.ToUserId] -= settlement.Amount;
            }
        }

        return balances.Select(b => new BalanceResponse(b.Key, b.Value));
    }

    public async Task<IEnumerable<SettlementSuggestionResponse>> GetGroupSettlementsAsync(Guid groupId)
    {
        var balancesResult = await GetGroupBalancesAsync(groupId);

        var debtors = balancesResult.Where(b => b.Balance < 0)
            .Select(b => new { UserId = b.UserId, Amount = -b.Balance })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var creditors = balancesResult.Where(b => b.Balance > 0)
            .Select(b => new { UserId = b.UserId, Amount = b.Balance })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var suggestedSettlements = new List<SettlementSuggestionResponse>();

        int debtorIndex = 0;
        int creditorIndex = 0;

        while (debtorIndex < debtors.Count && creditorIndex < creditors.Count)
        {
            var debtor = debtors[debtorIndex];
            var creditor = creditors[creditorIndex];

            var amountToSettle = Math.Min(debtor.Amount, creditor.Amount);

            if (amountToSettle > 0)
            {
                suggestedSettlements.Add(new SettlementSuggestionResponse(
                    debtor.UserId,
                    creditor.UserId,
                    amountToSettle
                ));
            }

            debtors[debtorIndex] = debtor with { Amount = debtor.Amount - amountToSettle };
            creditors[creditorIndex] = creditor with { Amount = creditor.Amount - amountToSettle };

            if (debtors[debtorIndex].Amount == 0) debtorIndex++;
            if (creditors[creditorIndex].Amount == 0) creditorIndex++;
        }

        return suggestedSettlements;
    }
}