namespace ChainOfResponsibilityPattern;

public enum Priority { Low, Medium, High, Critical }
public enum Category { Account, Billing, Technical, Outage }

public sealed record SupportTicket(
    string   Id,
    string   Subject,
    Priority Priority,
    Category Category);
