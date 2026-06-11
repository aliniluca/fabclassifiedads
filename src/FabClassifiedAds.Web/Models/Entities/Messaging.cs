namespace FabClassifiedAds.Web.Models.Entities;

public class Conversation
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public Listing Listing { get; set; } = null!;
    public string BuyerId { get; set; } = "";
    public ApplicationUser Buyer { get; set; } = null!;
    public string SellerId { get; set; } = "";
    public ApplicationUser Seller { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public List<Message> Messages { get; set; } = [];
}

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;
    public string SenderId { get; set; } = "";
    public ApplicationUser Sender { get; set; } = null!;
    public string Body { get; set; } = "";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }

    /// <summary>Comma-separated anti-scam flags set by TrustService (e.g. "AdvancePayment,SuspiciousLink").</summary>
    public string? RiskFlags { get; set; }
}
