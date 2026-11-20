using Microsoft.EntityFrameworkCore;
using task2.Data;
using task2.Domain.Entities;
using task2.Domain.Enums;
using task2.Dtos;
using task2.Services.Interfaces;
using task2.Exceptions;

namespace task2.Services.Implementations;

public class ExpenseService : IExpenseService
{
    private readonly SplitwiseDbContext _context;

    public ExpenseService(SplitwiseDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ExpenseResponse>> GetAllAsync()
    {
        var expenses = await _context.Expenses
            .Include(e => e.Splits)
            .ToListAsync();

        return expenses.Select(MapToResponse);
    }

    public async Task<ExpenseResponse> GetByIdAsync(Guid id)
    {
        var expense = await _context.Expenses
            .Include(e => e.Splits)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense == null) throw new NotFoundException("Expense not found");
        return MapToResponse(expense);
    }

    public async Task<IEnumerable<ExpenseResponse>> GetByGroupIdAsync(Guid groupId)
    {
        var expenses = await _context.Expenses
            .Include(e => e.Splits)
            .Where(e => e.GroupId == groupId)
            .ToListAsync();

        return expenses.Select(MapToResponse);
    }

    public async Task<ExpenseResponse> CreateAsync(CreateExpenseRequest request, Guid currentUserId)
    {
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            Description = request.Description,
            Amount = request.Amount,
            Currency = request.Currency,
            Category = request.Category,
            Date = request.Date,
            GroupId = request.GroupId,
            PaidById = request.PaidById,
            CreatedById = currentUserId,
            SplitType = request.SplitType,
            CreatedAt = DateTime.UtcNow
        };

        var calculatedSplits = CalculateSplits(request, expense.Id);

        _context.Expenses.Add(expense);
        _context.ExpenseSplits.AddRange(calculatedSplits);

        await _context.SaveChangesAsync();

        expense.Splits = calculatedSplits;
        return MapToResponse(expense);
    }

    public async Task DeleteAsync(Guid id, Guid currentUserId)
    {
        var expense = await _context.Expenses.Include(e => e.Splits).FirstOrDefaultAsync(e => e.Id == id);

        if (expense == null) throw new ForbiddenException("Expense not found");

        if (expense.CreatedById != currentUserId && expense.PaidById != currentUserId)
            throw new UnauthorizedException("You are not authorized to delete this expense.");

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();
    }

    private List<ExpenseSplit> CalculateSplits(CreateExpenseRequest request, Guid expenseId)
    {
        var splits = new List<ExpenseSplit>();
        if (request.Participants == null || !request.Participants.Any())
            return splits;

        int participantsCount = request.Participants.Count;

        foreach (var participant in request.Participants)
        {
            decimal amountOwed = 0;

            switch (request.SplitType)
            {
                case SplitType.Equal:
                    amountOwed = request.Amount / participantsCount;
                    break;
                case SplitType.Exact:
                    amountOwed = participant.Amount ?? 0;
                    break;
                case SplitType.Percentage: 
                    amountOwed = request.Amount * ((participant.Percent ?? 0) / 100m);
                    break;
                case SplitType.Shares:
                    decimal totalShares = request.Participants.Sum(p => p.Shares ?? 1);
                    decimal userShares = participant.Shares ?? 1;
                    amountOwed = request.Amount * (userShares / totalShares);
                    break;
            }

            amountOwed = Math.Round(amountOwed, 2);

            splits.Add(new ExpenseSplit
            {
                Id = Guid.NewGuid(),
                ExpenseId = expenseId,
                UserId = participant.UserId,
                AmountOwed = amountOwed,
                ShareValue = participant.Shares
            });
        }

        return splits;
    }

    private static ExpenseResponse MapToResponse(Expense e)
    {
        return new ExpenseResponse(
            e.Id,
            e.GroupId,
            e.Description,
            e.Amount,
            e.Currency,
            e.Category,
            e.Date,
            e.PaidById,
            e.CreatedById,
            e.SplitType,
            e.CreatedAt,
            e.Splits?.Select(s => new ExpenseSplitResponse(
                s.UserId,
                s.AmountOwed,
                s.ShareValue
            )).ToList() ?? new List<ExpenseSplitResponse>()
        );
    }
}