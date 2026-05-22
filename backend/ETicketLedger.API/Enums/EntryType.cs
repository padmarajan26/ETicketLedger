namespace ETicketLedger.API.Enums;

////Remove this //
/// <summary>
/// Represents one side (Debit OR Credit) of a double-entry ledger posting.
/// For every transaction exactly two entries are created:
///   Debit  → Cash / Payment Gateway Account
///   Credit → Ticket Sales Revenue Account
/// The sum of all Debits must always equal the sum of all Credits.
/// </summary>
public enum EntryType
{
    Debit = 1,
    Credit = 2
}
