using Application.Common.Messaging;
using Application.Common.UnitOfWork;
using ErrorOr;
using MediatR;

namespace Application.Common.Behaviors;

public class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand, IRequest<TResponse>
    where TResponse : IErrorOr
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (!response.IsError)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
