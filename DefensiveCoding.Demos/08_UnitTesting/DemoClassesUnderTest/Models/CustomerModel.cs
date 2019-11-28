namespace DefensiveCoding.Demos._08_UnitTesting.DemoClassesUnderTest.Models
{
    /// <summary>
    /// Simplified customer model for demo
    /// In a real example, Message would probably be in some kind of response wrapper
    /// </summary>
    internal class CustomerModel
    {
        public int CustomerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
    }
}
