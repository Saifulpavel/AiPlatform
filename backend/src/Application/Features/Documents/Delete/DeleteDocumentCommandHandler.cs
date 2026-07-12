using AiPlatform.Application.Common.Interfaces;
using AiPlatform.Application.Common.Results;
using MediatR;

namespace AiPlatform.Application.Features.Documents.Delete;

public class DeleteDocumentCommandHandler(IDocumentRepository repository)
    : IRequestHandler<DeleteDocumentCommand, Result>
{
    public async Task<Result> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await repository.GetAsync(request.Id, cancellationToken);
        if (document is null)
            return Error.NotFound("Document.NotFound", "Document not found.");

        document.IsDeleted = true;
        document.DeletedAtUtc = DateTime.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
