namespace ResultPattern.Domain;

public sealed record LoanApproval(
    string  ApplicationRef,
    string  ApplicantName,
    decimal ApprovedAmountCAD,
    decimal MonthlyPaymentCAD,
    decimal AnnualInterestRate,
    int     TermMonths);
