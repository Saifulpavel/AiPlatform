using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Results;
using AiPlatform.Application.Features.Documents.Delete;
using AiPlatform.Application.Features.Documents.List;
using AiPlatform.Domain.Entities;
using NSubstitute;

namespace AiPlatform.Application.Tests.Documents;

public class DocumentsTests
{
    private readonly IDocumentRepository _repo = Substitute.For<IDocumentRepository>();

    [Fact]
    public async Task List_MapsItems_AndComputesTotalPages()
    {
        var docs = new List<Document> { new() { FileName = "a.txt" }, new() { FileName = "b.txt" } };
        _repo.ListAsync(1, 20, Arg.Any<CancellationToken>()).Returns(((IReadOnlyList<Document>)docs, 45));

        var result = await new ListDocumentsQueryHandler(_repo).Handle(new ListDocumentsQuery(1, 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(45, result.Value.TotalCount);
        Assert.Equal(3, result.Value.TotalPages); // ceil(45/20)
    }

    [Fact]
    public async Task Delete_ExistingDocument_MarksDeleted_AndSaves()
    {
        var doc = new Document { FileName = "a.txt" };
        _repo.GetAsync(doc.Id, Arg.Any<CancellationToken>()).Returns(doc);

        var result = await new DeleteDocumentCommandHandler(_repo).Handle(new DeleteDocumentCommand(doc.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(doc.IsDeleted);
        Assert.NotNull(doc.DeletedAtUtc);
        await _repo.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_MissingDocument_ReturnsNotFound()
    {
        _repo.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Document?)null);

        var result = await new DeleteDocumentCommandHandler(_repo).Handle(new DeleteDocumentCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 20, false)]   // page < 1
    [InlineData(1, 0, false)]    // pageSize < 1
    [InlineData(1, 101, false)]  // pageSize > 100
    [InlineData(1, 20, true)]    // valid
    public void ListValidator_EnforcesBounds(int page, int pageSize, bool valid)
    {
        var result = new ListDocumentsQueryValidator().Validate(new ListDocumentsQuery(page, pageSize));
        Assert.Equal(valid, result.IsValid);
    }
}
