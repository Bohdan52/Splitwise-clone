using Microsoft.EntityFrameworkCore;
using task2.Data;
using task2.Domain.Entities;
using task2.Domain.Enums;
using task2.Dtos;
using task2.Exceptions;
using task2.Services.Implemetations;
using task2.Services.Interfaces;

namespace task2.Services.Implementations;

public class ExpenseService : IExpenseService
{
    private readonly SplitwiseDbContext _context;

    public ExpenseService(SplitwiseDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ExpenseResponse>> GetAllAsync(Guid currentUserId) 
    { 
        var expenses = await _context.Expenses
            .Include(e => e.Splits)
            .Where(e => e.PaidById == currentUserId 
                || e.Splits.Any(s => s.UserId == currentUserId)
                || e.GroupId == null &&
                     _context.GroupMembers.Any(m => m.GroupId == e.GroupId && m.UserId == currentUserId))   
            .ToListAsync();

        return expenses.Select(MapToResponse);
    }

    public async Task<ExpenseResponse> GetByIdAsync(Guid id, Guid currentUserId)
    {

        var expense = await _context.Expenses
            .Include(e => e.Splits)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (expense == null) throw new NotFoundException("Expense not found");
        await EnsureCanViewAsync(expense, currentUserId);
        return MapToResponse(expense);
    }

    public async Task<IEnumerable<ExpenseResponse>> GetByGroupIdAsync(Guid groupId, Guid currentUserId)
    {
        await _context.EnsureMemberAsync(groupId, currentUserId);
        var expenses = await _context.Expenses
            .Include(e => e.Splits)
            .Where(e => e.GroupId == groupId)
            .ToListAsync();

        return expenses.Select(MapToResponse);
    }

    public async Task<ExpenseResponse> CreateAsync(CreateExpenseRequest request, Guid currentUserId)
    {
        await EnsureValidParticipantsAsync(request, currentUserId);
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

        if (expense == null) throw new NotFoundException("Expense not found");

        if (expense.CreatedById != currentUserId && expense.PaidById != currentUserId)
            throw new ForbiddenException("You are not authorized to delete this expense.");

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();
    }

    private List<ExpenseSplit> CalculateSplits(CreateExpenseRequest request, Guid expenseId)
    {
        var participants = request.Participants;
        if (participants == null || participants.Count == 0)
            return new List<ExpenseSplit>();

        decimal[] owed;

        switch (request.SplitType)
        {
            case SplitType.Equal:
                owed = MoneyAllocator.Allocate(request.Amount, participants.Select(_ => 1m).ToList());
                break;

            case SplitType.Exact:
                owed = participants.Select(p => p.Amount ?? 0).ToArray();
                if (owed.Any(a => a < 0 || decimal.Round(a, 2) != a))
                    throw new BadRequestException("The participants' amounts must be non-negative and have no more than 2 decimal places.");
                if (owed.Sum() != request.Amount)
                    throw new BadRequestException("The sum of participants' amounts must equal the expense amount.");
                break;

            case SplitType.Percentage:
                var percents = participants.Select(p => p.Percent ?? 0).ToList();
                if (percents.Any(p => p <= 0))
                    throw new BadRequestException("The percentage of each participant must be greater than zero.");
                if (percents.Sum() != 100m)
                    throw new BadRequestException("The percentages of participants must add up to 100.");
                owed = MoneyAllocator.Allocate(request.Amount, percents);
                break;

            case SplitType.Shares:
                var shares = participants.Select(p => p.Shares ?? 1).ToList();
                if (shares.Any(s => s <= 0))
                    throw new BadRequestException("The number of shares for each participant must be greater than zero.");
                owed = MoneyAllocator.Allocate(request.Amount, shares);
                break;

            default:
                throw new BadRequestException("Unknown split type.");
        }

        return participants
            .Select((p, i) => new ExpenseSplit
            {
                Id = Guid.NewGuid(),
                ExpenseId = expenseId,
                UserId = p.UserId,
                AmountOwed = owed[i],
                ShareValue = p.Shares
            })
            .ToList();
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
    private async Task EnsureCanViewAsync(Expense expense, Guid userId)
    {
        if (expense.GroupId is { } groupId)
        {
            await _context.EnsureMemberAsync(groupId, userId);
            return;
        }

        if (expense.PaidById != userId && expense.Splits.All(s => s.UserId != userId))
            throw new ForbiddenException("Ви не маєте доступу до цієї витрати.");
    }

    private async Task EnsureValidParticipantsAsync(CreateExpenseRequest request, Guid currentUserId)
    {
        if (request.GroupId is { } groupId)
        {
            await _context.EnsureMemberAsync(groupId, currentUserId);

            var memberIds = await _context.GroupMembers
                .Where(m => m.GroupId == groupId)
                .Select(m => m.UserId)
                .ToListAsync();

            var involved = request.Participants.Select(p => p.UserId).Append(request.PaidById).Distinct();
            if (involved.Any(id => !memberIds.Contains(id)))
                throw new BadRequestException("Платник і всі учасники мають бути учасниками групи.");

            return;
        }

        if (request.PaidById != currentUserId && request.Participants.All(p => p.UserId != currentUserId))
            throw new ForbiddenException("Можна створювати лише витрати, у яких ви берете участь.");
    }
}