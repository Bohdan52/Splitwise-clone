using Microsoft.AspNetCore.Identity;

namespace task2.Domain.Entities;

public class User : IdentityUser<Guid>
{
    public string FullName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string DefaultCurrency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
    public ICollection<Expense> ExpensesPaid { get; set; } = new List<Expense>();
    public ICollection<ExpenseSplit> ExpenseSplits { get; set; } = new List<ExpenseSplit>();
}