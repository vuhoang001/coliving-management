using Coliving.Infrastructure.Services;
using Xunit;

namespace Coliving.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_Then_Verify_Succeeds()
    {
        var hash = _hasher.Hash("S3cret!");
        Assert.NotEqual("S3cret!", hash);
        Assert.True(_hasher.Verify("S3cret!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_Fails()
    {
        var hash = _hasher.Hash("S3cret!");
        Assert.False(_hasher.Verify("wrong", hash));
    }

    [Fact]
    public void Hash_SamePassword_ProducesDifferentHashes()
    {
        // Mỗi lần băm dùng salt ngẫu nhiên → hash khác nhau nhưng đều verify được.
        var h1 = _hasher.Hash("same");
        var h2 = _hasher.Hash("same");
        Assert.NotEqual(h1, h2);
        Assert.True(_hasher.Verify("same", h1));
        Assert.True(_hasher.Verify("same", h2));
    }

    [Fact]
    public void Verify_MalformedHash_Fails()
    {
        Assert.False(_hasher.Verify("x", "not-a-valid-hash"));
    }
}
