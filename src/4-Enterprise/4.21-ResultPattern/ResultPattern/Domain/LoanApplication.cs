namespace ResultPattern.Domain;

public sealed record LoanApplication(
    string  ApplicantId,
    string  ApplicantName,
    decimal AnnualIncomeCAD,
    int     CreditScore,
    decimal MonthlyDebtPaymentsCAD,
    decimal RequestedAmountCAD,
    int     TermMonths);
